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

    [JsonPropertyName("customer_facts")]
    [Description("Customer/account facts retrieved from SupportOps MCP, or lookup status when no profile was found.")]
    public CustomerFacts CustomerFacts { get; set; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CustomerLookupStatus
{
    NotAttempted,
    Found,
    NotFound,
    Unavailable
}

[Description("Structured customer/account facts available to support policy rules.")]
public sealed class CustomerFacts
{
    [JsonPropertyName("lookup_status")]
    [Description("Status of SupportOps lookup. Must be one of: NotAttempted, Found, NotFound, Unavailable.")]
    public CustomerLookupStatus LookupStatus { get; set; } = CustomerLookupStatus.NotAttempted;

    [JsonPropertyName("email")]
    [Description("Customer email from SupportOps, when found.")]
    public string? Email { get; set; }

    [JsonPropertyName("plan")]
    [Description("Customer subscription plan from SupportOps, when found.")]
    public string? Plan { get; set; }

    [JsonPropertyName("signup_date")]
    [Description("Customer signup date from SupportOps as yyyy-MM-dd, when found.")]
    public string? SignupDate { get; set; }

    [JsonPropertyName("last_charge_amount")]
    [Description("Most recent charge amount from SupportOps, when found.")]
    public decimal? LastChargeAmount { get; set; }

    [JsonPropertyName("last_charge_date")]
    [Description("Most recent charge date from SupportOps as yyyy-MM-dd, when found.")]
    public string? LastChargeDate { get; set; }

    [JsonPropertyName("refunds_last_12_months")]
    [Description("Number of refunds in the last 12 months from SupportOps, when found.")]
    public int? RefundsLast12Months { get; set; }

    [JsonPropertyName("contacts_last_30_days")]
    [Description("Number of support contacts in the last 30 days from SupportOps, when found.")]
    public int? ContactsLast30Days { get; set; }

    [JsonPropertyName("account_status")]
    [Description("Customer account status from SupportOps, when found.")]
    public string? AccountStatus { get; set; }
}
