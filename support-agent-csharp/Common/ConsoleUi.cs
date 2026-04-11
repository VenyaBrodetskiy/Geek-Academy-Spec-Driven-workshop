namespace Common;

public static class ConsoleUi
{
    public static void WriteSectionTitle(string title, ConsoleColor color = ConsoleColor.Cyan)
    {
        Console.ForegroundColor = color;
        Console.WriteLine($"\n=== {title} ===");
        Console.ResetColor();
    }

    public static void WriteColoredLine(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}
