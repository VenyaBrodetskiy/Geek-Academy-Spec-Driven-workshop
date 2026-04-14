using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;

namespace SupportAgent.Workflow.Events;

internal sealed class WorkflowTraceEvent(WorkflowTraceStep step) : WorkflowEvent(step)
{
    public WorkflowTraceStep Step { get; } = step;
}
