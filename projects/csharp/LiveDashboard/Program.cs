using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.Enumerators;
using MESL.SqlRace.Domain.Query;
using Microsoft.Extensions.Configuration;
using SqlRace.Examples.Core;
using LiveDashboard;

// ─── Configuration ───────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var pollingMs = configuration.GetValue("Dashboard:PollingIntervalMs", 2000);
var windowSize = configuration.GetValue("Dashboard:SampleWindowSize", 100);
var paramIds = configuration.GetSection("Dashboard:ParameterIdentifiers")
    .Get<string[]>() ?? ["Temperature:Sensors", "Pressure:Inlet", "Speed:Shaft"];
var configuredSessionId = configuration.GetValue<string?>("Dashboard:SessionIdentifier");

var connectionString = ConnectionStringProvider.Resolve(
    configuration.GetConnectionString("SqlRace"));

// ─── Cancellation (Ctrl+C) ──────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// ─── Initialise ──────────────────────────────────────────────
SqlRaceBootstrapper.Initialize();

// ─── Find most recent session ────────────────────────────────
var queryManager = QueryManager.CreateQueryManager(connectionString);
var sessions = queryManager.ExecuteQuery().ToList();

if (sessions.Count == 0)
{
    Console.WriteLine("No sessions found in the database.");
    Console.WriteLine($"Connection: {connectionString}");
    Console.WriteLine("Tip: Run ServerListenerRecorder or GettingStarted to create sessions.");
    return;
}

// Use configured session key, or find the most recent session with data
var sessionManager = SessionManager.CreateSessionManager();
IClientSession? clientSession = null;
Session? session = null;
dynamic? loadKey = null; // Store the key for reloading

if (!string.IsNullOrEmpty(configuredSessionId))
{
    var match = sessions.FirstOrDefault(s => s.Identifier == configuredSessionId);
    if (match is null)
    {
        Console.WriteLine($"Session '{configuredSessionId}' not found. Available:");
        foreach (var s in sessions.OrderByDescending(s => s.TimeOfRecording).Take(5))
            Console.WriteLine($"  {s.Identifier} ({s.Key})");
        return;
    }

    clientSession = sessionManager.Load(match.Key, connectionString);
    session = clientSession.Session;
    loadKey = match.Key;
    Console.WriteLine($"Loading configured session: {match.Identifier} ({match.Key})");
}
else
{
    // Try sessions newest-first, skip any with no sample data
    foreach (var candidate in sessions.OrderByDescending(s => s.TimeOfRecording))
    {
        var tempClient = sessionManager.Load(candidate.Key, connectionString);
        var tempSession = tempClient.Session;

        // Check if the first configured parameter has any data
        var testParam = paramIds.FirstOrDefault(id =>
            tempSession.Parameters.Any(p => p.Identifier == id));

        if (testParam != null)
        {
            using var testPda = tempSession.CreateParameterDataAccess(testParam);
            testPda.GoTo(long.MaxValue);
            var testSamples = testPda.GetNextSamples(1, StepDirection.Reverse);

            if (testSamples.SampleCount > 0)
            {
                clientSession = tempClient;
                session = tempSession;
                loadKey = candidate.Key;
                Console.WriteLine($"Loading session: {candidate.Identifier} ({candidate.Key})");
                break;
            }
        }

        tempClient.Dispose();
        Console.WriteLine($"Skipping session {candidate.Identifier} (no sample data)");
    }

    if (session == null)
    {
        Console.WriteLine("No sessions with sample data found.");
        return;
    }
}

// ─── Resolve available parameters ────────────────────────────
var availableParams = paramIds
    .Where(id => session.Parameters.Any(p => p.Identifier == id))
    .ToList();

if (availableParams.Count == 0)
{
    Console.WriteLine("None of the configured parameters exist in this session.");
    Console.WriteLine($"Session parameters: {string.Join(", ", session.Parameters.Select(p => p.Identifier))}");
    return;
}

// ─── Polling loop ─────────────────────────────────────────────
var renderer = new DashboardRenderer(pollingMs);
var sessionId = session.Identifier;

while (!cts.Token.IsCancellationRequested)
{
    // Reload session each cycle to pick up newly flushed data
    clientSession?.Dispose();
    clientSession = sessionManager.Load(loadKey!, connectionString);
    session = clientSession.Session;

    var stats = new List<DashboardRenderer.ParameterStats>();

    foreach (var paramId in availableParams)
    {
        // Create a fresh PDA each cycle so we always see the latest live data
        using var pda = session.CreateParameterDataAccess(paramId);
        pda.GoTo(long.MaxValue);
        var samples = pda.GetNextSamples(windowSize, StepDirection.Reverse);

        if (samples.SampleCount == 0)
        {
            stats.Add(new DashboardRenderer.ParameterStats(
                paramId, double.NaN, double.NaN, double.NaN, double.NaN, 0));
            continue;
        }

        var latest = samples.Data[0];
        var min = double.MaxValue;
        var max = double.MinValue;
        var sum = 0.0;

        for (var j = 0; j < samples.SampleCount; j++)
        {
            var v = samples.Data[j];
            if (v < min) min = v;
            if (v > max) max = v;
            sum += v;
        }

        stats.Add(new DashboardRenderer.ParameterStats(
            paramId, latest, min, max, sum / samples.SampleCount, samples.SampleCount));
    }

    renderer.Render(
        sessionId,
        session.EndTime > session.StartTime ? "Data" : "Empty",
        stats);

    try
    {
        await Task.Delay(pollingMs, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

Console.SetCursorPosition(0, Console.CursorTop + 1);
Console.WriteLine("Dashboard stopped.");
clientSession?.Dispose();
