using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SupportAgent.Models;

[Description("Structured result of an agentic SupportOps ticket action.")]
public sealed class TicketActionDecision
{
    [JsonPropertyName("ticket_created")]
    [Description("True only after create_support_ticket has successfully created a ticket.")]
    public bool TicketCreated { get; set; }

    [JsonPropertyName("ticket_id")]
    [Description("Created SupportOps ticket id, for example SUP-1001.")]
    public string? TicketId { get; set; }

    [JsonPropertyName("ticket_kind")]
    [Description("Ticket kind. Must be one of: refund_review, cancellation, escalation.")]
    public string? TicketKind { get; set; }

    [JsonPropertyName("queue")]
    [Description("SupportOps queue returned by the ticket creation tool.")]
    public string? Queue { get; set; }

    [JsonPropertyName("summary")]
    [Description("Short internal ticket summary.")]
    public string? Summary { get; set; }

    [JsonPropertyName("reason")]
    [Description("Why this ticket was created.")]
    public string? Reason { get; set; }

    [JsonPropertyName("recommended_next_action")]
    [Description("Recommended next action for the operator.")]
    public string? RecommendedNextAction { get; set; }
}
