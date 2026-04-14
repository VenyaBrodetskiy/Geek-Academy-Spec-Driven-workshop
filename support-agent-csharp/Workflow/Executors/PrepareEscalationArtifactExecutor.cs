using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;
using SupportAgent.Persistence;
using SupportAgent.Workflow.Events;
using SupportAgent.Workflow.State;

namespace SupportAgent.Workflow.Executors;

internal sealed class PrepareEscalationArtifactExecutor(string id) : Executor<PolicyContext, EscalationContext>(id)
{
    public override async ValueTask<EscalationContext> HandleAsync(PolicyContext message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        await context.AddEventAsync(new WorkflowTraceEvent(
            new WorkflowTraceStep(
                "Artifact",
                "Preparing escalation handoff artifact.",
                WorkflowTraceStepKind.Detail)), cancellationToken);

        var queue = SupportPolicyRules.BuildEscalationQueue(message.Request, message.Intake);
        var sla = SupportPolicyRules.BuildEscalationSla(message.Intake.Urgency);
        var nextSteps = SupportPolicyRules.BuildEscalationNextSteps(message.Request, message.Intake);
        var summary = SupportPolicyRules.BuildEscalationSummary(message);

        var artifact = new SimulatedArtifact
        {
            ArtifactType = ArtifactType.EscalationHandoff,
            DisplayTitle = "Escalation Handoff Preview",
            Payload =
                $"""
                Tag: ESCALATE
                Queue: {queue}
                SLA: {sla}
                Summary: {summary}

                Next steps:
                - {string.Join("\n- ", nextSteps)}
                """,
            Metadata = new Dictionary<string, string>
            {
                ["queue"] = queue,
                ["sla"] = sla
            }
        };

        LocalTicketStorage.PersistIfTicketArtifact(artifact);
        await context.AddEventAsync(new WorkflowTraceEvent(
            new WorkflowTraceStep(
                "Storage",
                $"Persisted ticket artifact {artifact.Metadata["ticket_id"]}.",
                WorkflowTraceStepKind.Detail)), cancellationToken);

        var escalationContext = new EscalationContext
        {
            PolicyContext = message,
            Artifact = artifact,
            Queue = queue,
            Sla = sla,
            NextSteps = nextSteps
        };

        await context.AddEventAsync(new EscalationPreparedEvent(escalationContext), cancellationToken);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyArtifact, artifact, scopeName: SupportWorkflowState.ScopeName);

        return escalationContext;
    }
}
