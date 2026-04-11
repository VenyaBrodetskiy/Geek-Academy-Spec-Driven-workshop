using SupportAgent.Models;

namespace Common;

public static class SupportRequestRenderer
{
    public static void Render(SupportRequestResult result)
    {
        ConsoleUi.WriteSectionTitle("Classification", ConsoleColor.Cyan);
        WriteField("Intent", result.Intent.ToString());
        WriteField("Sentiment", result.Sentiment.ToString());
        WriteField("Urgency", result.Urgency.ToString());

        ConsoleUi.WriteSectionTitle("Reasoning", ConsoleColor.Cyan);
        foreach (var step in result.Reasoning)
        {
            ConsoleUi.WriteColoredLine($"  - {step}", ConsoleColor.Gray);
        }

        ConsoleUi.WriteSectionTitle("Action", ConsoleColor.Cyan);
        WriteField("Taken", result.ActionTaken.ToString());
        if (!string.IsNullOrWhiteSpace(result.RecommendedNextAction))
        {
            WriteField("Next", result.RecommendedNextAction);
        }

        ConsoleUi.WriteSectionTitle("Customer-Facing Response", ConsoleColor.Green);
        ConsoleUi.WriteColoredLine(result.CustomerFacingResponse, ConsoleColor.Yellow);
    }

    private static void WriteField(string label, string value)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"  {label,-10} ");
        Console.ResetColor();
        Console.WriteLine(value);
    }
}
