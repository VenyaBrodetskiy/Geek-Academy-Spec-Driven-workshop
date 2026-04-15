from __future__ import annotations

import re
from pathlib import Path

from app.models import (
    ActionTaken,
    ArtifactMode,
    IntakeAssessment,
    Intent,
    MessageMode,
    ParsedSupportRequest,
    PolicyContext,
    PolicyDecision,
    PolicyRoute,
    Sentiment,
    Urgency,
)


REFUND_KEYWORD = "refund"
CHARGED_TWICE_PHRASE = "charged twice"
DOUBLE_CHARGE_PHRASE = "double charge"
LAWYER_KEYWORD = "lawyer"
CHARGEBACK_KEYWORD = "chargeback"

AMOUNT_RE = re.compile(r"\$(?P<amount>\d+(?:\.\d{1,2})?)")
DATE_RE = re.compile(
    r"\b(?:jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\s+\d{1,2}\b"
    r"|\b\d{1,2}/\d{1,2}(?:/\d{2,4})?\b",
    re.IGNORECASE,
)


class SupportPolicyRules:
    def __init__(self, handbook_text: str) -> None:
        self.handbook_text = handbook_text

    @staticmethod
    def load_handbook(base_directory: Path | None = None) -> str:
        root = base_directory or Path(__file__).resolve().parents[1]
        handbook_path = root / "data" / "support_handbook.md"
        if not handbook_path.exists():
            raise FileNotFoundError(f"Could not locate {handbook_path}.")
        return handbook_path.read_text(encoding="utf-8")

    def parse_request(self, raw_text: str) -> ParsedSupportRequest:
        sender: str | None = None
        subject: str | None = None
        body_lines: list[str] = []

        for line in raw_text.replace("\r\n", "\n").split("\n"):
            if line.strip() == "---":
                break

            if sender is None and line.lower().startswith("from:"):
                sender = line[5:].strip()
                continue

            if subject is None and line.lower().startswith("subject:"):
                subject = line[8:].strip()
                continue

            body_lines.append(line)

        body = "\n".join(body_lines).strip()
        normalized = normalize_whitespace(body)
        detected_signals = detect_signals(f"{subject or ''}\n{body}")

        return ParsedSupportRequest(
            raw_text=raw_text.strip(),
            normalized_text=normalized,
            sender=sender,
            subject=subject,
            body=body,
            detected_signals=detected_signals,
        )

    @staticmethod
    def evaluate_policy(request: ParsedSupportRequest, intake: IntakeAssessment) -> PolicyDecision:
        normalized = request.normalized_text
        lower = normalized.lower()
        reasoning = [
            f"Intake summary: {intake.summary}",
            f"Intent={intake.primary_intent.value}, Sentiment={intake.sentiment.value}, Urgency={intake.urgency.value}.",
        ]
        applied_policies: list[str] = []

        escalation_decision = try_build_escalation_decision(lower, intake, applied_policies, reasoning)
        if escalation_decision is not None:
            return escalation_decision

        if should_refuse_disclosure(lower):
            applied_policies.append(
                "Do not share customer information between accounts, even when asked politely."
            )
            reasoning.append(
                "The request asks for account details belonging to another person, so it must be refused directly rather than clarified."
            )

            return PolicyDecision(
                route=PolicyRoute.Reply,
                action_taken=ActionTaken.ReplySent,
                recommended_next_action=None,
                applied_policies=applied_policies,
                reasoning_steps=reasoning,
                message_mode=MessageMode.Reply,
                artifact_mode=None,
            )

        if is_clear_first_month_refund(request):
            applied_policies.append(
                "Be flexible on refunds in the first month, especially for first-time customers who barely used the product."
            )
            applied_policies.append(
                "Refunds go back to the original payment method and usually take 5-7 business days."
            )
            reasoning.append(
                "The message clearly fits the first-month low-usage refund pattern, so it should move straight to refund handling."
            )

            return PolicyDecision(
                route=PolicyRoute.RefundOrCancellation,
                action_taken=ActionTaken.RefundTicketCreated,
                recommended_next_action=(
                    "Process the refund review under the first-month flexibility policy and confirm the 5-7 business day timing."
                ),
                applied_policies=applied_policies,
                reasoning_steps=reasoning,
                message_mode=MessageMode.OperationalAcknowledgement,
                artifact_mode=ArtifactMode.RefundTicket,
            )

        if is_cancellation_operation(lower):
            applied_policies.append(
                "Cancellation is effective immediately, but paid access continues through the current billing period."
            )
            applied_policies.append("Data is retained for about 90 days after cancellation.")
            reasoning.append(
                "The customer is explicitly asking to cancel, so this should create a cancellation handling artifact."
            )

            return PolicyDecision(
                route=PolicyRoute.RefundOrCancellation,
                action_taken=ActionTaken.CancellationTicketCreated,
                recommended_next_action=(
                    "Process the cancellation and confirm access through the current billing period."
                ),
                applied_policies=applied_policies,
                reasoning_steps=reasoning,
                message_mode=MessageMode.OperationalAcknowledgement,
                artifact_mode=ArtifactMode.CancellationTicket,
            )

        if is_direct_policy_question(lower):
            applied_policies.extend(get_direct_reply_policies(lower))
            reasoning.append(
                "The message is a handbook-safe plan or policy question, so classifier missing-information noise is ignored."
            )

            return PolicyDecision(
                route=PolicyRoute.Reply,
                action_taken=ActionTaken.ReplySent,
                recommended_next_action=None,
                applied_policies=applied_policies,
                reasoning_steps=reasoning,
                message_mode=MessageMode.Reply,
                artifact_mode=None,
            )

        if is_general_non_operational_complaint(lower, intake):
            applied_policies.extend(get_direct_reply_policies(lower))
            applied_policies.append(
                "For general product complaints without billing or account signals, acknowledge the concern and invite specific product examples; do not ask for billing, order, or transaction details."
            )
            reasoning.append(
                "The message is a general complaint without a refund, billing, cancellation, account, or escalation signal, so it should receive a direct acknowledgement instead of an operational clarification."
            )

            return PolicyDecision(
                route=PolicyRoute.Reply,
                action_taken=ActionTaken.ReplySent,
                recommended_next_action=None,
                applied_policies=applied_policies,
                reasoning_steps=reasoning,
                message_mode=MessageMode.Reply,
                artifact_mode=None,
            )

        missing_info = merge_missing_information(request, intake, lower)
        if should_handle_refund(lower, intake) and has_enough_refund_detail_to_proceed(lower, request):
            missing_info.clear()

        if missing_info:
            reasoning.append(
                f"Clarification required before safe handling: {', '.join(missing_info)}."
            )
            applied_policies.append(
                "Do not guess when billing or refund details are missing; ask for the specific facts needed to continue."
            )

            return PolicyDecision(
                route=PolicyRoute.Clarification,
                action_taken=ActionTaken.ClarificationRequested,
                recommended_next_action="Wait for the customer to reply with the requested billing details.",
                applied_policies=applied_policies,
                reasoning_steps=reasoning,
                message_mode=MessageMode.ClarificationEmail,
                artifact_mode=ArtifactMode.ClarificationEmail,
            )

        if should_handle_refund(lower, intake):
            applied_policies.extend(get_refund_policies(lower))
            reasoning.append(
                "The request is refund-related and contains enough information to prepare a refund review artifact."
            )

            return PolicyDecision(
                route=PolicyRoute.RefundOrCancellation,
                action_taken=ActionTaken.RefundTicketCreated,
                recommended_next_action=build_refund_next_action(lower),
                applied_policies=applied_policies,
                reasoning_steps=reasoning,
                message_mode=MessageMode.OperationalAcknowledgement,
                artifact_mode=ArtifactMode.RefundTicket,
            )

        applied_policies.extend(get_direct_reply_policies(lower))
        reasoning.append(
            "This can be handled as a direct reply without clarification, escalation, or simulated operational work."
        )

        return PolicyDecision(
            route=PolicyRoute.Reply,
            action_taken=ActionTaken.ReplySent,
            recommended_next_action=None,
            applied_policies=applied_policies,
            reasoning_steps=reasoning,
            message_mode=MessageMode.Reply,
            artifact_mode=None,
        )

    def build_handbook_excerpt(self, policy_decision: PolicyDecision) -> str:
        if not policy_decision.applied_policies:
            return self.handbook_text
        return "Relevant handbook facts:\n- " + "\n- ".join(policy_decision.applied_policies)


