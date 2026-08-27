// ─────────────────────────────────────────────────────────────
// SQL Race Example: Event Definitions and Data
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Creating EventDefinition objects and the event data API
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own session)
// Output: Event definitions structure and API usage patterns
// Note: two things are easy to get wrong here. An EventDefinition's conversion
//       function names must be registered with AddConversion before Commit(), and
//       the session needs channel data before it will accept any event. Neither
//       failure raises anything - the events are simply discarded.
//       Events are also readable only within the recording session; they do not
//       survive a reload, whereas parameters do.
//
// Related: snippets/csharp/session-management/marker-management.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
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

// The fourth EventDefinition argument lists CONVERSION FUNCTION names, not field
// labels, and each must be registered on the same configuration. Commit() drops
// any definition whose conversions are missing - it logs an error but does not
// throw, so the failure surfaces later as "event definition not found" from
// AddEventData.
foreach (var conversionName in new[] { "Value", "Threshold", "SensorId", "FaultCode", "Parameter", "Direction" })
{
    config.AddConversion(RationalConversion.CreateSimple1To1Conversion(conversionName, "", "%5.2f"));
}

// Events are stored in time segments, so the session needs a time base before it
// can accept any. A session with no channel data has StartTime 0, and AddEventData
// then discards the event without raising anything.
uint signalChannelId = 1;
config.AddConversion(RationalConversion.CreateSimple1To1Conversion("signal_conv", "", "%5.2f"));
config.AddChannel(new Channel(signalChannelId, "Signal", new Frequency(100, FrequencyUnit.Hz).ToInterval(),
    DataType.Double64Bit, ChannelDataSourceType.Periodic, "Signal:Diagnostics", false));
config.AddParameterGroup(new ParameterGroup("Diag", "Diag"));
config.AddGroup(new ApplicationGroup("Diagnostics", "Diagnostics", new List<string> { "Diag" }) { SupportsRda = false });
config.AddParameter(new Parameter(
    "Signal:Diagnostics", "Signal", "Signal being monitored",
    100, 0, 100, 0, 0.0, 0u, 0u, "signal_conv",
    new List<string> { "Diag" }, signalChannelId, "Diagnostics"));

config.AddEventDefinition(new EventDefinition(1, "LimitExceedance", EventPriorityType.High, new List<string> { "Value", "Threshold" }, "Diagnostics"));
config.AddEventDefinition(new EventDefinition(2, "SensorFault", EventPriorityType.Medium, new List<string> { "SensorId", "FaultCode" }, "Diagnostics"));
config.AddEventDefinition(new EventDefinition(3, "ThresholdCrossing", EventPriorityType.Low, new List<string> { "Parameter", "Direction" }, "Diagnostics"));
config.Commit();

// --- Give the session a time base ---
for (var i = 0; i < 250; i++)
{
    session.AddChannelData(signalChannelId, baseTime + i * 100_000_000L, 1, BitConverter.GetBytes(20.0 + (i * 0.1)));
}

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

// Events come back in segment order, not time order.
foreach (var evt in events.OrderBy(e => e.TimeStamp))
{
    var dataStr = string.Join(", ", evt.RawData);
    Console.WriteLine($"{evt.TimeStamp,20} {evt.EventDefinitionKey,-10} {evt.GroupName,-15} [{dataStr}]");
}
