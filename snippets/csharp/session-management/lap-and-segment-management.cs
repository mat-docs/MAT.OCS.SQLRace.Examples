// ─────────────────────────────────────────────────────────────
// SQL Race Example: Lap and Segment Management
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Creating Lap objects as test segments, adding items, reading back
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own session)
// Output: Segment list with metadata
//
// Related: snippets/csharp/session-management/marker-management.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";
var sessionKey = SessionKey.NewKey();

var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.CreateSession(connectionString, sessionKey, "Segment Demo", DateTime.UtcNow, "Example");
var session = clientSession.Session;

var baseTime = 36_000_000_000_000L; // 10:00:00.000

// --- Add segments (laps) representing test phases ---
var segments = new[]
{
    new Lap(baseTime,                      1, 0, "Warmup",          false),
    new Lap(baseTime +  60_000_000_000L,   2, 0, "Steady State 1",  false),
    new Lap(baseTime + 180_000_000_000L,   3, 0, "Ramp Up",         false),
    new Lap(baseTime + 240_000_000_000L,   4, 0, "Steady State 2",  false),
    new Lap(baseTime + 360_000_000_000L,   5, 0, "Cooldown",        false),
};

foreach (var segment in segments)
    session.LapCollection.Add(segment);

Console.WriteLine($"Session {sessionKey}: {session.LapCollection.Count} segments\n");
Console.WriteLine($"{"#",3} {"Name",-20} {"Start (ns)",20} {"Offset (s)",12}");
Console.WriteLine(new string('─', 58));

foreach (var lap in session.LapCollection)
{
    var offsetS = (lap.StartTime - baseTime) / 1_000_000_000.0;
    Console.WriteLine($"{lap.Number,3} {lap.Name,-20} {lap.StartTime,20} {offsetS,12:F0}");
}
