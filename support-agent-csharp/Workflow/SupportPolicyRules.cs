using System.Globalization;
using System.Text.RegularExpressions;
using SupportAgent.Models;

namespace SupportAgent.Workflow;

public sealed class SupportPolicyRules
{
    private const string RefundKeyword = "refund";
    private const string ChargedTwicePhrase = "charged twice";
    private const string DoubleChargePhrase = "double charge";
    private const string LawyerKeyword = "lawyer";
    private const string ChargebackKeyword = "chargeback";
    private static readonly CultureInfo CurrencyCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly Regex AmountRegex = new(@"\$(?<amount>\d+(?:\.\d{1,2})?)", RegexOptions.Compiled);
    private static readonly Regex DateRegex = new(@"\b(?:jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\s+\d{1,2}\b|\b\d{1,2}/\d{1,2}(?:/\d{2,4})?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SupportPolicyRules(string handbookText)
    {
        HandbookText = handbookText;
    }

    public string HandbookText { get; }

    public static string LoadHandbook(string baseDirectory)
    {
        var handbookPath = Path.Combine(baseDirectory, "Data", "support_handbook.md");
        if (!File.Exists(handbookPath))
        {
            throw new FileNotFoundException("Could not locate Data/support_handbook.md in the build output.", handbookPath);
        }

        return File.ReadAllText(handbookPath);
    }

    public ParsedSupportRequest ParseRequest(string rawText)
    {
        var lines = rawText.Replace("\r\n", "\n").Split('\n');
        string? sender = null;
        string? subject = null;
        var bodyLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                break;
            }

            if (sender is null && line.StartsWith("From:", StringComparison.OrdinalIgnoreCase))
            {
                sender = line[5..].Trim();
                continue;
            }

            if (subject is null && line.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
            {
                subject = line[8..].Trim();
                continue;
            }

            bodyLines.Add(line);
        }

        var body = string.Join("\n", bodyLines).Trim();
        var normalized = NormalizeWhitespace(body);
        var detectedSignals = DetectSignals($"{subject}\n{body}");

        return new ParsedSupportRequest
        {
            RawText = rawText.Trim(),
            NormalizedText = normalized,
            Sender = sender,
            Subject = subject,
            Body = body,
            DetectedSignals = detectedSignals
        };
    }

