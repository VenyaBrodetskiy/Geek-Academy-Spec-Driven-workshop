using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;

namespace SupportAgent.Workflow.Events;

internal sealed class EscalationPreparedEvent(EscalationContext context) : WorkflowEvent(context)
{
    public EscalationContext Context { get; } = context;
}