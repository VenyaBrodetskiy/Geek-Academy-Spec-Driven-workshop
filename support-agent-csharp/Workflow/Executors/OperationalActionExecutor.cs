using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;
using SupportAgent.Workflow.Events;
using SupportAgent.Workflow.State;

namespace SupportAgent.Workflow.Executors;

internal sealed class OperationalActionExecutor(string id) : Executor(id)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder.ConfigureRoutes(routeBuilder =>
            routeBuilder
                .AddHandler<PolicyContext, OperationalActionContext>(HandlePolicyAsync)
                .AddHandler<DraftedResponseContext, OperationalActionContext>(HandleDraftAsync));
    }

    private static async ValueTask<OperationalActionContext> HandlePolicyAsync(PolicyContext message, IWorkflowContext context)
    {
        var artifact = message.Policy.ActionTaken switch
        {
            ActionTaken.RefundTicketCreated => BuildRefundArtifact(message),
            ActionTaken.CancellationTicketCreated => BuildCancellationArtifact(message),
            _ => throw new InvalidOperationException($"Unsupported action for operational handling: {message.Policy.ActionTaken}.")
        };

        await context.AddEventAsync(new ArtifactPreparedEvent(artifact));
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyArtifact, artifact, scopeName: SupportWorkflowState.ScopeName);

        return new OperationalActionContext
        {
            PolicyContext = message,
            Artifact = artifact,
            OperatorNote = message.Policy.RecommendedNextAction
        };
    }

    private static async ValueTask<OperationalActionContext> HandleDraftAsync(DraftedResponseContext message, IWorkflowContext context)
    {
        var artifact = new SimulatedArtifact
        {
            ArtifactType = ArtifactType.ClarificationEmail,
            DisplayTitle = "Clarification Email Preview",
            Payload = $"Subject: {message.Draft.SubjectLine ?? "More details needed for your support request"}\n\n{message.Draft.Body}",
            Metadata = new Dictionary<string, string>
            {
                ["route"] = "clarification",
                ["recipient"] = message.PolicyContext.Request.Sender ?? "unknown"
            }
        };

        await context.AddEventAsync(new ArtifactPreparedEvent(artifact));
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyArtifact, artifact, scopeName: SupportWorkflowState.ScopeName);

        return new OperationalActionContext
        {
            PolicyContext = message.PolicyContext,
            Artifact = artifact,
            OperatorNote = message.PolicyContext.Policy.RecommendedNextAction
        };
    }

    private static SimulatedArtifact BuildRefundArtifact(PolicyContext message)
    {
        return new SimulatedArtifact
        {
            ArtifactType = ArtifactType.RefundTicket,
            DisplayTitle = "Refund Review Ticket Preview",
            Payload =
                $"""
                Create refund review ticket.

                Customer: {message.Request.Sender ?? "unknown"}
                Summary: {message.Intake.Summary}
                Handbook basis: {string.Join("; ", message.Policy.AppliedPolicies)}
                Recommended next action: {message.Policy.RecommendedNextAction}
                """,
            Metadata = new Dictionary<string, string>
            {
                ["route"] = "refund_or_cancellation",
                ["action"] = nameof(ActionTaken.RefundTicketCreated)
            }
        };
    }

    private static SimulatedArtifact BuildCancellationArtifact(PolicyContext message)
    {
        return new SimulatedArtifact
        {
            ArtifactType = ArtifactType.CancellationTicket,
            DisplayTitle = "Cancellation Task Preview",
            Payload =
                $"""
                Create cancellation task.

                Customer: {message.Request.Sender ?? "unknown"}
                Summary: {message.Intake.Summary}
                Handbook basis: {string.Join("; ", message.Policy.AppliedPolicies)}
                Recommended next action: {message.Policy.RecommendedNextAction}
                """,
            Metadata = new Dictionary<string, string>
            {
                ["route"] = "refund_or_cancellation",
                ["action"] = nameof(ActionTaken.CancellationTicketCreated)
            }
        };
    }
}