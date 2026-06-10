// ─────────────────────────────────────────────────────────────
// SQL Race Example: Subscribe to Lap Events
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Subscribing to session.LapStarted with async/await and cancellation
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8, a live data source
// Input: A live session key and connection string
// Output: Lap start notifications as they arrive
//
// Related: snippets/csharp/live-data/subscribe-to-data-events.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Collections;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = args.Length > 1 ? args[1]
    : $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <session-guid> [connection-string]");
    Console.WriteLine("NOTE: This example requires a live session with laps being added.");
    return;
}

var sessionKey = SessionKey.Parse(args[0]);
var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.Load(sessionKey, connectionString);
var session = clientSession.Session;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

// --- Subscribe to lap events with async discipline ---
var lapCount = 0;
EventHandler<LapEventArgs> handler = (sender, e) =>
{
    Interlocked.Increment(ref lapCount);
    Console.WriteLine($"  Lap started: #{e.Lap.Number} \"{e.Lap.Name}\" at {e.Lap.StartTime} ns");
};

session.LapStarted += handler;
Console.WriteLine("Listening for lap events (30s timeout)...");

try
{
    // NOTE: In a live session, laps are added by the data source.
    // This loop waits for events or cancellation.
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine($"\nTimeout reached. Received {lapCount} lap event(s).");
}
finally
{
    session.LapStarted -= handler;
    Console.WriteLine("Unsubscribed from lap events.");
}
