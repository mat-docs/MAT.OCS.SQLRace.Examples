using LiveDashboard;

using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Domain.Query;

using Microsoft.Extensions.Configuration;

using SqlRace.Examples.Core;

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

if (!string.IsNullOrEmpty(configuredSessionId))
{
    var match = sessions.FirstOrDefault(s => s.Identifier == configuredSessionId);
    if (match is null)
    {
        Console.WriteLine($"Session '{configuredSessionId}' not found. Available:");
        foreach (var s in sessions.OrderByDescending(s => s.TimeOfRecording).Take(5))
        {
            Console.WriteLine($"  {s.Identifier} ({s.Key})");
        }

        return;
    }

    var sessionConnectionString = match.GetConnectionString();
    clientSession = sessionManager.Load(match.Key, sessionConnectionString);
    session = clientSession.Session;
    Console.WriteLine($"Loading configured session: {match.Identifier} ({match.Key})");
}
else
{
    // Try sessions newest-first, skip any with no sample data
    foreach (var candidate in sessions.OrderByDescending(s => s.TimeOfRecording))
    {
        var sessionConnectionString = candidate.GetConnectionString();
        var tempClient = sessionManager.Load(candidate.Key, sessionConnectionString);
        var tempSession = tempClient.Session;

        // Check if the first configured parameter has any data
        var testParam = paramIds.FirstOrDefault(id =>
            tempSession.Parameters.Any(p => p.Identifier == id));

        if (testParam == null)
        {
            tempClient.Close();
            Console.WriteLine($"Skipping session {candidate.Identifier} (test parameter not found)");
            continue;
        }

        using var testPda = tempSession.CreateParameterDataAccess(testParam);
        var sampleCount = testPda.GetSamplesCount(tempSession.StartTime, tempSession.EndTime);

        if (sampleCount == 0)
        {
            Console.WriteLine($"Skipping session {candidate.Identifier} (no sample data)");
            tempClient.Close();
            continue;
        }

        clientSession = tempClient;
        session = tempSession;
        Console.WriteLine($"Loading session: {candidate.Identifier} ({candidate.Key})");
        break;
    }
}

if (clientSession == null ||
    session == null)
{
    Console.WriteLine("No sessions with sample data found.");
    return;
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

var lastEndTime = long.MinValue;
// Create a list of PDAs for the available parameters.
var parameterPdas = new List<ParameterDataAccessBase>(availableParams.Count);
foreach (var param in availableParams)
{
    var pda = session.CreateParameterDataAccess(param);
    parameterPdas.Add(pda);
}

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        await Task.Delay(pollingMs, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }

    var stats = new List<DashboardRenderer.ParameterStats>();
    if (lastEndTime >= session.EndTime)
    {
        // -- No updates to the session, skip this polling cycle --
        continue;
    }

    lastEndTime = session.EndTime;

    foreach (var pda in parameterPdas)
    {
        // Query the PDA for new samples
        var pdaEndTime = pda.GetEndTime();
        if (pdaEndTime == null)
        {
            // No samples are available for the PDA.
            stats.Add(
                new DashboardRenderer.ParameterStats(
                    pda.ParameterIdentifier,
                    double.NaN,
                    double.NaN,
                    double.NaN,
                    double.NaN,
                    0));
            continue;
        }

        var maxSampleIntervalNs = (long)(1_000_000_000 / pda.GetMaximumFrequency());
        var duration = maxSampleIntervalNs * windowSize;
        var startTime = pdaEndTime.Value - duration;
        // Grab specifically the end, Max, Min, and Average values for the parameter over the window.
        var dataStatistics = pda.GetDataStatistics(
            startTime,
            duration,
            statisticOption: StatisticOption.End | StatisticOption.Max | StatisticOption.Min | StatisticOption.Mean);

        if (dataStatistics.NumberOfSamples == 0)
        {
            // No samples are available for the PDA in the specified window.
            stats.Add(
                new DashboardRenderer.ParameterStats(
                    pda.ParameterIdentifier,
                    double.NaN,
                    double.NaN,
                    double.NaN,
                    double.NaN,
                    0));
            continue;
        }

        var latest = dataStatistics.EndValue;
        var min = dataStatistics.MinimumValue;
        var max = dataStatistics.MaximumValue;

        stats.Add(
            new DashboardRenderer.ParameterStats(
                pda.ParameterIdentifier,
                latest,
                min,
                max,
                dataStatistics.MeanValue,
                dataStatistics.NumberOfSamples));
    }

    renderer.Render(
        sessionId,
        session.EndTime > session.StartTime ? "Data" : "Empty",
        stats);
}

Console.SetCursorPosition(0, Console.CursorTop + 1);
Console.WriteLine("Dashboard stopped.");
// Dispose all PDAs once we are done.
parameterPdas.ForEach(x => x.Dispose());
clientSession.Close();