def build_escalation_summary(context: PolicyContext) -> str:
    return f"Escalate {context.intake.primary_intent.value.lower()} case: {context.intake.summary}"


def build_escalation_queue(request: ParsedSupportRequest, intake: IntakeAssessment) -> str:
    lower = request.normalized_text.lower()
    if contains_any(lower, "gdpr", LAWYER_KEYWORD, "lawyers", "regulator", CHARGEBACK_KEYWORD):
        return "Support - Legal Review"

    return "Support - Priority" if intake.urgency == Urgency.High else "Support - Senior Review"


def build_escalation_sla(urgency: Urgency) -> str:
    return "4 business hours" if urgency == Urgency.High else "1 business day"


def build_escalation_next_steps(request: ParsedSupportRequest, intake: IntakeAssessment) -> list[str]:
    steps = [
        "Review the full account and billing timeline before replying.",
        "Confirm the handbook-backed resolution path before promising credits or refunds.",
    ]

    lower = request.normalized_text.lower()

    if contains_any(lower, CHARGEBACK_KEYWORD, "bank", "disputing"):
        steps.append(
            "Coordinate with billing before responding because the customer mentioned a chargeback or bank dispute."
        )

    if contains_any(lower, "gdpr", LAWYER_KEYWORD, "regulator"):
        steps.append("Route the case through the legal-sensitive review path before any customer follow-up.")

    return steps


