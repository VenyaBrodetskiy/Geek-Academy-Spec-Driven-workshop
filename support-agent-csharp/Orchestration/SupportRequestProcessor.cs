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

    public async Task<SupportProcessingOutcome> ProcessAsync(
        string customerMessage,
        Action<WorkflowTraceStep>? onTraceStep = null)
    {
        var workflow = BuildWorkflow(_chatClient, _policyRules, _supportOpsMcpToolProvider);

        await using var run = await InProcessExecution.OffThread.RunStreamingAsync(workflow, customerMessage);

        SupportRequestResult? result = null;
        SimulatedArtifact? artifact = null;
        var trace = new List<WorkflowTraceStep>();

        await foreach (var workflowEvent in run.WatchStreamAsync())
        {
            switch (workflowEvent)
            {
                case IntakeCompletedEvent intakeCompleted:
                    AddTrace(BuildIntakeTrace(intakeCompleted.Context));
                    break;

                case PolicyAppliedEvent policyApplied:
                    AddTrace(BuildPolicyTrace(policyApplied.Context));
                    break;

                case ArtifactPreparedEvent artifactPrepared:
                    artifact = artifactPrepared.Artifact;
                    AddTrace(BuildArtifactTrace(artifactPrepared.Artifact));
                    break;

                case ResponseDraftedEvent responseDrafted:
                    AddTrace(BuildDraftTrace(responseDrafted.Draft));
                    break;

                case WorkflowErrorEvent errorEvent:
                    throw new InvalidOperationException($"Workflow execution failed: {errorEvent}");

                case WorkflowOutputEvent outputEvent when outputEvent.Is<SupportRequestResult>():
                    result = outputEvent.As<SupportRequestResult>();
                    AddTrace(new WorkflowTraceStep("Complete", "SupportRequestResult produced."));
                    break;
            }
        }

        if (result is null)
        {
            throw new InvalidOperationException("Workflow completed without producing a SupportRequestResult.");
        }

        return new SupportProcessingOutcome(result, artifact, trace);

        void AddTrace(WorkflowTraceStep step)
        {
            trace.Add(step);
            onTraceStep?.Invoke(step);
        }
    }

    private static WorkflowTraceStep BuildIntakeTrace(IntakeContext context)
    {
        var facts = context.Intake.CustomerFacts;
        return new WorkflowTraceStep(
            "Intake",
            $"Intent={context.Intake.PrimaryIntent}; Urgency={context.Intake.Urgency}; Lookup={facts.LookupStatus}; Summary={context.Intake.Summary}");
    }

    private static WorkflowTraceStep BuildPolicyTrace(PolicyContext context)
    {
        return new WorkflowTraceStep(
            "Policy",
            $"Route={context.Policy.Route}; Action={context.Policy.ActionTaken}; Next={context.Policy.RecommendedNextAction ?? "(none)"}");
    }

    private static WorkflowTraceStep BuildArtifactTrace(SimulatedArtifact artifact)
    {
        artifact.Metadata.TryGetValue("ticket_id", out var ticketId);
        var suffix = string.IsNullOrWhiteSpace(ticketId) ? string.Empty : $"; Ticket={ticketId}";

        return new WorkflowTraceStep(
            "Artifact",
            $"Type={artifact.ArtifactType}; Title={artifact.DisplayTitle}{suffix}");
    }

    private static WorkflowTraceStep BuildDraftTrace(CustomerMessageDraft draft)
    {
        return new WorkflowTraceStep(
            "Draft",
            $"Mode={draft.Mode}; Subject={draft.SubjectLine ?? "(none)"}");
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
