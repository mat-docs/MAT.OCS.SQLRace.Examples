// ─────────────────────────────────────────────────────────────
// SQL Race Example: RDA Parameter Change Detection
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Detecting when new parameters appear in a live session via
//               session.RdaParametersChanged, then creating a PDA once available
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8, a live session
// Input: A live session key
// Output: Notification when a target parameter becomes available
//
// Related: snippets/csharp/live-data/subscribe-to-lap-events.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.Enumerators;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = args.Length > 1 ? args[1]
    : $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <session-guid> [connection-string]");
    Console.WriteLine("NOTE: This example requires a live session where parameters appear dynamically.");
    return;
}

var sessionKey = SessionKey.Parse(args[0]);
var targetParam = args.Length > 2 ? args[2] : "Temperature:Sensors";

var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.Load(sessionKey, connectionString);
var session = clientSession.Session;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
var paramFound = new TaskCompletionSource<bool>();

// --- Check if parameter already exists ---
if (session.Parameters.Any(p => p.Identifier == targetParam))
{
    Console.WriteLine($"Parameter '{targetParam}' already available.");
    paramFound.TrySetResult(true);
}

// --- Subscribe to parameter changes ---
EventHandler<SessionEventArgs> handler = (sender, e) =>
{
    Console.WriteLine($"  RDA parameters changed — {e.EventType}: {e.Message}");

    if (session.Parameters.Any(p => p.Identifier == targetParam))
    {
        Console.WriteLine($"  Target parameter '{targetParam}' is now available!");
        paramFound.TrySetResult(true);
    }
};

session.RdaParametersChanged += handler;
Console.WriteLine($"Waiting for '{targetParam}' to appear (60s timeout)...");

try
{
    var completed = await Task.WhenAny(paramFound.Task, Task.Delay(Timeout.Infinite, cts.Token));

    if (paramFound.Task.IsCompleted)
    {
        using var pda = session.CreateParameterDataAccess(targetParam);
        pda.GoTo(long.MaxValue);
        var samples = pda.GetNextSamples(10, StepDirection.Reverse);
        Console.WriteLine($"\nCreated PDA — {samples.SampleCount} samples retrieved, latest={samples.Data[0]:F2}.");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine($"\nTimeout: '{targetParam}' did not appear within 60 seconds.");
}
finally
{
    session.RdaParametersChanged -= handler;
}
