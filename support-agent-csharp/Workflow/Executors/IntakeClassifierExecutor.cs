using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using SupportAgent.Integrations;
using SupportAgent.Models;
using SupportAgent.Workflow.Events;
using SupportAgent.Workflow.State;

namespace SupportAgent.Workflow.Executors;

internal sealed class IntakeClassifierExecutor : Executor<ParsedSupportRequest, IntakeContext>
{
    private readonly IChatClient _chatClient;
    private readonly SupportOpsMcpToolProvider _supportOpsMcpToolProvider;
    private AIAgent? _agent;
    private AgentSession? _session;

    public IntakeClassifierExecutor(
        string id,
        IChatClient chatClient,
        SupportOpsMcpToolProvider supportOpsMcpToolProvider) : base(id)
    {
        _chatClient = chatClient;
        _supportOpsMcpToolProvider = supportOpsMcpToolProvider;
    }

    public override async ValueTask<IntakeContext> HandleAsync(ParsedSupportRequest message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var agent = await GetAgentAsync(cancellationToken);
        _session ??= await agent.CreateSessionAsync(cancellationToken);

        var prompt =
            $"""
            Classify this customer support request.

            Subject: {message.Subject ?? "(none)"}
            From: {message.Sender ?? "(unknown)"}
            Detected signals: {string.Join(", ", message.DetectedSignals.DefaultIfEmpty("(none)"))}

            Request:
            {message.Body}
            """;

        var response = await agent.RunAsync(prompt, _session, cancellationToken: cancellationToken);
        var intake = await DeserializeIntakeAsync(response.Text, cancellationToken);

        intake.MissingInformation ??= [];
        intake.EscalationSignals ??= [];
        intake.Summary ??= string.Empty;
        intake.ConfidenceNotes ??= string.Empty;
        intake.CustomerFacts ??= new CustomerFacts();
        NormalizeCustomerFacts(intake.CustomerFacts);

        var intakeContext = new IntakeContext
        {
            Request = message,
            Intake = intake
        };

        await context.AddEventAsync(new IntakeCompletedEvent(intakeContext), cancellationToken);
        await context.QueueStateUpdateAsync(SupportWorkflowState.KeyIntake, intakeContext, scopeName: SupportWorkflowState.ScopeName);

        return intakeContext;
    }

    private async Task<AIAgent> GetAgentAsync(CancellationToken cancellationToken)
    {
        if (_agent is not null)
        {
            return _agent;
        }

        var tools = await _supportOpsMcpToolProvider.GetIntakeToolsAsync();
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
                    Use only these lookup_status values in customer_facts: NotAttempted, Found, NotFound, Unavailable.

                    If the sender email is available and account or billing facts may help decide what information is missing, use the available customer lookup tool.
                    Copy verified lookup results into customer_facts. Do not invent customer facts.
                    If lookup is unavailable or does not find the customer, continue from the message and reflect that in customer_facts.lookup_status.

                    Be conservative. If details are still missing for safe billing or refund handling, populate missing_information.
                    If you are unsure whether the customer is asking for a refund or only asking a question, prefer Question or Unclear and explain the uncertainty in confidence_notes.
                    """,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<IntakeAssessment>(),
                Tools = [.. tools]
            }
        };

        _agent = new ChatClientAgent(_chatClient, agentOptions);
        return _agent;
    }

    private async Task<IntakeAssessment> DeserializeIntakeAsync(string responseText, CancellationToken cancellationToken)
    {
        if (StructuredOutputJson.TryDeserialize<IntakeAssessment>(responseText, out var intake))
        {
            return intake!;
        }

        var agent = await GetAgentAsync(cancellationToken);
        _session ??= await agent.CreateSessionAsync(cancellationToken);

        var repairPrompt =
            $$"""
            Your previous reply could not be parsed as valid JSON for the IntakeAssessment schema.
            Return only valid JSON that matches the schema exactly.

            Previous reply:
            {{responseText}}
            """;

        var repaired = await agent.RunAsync(repairPrompt, _session, cancellationToken: cancellationToken);
        if (StructuredOutputJson.TryDeserialize<IntakeAssessment>(repaired.Text, out intake))
        {
            return intake!;
        }

        throw new InvalidOperationException("Failed to deserialize IntakeAssessment after JSON repair retry.");
    }

    private static void NormalizeCustomerFacts(CustomerFacts customerFacts)
    {
        customerFacts.Email = NormalizeString(customerFacts.Email);
        customerFacts.Plan = NormalizeString(customerFacts.Plan);
        customerFacts.SignupDate = NormalizeString(customerFacts.SignupDate);
        customerFacts.LastChargeDate = NormalizeString(customerFacts.LastChargeDate);
        customerFacts.AccountStatus = NormalizeString(customerFacts.AccountStatus);
    }

    private static string? NormalizeString(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
