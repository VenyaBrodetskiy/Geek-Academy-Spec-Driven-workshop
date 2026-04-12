namespace SupportAgent.Models;

public sealed class ParsedSupportRequest
{
    public string RawText { get; init; } = string.Empty;

    public string NormalizedText { get; init; } = string.Empty;

    public string? Sender { get; init; }

    public string? Subject { get; init; }

    public string Body { get; init; } = string.Empty;

    public List<string> DetectedSignals { get; init; } = [];
}