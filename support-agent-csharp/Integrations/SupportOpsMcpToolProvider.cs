using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

namespace SupportAgent.Integrations;

internal sealed class SupportOpsMcpToolProvider
{
    private const string LookupCustomerToolName = "lookup_customer";
    private const string CreateSupportTicketToolName = "create_support_ticket";
    private readonly bool _enabled;
    private readonly Uri _endpoint;
    private readonly Lazy<Task<IReadOnlyList<AITool>>> _intakeTools;
    private readonly Lazy<Task<IReadOnlyList<AITool>>> _ticketActionTools;

    public SupportOpsMcpToolProvider(IConfiguration config)
    {
        _enabled = !bool.TryParse(config["SupportOpsMcp:Enabled"], out var enabled) || enabled;
        _endpoint = BuildEndpoint(config["SupportOpsMcp:Endpoint"]);
        _intakeTools = new Lazy<Task<IReadOnlyList<AITool>>>(() => LoadFilteredToolAsync(LookupCustomerToolName, strict: false));
        _ticketActionTools = new Lazy<Task<IReadOnlyList<AITool>>>(() => LoadFilteredToolAsync(CreateSupportTicketToolName, strict: true));
    }

    public Task<IReadOnlyList<AITool>> GetIntakeToolsAsync() => _intakeTools.Value;

    public Task<IReadOnlyList<AITool>> GetTicketActionToolsAsync() => _ticketActionTools.Value;

    private async Task<IReadOnlyList<AITool>> LoadFilteredToolAsync(string toolName, bool strict)
    {
        if (!_enabled)
        {
            if (strict)
            {
                throw new InvalidOperationException("SupportOps MCP is disabled, but ticket creation requires create_support_ticket.");
            }

            return [];
        }

        try
        {
            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = "SupportOps",
                Endpoint = _endpoint,
                TransportMode = HttpTransportMode.StreamableHttp,
                ConnectionTimeout = TimeSpan.FromSeconds(2)
            });

            var client = await McpClient.CreateAsync(transport);
            var tools = await client.ListToolsAsync();
            var matchingTools = tools
                .Where(tool => string.Equals(tool.Name, toolName, StringComparison.Ordinal))
                .Cast<AITool>()
                .ToList();

            if (strict && matchingTools.Count == 0)
            {
                throw new InvalidOperationException($"SupportOps MCP did not expose required tool '{toolName}'.");
            }

            return matchingTools;
        }
        catch when (!strict)
        {
            return [];
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"SupportOps MCP ticket tool error: {ex.Message}");
            throw new InvalidOperationException(
                $"SupportOps MCP is required for ticket creation, but '{toolName}' could not be loaded from {_endpoint}.",
                ex);
        }
    }

    private static Uri BuildEndpoint(string? configuredEndpoint)
    {
        var endpoint = string.IsNullOrWhiteSpace(configuredEndpoint)
            ? "http://localhost:5058/mcp"
            : configuredEndpoint.Trim();

        return new Uri(endpoint, UriKind.Absolute);
    }
}
