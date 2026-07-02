using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;
using Microsoft.Extensions.Configuration;
using SqlRace.Examples.Core;
using ServerListenerRecorder;

// ─── Configuration ───────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var recorderConfig = new RecorderConfig();
configuration.GetSection("Recorder").Bind(recorderConfig);

var connectionString = ConnectionStringProvider.Resolve(
    configuration.GetConnectionString("SqlRace"));

// ─── Cancellation (Ctrl+C) ──────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nShutdown requested...");
};

// ─── Initialise SQL Race ─────────────────────────────────────
SqlRaceBootstrapper.Initialize();
Console.WriteLine("SQL Race initialised");
Console.WriteLine($"Connection: {connectionString}");
Console.WriteLine($"Listener:   {recorderConfig.ListenerIpAddress}:{recorderConfig.ListenerPort}");

// ─── Configure Server Listener ───────────────────────────────
// The server listener (Core.ConfigureServer(bool, IPEndPoint) + RecordersConfiguration)
// receives data pushed from ATLAS or another telemetry source.
// In a real deployment, the server listener port in this application and the
// corresponding port in ATLAS (Tools → SqlRace → Settings) must be different.
Console.WriteLine($"Data source: {recorderConfig.ResolvedDataSourcePath}");

// ─── Create Session ──────────────────────────────────────────
var sessionName = $"{recorderConfig.SessionIdentifier}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
IClientSession? clientSession = null;

try
{
    clientSession = SessionFactory.CreateSession(sessionName, connectionString);
    var session = clientSession.Session;
    Console.WriteLine($"Session created: {session.Key}");
    Console.WriteLine($"  Name: {sessionName}");

    // ─── Create Parameters ───────────────────────────────────
    var builders = new[]
    {
        new ParameterBuilder(session)
            .WithIdentifier("Temperature:Sensors")
            .WithDescription("Inlet temperature sensor")
            .InApplicationGroup("Sensors").InParameterGroup("Thermal")
            .WithFrequency(100, FrequencyUnit.Hz)
            .WithDataType(DataType.Double64Bit)
            .WithConversion("degC", "%5.2f")
            .WithRange(min: 0, max: 500),

        new ParameterBuilder(session)
            .WithIdentifier("Pressure:Inlet")
            .WithDescription("Inlet pressure transducer")
            .InApplicationGroup("Sensors").InParameterGroup("Hydraulic")
            .WithFrequency(50, FrequencyUnit.Hz)
            .WithDataType(DataType.Double64Bit)
            .WithConversion("bar", "%5.3f")
            .WithRange(min: 0, max: 10),

        new ParameterBuilder(session)
            .WithIdentifier("Speed:Shaft")
            .WithDescription("Main shaft rotational speed")
            .InApplicationGroup("Powertrain").InParameterGroup("Drivetrain")
            .WithFrequency(200, FrequencyUnit.Hz)
            .WithDataType(DataType.Double64Bit)
            .WithConversion("rpm", "%6.0f")
            .WithRange(min: 0, max: 10000),
    };

    var parameters = ParameterBuilder.BuildBatch(session, builders);
    Console.WriteLine($"  Parameters: {parameters.Count} configured");

    // ─── Data Generation Loop ────────────────────────────────
    // In production, this loop would be replaced by data arriving
    // via the server listener. Here we simulate with sine waves.
    var baseTime = 36_000_000_000_000L; // 10:00:00.000
    var tempInterval = new Frequency(100, FrequencyUnit.Hz).ToInterval();
    var pressInterval = new Frequency(50, FrequencyUnit.Hz).ToInterval();
    var speedInterval = new Frequency(200, FrequencyUnit.Hz).ToInterval();

    Console.WriteLine("\nRecording simulated data (Ctrl+C to stop)...\n");

    var sampleIndex = 0L;
    var writeIntervalMs = 100; // Write batch every 100ms

    while (!cts.Token.IsCancellationRequested)
    {
        var batchStart = sampleIndex;

        // Temperature: 100 Hz → 10 samples per 100ms batch
        for (var i = 0; i < 10; i++)
        {
            var ts = baseTime + (sampleIndex * 10 + i) * tempInterval;
            var value = 75.0 + 15.0 * Math.Sin((sampleIndex * 10 + i) * 0.02);
            session.AddChannelData(parameters[0].ChannelId, ts, 1, BitConverter.GetBytes(value));
        }

        // Pressure: 50 Hz → 5 samples per 100ms batch
        for (var i = 0; i < 5; i++)
        {
            var ts = baseTime + (sampleIndex * 5 + i) * pressInterval;
            var value = 3.5 + 0.8 * Math.Sin((sampleIndex * 5 + i) * 0.03);
            session.AddChannelData(parameters[1].ChannelId, ts, 1, BitConverter.GetBytes(value));
        }

        // Speed: 200 Hz → 20 samples per 100ms batch
        for (var i = 0; i < 20; i++)
        {
            var ts = baseTime + (sampleIndex * 20 + i) * speedInterval;
            var value = 3000.0 + 500.0 * Math.Sin((sampleIndex * 20 + i) * 0.01);
            session.AddChannelData(parameters[2].ChannelId, ts, 1, BitConverter.GetBytes(value));
        }

        sampleIndex++;
        Console.WriteLine(
            $"  Batch {sampleIndex}: Temp=10 Press=5 Speed=20 samples " +
            $"(total: {sampleIndex * 35})");

        try
        {
            await Task.Delay(writeIntervalMs, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }

    Console.WriteLine($"\nRecording stopped. Total batches: {sampleIndex}");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
}
finally
{
    clientSession?.Dispose();
    Console.WriteLine("Session closed.");
}
