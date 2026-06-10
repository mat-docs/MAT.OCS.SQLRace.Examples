// ─────────────────────────────────────────────────────────────
// SQL Race Example: Subscribe to Data Events
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Subscribing to session.EventDataAdded for real-time event notification
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8, a live data source
// Input: A live session key and connection string
// Output: Event details as they arrive in real time
//
// Related: snippets/csharp/live-data/subscribe-to-lap-events.cs
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
    Console.WriteLine("NOTE: This example requires a live session with events being added.");
    return;
}

var sessionKey = SessionKey.Parse(args[0]);
var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.Load(sessionKey, connectionString);
var session = clientSession.Session;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

var eventCount = 0;
EventHandler<ItemEventArgs> handler = (sender, e) =>
{
    Interlocked.Increment(ref eventCount);
    Console.WriteLine($"  Event data added: {e.Item} (session {e.SessionKey})");
};

session.EventDataAdded += handler;
Console.WriteLine("Listening for data events (30s timeout)...");

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine($"\nTimeout reached. Received {eventCount} event(s).");
}
finally
{
    session.EventDataAdded -= handler;
    Console.WriteLine("Unsubscribed from data events.");
}
