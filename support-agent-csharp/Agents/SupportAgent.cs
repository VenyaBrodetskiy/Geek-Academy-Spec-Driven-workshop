using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;

namespace SupportAgent.Agents;

public static class AgentFactory
{
    public static IChatClient CreateChatClient(IConfiguration config)
    {
        var endpoint = RequireConfig(config, "Endpoint");
        var apiKey = RequireConfig(config, "ApiKey");
        var modelName = RequireConfig(config, "ModelName");

        return new AzureOpenAIClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey))
            .GetChatClient(modelName)
            .AsIChatClient();
    }

    private static string RequireConfig(IConfiguration config, string key)
    {
        var value = config[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Missing configuration value '{key}'. Add it to appsettings.Development.json.");
        }

        return value;
    }
}
