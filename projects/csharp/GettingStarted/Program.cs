// ============================================================================
// Getting Started — using the Core library
// ============================================================================
// This console app demonstrates the same operations as the getting-started
// snippets, but using the fluent Core library API for cleaner code.
//
// Run: dotnet run --project GettingStarted
// ============================================================================

using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;
using SqlRace.Examples.Core;

// --- 1. Initialise (idempotent — safe to call multiple times) ---
SqlRaceBootstrapper.Initialize();
Console.WriteLine($"SQL Race initialised: {SqlRaceBootstrapper.IsInitialized}");

// --- 2. Resolve connection string (uses default SQLite in temp) ---
var connectionString = ConnectionStringProvider.Resolve();
Console.WriteLine($"Connection: {connectionString}");

// --- 3. Create a new session ---
using var clientSession = SessionFactory.CreateSession("Getting Started Demo");
var session = clientSession.Session;
Console.WriteLine($"Session created: {session.Key}");

// --- 4. Add parameters using the fluent builder ---
var temperature = new ParameterBuilder(session)
    .WithIdentifier("Temperature:Sensors")
    .WithDescription("Inlet temperature sensor")
    .InApplicationGroup("Sensors")
    .InParameterGroup("Thermal")
    .WithFrequency(100, FrequencyUnit.Hz)
    .WithDataType(DataType.Double64Bit)
    .WithConversion("degC", "%5.2f")
    .WithRange(min: 0, max: 500)
    .Build();

var pressure = new ParameterBuilder(session)
    .WithIdentifier("Pressure:Inlet")
    .WithDescription("Inlet pressure sensor")
    .InApplicationGroup("Sensors")
    .InParameterGroup("Hydraulic")
    .WithFrequency(50, FrequencyUnit.Hz)
    .WithDataType(DataType.Double64Bit)
    .WithConversion("bar", "%5.3f")
    .WithRange(min: 0, max: 10)
    .Build();

Console.WriteLine($"Parameters: {session.Parameters.Count}");

// --- 5. Write sample data ---
var startTime = 36_000_000_000_000L; // 10:00:00.000
var tempInterval = new Frequency(100, FrequencyUnit.Hz).ToInterval();
var pressInterval = new Frequency(50, FrequencyUnit.Hz).ToInterval();

// 1 second of temperature data at 100 Hz
for (var i = 0; i < 100; i++)
{
    var timestamp = startTime + i * tempInterval;
    var value = 20.0 + Math.Sin(i * 0.1) * 5.0;
    session.AddChannelData(temperature.ChannelId, timestamp, 1, BitConverter.GetBytes(value));
}

// 1 second of pressure data at 50 Hz
for (var i = 0; i < 50; i++)
{
    var timestamp = startTime + i * pressInterval;
    var value = 2.5 + Math.Cos(i * 0.2) * 0.5;
    session.AddChannelData(pressure.ChannelId, timestamp, 1, BitConverter.GetBytes(value));
}

Console.WriteLine("Wrote 100 temperature + 50 pressure samples");

// --- 6. Read back and verify ---
using var tempPda = session.CreateParameterDataAccess("Temperature:Sensors");
var tempSamples = tempPda.GetSamplesBetween(startTime, startTime + 1_000_000_000L);

using var pressPda = session.CreateParameterDataAccess("Pressure:Inlet");
var pressSamples = pressPda.GetSamplesBetween(startTime, startTime + 1_000_000_000L);

Console.WriteLine($"Read back: {tempSamples.SampleCount} temperature, {pressSamples.SampleCount} pressure");
Console.WriteLine($"Temperature[0] = {tempSamples.Data[0]:F2} degC");
Console.WriteLine($"Pressure[0]    = {pressSamples.Data[0]:F3} bar");

Console.WriteLine();
Console.WriteLine("Done. Use this session key to load it in other examples:");
Console.WriteLine($"  {session.Key}");
