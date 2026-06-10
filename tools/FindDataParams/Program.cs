// Quick diagnostic: find parameters with data in the latest session
using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.Enumerators;
using MESL.SqlRace.Domain.Query;
using MESL.SqlRace.Enumerators;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var cs = Environment.GetEnvironmentVariable("SQLRACE_CONNECTION_STRING")
    ?? $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")}";
var qm = QueryManager.CreateQueryManager(cs);
var sessions = qm.ExecuteQuery().OrderByDescending(s => s.TimeOfRecording).ToList();

Console.WriteLine($"Found {sessions.Count} sessions\n");
var latest = sessions.First();
Console.WriteLine($"Latest: {latest.Identifier} (Key: {latest.Key})");
Console.WriteLine($"Recorded: {latest.TimeOfRecording}\n");

var sm = SessionManager.CreateSessionManager();
using var client = sm.Load(latest.Key, cs);
var session = client.Session;
Console.WriteLine($"StartTime: {session.StartTime}");
Console.WriteLine($"EndTime:   {session.EndTime}");
Console.WriteLine($"Duration:  {(session.EndTime - session.StartTime) / 1_000_000_000.0:F1}s");
Console.WriteLine($"Parameters: {session.Parameters.Count}");
Console.WriteLine($"Laps:       {session.LapCollection.Count}\n");

// List the 5 most recent sessions
Console.WriteLine("Recent sessions:");
foreach (var s in sessions.Take(5))
    Console.WriteLine($"  {s.Identifier} | {s.TimeOfRecording} | {s.Key}");
Console.WriteLine();

// Try known common identifiers first
// Try domain-neutral names first, then common legacy names found in existing databases
var knownParams = new[] {
    "Temperature:Sensors", "Pressure:Inlet", "Speed:Shaft",
    "RPM:Turbine", "Voltage:Battery", "FlowRate:Coolant",
    "Force:Actuator01", "Acceleration:Platform"
};

Console.WriteLine("Trying known parameter names...");
foreach (var pid in knownParams)
{
    var exists = session.Parameters.Any(p => p.Identifier == pid);
    if (!exists) { Console.WriteLine($"  [not in session] {pid}"); continue; }
    try
    {
        using var pda = session.CreateParameterDataAccess(pid);
        var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);
        Console.WriteLine($"  [{samples.SampleCount,8} samples] {pid} = {(samples.SampleCount > 0 ? samples.Data[0].ToString("F4") : "no data")}");
    }
    catch (Exception ex) { Console.WriteLine($"  ERROR: {pid} - {ex.Message}"); }
}

// Also scan a random selection across the full list
Console.WriteLine("\nScanning random 200 parameters for data (via GoTo + GetNextSamples)...");
var rng = new Random(42);
var allParams = session.Parameters.ToList();
var indices = Enumerable.Range(0, allParams.Count).OrderBy(_ => rng.Next()).Take(200).ToList();
var found = 0;
foreach (var idx in indices)
{
    var p = allParams[idx];
    try
    {
        using var pda = session.CreateParameterDataAccess(p.Identifier);
        // Try GoTo + GetNextSamples (works better for live sessions)
        pda.GoTo(session.EndTime);
        var samples = pda.GetNextSamples(10, StepDirection.Reverse);
        if (samples.SampleCount > 0)
        {
            Console.WriteLine($"  [{samples.SampleCount,8} samples] {p.Identifier} = {samples.Data[0]:F4}");
            found++;
            if (found >= 10) break;
        }
    }
    catch { }
}

// Also try GoTo for the known params
Console.WriteLine("\nRetrying known params with GoTo...");
foreach (var pid in knownParams)
{
    if (!session.Parameters.Any(p => p.Identifier == pid)) continue;
    try
    {
        using var pda = session.CreateParameterDataAccess(pid);
        pda.GoTo(session.EndTime);
        var samples = pda.GetNextSamples(10, StepDirection.Reverse);
        Console.WriteLine($"  [{samples.SampleCount,8} samples] {pid} = {(samples.SampleCount > 0 ? samples.Data[0].ToString("F4") : "no data")}");
    }
    catch (Exception ex) { Console.WriteLine($"  ERROR: {pid} - {ex.Message}"); }
}

if (found == 0)
    Console.WriteLine("\n  Still no data found. Session may be live but data hasn't arrived yet.");
