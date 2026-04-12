using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;

namespace SupportAgent.Workflow.Events;

internal sealed class ResponseDraftedEvent(CustomerMessageDraft draft) : WorkflowEvent(draft)
{
    public CustomerMessageDraft Draft { get; } = draft;
}