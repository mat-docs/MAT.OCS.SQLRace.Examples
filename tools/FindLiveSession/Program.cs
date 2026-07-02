// Finds the latest live (actively recording) session in a SQL Race database.
// A session is considered "live" if its EndTime increases between two loads.
//
// Usage:
//   dotnet run --project tools/FindLiveSession
//   dotnet run --project tools/FindLiveSession -- [connection-string] [--wait seconds]
//
// The --wait flag controls how long to monitor the best candidate for EndTime growth.
// Default is 15s. Data flushes happen every 60-70s, so use --wait 90 for reliable detection.
//
// Output (when a live session is found):
//   LIVE_SESSION_KEY=69337cf2-754c-4153-b48c-80ea070c89d9
//   LIVE_SESSION_ID=my-session-001
//   LIVE_PARAM=Temperature:Sensors

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.Enumerators;
using MESL.SqlRace.Domain.Query;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

// Parse args: [connection-string] [--wait seconds]
var connectionString = Environment.GetEnvironmentVariable("SQLRACE_CONNECTION_STRING")
    ?? $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")}";
var waitSeconds = 15;

for (var a = 0; a < args.Length; a++)
{
    if (args[a] == "--wait" && a + 1 < args.Length && int.TryParse(args[a + 1], out var w))
    {
        waitSeconds = w;
        a++;
    }
    else if (!args[a].StartsWith("--"))
    {
        connectionString = args[a];
    }
}

Console.WriteLine($"Connection: {connectionString}");
Console.WriteLine($"Wait time:  {waitSeconds}s (use --wait 90 for reliable flush detection)");
Console.WriteLine("Querying sessions...\n");

var queryManager = QueryManager.CreateQueryManager(connectionString);
var sessions = queryManager.ExecuteQuery()
    .OrderByDescending(s => s.TimeOfRecording)
    .ToList();

if (sessions.Count == 0)
{
    Console.WriteLine("ERROR: No sessions found in database.");
    Environment.Exit(1);
}

Console.WriteLine($"Found {sessions.Count} sessions.\n");

var sessionManager = SessionManager.CreateSessionManager();

// ─── Phase 1: Quick scan — load each recent session, capture EndTime & metadata ───
var candidateCount = Math.Min(10, sessions.Count);
var candidates = new List<(dynamic Info, long EndTime, int ParamCount, double DurationSec)>();

Console.WriteLine("─── Recent sessions ─────────────────────────────────────");
for (var i = 0; i < candidateCount; i++)
{
    var s = sessions[i];
    using var client = sessionManager.Load(s.Key, connectionString);
    var session = client.Session;
    var paramCount = session.Parameters.Count;
    var endTime = session.EndTime;
    var duration = (endTime - session.StartTime) / 1_000_000_000.0;

    var status = paramCount == 0 ? "no-params" : $"{paramCount} params, {duration:F0}s";
    Console.WriteLine($"  {s.Identifier,-30} {s.TimeOfRecording:yyyy-MM-dd HH:mm:ss}  ({status})");

    if (paramCount > 0)
        candidates.Add((s, endTime, paramCount, duration));
}
Console.WriteLine();

if (candidates.Count == 0)
{
    Console.WriteLine("ERROR: No sessions with parameters found.");
    Environment.Exit(1);
}

// ─── Phase 2: Monitor EndTime growth on top candidates ────────────────────
// Check the most recent sessions with parameters. Wait and reload to detect growth.
var checkCount = Math.Min(3, candidates.Count);
Console.WriteLine($"Monitoring top {checkCount} candidate(s) for {waitSeconds}s...\n");

var pollInterval = Math.Min(5, waitSeconds);
var elapsed = 0;

while (elapsed < waitSeconds)
{
    await Task.Delay(pollInterval * 1000);
    elapsed += pollInterval;

    for (var c = 0; c < checkCount; c++)
    {
        var (info, endTime1, paramCount, _) = candidates[c];
        using var client = sessionManager.Load(info.Key, connectionString);
        var endTime2 = client.Session.EndTime;

        if (endTime2 > endTime1)
        {
            Console.WriteLine($"  {info.Identifier}: EndTime GREW at {elapsed}s! (+{(endTime2 - endTime1) / 1_000_000_000.0:F1}s)");

            // Found a live session
            var paramWithData = FindParameterWithData(client.Session);
            PrintLiveResult(info, client.Session, paramCount, endTime1, endTime2, paramWithData, connectionString);
            return;
        }
    }

    Console.Write($"  [{elapsed}/{waitSeconds}s] no growth detected...\r");
}

Console.WriteLine();

// ─── Phase 3: No live session found — show the best candidate anyway ──────
var best = candidates[0];
Console.WriteLine($"\nNo live sessions detected (EndTime static across {waitSeconds}s).");
Console.WriteLine();

