// ─────────────────────────────────────────────────────────────
// SQL Race Example: Bulk Parameter Read
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Reading multiple parameters from a session efficiently by
//               managing PDA lifecycle correctly (create, read, dispose)
// Prerequisites: MESL.SQLRace.API, a session with multiple parameters
// Expected output: Summary of each parameter's data range and sample count
// Related: multi-rate-alignment.cs, data-status-filtering.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <session-guid>");
    return;
}

SessionKey sessionKey;
try { sessionKey = SessionKey.Parse(args[0]); }
catch (Exception ex) { Console.WriteLine($"ERROR: '{args[0]}' is not a valid GUID. {ex.Message}"); return; }

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

// --- Get all parameter identifiers ---
var identifiers = session.Parameters.Select(p => p.Identifier).ToList();

if (identifiers.Count == 0)
{
    Console.WriteLine("Session has no parameters.");
    return;
}

Console.WriteLine($"Session: {sessionKey}");
Console.WriteLine($"Reading {identifiers.Count} parameter(s)...");
Console.WriteLine($"{"Parameter",-35} {"Samples",8} {"Min",12} {"Max",12}");
Console.WriteLine(new string('-', 70));

// --- Read each parameter, tracking PDA lifecycle ---
foreach (var identifier in identifiers)
{
    using var pda = session.CreateParameterDataAccess(identifier);
    var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

    if (samples.SampleCount == 0)
    {
        Console.WriteLine($"{identifier,-35} {"(empty)",8}");
        continue;
    }

    var min = double.MaxValue;
    var max = double.MinValue;
    for (var i = 0; i < samples.SampleCount; i++)
    {
        if (samples.Data[i] < min) min = samples.Data[i];
        if (samples.Data[i] > max) max = samples.Data[i];
    }

    Console.WriteLine($"{identifier,-35} {samples.SampleCount,8} {min,12:F4} {max,12:F4}");
}
}
