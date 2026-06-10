// ─────────────────────────────────────────────────────────────
// SQL Race Example: Conversion Types
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Creating RationalConversion (linear scaling) and TableConversion
//               (lookup table), applying each to a parameter, and reading back
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own session)
// Output: Data read back through both conversion types
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
using var clientSession = sessionManager.CreateSession(connectionString, sessionKey, "Conversion Demo", DateTime.UtcNow, "Example");
var session = clientSession.Session;

var config = session.CreateConfiguration();
var interval = new Frequency(10, FrequencyUnit.Hz).ToInterval();

// --- 1. RationalConversion: linear scaling (raw mV → degC) ---
// Formula: engineering = (c0 + c1*raw) / (c2 + c3*raw)
// For y = 0.1*x - 10: c0=-10, c1=0.1, c2=0, c3=1 → not quite right
// Simpler: 1:1 pass-through (c0=0, c1=1, c2=0, c3=1)
var rational = RationalConversion.CreateSimple1To1Conversion("linear_degC", "degC", "%5.2f");
config.AddConversion(rational);
Console.WriteLine("1. RationalConversion 'linear_degC': 1:1 pass-through");

config.AddChannel(new Channel(1u, "TempLinear", interval, DataType.Double64Bit, ChannelDataSourceType.Periodic, "TempLinear:Sensors", false));
config.AddParameterGroup(new ParameterGroup("Thermal", "Thermal"));
config.AddGroup(new ApplicationGroup("Sensors", "Sensors", new List<string> { "Thermal" }) { SupportsRda = false });
config.AddParameter(new Parameter("TempLinear:Sensors", "TempLinear", "Linear conversion", 500, 0, 500, 0, 0, 0u, 0u, "linear_degC", new List<string> { "Thermal" }, 1u, "Sensors"));

// --- 2. TableConversion: lookup table (raw ADC counts → bar) ---
var rawValues = new double[] { 0, 1024, 2048, 3072, 4095 };
var calValues = new double[] { 0.0, 2.5, 5.0, 7.5, 10.0 };
var table = new TableConversion("table_bar", "bar", "%5.3f", rawValues, calValues, true);
config.AddConversion(table);
Console.WriteLine("2. TableConversion 'table_bar': ADC counts → bar (5-point lookup)");

config.AddChannel(new Channel(2u, "PressTable", interval, DataType.Double64Bit, ChannelDataSourceType.Periodic, "PressTable:Sensors", false));
config.AddParameter(new Parameter("PressTable:Sensors", "PressTable", "Table conversion", 10, 0, 10, 0, 0, 0u, 0u, "table_bar", new List<string> { "Thermal" }, 2u, "Sensors"));

config.Commit();

// --- Write data and read back through each conversion ---
var startTime = 36_000_000_000_000L;
for (var i = 0; i < 5; i++)
{
    session.AddChannelData(1u, startTime + i * interval, 1, BitConverter.GetBytes(20.0 + i * 5.0));
    session.AddChannelData(2u, startTime + i * interval, 1, BitConverter.GetBytes(rawValues[i]));
}

using var pdaLinear = session.CreateParameterDataAccess("TempLinear:Sensors");
using var pdaTable = session.CreateParameterDataAccess("PressTable:Sensors");
var linearSamples = pdaLinear.GetSamplesBetween(startTime, startTime + 5 * interval);
var tableSamples = pdaTable.GetSamplesBetween(startTime, startTime + 5 * interval);

Console.WriteLine($"\n{"Index",6} {"Linear (degC)",14} {"Table (bar)",14}");
Console.WriteLine(new string('─', 36));
for (var i = 0; i < linearSamples.SampleCount; i++)
    Console.WriteLine($"{i,6} {linearSamples.Data[i],14:F2} {tableSamples.Data[i],14:F3}");
