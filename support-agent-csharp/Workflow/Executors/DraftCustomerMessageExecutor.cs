using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using SupportAgent.Models;
using SupportAgent.Workflow.Events;
using SupportAgent.Workflow.State;

namespace SupportAgent.Workflow.Executors;

internal sealed class DraftCustomerMessageExecutor : Executor
{
    private readonly AIAgent _agent;
    private AgentSession? _session;
    private readonly string _handbookText;

    public DraftCustomerMessageExecutor(string id, IChatClient chatClient, string handbookText) : base(id)
    {
        _handbookText = handbookText;

        ChatClientAgentOptions agentOptions = new()
        {
            ChatOptions = new()
            {
                Instructions =
                    """
                    You draft customer support messages.
                    Return JSON that matches the schema exactly.
                    Keep the tone short, human, and calm.
                    Do not invent company rules, features, or exceptions.
                    Do not use markdown.
                    """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<CustomerMessageDraft>()
            }
        };

        _agent = new ChatClientAgent(chatClient, agentOptions);
    }

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder.ConfigureRoutes(routeBuilder =>
            routeBuilder
                .AddHandler<PolicyContext, DraftedResponseContext>(HandlePolicyAsync)
                .AddHandler<OperationalActionContext, DraftedResponseContext>(HandleOperationalAsync));
    }

    private async ValueTask<DraftedResponseContext> HandlePolicyAsync(PolicyContext message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _session ??= await _agent.CreateSessionAsync(cancellationToken);
        await AddTraceAsync(context, "Draft", $"Drafting {message.Policy.MessageMode} response.", cancellationToken);

        var prompt =
            $"""
            Draft a customer support message.

            Mode: {message.Policy.MessageMode}
            Intent: {message.Intake.PrimaryIntent}
            Sentiment: {message.Intake.Sentiment}
            Urgency: {message.Intake.Urgency}
            Tone guidance: {BuildToneGuidance(message.Intake.Sentiment)}
            Summary: {message.Intake.Summary}
            Missing information: {FormatList(message.Intake.MissingInformation)}
            Applied handbook facts: {FormatList(message.Policy.AppliedPolicies)}
            Recommended next action: {message.Policy.RecommendedNextAction ?? "(none)"}

            Relevant handbook context:
            {_handbookText}

            Customer request:
            {message.Request.Body}

            Draft requirements:
            - Acknowledge the customer briefly before the answer.
            - Follow the tone guidance exactly and match the empathy to the customer's sentiment.
            - If Mode is ClarificationEmail, ask only for the missing details needed to continue.
            - If Mode is Reply, answer directly and clearly.
            - Keep the message concise.
            """;

        var response = await _agent.RunAsync(prompt, _session, cancellationToken: cancellationToken);
        await AddTraceAsync(context, "Draft", "Customer message agent returned a response.", cancellationToken);
        var draft = await DeserializeDraftAsync(response.Text, cancellationToken);

        draft.Mode = message.Policy.MessageMode;
        draft.Body = draft.Body?.Trim() ?? string.Empty;

        await context.AddEventAsync(new ResponseDraftedEvent(draft), cancellationToken);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyDraft, draft, scopeName: SupportWorkflowState.ScopeName);

        return new DraftedResponseContext
        {
            PolicyContext = message,
            Draft = draft,
            Artifact = null
        };
    }

    private async ValueTask<DraftedResponseContext> HandleOperationalAsync(OperationalActionContext message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _session ??= await _agent.CreateSessionAsync(cancellationToken);
        await AddTraceAsync(
            context,
            "Draft",
            $"Drafting {message.PolicyContext.Policy.MessageMode} response using artifact {message.Artifact.ArtifactType}.",
            cancellationToken);

        var prompt =
            $"""
            Draft an acknowledgement for a support operation.

            Mode: {message.PolicyContext.Policy.MessageMode}
            Intent: {message.PolicyContext.Intake.PrimaryIntent}
            Sentiment: {message.PolicyContext.Intake.Sentiment}
            Tone guidance: {BuildToneGuidance(message.PolicyContext.Intake.Sentiment)}
            Summary: {message.PolicyContext.Intake.Summary}
            Applied handbook facts: {FormatList(message.PolicyContext.Policy.AppliedPolicies)}
            Recommended next action: {message.PolicyContext.Policy.RecommendedNextAction ?? "(none)"}

            SupportOps artifact:
            {message.Artifact.Payload}

            Customer request:
            {message.PolicyContext.Request.Body}

            Draft requirements:
            - Acknowledge the customer's request.
            - Follow the tone guidance exactly and show appropriate empathy without becoming long or dramatic.
            - Confirm the next step without promising unsupported exceptions.
            - Keep it short and human.
            """;

        var response = await _agent.RunAsync(prompt, _session, cancellationToken: cancellationToken);
        await AddTraceAsync(context, "Draft", "Customer message agent returned a response.", cancellationToken);
        var draft = await DeserializeDraftAsync(response.Text, cancellationToken);

        draft.Mode = message.PolicyContext.Policy.MessageMode;
        draft.Body = draft.Body?.Trim() ?? string.Empty;

        await context.AddEventAsync(new ResponseDraftedEvent(draft), cancellationToken);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyDraft, draft, scopeName: SupportWorkflowState.ScopeName);

        return new DraftedResponseContext
        {
            PolicyContext = message.PolicyContext,
            Draft = draft,
            Artifact = message.Artifact
        };
    }

    private static string FormatList(IEnumerable<string> items)
    {
        var values = items.Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
        return values.Count == 0 ? "(none)" : string.Join("; ", values);
    }

    private static string BuildToneGuidance(Sentiment sentiment)
    {
        return sentiment switch
        {
            Sentiment.Frustrated => "Acknowledge the frustration explicitly with one short empathetic sentence before moving to the next step.",
            Sentiment.Angry => "Start with a short apology and a calm acknowledgement of the frustration, then move straight to the next step without sounding defensive.",
            Sentiment.Confused => "Sound reassuring and clear, acknowledging the confusion before explaining the next step.",
            _ => "Keep the acknowledgement warm, brief, and professional."
        };
    }

    private async Task<CustomerMessageDraft> DeserializeDraftAsync(string responseText, CancellationToken cancellationToken)
    {
        if (StructuredOutputJson.TryDeserialize<CustomerMessageDraft>(responseText, out var draft))
        {
            return draft!;
        }

        _session ??= await _agent.CreateSessionAsync(cancellationToken);

        var repairPrompt =
            $$"""
            Your previous reply could not be parsed as valid JSON for the CustomerMessageDraft schema.
            Return only valid JSON that matches the schema exactly.

            Previous reply:
            {{responseText}}
            """;

        var repaired = await _agent.RunAsync(repairPrompt, _session, cancellationToken: cancellationToken);
        if (StructuredOutputJson.TryDeserialize<CustomerMessageDraft>(repaired.Text, out draft))
        {
            return draft!;
        }

        throw new InvalidOperationException("Failed to deserialize CustomerMessageDraft after JSON repair retry.");
    }

    private static ValueTask AddTraceAsync(
        IWorkflowContext context,
        string stage,
        string detail,
        CancellationToken cancellationToken)
        => context.AddEventAsync(new WorkflowTraceEvent(
            new WorkflowTraceStep(stage, detail, WorkflowTraceStepKind.Detail)), cancellationToken);
}
