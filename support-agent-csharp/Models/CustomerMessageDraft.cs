using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SupportAgent.Models;

[Description("A drafted customer-facing support message.")]
public sealed class CustomerMessageDraft
{
    [JsonPropertyName("mode")]
    [Description("The message mode: Reply, ClarificationEmail, OperationalAcknowledgement, or EscalationAcknowledgement.")]
    public MessageMode Mode { get; set; }

    [JsonPropertyName("subject_line")]
    [Description("Optional subject line for email-style responses.")]
    public string? SubjectLine { get; set; }

    [JsonPropertyName("body")]
    [Description("Customer-facing body text. Plain text only, no markdown.")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("tone_checks")]
    [Description("Short checklist of tone checks the draft satisfies.")]
    public List<string> ToneChecks { get; set; } = [];
}