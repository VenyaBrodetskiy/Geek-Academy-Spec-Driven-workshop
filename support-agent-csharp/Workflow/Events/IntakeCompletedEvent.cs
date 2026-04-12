using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;

namespace SupportAgent.Workflow.Events;

internal sealed class IntakeCompletedEvent(IntakeContext context) : WorkflowEvent(context)
{
    public IntakeContext Context { get; } = context;
}