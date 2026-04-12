using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;
using SupportAgent.Workflow.Events;
using SupportAgent.Workflow.State;

namespace SupportAgent.Workflow.Executors;

internal sealed class PolicyGateExecutor(string id) : Executor<IntakeContext, PolicyContext>(id)
{
    public override async ValueTask<PolicyContext> HandleAsync(IntakeContext message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var decision = SupportPolicyRules.EvaluatePolicy(message.Request, message.Intake);

        var policyContext = new PolicyContext
        {
            Request = message.Request,
            Intake = message.Intake,
            Policy = decision
        };

        await context.AddEventAsync(new PolicyAppliedEvent(policyContext), cancellationToken);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyPolicy, policyContext, scopeName: SupportWorkflowState.ScopeName);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyRoute, decision.Route.ToString(), scopeName: SupportWorkflowState.ScopeName);

        return policyContext;
    }
}