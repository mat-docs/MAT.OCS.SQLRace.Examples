// 
// SQL Race Example: SQL Race Example: Read Samples Between
// 
// Demonstrates: Using GetSamplesBetween to read data in a specific time range
// Prerequisites: MESL.SQLRace.API, a session with data
// Expected output: Sample count and values within the specified range
// Related: read-subsampled-data.cs, reverse-iteration.cs
// 

using MAT.OCS.Core;
using MESL.SqlRace.Domain;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <session-guid> [parameter-identifier]");
    return;
}

SessionKey sessionKey;
try { sessionKey = SessionKey.Parse(args[0]); }
catch (Exception ex) { Console.WriteLine($"ERROR: '{args[0]}' is not a valid GUID. {ex.Message}"); return; }
var paramId = args.Length > 1 ? args[1] : "Temperature:Sensors";

var sessionManager = SessionManager.CreateSessionManager();
IClientSession clientSession;
try { clientSession = sessionManager.Load(sessionKey, connectionString); }
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Could not load session '{sessionKey}': {ex.Message}");
    return;
}
using (clientSession)
{
var session = clientSession.Session;

if (!session.Parameters.Any(p => p.Identifier == paramId))
{
    Console.WriteLine($"ERROR: Parameter '{paramId}' not found in session.");
    Console.WriteLine($"Available: {string.Join(", ", session.Parameters.Take(10).Select(p => p.Identifier))}");
    return;
}

using var pda = session.CreateParameterDataAccess(paramId);

// --- Define a time range: first 500ms of data ---
var rangeStart = session.StartTime;
var rangeEnd = session.StartTime + 500_000_000L; // 0.5 seconds in nanoseconds

var samples = pda.GetSamplesBetween(rangeStart, rangeEnd);

Console.WriteLine($"Parameter: {paramId}");
Console.WriteLine($"Range:     {rangeStart} – {rangeEnd} ns ({(rangeEnd - rangeStart) / 1e9:F3} s)");
Console.WriteLine($"Samples:   {samples.SampleCount}");

for (var i = 0; i < Math.Min(samples.SampleCount, 10); i++)
{
    var relativeMs = (samples.Timestamp[i] - rangeStart) / 1_000_000.0;
    Console.WriteLine($"  t+{relativeMs,8:F3} ms  {samples.Data[i]:F4}");
}

if (samples.SampleCount > 10)
    Console.WriteLine($"  ... and {samples.SampleCount - 10} more");
}
