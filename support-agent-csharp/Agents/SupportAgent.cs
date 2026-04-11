using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace SupportAgent.Agents;

public static class AgentFactory
{
    public static ChatClientAgent Create(IConfiguration config, string customersJson, string policiesJson)
    {
        var endpoint = RequireConfig(config, "Endpoint");
        var apiKey = RequireConfig(config, "ApiKey");
        var modelName = RequireConfig(config, "ModelName");

        var chatClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey))
            .GetChatClient(modelName);

#pragma warning disable OPENAI001
        return chatClient.AsAIAgent(
            name: "CustomerSupportAgent",
            instructions: $"""
                You are a helpful customer support agent.
                Use the customer and policy data below to resolve issues accurately.
                If the data does not contain enough information, ask a concise follow-up question.

                Customers:
                {customersJson}

                Policies:
                {policiesJson}
                """);
#pragma warning restore OPENAI001
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
