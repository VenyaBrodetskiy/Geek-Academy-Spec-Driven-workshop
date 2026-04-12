using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SupportAgent.Models;

[Description("Structured intake classification for a customer support request.")]
public sealed class IntakeAssessment
{
    [JsonPropertyName("primary_intent")]
    [Description("Primary intent. Must be one of: Unclear, Refund, Cancellation, Question, Complaint.")]
    public Intent PrimaryIntent { get; set; }

    [JsonPropertyName("sentiment")]
    [Description("Customer sentiment. Must be one of: Neutral, Frustrated, Angry, Confused.")]
    public Sentiment Sentiment { get; set; }

    [JsonPropertyName("urgency")]
    [Description("Urgency. Must be one of: Low, Medium, High.")]
    public Urgency Urgency { get; set; }

    [JsonPropertyName("missing_information")]
    [Description("Specific missing details required before handling the case safely. Empty list when none are needed.")]
    public List<string> MissingInformation { get; set; } = [];

    [JsonPropertyName("escalation_signals")]
    [Description("Concrete escalation cues found in the message, such as manager request, chargeback threat, legal language, or repeated unresolved contact.")]
    public List<string> EscalationSignals { get; set; } = [];

    [JsonPropertyName("summary")]
    [Description("One short summary of the customer issue.")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("confidence_notes")]
    [Description("Short note explaining uncertainty, ambiguity, or confidence level.")]
    public string ConfidenceNotes { get; set; } = string.Empty;
}