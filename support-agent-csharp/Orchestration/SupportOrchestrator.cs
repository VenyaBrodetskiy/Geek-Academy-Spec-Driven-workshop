using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using SupportAgent.Agents;

namespace SupportAgent.Orchestration;

public class SupportOrchestrator
{
    private readonly ChatClientAgent _agent;
    private readonly AgentSession _session;

    public SupportOrchestrator(IConfiguration config)
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        var customersJson = File.ReadAllText(Path.Combine(dataDir, "customers.json"));
        var policiesJson = File.ReadAllText(Path.Combine(dataDir, "policies.json"));

        _agent = AgentFactory.Create(config, customersJson, policiesJson);
        _session = _agent.CreateSessionAsync().GetAwaiter().GetResult();
    }

    public async Task<string> HandleAsync(string userMessage)
    {
        var response = new StringBuilder();

        await foreach (var text in HandleStreamingAsync(userMessage))
        {
            response.Append(text);
        }

        return response.ToString().Trim();
    }

    public async IAsyncEnumerable<string> HandleStreamingAsync(string userMessage)
    {
        await foreach (var chunk in _agent.RunStreamingAsync(userMessage, _session))
        {
            if (!string.IsNullOrEmpty(chunk.Text))
            {
                yield return chunk.Text;
            }
        }
    }
}
