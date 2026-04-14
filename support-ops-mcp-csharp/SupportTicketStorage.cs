using System.Globalization;
using System.Text;
using SupportOpsMcp.Models;

namespace SupportOpsMcp;

public sealed class SupportTicketStorage(IWebHostEnvironment environment)
{
    private int _sequence;

    public SupportTicketResult CreateResult(
        string kind,
        string queue,
        string customerEmail,
        string summary,
        string priority,
        string reason,
        string? recommendedNextAction)
    {
        var createdAtUtc = DateTimeOffset.UtcNow;
        var ticketId = CreateTicketId(createdAtUtc);

        var result = new SupportTicketResult(
            TicketId: ticketId,
            Status: "created",
            Kind: kind,
            Queue: queue,
            CustomerEmail: customerEmail,
            CreatedAtUtc: createdAtUtc,
            Summary: summary,
            Priority: priority,
            Reason: reason,
            RecommendedNextAction: recommendedNextAction);

        Save(result);
        return result;
    }

    private string CreateTicketId(DateTimeOffset createdAtUtc)
    {
        var sequence = Interlocked.Increment(ref _sequence) % 1000;
        return $"SUP-{createdAtUtc:yyyyMMdd-HHmmss-fff}-{sequence:D3}";
    }

    private void Save(SupportTicketResult result)
    {
        var storagePath = Path.Combine(environment.ContentRootPath, "storage");
        Directory.CreateDirectory(storagePath);

        var filePath = Path.Combine(storagePath, $"{result.TicketId}.md");
        File.WriteAllText(filePath, BuildMarkdown(result), Encoding.UTF8);
    }

    private static string BuildMarkdown(SupportTicketResult result)
    {
        return $$"""
        # {{result.TicketId}}

        | Field | Value |
        | --- | --- |
        | Status | {{Escape(result.Status)}} |
        | Kind | {{Escape(result.Kind)}} |
        | Queue | {{Escape(result.Queue)}} |
        | Customer email | {{Escape(result.CustomerEmail)}} |
        | Created UTC | {{Escape(result.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture))}} |
        | Priority | {{Escape(result.Priority)}} |

        ## Summary

        {{Escape(result.Summary)}}

        ## Reason

        {{Escape(result.Reason)}}

        ## Recommended Next Action

        {{Escape(result.RecommendedNextAction ?? "(none)")}}
        """;
    }

    private static string Escape(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("|", "\\|", StringComparison.Ordinal);
}
