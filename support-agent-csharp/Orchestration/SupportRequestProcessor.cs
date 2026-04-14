using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using SupportAgent.Agents;
using SupportAgent.Integrations;
using SupportAgent.Models;
using SupportAgent.Workflow;
using SupportAgent.Workflow.Events;
using SupportAgent.Workflow.Executors;

namespace SupportAgent.Orchestration;

public class SupportRequestProcessor
{
    private readonly IChatClient _chatClient;
    private readonly SupportPolicyRules _policyRules;
    private readonly SupportOpsMcpToolProvider _supportOpsMcpToolProvider;

    public SupportRequestProcessor(IConfiguration config)
    {
        _chatClient = AgentFactory.CreateChatClient(config);
        _policyRules = new SupportPolicyRules(SupportPolicyRules.LoadHandbook(AppContext.BaseDirectory));
        _supportOpsMcpToolProvider = new SupportOpsMcpToolProvider(config);
    }

    public async Task<SupportProcessingOutcome> ProcessAsync(string customerMessage)
    {
        var workflow = BuildWorkflow(_chatClient, _policyRules, _supportOpsMcpToolProvider);

        var run = await InProcessExecution.RunAsync(workflow, customerMessage);

        SupportRequestResult? result = null;
        SimulatedArtifact? artifact = null;

        foreach (var workflowEvent in run.NewEvents)
        {
            switch (workflowEvent)
            {
                case ArtifactPreparedEvent artifactPrepared:
                    artifact = artifactPrepared.Artifact;
                    break;

                case EscalationPreparedEvent escalationPrepared:
                    artifact = escalationPrepared.Context.Artifact;
                    break;

                case WorkflowErrorEvent errorEvent:
                    throw new InvalidOperationException($"Workflow execution failed: {errorEvent}");

                case WorkflowOutputEvent outputEvent when outputEvent.Is<SupportRequestResult>():
                    result = outputEvent.As<SupportRequestResult>();
                    break;
            }
        }

        if (result is null)
        {
            throw new InvalidOperationException("Workflow completed without producing a SupportRequestResult.");
        }

        return new SupportProcessingOutcome(result, artifact);
    }

    private static Microsoft.Agents.AI.Workflows.Workflow BuildWorkflow(
        IChatClient chatClient,
        SupportPolicyRules policyRules,
        SupportOpsMcpToolProvider supportOpsMcpToolProvider)
    {
        var normalize = new NormalizeRequestExecutor("normalize_request", policyRules);
        var intake = new IntakeClassifierExecutor("intake_classifier", chatClient, supportOpsMcpToolProvider);
        var policyGate = new PolicyGateExecutor("policy_gate");
        var draft = new DraftCustomerMessageExecutor("draft_customer_message", chatClient, policyRules.HandbookText);
        var action = new OperationalActionExecutor("operational_action");
        var escalation = new PrepareEscalationArtifactExecutor("prepare_escalation");
        var assemble = new AssembleSupportResultExecutor("assemble_result");

        return new WorkflowBuilder(normalize)
            .AddEdge(normalize, intake)
            .AddEdge(intake, policyGate)
            .AddEdge<PolicyContext>(policyGate, draft, ctx => ctx is not null && ctx.Policy.Route is PolicyRoute.Reply or PolicyRoute.Clarification)
            .AddEdge<PolicyContext>(policyGate, action, ctx => ctx is not null && ctx.Policy.Route == PolicyRoute.RefundOrCancellation)
            .AddEdge<PolicyContext>(policyGate, escalation, ctx => ctx is not null && ctx.Policy.Route == PolicyRoute.Escalation)
            .AddEdge<DraftedResponseContext>(draft, action, ctx => ctx is not null && ctx.PolicyContext.Policy.Route == PolicyRoute.Clarification)
            .AddEdge<DraftedResponseContext>(draft, assemble, ctx => ctx is not null && ctx.PolicyContext.Policy.Route != PolicyRoute.Clarification)
            .AddEdge<OperationalActionContext>(action, draft, ctx => ctx is not null && ctx.PolicyContext.Policy.Route == PolicyRoute.RefundOrCancellation)
            .AddEdge<OperationalActionContext>(action, assemble, ctx => ctx is not null && ctx.PolicyContext.Policy.Route == PolicyRoute.Clarification)
            .AddEdge(escalation, draft)
            .WithOutputFrom(assemble)
            .Build();
    }
}
