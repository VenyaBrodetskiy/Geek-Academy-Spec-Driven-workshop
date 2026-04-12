using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;

namespace SupportAgent.Workflow.Events;

internal sealed class PolicyAppliedEvent(PolicyContext context) : WorkflowEvent(context)
{
    public PolicyContext Context { get; } = context;
}