using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using SupportAgent.Models;
using SupportAgent.Workflow.Events;
using SupportAgent.Workflow.State;

namespace SupportAgent.Workflow.Executors;

internal sealed class IntakeClassifierExecutor : Executor<ParsedSupportRequest, IntakeContext>
{
    private readonly AIAgent _agent;
    private AgentSession? _session;

    public IntakeClassifierExecutor(string id, IChatClient chatClient) : base(id)
    {
        ChatClientAgentOptions agentOptions = new()
        {
            ChatOptions = new()
            {
                Instructions =
                    """
                    You are a support intake classifier.
                    Return JSON that matches the schema exactly.
                    Use only these intent values: Unclear, Refund, Cancellation, Question, Complaint.
                    Use only these sentiment values: Neutral, Frustrated, Angry, Confused.
                    Use only these urgency values: Low, Medium, High.
                    Be conservative. If details are missing for safe billing or refund handling, populate missing_information.
                    If you are unsure whether the customer is asking for a refund or only asking a question, prefer Question or Unclear and explain the uncertainty in confidence_notes.
                    """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<IntakeAssessment>()
            }
        };

        _agent = new ChatClientAgent(chatClient, agentOptions);
    }

    public override async ValueTask<IntakeContext> HandleAsync(ParsedSupportRequest message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        _session ??= await _agent.CreateSessionAsync(cancellationToken);

        var prompt =
            $"""
            Classify this customer support request.

            Subject: {message.Subject ?? "(none)"}
            From: {message.Sender ?? "(unknown)"}
            Detected signals: {string.Join(", ", message.DetectedSignals.DefaultIfEmpty("(none)"))}

            Request:
            {message.Body}
            """;

        var response = await _agent.RunAsync(prompt, _session, cancellationToken: cancellationToken);
        var intake = await DeserializeIntakeAsync(response.Text, cancellationToken);

        intake.MissingInformation ??= [];
        intake.EscalationSignals ??= [];
        intake.Summary ??= string.Empty;
        intake.ConfidenceNotes ??= string.Empty;

        var intakeContext = new IntakeContext
        {
            Request = message,
            Intake = intake
        };

        await context.AddEventAsync(new IntakeCompletedEvent(intakeContext), cancellationToken);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyIntake, intakeContext, scopeName: SupportWorkflowState.ScopeName);

        return intakeContext;
    }

    private async Task<IntakeAssessment> DeserializeIntakeAsync(string responseText, CancellationToken cancellationToken)
    {
        if (StructuredOutputJson.TryDeserialize<IntakeAssessment>(responseText, out var intake))
        {
            return intake!;
        }

        _session ??= await _agent.CreateSessionAsync(cancellationToken);

        var repairPrompt =
            $$"""
            Your previous reply could not be parsed as valid JSON for the IntakeAssessment schema.
            Return only valid JSON that matches the schema exactly.

            Previous reply:
            {{responseText}}
            """;

        var repaired = await _agent.RunAsync(repairPrompt, _session, cancellationToken: cancellationToken);
        if (StructuredOutputJson.TryDeserialize<IntakeAssessment>(repaired.Text, out intake))
        {
            return intake!;
        }

        throw new InvalidOperationException("Failed to deserialize IntakeAssessment after JSON repair retry.");
    }
}