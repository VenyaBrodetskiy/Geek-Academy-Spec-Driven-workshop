using System.Text.Json;

namespace SupportAgent.Workflow.Executors;

internal static class StructuredOutputJson
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryDeserialize<T>(string payload, out T? value) where T : class
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(payload, JsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
