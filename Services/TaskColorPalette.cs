namespace Seal.Services;

public static class TaskColorPalette
{
    private static readonly string[] Colors =
    [
        "#00E5FF", "#FF4081", "#76FF03", "#FFAB00", "#B388FF",
        "#FF6E40", "#18FFFF", "#FFEA00", "#E040FB", "#00E676",
        "#2979FF", "#FF1744", "#C6FF00", "#FF9100", "#651FFF",
        "#F50057", "#00BFA5", "#64DD17", "#D500F9", "#0091EA"
    ];

    public static string Next(IEnumerable<string> assignedColors)
    {
        var used = assignedColors.ToList();
        return Colors.OrderBy(color => used.Count(assigned => assigned == color)).First();
    }

    public static bool Contains(string color) => Colors.Contains(color);
}
