using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SupportOpsMcp;

internal static class SupportOpsJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        WriteIndented = true
    };
}
