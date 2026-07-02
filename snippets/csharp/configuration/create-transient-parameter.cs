// ─────────────────────────────────────────────────────────────
// SQL Race Example: Create Transient Parameter
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Creating a transient parameter that exists only in memory
//               and is not persisted to the database
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own session)
// Output: Transient parameter created, data written/read, then verified gone after reload
//
// Related: snippets/csharp/configuration/create-parameter-config.cs
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
using var clientSession = sessionManager.CreateSession(connectionString, sessionKey, "Transient Demo", DateTime.UtcNow, "Example");
var session = clientSession.Session;

// --- Create a TRANSIENT configuration (not persisted) ---
var config = session.CreateTransientConfiguration();

uint channelId = 200;
var interval = new Frequency(10, FrequencyUnit.Hz).ToInterval();

config.AddConversion(RationalConversion.CreateSimple1To1Conversion("temp_conv", "degC", "%5.1f"));
config.AddChannel(new Channel(channelId, "TempCalc", interval, DataType.Double64Bit, ChannelDataSourceType.Periodic, "CalculatedTemp:Derived", false));
config.AddParameterGroup(new ParameterGroup("Calculated", "Calculated"));
config.AddGroup(new ApplicationGroup("Derived", "Derived", new List<string> { "Calculated" }) { SupportsRda = false });
config.AddParameter(new Parameter(
    "CalculatedTemp:Derived", "TempCalc", "Transient calculated temperature",
    1000, 0, 1000, 0, 0, 0u, 0u, "temp_conv",
    new List<string> { "Calculated" }, channelId, "Derived"));
config.Commit();

Console.WriteLine("Transient parameter 'CalculatedTemp:Derived' created (in-memory only)");

// --- Write data to the transient parameter ---
var startTime = 36_000_000_000_000L;
for (var i = 0; i < 10; i++)
    session.AddChannelData(channelId, startTime + i * interval, 1, BitConverter.GetBytes(100.0 + i * 2.5));

// --- Read it back (works while session is open) ---
using var pda = session.CreateParameterDataAccess("CalculatedTemp:Derived");
var samples = pda.GetSamplesBetween(startTime, startTime + 10 * interval);
Console.WriteLine($"Read {samples.SampleCount} transient samples: [{samples.Data[0]:F1} ... {samples.Data[^1]:F1}]");

// --- Verify it's gone after reload ---
clientSession.Close();
using var reloaded = sessionManager.Load(sessionKey, connectionString);
var hasParam = reloaded.Session.Parameters.Any(p => p.Identifier == "CalculatedTemp:Derived");
Console.WriteLine($"\nAfter reload: parameter exists = {hasParam}");
Console.WriteLine("Transient parameters are not persisted — they exist only in-memory.");
