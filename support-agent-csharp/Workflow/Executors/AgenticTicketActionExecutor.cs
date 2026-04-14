using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using SupportAgent.Integrations;
using SupportAgent.Models;
using SupportAgent.Workflow.Events;
using SupportAgent.Workflow.State;

namespace SupportAgent.Workflow.Executors;

internal sealed class AgenticTicketActionExecutor : Executor<PolicyContext, OperationalActionContext>
{
    private readonly IChatClient _chatClient;
    private readonly SupportOpsMcpToolProvider _supportOpsMcpToolProvider;
    private AIAgent? _agent;
    private AgentSession? _session;

    public AgenticTicketActionExecutor(
        string id,
        IChatClient chatClient,
        SupportOpsMcpToolProvider supportOpsMcpToolProvider) : base(id)
    {
        _chatClient = chatClient;
        _supportOpsMcpToolProvider = supportOpsMcpToolProvider;
    }

    public override async ValueTask<OperationalActionContext> HandleAsync(
        PolicyContext message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (message.Policy.Route is not (PolicyRoute.RefundOrCancellation or PolicyRoute.Escalation))
        {
            throw new InvalidOperationException($"Ticket action is not supported for route {message.Policy.Route}.");
        }

        var expectedTicketKind = ResolveTicketKind(message.Policy.ActionTaken);
        var agent = await GetAgentAsync(cancellationToken);
        _session ??= await agent.CreateSessionAsync(cancellationToken);

        var prompt =
            $"""
            Create one SupportOps ticket for this policy-approved support action.

            Required ticket_kind: {expectedTicketKind}
            Customer email: {message.Request.Sender ?? "(unknown)"}
            Priority: {ResolvePriority(message.Intake.Urgency)}
            Policy route: {message.Policy.Route}
            Action taken: {message.Policy.ActionTaken}
            Message mode: {message.Policy.MessageMode}
            Intake summary: {message.Intake.Summary}
            Customer facts: {FormatCustomerFacts(message.Intake.CustomerFacts)}
            Applied handbook facts: {FormatList(message.Policy.AppliedPolicies)}
            Policy recommended next action: {message.Policy.RecommendedNextAction ?? "(none)"}

            Customer request:
            {message.Request.Body}

            Requirements:
            - Use the create_support_ticket tool exactly once before returning.
            - Use the required ticket_kind exactly.
            - Use the customer email from this prompt.
            - Return JSON that matches the TicketActionDecision schema exactly.
            - Do not draft a customer-facing reply.
            """;

        var response = await agent.RunAsync(prompt, _session, cancellationToken: cancellationToken);
        var decision = await DeserializeDecisionAsync(response.Text, cancellationToken);
        NormalizeDecision(decision);
        ValidateDecision(decision, expectedTicketKind);

        var artifact = BuildArtifact(message, decision);
        await context.AddEventAsync(new ArtifactPreparedEvent(artifact), cancellationToken);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyArtifact, artifact, scopeName: SupportWorkflowState.ScopeName);

