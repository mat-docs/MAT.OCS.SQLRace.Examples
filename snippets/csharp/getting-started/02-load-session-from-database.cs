// 
// SQL Race Example: SQL Race Example: Load Session from Database
// 
// Demonstrates: Loading a session from SQLite by session key using SessionManager
// Prerequisites: MESL.SQLRace.API NuGet package, a SQLite database with sessions
// Expected output: Session summary (name, start time, parameter count)
// Related: 01-load-session-from-file.cs, 04-create-session-write-data.cs
// 

using MAT.OCS.Core;
using MESL.SqlRace.Domain;

// --- Initialise the SQL Race runtime ---
Core.LicenceProgramName = "SQLRace";
Core.Initialize();

// --- Connection string (defaults to SQLite in temp directory) ---
var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";

// --- Session key to load (pass as argument or use a known key) ---
if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <session-guid>");
    Console.WriteLine($"Database: {connectionString}");
    Console.WriteLine("Tip: Create a session first with 04-create-session-write-data.cs, which prints the session key.");
    return;
}

var sessionKey = SessionKey.Parse(args[0]);

// --- Load the session ---
var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.Load(sessionKey, connectionString);

if (clientSession is null)
{
    Console.WriteLine($"Session not found: {sessionKey}");
    Console.WriteLine($"Check the session key and database: {connectionString}");
    return;
}

var session = clientSession.Session;

// --- Print session summary ---
Console.WriteLine($"Session loaded: {sessionKey}");
Console.WriteLine($"  Identifier:     {session.Identifier ?? "(unnamed)"}");
Console.WriteLine($"  Start time:     {session.StartTime} ns");
Console.WriteLine($"  End time:       {session.EndTime} ns");
Console.WriteLine($"  Parameters:     {session.Parameters.Count}");
Console.WriteLine($"  Laps:           {session.LapCollection.Count}");
Console.WriteLine($"  Constants:      {session.Constants.Count}");
