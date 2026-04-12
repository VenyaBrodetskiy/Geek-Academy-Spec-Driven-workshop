using System.Text.Json.Serialization;

namespace SupportAgent.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PolicyRoute
{
    Reply,
    Clarification,
    RefundOrCancellation,
    Escalation
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageMode
{
    Reply,
    ClarificationEmail,
    OperationalAcknowledgement,
    EscalationAcknowledgement
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArtifactMode
{
    ClarificationEmail,
    RefundTicket,
    CancellationTicket,
    EscalationHandoff
}

public sealed class PolicyDecision
{
    public PolicyRoute Route { get; init; }

    public ActionTaken ActionTaken { get; init; }

    public string? RecommendedNextAction { get; init; }

    public List<string> AppliedPolicies { get; init; } = [];

    public List<string> ReasoningSteps { get; init; } = [];

    public MessageMode MessageMode { get; init; }

    public ArtifactMode? ArtifactMode { get; init; }
}