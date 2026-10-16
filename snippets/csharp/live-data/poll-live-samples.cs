// ─────────────────────────────────────────────────────────────
// SQL Race Example: Poll Live Samples
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Polling a live session's latest samples with async delay and cancellation
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8, a live session recorded with the
//                Server Listener enabled (see docs/server-listener.md)
// Input: A live session key, the connection string of the database it is recording to,
//        and a parameter identifier
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
var sessionSummary = sessionManager.FindSummaryBy(sessionKey, connectionString);

// --- Live data only comes from the recorder's Server Listener ---
// For a Live session GetConnectionString() returns the listener's "SqlRaceServer:..." string.
// A LiveNotInServer session has no listener: its database copy trails the leading edge by
// the recorder's flush delay (5 minutes by default), so it never gives live data.
var liveConnectionString = sessionSummary?.State == SessionState.Live ? sessionSummary.GetConnectionString() : null;
if (liveConnectionString is null || !liveConnectionString.StartsWith("SqlRaceServer", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine($"ERROR: Session is {sessionSummary?.State.ToString() ?? "not found"}, not Live with a Server Listener.");
    Console.WriteLine("Enable the Server Listener on the recorder (docs/server-listener.md) and record a new session.");
    return;
}

using var clientSession = sessionManager.Load(sessionSummary.Key, liveConnectionString);
var session = clientSession.Session;

if (!session.Parameters.Any(p => p.Identifier == paramId))
{
    Console.WriteLine($"ERROR: Parameter '{paramId}' not found in session.");
    Console.WriteLine("Available (first 20):");
    foreach (var p in session.Parameters.Take(20))
        Console.WriteLine($"  {p.Identifier}");
    return;
}

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
Console.WriteLine($"Polling {paramId} every 2s (30s timeout)...\n");

using var pda = session.CreateParameterDataAccess(paramId);
var pollCount = 0;
var lastEndTime = long.MinValue;
while (!cts.Token.IsCancellationRequested && session.State == SessionState.Live)
{
    try { await Task.Delay(2000, cts.Token); }
    catch (OperationCanceledException) { break; }

    // --- EndTime is kept up to date by the Server Listener while the session is live ---
    var endTime = session.EndTime;
    if (lastEndTime >= endTime)
    {
        Console.WriteLine($"  Poll {++pollCount}: no new data");
        continue;
    }

    lastEndTime = endTime;
    pda.GoTo(endTime);
    var samples = pda.GetNextSamples(5, StepDirection.Reverse);

    if (samples.SampleCount > 0)
    {
        // Reverse reads return newest first.
        var latest = samples.Data[0];
        var latestTime = samples.Timestamp[0];
        var behindMs = (endTime - latestTime) / 1_000_000.0;
        Console.WriteLine($"  Poll {++pollCount}: latest={latest:F2} at {latestTime} ns, {behindMs:F0} ms behind session end");
    }
    else
    {
        Console.WriteLine($"  Poll {++pollCount}: no data yet");
    }
}

Console.WriteLine($"\nPolling complete — {pollCount} polls.");
