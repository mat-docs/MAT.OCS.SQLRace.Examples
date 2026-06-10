// ─────────────────────────────────────────────────────────────
// SQL Race Example: Session Metadata CRUD
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Creating and reading SessionDataItem entries on a session
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own session)
// Output: Session data items written and read back
//
// Related: snippets/csharp/session-management/query-with-composite-filter.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";
var sessionKey = SessionKey.NewKey();

// --- Create a session and add metadata items ---
var sessionManager = SessionManager.CreateSessionManager();
using (var client = sessionManager.CreateSession(connectionString, sessionKey, "Metadata Demo", DateTime.UtcNow, "Example"))
{
    if (client is null)
    {
        Console.WriteLine($"ERROR: Failed to create session at '{connectionString}'.");
        return;
    }
    var session = client.Session;

    session.Items.Add(new SessionDataItem("Facility", "Wind Tunnel A"));
    session.Items.Add(new SessionDataItem("Operator", "Test Engineer 1"));
    session.Items.Add(new SessionDataItem("TestObjective", "Thermal characterisation at 80% load"));
    session.Items.Add(new SessionDataItem("AmbientTemp", "22.5"));
    session.Items.Add(new SessionDataItem("RunNumber", "42"));

    Console.WriteLine($"Created session {sessionKey} with {session.Items.Count} metadata items");
}

// --- Reload and read back ---
IClientSession reloaded;
try { reloaded = sessionManager.Load(sessionKey, connectionString); }
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Could not reload session '{sessionKey}': {ex.Message}");
    return;
}
using (reloaded)
{
var reloadedSession = reloaded.Session;

Console.WriteLine($"\nReloaded session: {reloadedSession.Items.Count} metadata items");
Console.WriteLine($"{"Key",-20} {"Value"}");
Console.WriteLine(new string('─', 60));

foreach (var item in reloadedSession.Items)
{
    Console.WriteLine($"{item.Name,-20} {item.Value}");
}
}
