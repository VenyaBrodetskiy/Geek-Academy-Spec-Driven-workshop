namespace SupportAgent.Models;

public sealed class IntakeContext
{
    public required ParsedSupportRequest Request { get; init; }

    public required IntakeAssessment Intake { get; init; }
}

public sealed class PolicyContext
{
    public required ParsedSupportRequest Request { get; init; }

    public required IntakeAssessment Intake { get; init; }

    public required PolicyDecision Policy { get; init; }
}

public sealed class OperationalActionContext
{
    public required PolicyContext PolicyContext { get; init; }

    public required SimulatedArtifact Artifact { get; init; }

    public string? OperatorNote { get; init; }
}

public sealed class EscalationContext
{
    public required PolicyContext PolicyContext { get; init; }

    public required SimulatedArtifact Artifact { get; init; }

    public required string Queue { get; init; }

    public required string Sla { get; init; }

    public IReadOnlyList<string> NextSteps { get; init; } = [];
}

public sealed class DraftedResponseContext
{
    public required PolicyContext PolicyContext { get; init; }

    public required CustomerMessageDraft Draft { get; init; }

    public SimulatedArtifact? Artifact { get; init; }
}

public sealed record SupportProcessingOutcome(
    SupportRequestResult Result,
    SimulatedArtifact? Artifact,
    IReadOnlyList<WorkflowTraceStep> Trace);