def try_build_escalation_decision(
    lower: str,
    intake: IntakeAssessment,
    applied_policies: list[str],
    reasoning: list[str],
) -> PolicyDecision | None:
    triggers: list[str] = []

    if contains_any(lower, "manager", "supervisor"):
        triggers.append("explicit manager or supervisor request")

    if contains_any(lower, LAWYER_KEYWORD, "lawyers", "gdpr", "regulator", CHARGEBACK_KEYWORD, "bank dispute"):
        triggers.append("legal, regulatory, or chargeback language")

    amount_match = AMOUNT_RE.search(lower)
    if amount_match:
        amount = float(amount_match.group("amount"))
        if amount > 100:
            triggers.append("billing dispute over $100")

    if contains_any(lower, "third time", "3rd time", "three times", "week about the same", "again and again"):
        triggers.append("repeated unresolved contact pattern")

    if intake.sentiment == Sentiment.Angry and contains_any(lower, "idiot", "useless", "stupid", "terrible"):
        triggers.append("abusive tone that should be escalated instead of debated")

    if not triggers:
        return None

    applied_policies.append(
        "Escalate manager requests, legal-sensitive cases, chargeback threats, high-value billing disputes, and repeated unresolved complaints."
    )
    reasoning.append(f"Escalation triggers: {', '.join(triggers)}.")

    return PolicyDecision(
        route=PolicyRoute.Escalation,
        action_taken=ActionTaken.EscalatedToHuman,
        recommended_next_action="Senior support should review the case within about 4 business hours.",
        applied_policies=applied_policies,
        reasoning_steps=reasoning,
        message_mode=MessageMode.EscalationAcknowledgement,
        artifact_mode=ArtifactMode.EscalationHandoff,
    )


def merge_missing_information(
    request: ParsedSupportRequest,
    intake: IntakeAssessment,
    lower: str,
) -> list[str]:
    missing = {
        item.strip()
        for item in intake.missing_information
        if item and item.strip()
    }
    has_amount = AMOUNT_RE.search(lower) is not None
    has_date = DATE_RE.search(lower) is not None
    has_account_context = bool(request.sender) or contains_any(lower, "account", "plan", "subscription", "card")

    if contains_any(lower, CHARGED_TWICE_PHRASE, DOUBLE_CHARGE_PHRASE) and (not has_amount or not has_date):
        missing.add("charge date and amount")

    if contains_any(lower, "declined", "what is happening", "don't know what i'm paying for") and not has_account_context:
        missing.add("account or billing context")

    if (
        intake.primary_intent == Intent.Refund or contains_any(lower, REFUND_KEYWORD, "charged", "billing")
    ) and not has_amount and not has_date:
        missing.add("specific billing details such as date, amount, or charge reference")

    return list(missing)


def is_cancellation_operation(lower: str) -> bool:
    explicit_cancel = contains_any(lower, "cancel me", "cancel my", "please cancel", "i want to cancel", "close my account")
    question_only = contains_any(lower, "what happens if i cancel", "thinking about cancel", "if i cancel", "cancel now, do i keep access")
    return explicit_cancel and not question_only


def should_handle_refund(lower: str, intake: IntakeAssessment) -> bool:
    return intake.primary_intent == Intent.Refund or contains_any(
        lower,
        REFUND_KEYWORD,
        CHARGED_TWICE_PHRASE,
        DOUBLE_CHARGE_PHRASE,
        "wrong amount",
        "charged after cancellation",
    )


def is_direct_policy_question(lower: str) -> bool:
    return contains_any(
        lower,
        "api access",
        "api",
        "pause",
        "downgrade",
        "what happens if i cancel",
        "thinking about cancel",
        "if i cancel",
        "cancel now, do i keep access",
    )


def is_general_non_operational_complaint(lower: str, intake: IntakeAssessment) -> bool:
    if intake.primary_intent != Intent.Complaint:
        return False

    return not contains_any(
        lower,
        REFUND_KEYWORD,
        "billing",
        "charged",
        "charge",
        "card",
        "payment",
        "cancel",
        "account",
        "premium",
        "basic",
    )


