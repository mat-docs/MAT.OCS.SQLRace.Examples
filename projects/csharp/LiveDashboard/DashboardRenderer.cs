namespace LiveDashboard;

/// <summary>
/// Renders a text-based dashboard to the console with in-place updates.
/// </summary>
public sealed class DashboardRenderer
{
    private readonly int _pollingIntervalMs;
    private bool _firstRender = true;

    /// <summary>
    /// Represents the current statistics for a single parameter.
    /// </summary>
    public record ParameterStats(string Identifier, double Latest, double Min, double Max, double Mean, int SampleCount);

    public DashboardRenderer(int pollingIntervalMs)
    {
        _pollingIntervalMs = pollingIntervalMs;
    }

    /// <summary>
    /// Renders the dashboard frame to the console.
    /// </summary>
    public void Render(string sessionName, string sessionState, IReadOnlyList<ParameterStats> stats)
    {
        if (_firstRender)
        {
            Console.Clear();
            _firstRender = false;
        }
        else
        {
            Console.SetCursorPosition(0, 0);
        }

        var width = Math.Max(Console.WindowWidth, 70);
        var innerWidth = width - 4; // Account for border characters

        // Top border
        Console.WriteLine($"╔{new string('═', innerWidth)}╗");

        // Title
        var title = "SQL Race Live Dashboard";
        var polling = $"[Polling: {_pollingIntervalMs}ms]";
        var titleLine = $"  {title}{new string(' ', Math.Max(1, innerWidth - title.Length - polling.Length - 4))}{polling}  ";
        Console.WriteLine($"║{Fit(titleLine, innerWidth)}║");

        // Session info
        var sessionLine = $"  Session: {sessionName,-30} [{sessionState}]";
        Console.WriteLine($"║{Fit(sessionLine, innerWidth)}║");

        // Separator
        Console.WriteLine($"╠{new string('═', innerWidth)}╣");

        // Header
        var header = $"  {"Parameter",-28} {"Latest",10} {"Min",10} {"Max",10} {"Mean",10}  ";
        Console.WriteLine($"║{Fit(header, innerWidth)}║");
        Console.WriteLine($"║  {new string('─', innerWidth - 4)}  ║");

        // Parameter rows
        foreach (var s in stats)
        {
            var row = $"  {s.Identifier,-28} {s.Latest,10:F2} {s.Min,10:F2} {s.Max,10:F2} {s.Mean,10:F2}  ";
            Console.WriteLine($"║{Fit(row, innerWidth)}║");
        }

        // Fill if few parameters
        for (var i = stats.Count; i < 3; i++)
            Console.WriteLine($"║{new string(' ', innerWidth)}║");

        // Footer
        Console.WriteLine($"╠{new string('═', innerWidth)}╣");
        var footer = $"  Last updated: {DateTime.Now:HH:mm:ss.fff}{new string(' ', Math.Max(1, innerWidth - 50))}Press Ctrl+C to exit  ";
        Console.WriteLine($"║{Fit(footer, innerWidth)}║");
        Console.WriteLine($"╚{new string('═', innerWidth)}╝");
    }

    private static string Fit(string text, int width)
    {
        if (text.Length >= width)
            return text[..width];
        return text + new string(' ', width - text.Length);
    }
}
