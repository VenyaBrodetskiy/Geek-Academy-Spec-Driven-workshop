namespace SupportAgent.Models;

public enum Intent
{
    Unclear,
    Refund,
    Cancellation,
    Question,
    Complaint
}

public enum Sentiment
{
    Neutral,
    Frustrated,
    Angry,
    Confused
}

public enum Urgency
{
    Low,
    Medium,
    High
}

public enum ActionTaken
{
    None,
    ReplySent,
    ClarificationRequested,
    EscalatedToHuman,
    RefundTicketCreated,
    CancellationTicketCreated
}

public record SupportRequestResult(
    Intent Intent,
    Sentiment Sentiment,
    Urgency Urgency,
    IReadOnlyList<string> Reasoning,
    ActionTaken ActionTaken,
    string CustomerFacingResponse,
    string? RecommendedNextAction
);