// Still print the most recent session info — it's probably what the user wants
using var bestClient = sessionManager.Load(best.Info.Key, connectionString);
var bestParam = FindParameterWithData(bestClient.Session);

Console.WriteLine("─── Most recent session (not confirmed live) ─────────────");
Console.WriteLine($"  Session Key:  {best.Info.Key}");
Console.WriteLine($"  Identifier:   {best.Info.Identifier}");
Console.WriteLine($"  Recorded:     {best.Info.TimeOfRecording:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"  Parameters:   {best.ParamCount}");
Console.WriteLine($"  Duration:     {best.DurationSec:F0}s");
Console.WriteLine($"  Working param: {bestParam ?? "(none)"}");
Console.WriteLine();
Console.WriteLine($"SESSION_KEY={best.Info.Key}");
Console.WriteLine($"SESSION_ID={best.Info.Identifier}");
if (bestParam is not null)
    Console.WriteLine($"SESSION_PARAM={bestParam}");
Console.WriteLine();
Console.WriteLine("Tip: If recorder is running but flushes are slow, try: --wait 90");

static void PrintLiveResult(dynamic info, Session session, int paramCount,
    long endTime1, long endTime2, string? paramWithData, string connectionString)
{
    Console.WriteLine();
    Console.WriteLine("═══ LIVE SESSION FOUND ══════════════════════════════════");
    Console.WriteLine($"  Session Key:  {info.Key}");
    Console.WriteLine($"  Identifier:   {info.Identifier}");
    Console.WriteLine($"  Recorded:     {info.TimeOfRecording:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"  Parameters:   {paramCount}");
    Console.WriteLine($"  EndTime grew: {endTime1} → {endTime2} (+{(endTime2 - endTime1) / 1_000_000_000.0:F1}s)");
    Console.WriteLine($"  Working param: {paramWithData ?? "(none found)"}");

    Console.WriteLine();
    Console.WriteLine("─── Copy-paste values ───────────────────────────────────");
    Console.WriteLine($"LIVE_SESSION_KEY={info.Key}");
    Console.WriteLine($"LIVE_SESSION_ID={info.Identifier}");
    if (paramWithData is not null)
        Console.WriteLine($"LIVE_PARAM={paramWithData}");

    Console.WriteLine();
    Console.WriteLine("─── Snippet test commands ───────────────────────────────");
    var csArg = $"\"{connectionString}\"";
    var keyArg = info.Key.ToString();
    var pArg = paramWithData ?? "Temperature:Sensors";
    Console.WriteLine($"# Copy snippet then run:");
    Console.WriteLine($"# poll-live-samples");
    Console.WriteLine($"Copy-Item snippets\\csharp\\live-data\\poll-live-samples.cs tools\\SnippetRunner\\Program.cs -Force");
    Console.WriteLine($"dotnet run --project tools/SnippetRunner -- {keyArg} {csArg} \"{pArg}\"");
    Console.WriteLine($"# rda-parameter-change-detection");
    Console.WriteLine($"Copy-Item snippets\\csharp\\live-data\\rda-parameter-change-detection.cs tools\\SnippetRunner\\Program.cs -Force");
    Console.WriteLine($"dotnet run --project tools/SnippetRunner -- {keyArg} {csArg} \"{pArg}\"");
    Console.WriteLine("═════════════════════════════════════════════════════════");
}

static string? FindParameterWithData(Session session)
{
    // Try domain-neutral names first, then scan the session
    string[] wellKnown = [
        "Temperature:Sensors", "Pressure:Inlet", "Speed:Shaft",
        "RPM:Turbine", "Voltage:Battery", "FlowRate:Coolant",
        "Force:Actuator01", "Acceleration:Platform"
    ];

    foreach (var pid in wellKnown)
    {
        if (!session.Parameters.Any(p => p.Identifier == pid))
            continue;
        try
        {
            using var pda = session.CreateParameterDataAccess(pid);
            pda.GoTo(long.MaxValue);
            var samples = pda.GetNextSamples(1, StepDirection.Reverse);
            if (samples.SampleCount > 0)
                return pid;
        }
        catch { /* parameter exists but can't be read — skip */ }
    }

    // Fall back to scanning first 100 parameters
    foreach (var p in session.Parameters.Take(100))
    {
        try
        {
            using var pda = session.CreateParameterDataAccess(p.Identifier);
            pda.GoTo(long.MaxValue);
            var samples = pda.GetNextSamples(1, StepDirection.Reverse);
            if (samples.SampleCount > 0)
                return p.Identifier;
        }
        catch { /* skip */ }
    }

    return null;
}
