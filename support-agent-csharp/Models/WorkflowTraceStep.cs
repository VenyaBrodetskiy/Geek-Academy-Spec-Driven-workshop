namespace SupportAgent.Models;

public enum WorkflowTraceStepKind
{
    Milestone,
    Detail,
    Error
}

public sealed record WorkflowTraceStep(
    string Stage,
    string Detail,
    WorkflowTraceStepKind Kind = WorkflowTraceStepKind.Milestone);