        return new OperationalActionContext
        {
            PolicyContext = message,
            Artifact = artifact,
            OperatorNote = decision.RecommendedNextAction ?? message.Policy.RecommendedNextAction
        };
    }

    private async Task<AIAgent> GetAgentAsync(CancellationToken cancellationToken)
    {
        if (_agent is not null)
        {
            return _agent;
        }

        var tools = await _supportOpsMcpToolProvider.GetTicketActionToolsAsync();
        ChatClientAgentOptions agentOptions = new()
        {
            ChatOptions = new()
            {
                Instructions =
                    """
                    You create SupportOps tickets for policy-approved support work.
                    Return JSON that matches the schema exactly.
                    You may use only the available create_support_ticket tool.
                    For every request, create exactly one ticket before returning.
                    Use only these ticket_kind values: refund_review, cancellation, escalation.
                    Do not invent ticket ids, queues, or statuses. Copy them from the tool result.
                    Do not write customer-facing messages.
                    """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<TicketActionDecision>(),
                Tools = [.. tools]
            }
        };

        _agent = new ChatClientAgent(_chatClient, agentOptions);
        return _agent;
    }

    private async Task<TicketActionDecision> DeserializeDecisionAsync(string responseText, CancellationToken cancellationToken)
    {
        if (StructuredOutputJson.TryDeserialize<TicketActionDecision>(responseText, out var decision))
        {
            return decision!;
        }

        var agent = await GetAgentAsync(cancellationToken);
        _session ??= await agent.CreateSessionAsync(cancellationToken);

        var repairPrompt =
            $$"""
            Your previous reply could not be parsed as valid JSON for the TicketActionDecision schema.
            Return only valid JSON that matches the schema exactly.

            Previous reply:
            {{responseText}}
            """;

        var repaired = await agent.RunAsync(repairPrompt, _session, cancellationToken: cancellationToken);
        if (StructuredOutputJson.TryDeserialize<TicketActionDecision>(repaired.Text, out decision))
        {
            return decision!;
        }

        throw new InvalidOperationException("Failed to deserialize TicketActionDecision after JSON repair retry.");
    }

    private static string ResolveTicketKind(ActionTaken actionTaken)
    {
        return actionTaken switch
        {
            ActionTaken.RefundTicketCreated => "refund_review",
            ActionTaken.CancellationTicketCreated => "cancellation",
            ActionTaken.EscalatedToHuman => "escalation",
            _ => throw new InvalidOperationException($"Unsupported ticket action: {actionTaken}.")
        };
    }

    private static string ResolvePriority(Urgency urgency)
        => urgency == Urgency.High ? "high" : "normal";

    private static void ValidateDecision(TicketActionDecision decision, string expectedTicketKind)
    {
        if (!decision.TicketCreated)
        {
            throw new InvalidOperationException("Ticket action agent completed without creating a ticket.");
        }

        if (string.IsNullOrWhiteSpace(decision.TicketId))
        {
            throw new InvalidOperationException("Ticket action agent did not return a created ticket id.");
        }

        if (!decision.TicketId.StartsWith("SUP-", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Ticket action agent returned unexpected ticket id '{decision.TicketId}'.");
        }

        if (!string.Equals(decision.TicketKind, expectedTicketKind, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Ticket action agent returned ticket_kind '{decision.TicketKind}', expected '{expectedTicketKind}'.");
        }

        if (string.IsNullOrWhiteSpace(decision.Queue))
        {
            throw new InvalidOperationException("Ticket action agent did not return the SupportOps queue.");
        }
    }

    private static SimulatedArtifact BuildArtifact(PolicyContext policyContext, TicketActionDecision decision)
    {
        var artifactType = decision.TicketKind switch
        {
            "refund_review" => ArtifactType.RefundTicket,
            "cancellation" => ArtifactType.CancellationTicket,
            "escalation" => ArtifactType.EscalationHandoff,
            _ => throw new InvalidOperationException($"Unsupported ticket kind: {decision.TicketKind}.")
        };

        var title = artifactType switch
        {
            ArtifactType.RefundTicket => "SupportOps Refund Review Ticket",
            ArtifactType.CancellationTicket => "SupportOps Cancellation Ticket",
            ArtifactType.EscalationHandoff => "SupportOps Escalation Ticket",
            _ => "SupportOps Ticket"
        };

        return new SimulatedArtifact
        {
            ArtifactType = artifactType,
            DisplayTitle = title,
            Payload =
                $"""
                Ticket id: {decision.TicketId}
                Kind: {decision.TicketKind}
                Queue: {decision.Queue}
                Customer: {policyContext.Request.Sender ?? "unknown"}
                Summary: {decision.Summary}
                Reason: {decision.Reason}
                Recommended next action: {decision.RecommendedNextAction ?? policyContext.Policy.RecommendedNextAction ?? "(none)"}
                """,
            Metadata = new Dictionary<string, string>
            {
                ["ticket_id"] = decision.TicketId!,
                ["ticket_kind"] = decision.TicketKind!,
                ["queue"] = decision.Queue!,
                ["route"] = policyContext.Policy.Route.ToString(),
                ["action"] = policyContext.Policy.ActionTaken.ToString()
            }
        };
    }

    private static void NormalizeDecision(TicketActionDecision decision)
    {
        decision.TicketId = NormalizeString(decision.TicketId);
        decision.TicketKind = NormalizeString(decision.TicketKind)?.ToLowerInvariant();
        decision.Queue = NormalizeString(decision.Queue);
        decision.Summary = NormalizeString(decision.Summary);
        decision.Reason = NormalizeString(decision.Reason);
        decision.RecommendedNextAction = NormalizeString(decision.RecommendedNextAction);
    }

    private static string FormatCustomerFacts(CustomerFacts customerFacts)
    {
        return customerFacts.LookupStatus == CustomerLookupStatus.Found
            ? $"lookup_status={customerFacts.LookupStatus}; email={customerFacts.Email ?? "(unknown)"}; plan={customerFacts.Plan ?? "(unknown)"}; account_status={customerFacts.AccountStatus ?? "(unknown)"}; last_charge_amount={customerFacts.LastChargeAmount?.ToString() ?? "(unknown)"}; last_charge_date={customerFacts.LastChargeDate ?? "(unknown)"}"
            : $"lookup_status={customerFacts.LookupStatus}";
    }

    private static string FormatList(IEnumerable<string> items)
    {
        var values = items.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
        return values.Count == 0 ? "(none)" : string.Join("; ", values);
    }

    private static string? NormalizeString(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
