using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace SupportAgent.Agents;

// TODO (lab 1): This file is a placeholder to show the minimal pattern for creating
// and calling a MAF agent. Your real solution will almost certainly replace it with
// multiple specialized agents (classifier, responder, escalation-prep, etc.) inside
// a MAF workflow. See lab1.md.
public static class AgentFactory
{
    public static ChatClientAgent CreatePlaceholderAgent(IConfiguration config)
    {
        var endpoint = RequireConfig(config, "Endpoint");
        var apiKey = RequireConfig(config, "ApiKey");
        var modelName = RequireConfig(config, "ModelName");

        var chatClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey))
            .GetChatClient(modelName);

        return chatClient.AsAIAgent(
            name: "PlaceholderSupportAgent",
            instructions: """
                You are a friendly customer support assistant.
                Respond briefly and politely to acknowledge the customer's message.
                Do not try to solve the issue, do not promise anything, and do not ask
                for additional information. Keep the reply to two or three sentences.
                """);
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
