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
                .AddHandler<DraftedResponseContext, OperationalActionContext>(HandleDraftAsync));
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
}
