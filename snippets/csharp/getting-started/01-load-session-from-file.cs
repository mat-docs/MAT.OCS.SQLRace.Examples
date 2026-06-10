// SQL Race Example: Load Session from File
//
// Demonstrates: Loading a session from an .ssn2 SQLite file by discovering
//               available sessions, then loading the most recent one
// Prerequisites: MESL.SQLRace.API NuGet package, a .ssn2 file on disk
// Output: Session summary (identifier, start time, parameter count, lap count)
// Related: 02-load-session-from-database.cs, 03-read-parameter-samples.cs

using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Query;

// --- Initialise the SQL Race runtime (required once per application) ---
Core.LicenceProgramName = "SQLRace";
Core.Initialize();

// --- Set the path to your session file ---
var filePath = args.Length > 0
    ? args[0]
    : Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2");

if (!File.Exists(filePath))
{
    Console.WriteLine($"File not found: {filePath}");
    Console.WriteLine("Usage: dotnet run -- <path-to-ssn2-file>");
    Console.WriteLine("Tip: Create a session first with 04-create-session-write-data.cs");
    return;
}

// --- Discover sessions in the file ---
var connectionString = $"DbEngine=SQLite;Data Source={filePath};";
var sessionManager = SessionManager.CreateSessionManager();
using var queryManager = QueryManager.CreateQueryManager(connectionString);
var summaries = queryManager.ExecuteQuery().ToList();

if (summaries.Count == 0)
{
    Console.WriteLine($"No sessions found in: {filePath}");
    return;
}

Console.WriteLine($"File: {filePath}");
Console.WriteLine($"Sessions found: {summaries.Count}\n");

// --- Load the most recent session ---
var latest = summaries.OrderByDescending(s => s.TimeOfRecording).First();
IClientSession clientSession;
try { clientSession = sessionManager.Load(latest.Key, connectionString); }
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Could not load session '{latest.Key}' from '{filePath}': {ex.Message}");
    Console.WriteLine("Tip: The file may be corrupt or written by an incompatible SQL Race version.");
    return;
}
using (clientSession)
{
var session = clientSession.Session;

Console.WriteLine($"Loaded session: {latest.Key}");
Console.WriteLine($"  Identifier:     {session.Identifier}");
Console.WriteLine($"  Start time:     {session.StartTime} ns");
Console.WriteLine($"  End time:       {session.EndTime} ns");
Console.WriteLine($"  Duration:       {(session.EndTime - session.StartTime) / 1_000_000_000.0:F3} s");
Console.WriteLine($"  Parameters:     {session.Parameters.Count}");
Console.WriteLine($"  Laps:           {session.LapCollection.Count}");

if (session.Parameters.Count > 0)
{
    Console.WriteLine("  Parameters:");
    foreach (var param in session.Parameters.Take(5))
        Console.WriteLine($"    - {param.Identifier}");
}
}
