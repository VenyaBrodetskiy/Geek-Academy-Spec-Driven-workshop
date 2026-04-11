from dataclasses import dataclass
from enum import Enum


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


@dataclass(frozen=True)
class SupportRequestResult:
    intent: Intent
    sentiment: Sentiment
    urgency: Urgency
    reasoning: list[str]
    action_taken: ActionTaken
    customer_facing_response: str
    recommended_next_action: str | None
