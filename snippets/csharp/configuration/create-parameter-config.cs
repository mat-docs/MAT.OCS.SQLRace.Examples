// ─────────────────────────────────────────────────────────────
// SQL Race Example: Create Parameter Configuration
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: The full manual parameter configuration process step by step —
//               ParameterGroup, ApplicationGroup, RationalConversion, Channel, Parameter
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own session)
// Output: Configured parameter with data written and read back
//
// Related: snippets/csharp/configuration/create-transient-parameter.cs
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
using var clientSession = sessionManager.CreateSession(connectionString, sessionKey, "Config Demo", DateTime.UtcNow, "Example");
if (clientSession is null)
{
    Console.WriteLine($"ERROR: Failed to create session at '{connectionString}'.");
    Console.WriteLine("Tip: Check the target folder is writable and the file is not locked.");
    return;
}
var session = clientSession.Session;

// --- Step 1: Create a configuration batch ---
var config = session.CreateConfiguration();

// --- Step 2: Define the conversion (raw → engineering units) ---
// RationalConversion: engineering = (c0 + c1*raw) / (c2 + c3*raw)
// For a 1:1 pass-through: c0=0, c1=1, c2=0, c3=1 → gives raw/1 = raw
var conversion = RationalConversion.CreateSimple1To1Conversion("degC_conversion", "degC", "%5.2f");
config.AddConversion(conversion);
Console.WriteLine("1. Conversion: degC_conversion (1:1 rational)");

// --- Step 3: Define the channel (data storage) ---
uint channelId = 100;
var frequency = new Frequency(100, FrequencyUnit.Hz);
var interval = frequency.ToInterval(); // nanoseconds between samples
var channel = new Channel(channelId, "Temperature", interval, DataType.Double64Bit, ChannelDataSourceType.Periodic, "Temperature:Sensors", false);
config.AddChannel(channel);
Console.WriteLine($"2. Channel: id={channelId}, 100 Hz, Double64Bit, interval={interval} ns");

// --- Step 4: Define groups ---
var paramGroup = new ParameterGroup("Thermal", "Thermal measurements");
var appGroup = new ApplicationGroup("Sensors", "Sensor data sources", new List<string> { "Thermal" }) { SupportsRda = false };
config.AddParameterGroup(paramGroup);
config.AddGroup(appGroup);
Console.WriteLine("3. Groups: Thermal (parameter), Sensors (application)");

// --- Step 5: Define the parameter (links everything together) ---
var parameter = new Parameter(
    "Temperature:Sensors",   // identifier
    "Temperature",           // name
    "Inlet temperature sensor", // description
    500.0,                   // maximumValue
    0.0,                     // minimumValue
    500.0,                   // warningMaximumValue
    0.0,                     // warningMinimumValue
    0.0,                     // offsetValue
    0u,                      // dataBitMask
    0u,                      // errorBitMask
    "degC_conversion",       // conversionFunctionName
    new List<string> { "Thermal" }, // parameterGroupIdentifiers
    channelId,               // channelId (uint)
    "Sensors");              // applicationName
config.AddParameter(parameter);
Console.WriteLine("4. Parameter: Temperature:Sensors → channelId=100, conversion=degC_conversion");

// --- Step 6: Commit (atomically applies all configuration) ---
try
{
    config.Commit();
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Configuration commit failed: {ex.Message}");
    Console.WriteLine("Tip: Identifiers must be unique within a session and not previously committed.");
    return;
}
Console.WriteLine("5. Configuration committed.\n");

// --- Write and read back ---
var startTime = 36_000_000_000_000L;
for (var i = 0; i < 10; i++)
    session.AddChannelData(channelId, startTime + i * interval, 1, BitConverter.GetBytes(20.0 + i));

using var pda = session.CreateParameterDataAccess("Temperature:Sensors");
var samples = pda.GetSamplesBetween(startTime, startTime + 10 * interval);
Console.WriteLine($"Wrote 10 samples, read back {samples.SampleCount}: [{samples.Data[0]:F1} ... {samples.Data[^1]:F1}] degC");
