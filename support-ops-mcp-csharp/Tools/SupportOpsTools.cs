using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SupportOpsMcp.Models;

namespace SupportOpsMcp.Tools;

[McpServerToolType]
public static class SupportOpsTools
{
    private static readonly HashSet<string> SupportedTicketKinds =
    [
        "refund_review",
        "cancellation",
        "escalation"
    ];

    [McpServerTool(
        Name = "lookup_customer",
        Title = "Lookup customer",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Looks up a customer/account profile by email from local mock SupportOps data.")]
    public static CustomerLookupResult LookupCustomer(
        SupportOpsDataStore dataStore,
        [Description("Customer email address to look up.")] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return CustomerLookupResult.NotFound(string.Empty);
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var customer = dataStore.FindCustomer(normalizedEmail);

        return customer is null
            ? CustomerLookupResult.NotFound(normalizedEmail)
            : CustomerLookupResult.FromProfile(customer);
    }

    [McpServerTool(
        Name = "create_support_ticket",
        Title = "Create support ticket",
        Destructive = true,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(SupportTicketResult))]
    [Description("Creates a mock SupportOps ticket for refund review, cancellation, or escalation handling.")]
    public static CallToolResult CreateSupportTicket(
        SupportTicketStorage ticketStorage,
        [Description("Ticket kind. Supported values: refund_review, cancellation, escalation.")] string ticket_kind,
        [Description("Customer email address for the ticket.")] string customer_email,
        [Description("Short issue summary for support operators.")] string summary,
        [Description("Why this ticket is being created.")] string reason,
        [Description("Recommended internal next action, if any.")] string? recommended_next_action = null,
        [Description("Ticket priority, for example normal, high, or urgent.")] string priority = "normal")
    {
        if (!TryNormalizeRequired(ticket_kind, nameof(ticket_kind), out var kind, out var error))
        {
            return ToolError(error);
        }

        if (!TryNormalizeRequired(customer_email, nameof(customer_email), out var email, out error))
        {
            return ToolError(error);
        }

        if (!TryNormalizeRequired(summary, nameof(summary), out var safeSummary, out error))
        {
            return ToolError(error);
        }

        if (!TryNormalizeRequired(reason, nameof(reason), out var safeReason, out error))
        {
            return ToolError(error);
        }

        var safePriority = string.IsNullOrWhiteSpace(priority) ? "normal" : priority.Trim().ToLowerInvariant();

        if (!SupportedTicketKinds.Contains(kind))
        {
            return ToolError("Invalid ticket_kind. Supported values: refund_review, cancellation, escalation.");
        }

        var result = ticketStorage.CreateResult(
            kind,
            ResolveQueue(kind),
            email,
            safeSummary,
            safePriority,
            safeReason,
            string.IsNullOrWhiteSpace(recommended_next_action) ? null : recommended_next_action.Trim());

        return ToolSuccess(result);
    }

    private static bool TryNormalizeRequired(string? value, string parameterName, out string normalized, out string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            normalized = string.Empty;
            error = $"{parameterName} is required.";
            return false;
        }

        normalized = value.Trim();
        error = string.Empty;
        return true;
    }

    private static string ResolveQueue(string ticketKind)
    {
        return ticketKind switch
        {
            "refund_review" => "Billing Support",
            "cancellation" => "Account Support",
            "escalation" => "Senior Support",
            _ => "Support"
        };
    }

    private static CallToolResult ToolSuccess(SupportTicketResult result)
    {
        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(result, SupportOpsJson.Options)
                }
            ],
            StructuredContent = JsonSerializer.SerializeToElement(result, SupportOpsJson.Options),
            IsError = false
        };
    }

    private static CallToolResult ToolError(string message)
    {
        var payload = new
        {
            error = message,
            supported_ticket_kinds = SupportedTicketKinds.Order().ToArray()
        };

        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = message
                }
            ],
            StructuredContent = JsonSerializer.SerializeToElement(payload, SupportOpsJson.Options),
            IsError = true
        };
    }
}
