// ─────────────────────────────────────────────────────────────
// SQL Race Example: Event Definitions and Data
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Creating EventDefinition objects and the event data API
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own session)
// Output: Event definitions structure and API usage patterns
// TODO: Verify — EventDefinition persistence via ConfigurationSet.Commit() may require
//       a full config (with at least one channel/parameter) to work with SQLite sessions.
//
// Related: snippets/csharp/session-management/marker-management.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Enumerators;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";
var sessionKey = SessionKey.NewKey();

var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.CreateSession(connectionString, sessionKey, "Events Demo", DateTime.UtcNow, "Example");
var session = clientSession.Session;

var baseTime = 36_000_000_000_000L;

// --- Define event types (via configuration) ---
var config = session.CreateConfiguration();
config.AddEventDefinition(new EventDefinition(1, "LimitExceedance", EventPriorityType.High, new List<string> { "Value", "Threshold" }, "Diagnostics"));
config.AddEventDefinition(new EventDefinition(2, "SensorFault", EventPriorityType.Medium, new List<string> { "SensorId", "FaultCode" }, "Diagnostics"));
config.AddEventDefinition(new EventDefinition(3, "ThresholdCrossing", EventPriorityType.Low, new List<string> { "Parameter", "Direction" }, "Diagnostics"));
config.Commit();

// --- Add event instances ---
session.Events.AddEventData(1, "Diagnostics", baseTime + 5_000_000_000L, new List<double> { 127.3, 120.0 });
session.Events.AddEventData(2, "Diagnostics", baseTime + 10_000_000_000L, new List<double> { 3.0, 1.0 });
session.Events.AddEventData(3, "Diagnostics", baseTime + 15_000_000_000L, new List<double> { 0.0, 1.0 });
session.Events.AddEventData(1, "Diagnostics", baseTime + 20_000_000_000L, new List<double> { 135.1, 120.0 });

// --- Retrieve events in a time range ---
var events = session.Events.GetEventData(baseTime, baseTime + 25_000_000_000L);

Console.WriteLine($"Session {sessionKey}: event definitions and data\n");
Console.WriteLine($"{"Timestamp (ns)",20} {"DefKey",-10} {"Group",-15} {"Data"}");
Console.WriteLine(new string('─', 80));

foreach (var evt in events)
{
    var dataStr = string.Join(", ", evt.RawData);
    Console.WriteLine($"{evt.TimeStamp,20} {evt.EventDefinitionKey,-10} {evt.GroupName,-15} [{dataStr}]");
}
