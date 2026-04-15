from dataclasses import dataclass, field
from enum import Enum

from pydantic import BaseModel, Field


class Intent(Enum):
    Unclear = "Unclear"
    Refund = "Refund"
    Cancellation = "Cancellation"
    Question = "Question"
    Complaint = "Complaint"


class Sentiment(Enum):
    Neutral = "Neutral"
    Frustrated = "Frustrated"
    Angry = "Angry"
    Confused = "Confused"


class Urgency(Enum):
    Low = "Low"
    Medium = "Medium"
    High = "High"


class ActionTaken(Enum):
    None_ = "None"
    ReplySent = "ReplySent"
    ClarificationRequested = "ClarificationRequested"
    EscalatedToHuman = "EscalatedToHuman"
    RefundTicketCreated = "RefundTicketCreated"
    CancellationTicketCreated = "CancellationTicketCreated"


class PolicyRoute(Enum):
    Reply = "Reply"
    Clarification = "Clarification"
    RefundOrCancellation = "RefundOrCancellation"
    Escalation = "Escalation"


class MessageMode(Enum):
    Reply = "Reply"
    ClarificationEmail = "ClarificationEmail"
    OperationalAcknowledgement = "OperationalAcknowledgement"
    EscalationAcknowledgement = "EscalationAcknowledgement"


class ArtifactMode(Enum):
    ClarificationEmail = "ClarificationEmail"
    RefundTicket = "RefundTicket"
    CancellationTicket = "CancellationTicket"
    EscalationHandoff = "EscalationHandoff"


class ArtifactType(Enum):
    ClarificationEmail = "ClarificationEmail"
    RefundTicket = "RefundTicket"
    CancellationTicket = "CancellationTicket"
    EscalationHandoff = "EscalationHandoff"


class WorkflowTraceStepKind(Enum):
    Info = "Info"
    Detail = "Detail"
    Error = "Error"


@dataclass(frozen=True)
class SupportRequestResult:
    intent: Intent
    sentiment: Sentiment
    urgency: Urgency
    reasoning: list[str]
    action_taken: ActionTaken
    customer_facing_response: str
    recommended_next_action: str | None


@dataclass
class ParsedSupportRequest:
    raw_text: str = ""
    normalized_text: str = ""
    sender: str | None = None
    subject: str | None = None
    body: str = ""
    detected_signals: list[str] = field(default_factory=list)


class IntakeAssessment(BaseModel):
    primary_intent: Intent = Field(
        description="Primary intent. Must be one of: Unclear, Refund, Cancellation, Question, Complaint."
    )
    sentiment: Sentiment = Field(
        description="Customer sentiment. Must be one of: Neutral, Frustrated, Angry, Confused."
    )
    urgency: Urgency = Field(description="Urgency. Must be one of: Low, Medium, High.")
    missing_information: list[str] = Field(
        default_factory=list,
        description="Specific missing details required before handling the case safely.",
    )
    escalation_signals: list[str] = Field(
        default_factory=list,
        description="Concrete escalation cues found in the message.",
    )
    summary: str = Field(description="One short summary of the customer issue.")
    confidence_notes: str = Field(
        default="",
        description="Short note explaining uncertainty, ambiguity, or confidence level.",
    )


@dataclass
class PolicyDecision:
    route: PolicyRoute
    action_taken: ActionTaken
    recommended_next_action: str | None
    applied_policies: list[str]
    reasoning_steps: list[str]
    message_mode: MessageMode
    artifact_mode: ArtifactMode | None


class CustomerMessageDraft(BaseModel):
    mode: MessageMode = Field(
        description="The message mode: Reply, ClarificationEmail, OperationalAcknowledgement, or EscalationAcknowledgement."
    )
    subject_line: str | None = Field(
        default=None,
        description="Optional subject line for email-style responses.",
    )
    body: str = Field(description="Customer-facing body text. Plain text only, no markdown.")
    tone_checks: list[str] = Field(
        default_factory=list,
        description="Short checklist of tone checks the draft satisfies.",
    )


@dataclass
class SimulatedArtifact:
    artifact_type: ArtifactType
    display_title: str
    payload: str
    metadata: dict[str, str] = field(default_factory=dict)


@dataclass
class IntakeContext:
    request: ParsedSupportRequest
    intake: IntakeAssessment


@dataclass
class PolicyContext:
    request: ParsedSupportRequest
    intake: IntakeAssessment
    policy: PolicyDecision


@dataclass
class OperationalActionContext:
    policy_context: PolicyContext
    artifact: SimulatedArtifact
    operator_note: str | None = None


@dataclass
class EscalationContext:
    policy_context: PolicyContext
    artifact: SimulatedArtifact
    queue: str
    sla: str
    next_steps: list[str]


@dataclass
class DraftedResponseContext:
    policy_context: PolicyContext
    draft: CustomerMessageDraft
    artifact: SimulatedArtifact | None = None


@dataclass(frozen=True)
class WorkflowTraceStep:
    stage: str
    detail: str
    kind: WorkflowTraceStepKind = WorkflowTraceStepKind.Info


@dataclass(frozen=True)
class SupportProcessingOutcome:
    result: SupportRequestResult
    artifact: SimulatedArtifact | None
    trace: list[WorkflowTraceStep]