def has_enough_refund_detail_to_proceed(lower: str, request: ParsedSupportRequest) -> bool:
    has_amount = AMOUNT_RE.search(lower) is not None
    has_date = DATE_RE.search(lower) is not None
    first_month_signals = contains_any(lower, "signed up", "barely used", "few days", "not really what i need", "first-time")
    billing_error_signals = contains_any(lower, CHARGED_TWICE_PHRASE, DOUBLE_CHARGE_PHRASE, "wrong amount", "charged after cancellation")

    if is_clear_first_month_refund(request):
        return True

    return has_amount if first_month_signals else billing_error_signals and has_amount and has_date


def is_clear_first_month_refund(request: ParsedSupportRequest) -> bool:
    text = request.raw_text.lower()
    return (
        contains_any(text, REFUND_KEYWORD)
        and contains_any(text, "premium", "signed up", "barely used", "few days", "not really what i need")
        and AMOUNT_RE.search(text) is not None
    )


def should_refuse_disclosure(lower: str) -> bool:
    return contains_any(lower, "colleague", "another account", "someone else's account", "what plan they're on", "what card is being charged")


def get_refund_policies(lower: str) -> list[str]:
    policies: list[str] = []

    if contains_any(lower, "signed up", "barely used", "new premium", "new plan"):
        policies.append(
            "Be flexible on refunds in the first month, especially for first-time customers who barely used the product."
        )
        policies.append("Refunds go back to the original payment method and usually take 5-7 business days.")

    if contains_any(lower, CHARGED_TWICE_PHRASE, DOUBLE_CHARGE_PHRASE, "wrong amount", "charged after cancellation"):
        policies.append("Obvious billing errors should be refunded and acknowledged clearly.")

    if contains_any(lower, "forgot to cancel", "multiple months", "past months"):
        policies.append(
            "Do not promise retroactive multi-month refunds for forgotten cancellations; at most review the most recent month as goodwill."
        )

    if not policies:
        policies.append("Refund handling must stay within the handbook rules and avoid unsupported promises.")

    return policies


def build_refund_next_action(lower: str) -> str:
    if contains_any(lower, "forgot to cancel", "multiple months", "past months"):
        return "Review the request under the goodwill policy and avoid promising retroactive multi-month refunds."
    return "Review the refund request and confirm the outcome against the handbook before finalizing it."


def get_direct_reply_policies(lower: str) -> list[str]:
    policies: list[str] = []

    if contains_any(lower, "api access", "api"):
        policies.append("API access is Premium-only and cannot be granted as a one-off exception on Basic.")

    if contains_any(lower, "pause"):
        policies.append("There is no pause feature; customers can cancel and resubscribe later.")

    if contains_any(lower, "downgrade", "credit"):
        policies.append("No partial-month downgrade credits are offered; changes take effect next cycle.")

    if contains_any(lower, "colleague", "another account", "their plan", "someone else's account"):
        policies.append("Do not share customer information between accounts, even when asked politely.")

    if contains_any(lower, "cancel", "cancellation"):
        policies.append(
            "Cancellation is immediate in the system, paid access remains through the current billing period, and data is retained for about 90 days."
        )

    if contains_any(lower, "charged again", "renewal"):
        policies.append(
            "Explain calmly that the charge is usually a subscription renewal and only move into refund handling if the customer still wants that route."
        )

    if not policies:
        policies.append("Use only handbook-backed statements and avoid unsupported exceptions or disclosures.")

    return policies


def detect_signals(text: str) -> list[str]:
    lower = text.lower()
    signals: list[str] = []

    add_signal_if(signals, lower, REFUND_KEYWORD, "refund")
    add_signal_if(signals, lower, "cancel", "cancellation")
    add_signal_if(signals, lower, CHARGED_TWICE_PHRASE, "double-charge")
    add_signal_if(signals, lower, "manager", "manager-request")
    add_signal_if(signals, lower, CHARGEBACK_KEYWORD, "chargeback")
    add_signal_if(signals, lower, LAWYER_KEYWORD, "legal-language")
    add_signal_if(signals, lower, "gdpr", "regulatory-language")
    add_signal_if(signals, lower, "api", "api-question")
    add_signal_if(signals, lower, "pause", "pause-request")
    add_signal_if(signals, lower, "third time", "repeated-contact")

    return signals


def add_signal_if(signals: list[str], lower: str, needle: str, signal: str) -> None:
    if needle in lower:
        signals.append(signal)


def normalize_whitespace(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()


def contains_any(text: str, *needles: str) -> bool:
    return any(needle.lower() in text.lower() for needle in needles)
