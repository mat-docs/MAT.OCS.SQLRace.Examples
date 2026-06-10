// SQL Race Example: Create Session and Write Data
//
// Demonstrates: Creating a SQLite session with multi-rate parameters, writing and reading data
// Prerequisites: MESL.SQLRace.API NuGet package (no existing data needed)
// Output: Session key and verified sample values for Temperature (100Hz) and Pressure (50Hz)
// Related: 02-load-session-from-database.cs, 03-read-parameter-samples.cs

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";
var sessionKey = SessionKey.NewKey();

// --- Create a new session ---
var sessionManager = SessionManager.CreateSessionManager();
IClientSession clientSession;
try
{
    clientSession = sessionManager.CreateSession(connectionString, sessionKey, "Example Session", DateTime.UtcNow, "Example")
        ?? throw new InvalidOperationException("CreateSession returned null.");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Failed to create session at '{connectionString}': {ex.Message}");
    Console.WriteLine("Tip: Check the target folder is writable and the file is not locked by another process.");
    return;
}

using (clientSession)
{
    var session = clientSession.Session;
// --- Add parameter configuration ---
var config = session.CreateConfiguration();
uint tempChannelId = 1;
uint pressChannelId = 2;
var tempInterval = new Frequency(100, FrequencyUnit.Hz).ToInterval();
var pressInterval = new Frequency(50, FrequencyUnit.Hz).ToInterval();

config.AddConversion(RationalConversion.CreateSimple1To1Conversion("degC_conv", "degC", "%5.2f"));
config.AddConversion(RationalConversion.CreateSimple1To1Conversion("bar_conv", "bar", "%5.3f"));
config.AddChannel(new Channel(tempChannelId, "Temperature", tempInterval, DataType.Double64Bit, ChannelDataSourceType.Periodic, "Temperature:Sensors", false));
config.AddChannel(new Channel(pressChannelId, "Pressure", pressInterval, DataType.Double64Bit, ChannelDataSourceType.Periodic, "Pressure:Inlet", false));

config.AddParameterGroup(new ParameterGroup("Thermal", "Thermal"));
config.AddParameterGroup(new ParameterGroup("Hydraulic", "Hydraulic"));
config.AddGroup(new ApplicationGroup("Sensors", "Sensors", new List<string> { "Thermal", "Hydraulic" }) { SupportsRda = false });

config.AddParameter(new Parameter(
    "Temperature:Sensors", "Temperature", "Inlet temperature sensor",
    500, 0, 500, 0, 0.0, 0u, 0u, "degC_conv",
    new List<string> { "Thermal" }, tempChannelId, "Sensors"));
config.AddParameter(new Parameter(
    "Pressure:Inlet", "Pressure", "Inlet pressure sensor",
    10, 0, 10, 0, 0.0, 0u, 0u, "bar_conv",
    new List<string> { "Hydraulic" }, pressChannelId, "Sensors"));
try { config.Commit(); }
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Configuration commit failed: {ex.Message}");
    return;
}

// --- Write 1 second of data ---
var startTime = 36_000_000_000_000L; // 10:00:00.000

for (var i = 0; i < 100; i++) // 100 Hz temperature
{
    var timestamp = startTime + i * tempInterval;
    var value = 20.0 + Math.Sin(i * 0.1) * 5.0;
    session.AddChannelData(tempChannelId, timestamp, 1, BitConverter.GetBytes(value));
}

for (var i = 0; i < 50; i++) // 50 Hz pressure
{
    var timestamp = startTime + i * pressInterval;
    var value = 1.013 + Math.Sin(i * 0.05) * 0.2;
    session.AddChannelData(pressChannelId, timestamp, 1, BitConverter.GetBytes(value));
}

// --- Read back and verify ---
using var tempPda = session.CreateParameterDataAccess("Temperature:Sensors");
var tempSamples = tempPda.GetSamplesBetween(startTime, startTime + 100 * tempInterval);

using var pressPda = session.CreateParameterDataAccess("Pressure:Inlet");
var pressSamples = pressPda.GetSamplesBetween(startTime, startTime + 50 * pressInterval);

Console.WriteLine($"Session created: {sessionKey}");
Console.WriteLine($"Connection:      {connectionString}");
Console.WriteLine();
Console.WriteLine($"Temperature:Sensors (100 Hz): {tempSamples.SampleCount} samples");
Console.WriteLine($"  First: {tempSamples.Data[0]:F2} degC  Last: {tempSamples.Data[tempSamples.SampleCount - 1]:F2} degC");
Console.WriteLine($"Pressure:Inlet (50 Hz): {pressSamples.SampleCount} samples");
Console.WriteLine($"  First: {pressSamples.Data[0]:F3} bar  Last: {pressSamples.Data[pressSamples.SampleCount - 1]:F3} bar");
}
