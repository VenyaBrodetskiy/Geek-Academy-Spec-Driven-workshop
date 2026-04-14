using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;
using SupportAgent.Workflow.Events;
using SupportAgent.Workflow.State;

namespace SupportAgent.Workflow.Executors;

internal sealed class AssembleSupportResultExecutor(string id) : Executor(id)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder
            .ConfigureRoutes(routeBuilder =>
                routeBuilder
                    .AddHandler<DraftedResponseContext>(HandleDraftedAsync)
                    .AddHandler<OperationalActionContext>(HandleOperationalAsync)
                    .AddHandler<EscalationContext>(HandleEscalationAsync))
            .YieldsOutput<SupportRequestResult>();
    }

    private static async ValueTask HandleDraftedAsync(DraftedResponseContext message, IWorkflowContext context)
    {
        await AddTraceAsync(context, "Assemble", "Building final support result.");
        var result = BuildResult(message.PolicyContext, message.Draft.Body);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyResult, result, scopeName: SupportWorkflowState.ScopeName);
        await context.YieldOutputAsync(result);
    }

    private static async ValueTask HandleOperationalAsync(OperationalActionContext message, IWorkflowContext context)
    {
        await AddTraceAsync(context, "Assemble", "Building final support result with prepared artifact.");
        var draft = await context.ReadStateAsync<CustomerMessageDraft>(SupportWorkflowState.KeyDraft, scopeName: SupportWorkflowState.ScopeName);
        var responseText = draft?.Body ?? message.Artifact.Payload;
        var result = BuildResult(message.PolicyContext, responseText);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyResult, result, scopeName: SupportWorkflowState.ScopeName);
        await context.YieldOutputAsync(result);
    }

    private static async ValueTask HandleEscalationAsync(EscalationContext message, IWorkflowContext context)
    {
        await AddTraceAsync(context, "Assemble", "Building final escalation result.");
        var result = BuildResult(message.PolicyContext, message.Artifact.Payload);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyResult, result, scopeName: SupportWorkflowState.ScopeName);
        await context.YieldOutputAsync(result);
    }

    private static SupportRequestResult BuildResult(PolicyContext policyContext, string customerFacingResponse)
    {
        var reasoning = new List<string>(policyContext.Policy.ReasoningSteps)
        {
            $"Applied policies: {string.Join("; ", policyContext.Policy.AppliedPolicies)}"
        };

        return new SupportRequestResult(
            Intent: policyContext.Intake.PrimaryIntent,
            Sentiment: policyContext.Intake.Sentiment,
            Urgency: policyContext.Intake.Urgency,
            Reasoning: reasoning,
            ActionTaken: policyContext.Policy.ActionTaken,
            CustomerFacingResponse: customerFacingResponse.Trim(),
            RecommendedNextAction: policyContext.Policy.RecommendedNextAction);
    }

    private static ValueTask AddTraceAsync(IWorkflowContext context, string stage, string detail)
        => context.AddEventAsync(new WorkflowTraceEvent(
            new WorkflowTraceStep(stage, detail, WorkflowTraceStepKind.Detail)));
}
