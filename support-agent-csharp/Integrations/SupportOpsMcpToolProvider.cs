using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

namespace SupportAgent.Integrations;

internal sealed class SupportOpsMcpToolProvider
{
    private const string LookupCustomerToolName = "lookup_customer";
    private readonly bool _enabled;
    private readonly Uri _endpoint;
    private readonly Lazy<Task<IReadOnlyList<AITool>>> _tools;

    public SupportOpsMcpToolProvider(IConfiguration config)
    {
        _enabled = !bool.TryParse(config["SupportOpsMcp:Enabled"], out var enabled) || enabled;
        _endpoint = BuildEndpoint(config["SupportOpsMcp:Endpoint"]);
        _tools = new Lazy<Task<IReadOnlyList<AITool>>>(LoadToolsAsync);
    }

    public Task<IReadOnlyList<AITool>> GetIntakeToolsAsync() => _tools.Value;

    private async Task<IReadOnlyList<AITool>> LoadToolsAsync()
    {
        if (!_enabled)
        {
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

            return tools
                .Where(tool => string.Equals(tool.Name, LookupCustomerToolName, StringComparison.Ordinal))
                .Cast<AITool>()
                .ToList();
        }
        catch
        {
            return [];
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
