using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;
using Microsoft.Extensions.Configuration;
using SqlRace.Examples.Core;
using StandaloneRecorder;

// ─── Configuration ───────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var config = new RecorderConfig();
configuration.GetSection("Recorder").Bind(config);

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
Console.WriteLine($"ADS host:    {config.AdsHost}");
Console.WriteLine($"Listener:    {config.ListenerIpAddress}:{config.ListenerPort}");
Console.WriteLine($"Connection:  {connectionString}");

// ─── Initialise Recorder ─────────────────────────────────────
// TODO: Verify — Recorders.Initialise() API signature
// In production: Recorders.Initialise(config.ListenerIpAddress, config.ListenerPort);
Console.WriteLine("\nInitialising recorder subsystem...");

// ─── Create Data Server Telemetry Recorder ───────────────────
// TODO: Verify — DataServerTelemetryRecorder creation API
// var recorder = new DataServerTelemetryRecorder();
// recorder.ServerList.Refresh();
//
// Console.WriteLine("Available servers:");
// foreach (var server in recorder.ServerList)
//     Console.WriteLine($"  {server.Name} ({server.Address})");
//
// recorder.Connect(config.AdsHost);
// recorder.AutoRecordEnable = true;

// ─── Listen for new sessions ─────────────────────────────────
var sessionCreated = new TaskCompletionSource<SessionKey>();
var sessionManager = SessionManager.CreateSessionManager();

// TODO: Verify — SessionManager.SessionEventOccurred event signature
// sessionManager.SessionEventOccurred += (sender, e) =>
// {
//     if (e.EventType == SessionEventType.SessionCreated)
//     {
//         Console.WriteLine($"  Session detected: {e.SessionKey}");
//         sessionCreated.TrySetResult(e.SessionKey);
//     }
// };

Console.WriteLine("Waiting for session to be created...");
Console.WriteLine("(In production, this waits for ADS to start streaming data)");
Console.WriteLine();

// ─── Simulation: create a session with live-style data ───────
// Since we cannot connect to a real ADS in this example,
// we simulate the pattern by creating a session and writing data.
var sessionName = $"StandaloneRecorder-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
IClientSession? clientSession = null;

try
{
    clientSession = SessionFactory.CreateSession(sessionName, connectionString);
    var session = clientSession.Session;
    Console.WriteLine($"Session created: {session.Key}");
    Console.WriteLine($"  Name: {sessionName}");

    // ─── Configure source parameters ────────────────────────
    var sourceParam = new ParameterBuilder(session)
        .WithIdentifier("Temperature:Sensors")
        .WithDescription("Incoming temperature telemetry")
        .InApplicationGroup("Sensors").InParameterGroup("Thermal")
        .WithFrequency(100, FrequencyUnit.Hz)
        .WithDataType(DataType.Double64Bit)
        .WithConversion("degC", "%5.2f")
        .WithRange(min: 0, max: 500)
        .Build();

    // ─── Create a custom derived parameter ──────────────────
    var derivedParam = new ParameterBuilder(session)
        .WithIdentifier("TemperatureSmoothed:Derived")
        .WithDescription("Exponentially smoothed temperature")
        .InApplicationGroup("Derived").InParameterGroup("Thermal")
        .WithFrequency(100, FrequencyUnit.Hz)
        .WithDataType(DataType.Double64Bit)
        .WithConversion("degC", "%5.2f")
        .WithRange(min: 0, max: 500)
        .Build();

    Console.WriteLine("  Source:  Temperature:Sensors (100 Hz)");
    Console.WriteLine("  Derived: TemperatureSmoothed:Derived (EMA)");
    Console.WriteLine("\nRecording and processing (Ctrl+C to stop)...\n");

    // ─── Read-Transform-Write loop ──────────────────────────
    var baseTime = 36_000_000_000_000L;
    var interval = new Frequency(100, FrequencyUnit.Hz).ToInterval();
    var smoothed = 75.0; // EMA state
    const double alpha = 0.1; // Smoothing factor

    var sampleIndex = 0;
    while (!cts.Token.IsCancellationRequested)
    {
        // Simulate incoming data (10 samples per batch = 100ms at 100 Hz)
        for (var i = 0; i < 10; i++)
        {
            var idx = sampleIndex * 10 + i;
            var ts = baseTime + idx * interval;
            var raw = 75.0 + 15.0 * Math.Sin(idx * 0.02) + Random.Shared.NextDouble() * 2.0;

            // WRITE source data
            session.AddChannelData(sourceParam.ChannelId, ts, 1, BitConverter.GetBytes(raw));

            // TRANSFORM: exponential moving average
            smoothed = alpha * raw + (1.0 - alpha) * smoothed;

            // WRITE derived data
            session.AddChannelData(derivedParam.ChannelId, ts, 1, BitConverter.GetBytes(smoothed));
        }

        sampleIndex++;
        Console.WriteLine(
            $"  Batch {sampleIndex,4}: " +
            $"READ raw → TRANSFORM (EMA α={alpha}) → WRITE smoothed  " +
            $"[latest: raw={75.0 + 15.0 * Math.Sin(sampleIndex * 10 * 0.02):F2}, " +
            $"smoothed={smoothed:F2}]");

        try
        {
            await Task.Delay(100, cts.Token);
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
    // TODO: Verify — recorder.StopRecording(), recorder.AutoRecordEnable = false, recorder.Dispose()
    clientSession?.Dispose();
    Console.WriteLine("Session closed. Recorder shut down.");
}