    public static PolicyDecision EvaluatePolicy(ParsedSupportRequest request, IntakeAssessment intake)
    {
        var normalized = request.NormalizedText;
        var lower = normalized.ToLowerInvariant();
        var customerFacts = intake.CustomerFacts ?? new CustomerFacts();
        var reasoning = new List<string>
        {
            $"Intake summary: {intake.Summary}",
            $"Intent={intake.PrimaryIntent}, Sentiment={intake.Sentiment}, Urgency={intake.Urgency}.",
            BuildCustomerFactsReasoning(customerFacts)
        };
        var appliedPolicies = new List<string>();

        if (TryBuildEscalationDecision(lower, intake, appliedPolicies, reasoning, out var escalationDecision))
        {
            return escalationDecision;
        }

        if (ShouldRefuseDisclosure(lower))
        {
            appliedPolicies.Add("Do not share customer information between accounts, even when asked politely.");
            reasoning.Add("The request asks for account details belonging to another person, so it must be refused directly rather than clarified.");

            return new PolicyDecision
            {
                Route = PolicyRoute.Reply,
                ActionTaken = ActionTaken.ReplySent,
                RecommendedNextAction = null,
                AppliedPolicies = appliedPolicies,
                ReasoningSteps = reasoning,
                MessageMode = MessageMode.Reply,
                ArtifactMode = null
            };
        }

        if (IsClearFirstMonthRefund(request, intake))
        {
            appliedPolicies.Add("Be flexible on refunds in the first month, especially for first-time customers who barely used the product.");
            appliedPolicies.Add("Refunds go back to the original payment method and usually take 5-7 business days.");
            reasoning.Add("The request and available account facts fit the first-month refund pattern, so it should move straight to refund handling.");

            return new PolicyDecision
            {
                Route = PolicyRoute.RefundOrCancellation,
                ActionTaken = ActionTaken.RefundTicketCreated,
                RecommendedNextAction = "Process the refund review under the first-month flexibility policy and confirm the 5-7 business day timing.",
                AppliedPolicies = appliedPolicies,
                ReasoningSteps = reasoning,
                MessageMode = MessageMode.OperationalAcknowledgement,
                ArtifactMode = ArtifactMode.RefundTicket
            };
        }

        if (IsCancellationOperation(lower))
        {
            appliedPolicies.Add("Cancellation is effective immediately, but paid access continues through the current billing period.");
            appliedPolicies.Add("Data is retained for about 90 days after cancellation.");
            reasoning.Add("The customer is explicitly asking to cancel, so this should create a cancellation handling artifact.");

            return new PolicyDecision
            {
                Route = PolicyRoute.RefundOrCancellation,
                ActionTaken = ActionTaken.CancellationTicketCreated,
                RecommendedNextAction = "Process the cancellation and confirm access through the current billing period.",
                AppliedPolicies = appliedPolicies,
                ReasoningSteps = reasoning,
                MessageMode = MessageMode.OperationalAcknowledgement,
                ArtifactMode = ArtifactMode.CancellationTicket
            };
        }

        var missingInfo = MergeMissingInformation(request, intake, lower);
        if (ShouldHandleRefund(lower, intake) && HasEnoughRefundDetailToProceed(lower, request, customerFacts))
        {
            missingInfo.Clear();
        }

        if (missingInfo.Count > 0)
        {
            reasoning.Add($"Clarification required before safe handling: {string.Join(", ", missingInfo.Select(TrimTrailingPeriod))}.");
            appliedPolicies.Add("Do not guess when billing or refund details are missing; ask for the specific facts needed to continue.");

            return new PolicyDecision
            {
                Route = PolicyRoute.Clarification,
                ActionTaken = ActionTaken.ClarificationRequested,
                RecommendedNextAction = "Wait for the customer to reply with the requested billing details.",
                AppliedPolicies = appliedPolicies,
                ReasoningSteps = reasoning,
                MessageMode = MessageMode.ClarificationEmail,
                ArtifactMode = ArtifactMode.ClarificationEmail
            };
        }

        if (ShouldHandleRefund(lower, intake))
        {
            appliedPolicies.AddRange(GetRefundPolicies(lower));
            reasoning.Add("The request is refund-related and contains enough information to prepare a refund review artifact.");

            return new PolicyDecision
            {
                Route = PolicyRoute.RefundOrCancellation,
                ActionTaken = ActionTaken.RefundTicketCreated,
                RecommendedNextAction = BuildRefundNextAction(lower),
                AppliedPolicies = appliedPolicies,
                ReasoningSteps = reasoning,
                MessageMode = MessageMode.OperationalAcknowledgement,
                ArtifactMode = ArtifactMode.RefundTicket
            };
        }

        appliedPolicies.AddRange(GetDirectReplyPolicies(lower));
        reasoning.Add("This can be handled as a direct reply without clarification, escalation, or simulated operational work.");

        return new PolicyDecision
        {
            Route = PolicyRoute.Reply,
            ActionTaken = ActionTaken.ReplySent,
            RecommendedNextAction = null,
            AppliedPolicies = appliedPolicies,
            ReasoningSteps = reasoning,
            MessageMode = MessageMode.Reply,
            ArtifactMode = null
        };
    }

    public string BuildHandbookExcerpt(PolicyDecision policyDecision)
    {
        if (policyDecision.AppliedPolicies.Count == 0)
        {
            return HandbookText;
        }

        return string.Join("\n- ", policyDecision.AppliedPolicies.Prepend("Relevant handbook facts:"));
    }

    public static string BuildEscalationSummary(PolicyContext context)
    {
        return $"Escalate {context.Intake.PrimaryIntent.ToString().ToLowerInvariant()} case: {context.Intake.Summary}";
    }

    public static string BuildEscalationQueue(ParsedSupportRequest request, IntakeAssessment intake)
    {
        var lower = request.NormalizedText.ToLowerInvariant();
        if (ContainsAny(lower, "gdpr", LawyerKeyword, "lawyers", "regulator", ChargebackKeyword))
        {
            return "Support - Legal Review";
        }

        return intake.Urgency == Urgency.High ? "Support - Priority" : "Support - Senior Review";
    }

    public static string BuildEscalationSla(Urgency urgency) => urgency == Urgency.High ? "4 business hours" : "1 business day";

