namespace SupportOpsMcp.Models;

public sealed record SupportTicketResult(
    string TicketId,
    string Status,
    string Kind,
    string Queue,
    string CustomerEmail,
    DateTimeOffset CreatedAtUtc,
    string Summary,
    string Priority,
    string Reason,
    string? RecommendedNextAction
);
