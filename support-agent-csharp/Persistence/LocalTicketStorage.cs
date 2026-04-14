using System.Globalization;
using System.Text;
using SupportAgent.Models;

namespace SupportAgent.Persistence;

internal static class LocalTicketStorage
{
    private static int _sequence;

    public static void PersistIfTicketArtifact(SimulatedArtifact artifact)
    {
        if (artifact.ArtifactType == ArtifactType.ClarificationEmail)
        {
            return;
        }

        var createdAtUtc = DateTimeOffset.UtcNow;
        var ticketId = CreateTicketId(createdAtUtc);
        var storagePath = ResolveStoragePath();
        Directory.CreateDirectory(storagePath);

        var filePath = Path.Combine(storagePath, $"{ticketId}.md");
        artifact.Metadata["ticket_id"] = ticketId;
        artifact.Metadata["storage_path"] = filePath;
        artifact.Metadata["created_utc"] = createdAtUtc.ToString("O", CultureInfo.InvariantCulture);

        File.WriteAllText(filePath, BuildMarkdown(artifact, ticketId, createdAtUtc), Encoding.UTF8);
    }

    private static string CreateTicketId(DateTimeOffset createdAtUtc)
    {
        var sequence = Interlocked.Increment(ref _sequence) % 1000;
        return $"SUP-{createdAtUtc:yyyyMMdd-HHmmss-fff}-{sequence:D3}";
    }

    private static string ResolveStoragePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "storage"));

    private static string BuildMarkdown(SimulatedArtifact artifact, string ticketId, DateTimeOffset createdAtUtc)
    {
        var metadata = artifact.Metadata
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => $"| {Escape(item.Key)} | {Escape(item.Value)} |");

        return $$"""
        # {{ticketId}}

        | Field | Value |
        | --- | --- |
        | Ticket id | {{Escape(ticketId)}} |
        | Artifact type | {{Escape(artifact.ArtifactType.ToString())}} |
        | Title | {{Escape(artifact.DisplayTitle)}} |
        | Created UTC | {{Escape(createdAtUtc.ToString("O", CultureInfo.InvariantCulture))}} |

        ## Metadata

        | Key | Value |
        | --- | --- |
        {{string.Join(Environment.NewLine, metadata)}}

        ## Payload

        {{artifact.Payload.Trim()}}
        """;
    }

    private static string Escape(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal);
}
