using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;
using SupportAgent.Workflow.State;

namespace SupportAgent.Workflow.Executors;

internal sealed class NormalizeRequestExecutor(string id, SupportPolicyRules policyRules) : Executor<string, ParsedSupportRequest>(id)
{
    public override async ValueTask<ParsedSupportRequest> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var request = policyRules.ParseRequest(message);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyRequest, request, scopeName: SupportWorkflowState.ScopeName);
        return request;
    }
}