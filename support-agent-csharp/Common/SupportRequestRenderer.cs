using SupportAgent.Models;

namespace Common;

public static class SupportRequestRenderer
{
    public static void Render(SupportProcessingOutcome outcome, bool includeTrace = true)
    {
        var result = outcome.Result;

        if (includeTrace && outcome.Trace.Count > 0)
        {
            RenderTraceHeader();
            foreach (var step in outcome.Trace)
            {
                RenderTraceStep(step);
            }
        }

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

        if (outcome.Artifact is not null)
        {
            ConsoleUi.WriteSectionTitle(outcome.Artifact.DisplayTitle, ConsoleColor.Magenta);
            ConsoleUi.WriteColoredLine(outcome.Artifact.Payload, ConsoleColor.Gray);
        }
    }

    public static void RenderTraceHeader()
        => ConsoleUi.WriteSectionTitle("Agent Trace", ConsoleColor.DarkGray);

    public static void RenderTraceStep(WorkflowTraceStep step)
    {
        var (prefix, color) = step.Kind switch
        {
            WorkflowTraceStepKind.Detail => ("   .", ConsoleColor.DarkGray),
            WorkflowTraceStepKind.Error => ("  !", ConsoleColor.DarkGray),
            _ => ("  -", ConsoleColor.DarkGray)
        };

        ConsoleUi.WriteColoredLine($"{prefix} {step.Stage}: {step.Detail}", color);
    }

    private static void WriteField(string label, string value)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"  {label,-10} ");
        Console.ResetColor();
        Console.WriteLine(value);
    }
}
