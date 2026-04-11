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

    var orchestrator = new SupportOrchestrator(config);

    ConsoleUi.WriteSectionTitle("Customer Support Agent", ConsoleColor.Cyan);
    ConsoleUi.WriteColoredLine("Type 'quit' to exit.\n", ConsoleColor.DarkGray);

    while (true)
    {
        ConsoleUi.WriteUserPrompt();
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        ConsoleUi.WriteAgentPrompt();
        await foreach (var chunk in orchestrator.HandleStreamingAsync(input))
        {
            ConsoleUi.WriteAgentChunk(chunk);
        }
        Console.WriteLine("\n");
    }
}
catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
