using System.Text.Json.Serialization;

namespace SupportAgent.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArtifactType
{
    ClarificationEmail,
    RefundTicket,
    CancellationTicket,
    EscalationHandoff
}

public sealed class SimulatedArtifact
{
    public ArtifactType ArtifactType { get; init; }

    public string DisplayTitle { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public Dictionary<string, string> Metadata { get; init; } = [];
}