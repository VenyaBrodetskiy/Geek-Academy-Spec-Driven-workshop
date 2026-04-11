using Common;
using Microsoft.Extensions.Configuration;
using SupportAgent.Orchestration;

try
{
    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
        .Build();

    var processor = new SupportRequestProcessor(config);

    ConsoleUi.WriteSectionTitle("Customer Support — Request Processor", ConsoleColor.Cyan);
    ConsoleUi.WriteColoredLine(
        "Paste a customer message, then end it with a line containing only '---'.",
        ConsoleColor.DarkGray);
    ConsoleUi.WriteColoredLine("Type 'quit' on its own line to exit.", ConsoleColor.DarkGray);

    while (true)
    {
        ConsoleUi.WriteSectionTitle("Paste customer message (end with '---')", ConsoleColor.Cyan);

        var message = ReadMultilineInput();
        if (message is null)
        {
            break;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            continue;
        }

        var result = await processor.ProcessAsync(message);
        SupportRequestRenderer.Render(result);

        ConsoleUi.WriteColoredLine(
            "\n[Placeholder run — see SupportRequestProcessor.ProcessAsync to replace with your real flow]",
            ConsoleColor.DarkYellow);
    }
}
catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}

static string? ReadMultilineInput()
{
    var lines = new List<string>();
    while (true)
    {
        var line = Console.ReadLine();
        if (line is null)
        {
            return null;
        }

        var trimmed = line.Trim();

        if (lines.Count == 0 && trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (trimmed == "---")
        {
            break;
        }

        lines.Add(line);
    }

    return string.Join("\n", lines).Trim();
}