    public static IReadOnlyList<string> BuildEscalationNextSteps(ParsedSupportRequest request, IntakeAssessment intake)
    {
        var steps = new List<string>
        {
            "Review the full account and billing timeline before replying.",
            "Confirm the handbook-backed resolution path before promising credits or refunds."
        };

        var lower = request.NormalizedText.ToLowerInvariant();

        if (ContainsAny(lower, ChargebackKeyword, "bank", "disputing"))
        {
            steps.Add("Coordinate with billing before responding because the customer mentioned a chargeback or bank dispute.");
        }

        if (ContainsAny(lower, "gdpr", LawyerKeyword, "regulator"))
        {
            steps.Add("Route the case through the legal-sensitive review path before any customer follow-up.");
        }

        return steps;
    }

    private static bool TryBuildEscalationDecision(
        string lower,
        IntakeAssessment intake,
        List<string> appliedPolicies,
        List<string> reasoning,
        out PolicyDecision decision)
    {
        var triggers = new List<string>();

        if (ContainsAny(lower, "manager", "supervisor"))
        {
            triggers.Add("explicit manager or supervisor request");
        }

        if (ContainsAny(lower, LawyerKeyword, "lawyers", "gdpr", "regulator", ChargebackKeyword, "bank dispute"))
        {
            triggers.Add("legal, regulatory, or chargeback language");
        }

        var amountMatch = AmountRegex.Match(lower);
        if (amountMatch.Success && decimal.TryParse(amountMatch.Groups["amount"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) && amount > 100m)
        {
            triggers.Add("billing dispute over $100");
        }

        if (ContainsAny(lower, "third time", "3rd time", "three times", "week about the same", "again and again"))
        {
            triggers.Add("repeated unresolved contact pattern");
        }

        if (intake.Sentiment == Sentiment.Angry && ContainsAny(lower, "idiot", "useless", "stupid", "terrible"))
        {
            triggers.Add("abusive tone that should be escalated instead of debated");
        }

        if (triggers.Count == 0)
        {
            decision = default!;
            return false;
        }

        appliedPolicies.Add("Escalate manager requests, legal-sensitive cases, chargeback threats, high-value billing disputes, and repeated unresolved complaints.");
        reasoning.Add($"Escalation triggers: {string.Join(", ", triggers)}.");

        decision = new PolicyDecision
        {
            Route = PolicyRoute.Escalation,
            ActionTaken = ActionTaken.EscalatedToHuman,
            RecommendedNextAction = "Senior support should review the case within about 4 business hours.",
            AppliedPolicies = appliedPolicies,
            ReasoningSteps = reasoning,
            MessageMode = MessageMode.EscalationAcknowledgement,
            ArtifactMode = ArtifactMode.EscalationHandoff
        };

        return true;
    }

    private static List<string> MergeMissingInformation(ParsedSupportRequest request, IntakeAssessment intake, string lower)
    {
        var missing = new HashSet<string>(intake.MissingInformation.Where(item => !string.IsNullOrWhiteSpace(item)), StringComparer.OrdinalIgnoreCase);
        var customerFacts = intake.CustomerFacts ?? new CustomerFacts();
        var hasTrustedChargeFacts = HasTrustedChargeAmount(customerFacts) && HasTrustedChargeDate(customerFacts);
        var hasAmount = AmountRegex.IsMatch(lower) || HasTrustedChargeAmount(customerFacts);
        var hasDate = DateRegex.IsMatch(lower) || HasTrustedChargeDate(customerFacts);
        var hasAccountContext = !string.IsNullOrWhiteSpace(request.Sender)
            || HasTrustedCustomerFacts(customerFacts)
            || ContainsAny(lower, "account", "plan", "subscription", "card");

        if (IsDuplicateChargeDispute(lower))
        {
            missing.RemoveWhere(item => ContainsAny(item, "double", "duplicate", "second charge", "charged twice"));
            missing.Add("duplicate charge evidence such as both charge dates, both amounts, or a statement screenshot");
        }

        if (ContainsAny(lower, "declined", "what is happening", "don't know what i'm paying for") && !hasAccountContext)
        {
            missing.Add("account or billing context");
        }

        if ((intake.PrimaryIntent == Intent.Refund || ContainsAny(lower, RefundKeyword, "charged", "billing")) && !hasAmount && !hasDate)
        {
            missing.Add("specific billing details such as date, amount, or charge reference");
        }

        if (hasTrustedChargeFacts && !IsDuplicateChargeDispute(lower))
        {
            missing.RemoveWhere(item => ContainsAny(item, "billing details", "charge date", "amount", "charge reference"));
        }

        return missing.ToList();
    }

    private static bool IsCancellationOperation(string lower)
    {
        var explicitCancel = ContainsAny(lower, "cancel me", "cancel my", "please cancel", "i want to cancel", "close my account");
        var questionOnly = ContainsAny(lower, "what happens if i cancel", "thinking about cancel", "if i cancel", "cancel now, do i keep access");
        return explicitCancel && !questionOnly;
    }

    private static bool ShouldHandleRefund(string lower, IntakeAssessment intake)
    {
        return intake.PrimaryIntent == Intent.Refund
            || ContainsAny(lower, RefundKeyword, ChargedTwicePhrase, DoubleChargePhrase, "wrong amount", "charged after cancellation");
    }

    private static bool HasEnoughRefundDetailToProceed(string lower, ParsedSupportRequest request, CustomerFacts customerFacts)
    {
        var hasAmount = AmountRegex.IsMatch(lower) || HasTrustedChargeAmount(customerFacts);
        var hasDate = DateRegex.IsMatch(lower) || HasTrustedChargeDate(customerFacts);
        var firstMonthSignals = ContainsAny(lower, "signed up", "barely used", "few days", "not really what i need", "first-time");
        var billingErrorSignals = ContainsAny(lower, ChargedTwicePhrase, DoubleChargePhrase, "wrong amount", "charged after cancellation");

        if (IsDuplicateChargeDispute(lower))
        {
            return false;
        }

        if (IsClearFirstMonthRefund(request, new IntakeAssessment { PrimaryIntent = Intent.Refund, CustomerFacts = customerFacts }))
        {
            return true;
        }

        return firstMonthSignals
            ? hasAmount
            : billingErrorSignals && hasAmount && hasDate;
    }

    private static bool IsClearFirstMonthRefund(ParsedSupportRequest request, IntakeAssessment intake)
    {
        var text = request.RawText.ToLowerInvariant();

        var messageFitsExistingPattern = ContainsAny(text, RefundKeyword)
            && ContainsAny(text, "premium", "signed up", "barely used", "few days", "not really what i need")
            && AmountRegex.IsMatch(text);

        if (messageFitsExistingPattern)
        {
            return true;
        }

        return ShouldHandleRefund(text, intake)
            && HasInitialSubscriptionCharge(intake.CustomerFacts)
            && ContainsAny(text, RefundKeyword, "money back", "not really what i need", "barely used", "changed my mind");
    }

    private static string BuildCustomerFactsReasoning(CustomerFacts customerFacts)
    {
        if (!HasTrustedCustomerFacts(customerFacts))
        {
            return $"SupportOps lookup status={customerFacts.LookupStatus}.";
        }

        var charge = customerFacts.LastChargeAmount.HasValue && !string.IsNullOrWhiteSpace(customerFacts.LastChargeDate)
            ? $" Last charge={customerFacts.LastChargeAmount.Value.ToString("C", CurrencyCulture)} on {customerFacts.LastChargeDate}."
            : string.Empty;

        return $"SupportOps lookup found customer plan={customerFacts.Plan ?? "unknown"}, status={customerFacts.AccountStatus ?? "unknown"}.{charge}";
    }

    private static bool HasTrustedCustomerFacts(CustomerFacts? customerFacts)
        => customerFacts?.LookupStatus == CustomerLookupStatus.Found;

    private static bool HasTrustedChargeAmount(CustomerFacts customerFacts)
        => HasTrustedCustomerFacts(customerFacts) && customerFacts.LastChargeAmount.HasValue;

    private static bool HasTrustedChargeDate(CustomerFacts customerFacts)
        => HasTrustedCustomerFacts(customerFacts)
            && DateOnly.TryParse(customerFacts.LastChargeDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static bool HasInitialSubscriptionCharge(CustomerFacts? customerFacts)
    {
        if (!HasTrustedCustomerFacts(customerFacts)
            || !customerFacts!.LastChargeAmount.HasValue
            || !DateOnly.TryParse(customerFacts.SignupDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var signupDate)
            || !DateOnly.TryParse(customerFacts.LastChargeDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastChargeDate))
        {
            return false;
        }

        return signupDate == lastChargeDate
            && customerFacts.RefundsLast12Months.GetValueOrDefault() == 0;
    }

    private static bool ShouldRefuseDisclosure(string lower)
    {
        return ContainsAny(lower, "colleague", "another account", "someone else's account", "what plan they're on", "what card is being charged");
    }

    private static bool IsDuplicateChargeDispute(string lower)
        => ContainsAny(
            lower,
            ChargedTwicePhrase,
            DoubleChargePhrase,
            "charged me twice",
            "charged us twice",
            "charged my card twice",
            "charged two times",
            "charged 2 times",
            "duplicate charge",
            "duplicate billing");

    private static List<string> GetRefundPolicies(string lower)
    {
        var policies = new List<string>();

        if (ContainsAny(lower, "signed up", "barely used", "new premium", "new plan"))
        {
            policies.Add("Be flexible on refunds in the first month, especially for first-time customers who barely used the product.");
            policies.Add("Refunds go back to the original payment method and usually take 5-7 business days.");
        }

        if (ContainsAny(lower, ChargedTwicePhrase, DoubleChargePhrase, "wrong amount", "charged after cancellation"))
        {
            policies.Add("Obvious billing errors should be refunded and acknowledged clearly.");
        }

        if (ContainsAny(lower, "forgot to cancel", "multiple months", "past months"))
        {
            policies.Add("Do not promise retroactive multi-month refunds for forgotten cancellations; at most review the most recent month as goodwill.");
        }

        if (policies.Count == 0)
        {
            policies.Add("Refund handling must stay within the handbook rules and avoid unsupported promises.");
        }

        return policies;
    }

    private static string BuildRefundNextAction(string lower)
    {
        return ContainsAny(lower, "forgot to cancel", "multiple months", "past months")
            ? "Review the request under the goodwill policy and avoid promising retroactive multi-month refunds."
            : "Review the refund request and confirm the outcome against the handbook before finalizing it.";
    }

    private static List<string> GetDirectReplyPolicies(string lower)
    {
        var policies = new List<string>();

        if (ContainsAny(lower, "api access", "api"))
        {
            policies.Add("API access is Premium-only and cannot be granted as a one-off exception on Basic.");
        }

        if (ContainsAny(lower, "pause"))
        {
            policies.Add("There is no pause feature; customers can cancel and resubscribe later.");
        }

        if (ContainsAny(lower, "downgrade", "credit"))
        {
            policies.Add("No partial-month downgrade credits are offered; changes take effect next cycle.");
        }

        if (ContainsAny(lower, "colleague", "another account", "their plan", "someone else's account"))
        {
            policies.Add("Do not share customer information between accounts, even when asked politely.");
        }

        if (ContainsAny(lower, "cancel", "cancellation"))
        {
            policies.Add("Cancellation is immediate in the system, paid access remains through the current billing period, and data is retained for about 90 days.");
        }

        if (ContainsAny(lower, "charged again", "renewal"))
        {
            policies.Add("Explain calmly that the charge is usually a subscription renewal and only move into refund handling if the customer still wants that route.");
        }

        if (policies.Count == 0)
        {
            policies.Add("Use only handbook-backed statements and avoid unsupported exceptions or disclosures.");
        }

        return policies;
    }

    private static List<string> DetectSignals(string text)
    {
        var lower = text.ToLowerInvariant();
        var signals = new List<string>();

        AddSignalIf(signals, lower, RefundKeyword, "refund");
        AddSignalIf(signals, lower, "cancel", "cancellation");
        if (IsDuplicateChargeDispute(lower))
        {
            signals.Add("double-charge");
        }
        AddSignalIf(signals, lower, "manager", "manager-request");
        AddSignalIf(signals, lower, ChargebackKeyword, "chargeback");
        AddSignalIf(signals, lower, LawyerKeyword, "legal-language");
        AddSignalIf(signals, lower, "gdpr", "regulatory-language");
        AddSignalIf(signals, lower, "api", "api-question");
        AddSignalIf(signals, lower, "pause", "pause-request");
        AddSignalIf(signals, lower, "third time", "repeated-contact");

        return signals;
    }

    private static void AddSignalIf(List<string> signals, string lower, string needle, string signal)
    {
        if (lower.Contains(needle, StringComparison.Ordinal))
        {
            signals.Add(signal);
        }
    }

    private static string NormalizeWhitespace(string text)
    {
        var collapsed = Regex.Replace(text, @"\s+", " ");
        return collapsed.Trim();
    }

    private static string TrimTrailingPeriod(string value)
        => value.Trim().TrimEnd('.');

    private static bool ContainsAny(string text, params string[] needles)
        => needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
}
