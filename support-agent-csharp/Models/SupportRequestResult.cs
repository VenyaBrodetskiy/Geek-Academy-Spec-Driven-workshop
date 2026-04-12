using System.Text.Json.Serialization;

namespace SupportAgent.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Intent
{
    Unclear,
    Refund,
    Cancellation,
    Question,
    Complaint
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Sentiment
{
    Neutral,
    Frustrated,
    Angry,
    Confused
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Urgency
{
    Low,
    Medium,
    High
}

[JsonConverter(typeof(JsonStringEnumConverter))]
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
