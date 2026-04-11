using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using SupportAgent.Agents;
using SupportAgent.Models;

namespace SupportAgent.Orchestration;

public class SupportRequestProcessor
{
    private readonly ChatClientAgent _placeholderAgent;

    public SupportRequestProcessor(IConfiguration config)
    {
        _placeholderAgent = AgentFactory.CreatePlaceholderAgent(config);
    }

    // TODO (lab 1): Replace this method's body with your real support flow.
    //
    // Right now this method:
    //   1. Calls a trivial placeholder agent so you can see a live LLM response
    //      and verify your Foundry/Azure OpenAI connection.
    //   2. Returns a hardcoded SupportRequestResult with mocked classification,
    //      reasoning, and action values.
    //
    // Your job: replace this with a real flow (likely a MAF workflow with several
    // executors) that actually classifies the request, consults the support
    // handbook, decides what to do, and populates every field of
    // SupportRequestResult from real logic instead of the mock values below.
    public async Task<SupportRequestResult> ProcessAsync(string customerMessage)
    {
        var session = await _placeholderAgent.CreateSessionAsync();
        var response = new StringBuilder();

        await foreach (var chunk in _placeholderAgent.RunStreamingAsync(customerMessage, session))
        {
            if (!string.IsNullOrEmpty(chunk.Text))
            {
                response.Append(chunk.Text);
            }
        }

        return new SupportRequestResult(
            Intent: Intent.Unclear,
            Sentiment: Sentiment.Neutral,
            Urgency: Urgency.Low,
            Reasoning: new[]
            {
                "(mock) This reasoning is hardcoded in SupportRequestProcessor.ProcessAsync.",
                "(mock) Replace it with real reasoning steps from your workflow.",
                "(mock) Every field of SupportRequestResult should come from real logic."
            },
            ActionTaken: ActionTaken.ReplySent,
            CustomerFacingResponse: response.ToString().Trim(),
            RecommendedNextAction: "(mock) Replace SupportRequestProcessor.ProcessAsync with your real flow.");
    }
}
