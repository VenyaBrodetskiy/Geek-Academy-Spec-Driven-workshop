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
        var ticketAction = new AgenticTicketActionExecutor("agentic_ticket_action", chatClient, supportOpsMcpToolProvider);
        var clarificationAction = new OperationalActionExecutor("clarification_action");
        var assemble = new AssembleSupportResultExecutor("assemble_result");

        return new WorkflowBuilder(normalize)
            .AddEdge(normalize, intake)
            .AddEdge(intake, policyGate)
            .AddEdge<PolicyContext>(policyGate, draft, ctx => ctx is not null && ctx.Policy.Route is PolicyRoute.Reply or PolicyRoute.Clarification)
            .AddEdge<PolicyContext>(policyGate, ticketAction, ctx => ctx is not null && ctx.Policy.Route is PolicyRoute.RefundOrCancellation or PolicyRoute.Escalation)
            .AddEdge<DraftedResponseContext>(draft, clarificationAction, ctx => ctx is not null && ctx.PolicyContext.Policy.Route == PolicyRoute.Clarification)
            .AddEdge<DraftedResponseContext>(draft, assemble, ctx => ctx is not null && ctx.PolicyContext.Policy.Route != PolicyRoute.Clarification)
            .AddEdge<OperationalActionContext>(ticketAction, draft, ctx => ctx is not null)
            .AddEdge<OperationalActionContext>(clarificationAction, assemble, ctx => ctx is not null && ctx.PolicyContext.Policy.Route == PolicyRoute.Clarification)
            .WithOutputFrom(assemble)
            .Build();
    }
}
