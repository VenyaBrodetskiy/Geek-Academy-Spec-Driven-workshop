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

    Console.WriteLine("Customer Support Agent - type 'quit' to exit\n");

    while (true)
    {
        Console.Write("You: ");
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

        var response = await orchestrator.HandleAsync(input);
        Console.WriteLine($"Agent: {response}\n");
    }
}
catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
