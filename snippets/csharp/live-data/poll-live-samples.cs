// ─────────────────────────────────────────────────────────────
// SQL Race Example: Poll Live Samples
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Polling a live session's latest samples with async delay and cancellation
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8, a live session
// Input: A live session key and parameter identifier
// Output: Latest sample values printed at each poll interval
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
    Console.WriteLine("Usage: dotnet run -- <session-guid> [connection-string] [parameter-id]");
    Console.WriteLine("NOTE: This example requires a live session with data actively arriving.");
    return;
}

var sessionKey = SessionKey.Parse(args[0]);
var paramId = args.Length > 2 ? args[2] : "Temperature:Sensors";

var sessionManager = SessionManager.CreateSessionManager();

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

Console.WriteLine($"Polling {paramId} every 2s (30s timeout)...\n");

// --- Verify the parameter exists before polling ---
{
    using var initClient = sessionManager.Load(sessionKey, connectionString);
    if (!initClient.Session.Parameters.Any(p => p.Identifier == paramId))
    {
        Console.WriteLine($"ERROR: Parameter '{paramId}' not found in session.");
        Console.WriteLine("Available (first 20):");
        foreach (var p in initClient.Session.Parameters.Take(20))
            Console.WriteLine($"  {p.Identifier}");
        return;
    }
}

var pollCount = 0;
while (!cts.Token.IsCancellationRequested)
{
    // --- Reload session each cycle to pick up newly flushed data ---
    using var clientSession = sessionManager.Load(sessionKey, connectionString);
    var session = clientSession.Session;

    using var pda = session.CreateParameterDataAccess(paramId);
    pda.GoTo(long.MaxValue);
    var samples = pda.GetNextSamples(5, StepDirection.Reverse);

    if (samples.SampleCount > 0)
    {
        var latest = samples.Data[0];
        var latestTime = samples.Timestamp[0];
        Console.WriteLine($"  Poll {++pollCount}: latest={latest:F2} at {latestTime} ns ({samples.SampleCount} recent)");
    }
    else
    {
        Console.WriteLine($"  Poll {++pollCount}: no data yet");
    }

    try { await Task.Delay(2000, cts.Token); }
    catch (OperationCanceledException) { break; }
}

Console.WriteLine($"\nPolling complete — {pollCount} polls.");
