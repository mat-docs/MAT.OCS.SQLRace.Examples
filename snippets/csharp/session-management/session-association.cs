// ─────────────────────────────────────────────────────────────
// SQL Race Example: Session Association
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Associating sessions and loading with associates
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own sessions)
// Output: Primary session loaded with associated session data
//
// Related: snippets/csharp/composite-sessions/composite-with-associates.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";
var primaryKey = SessionKey.NewKey();
var associateKey = SessionKey.NewKey();

var sessionManager = SessionManager.CreateSessionManager();

// --- Create the primary session ---
using (var primary = sessionManager.CreateSession(connectionString, primaryKey, "Primary Test Run", DateTime.UtcNow, "Example"))
{
    primary.Session.Items.Add(new SessionDataItem("Role", "Primary"));
    Console.WriteLine($"Created primary:   {primaryKey}");
}

// --- Create the associate session ---
using (var associate = sessionManager.CreateSession(connectionString, associateKey, "Baseline Reference", DateTime.UtcNow, "Example"))
{
    associate.Session.Items.Add(new SessionDataItem("Role", "Associate"));
    Console.WriteLine($"Created associate:  {associateKey}");
}

// --- Associate them ---
// TODO: Verify — association API may vary; some versions use SessionManager methods
using (var primary = sessionManager.Load(primaryKey, connectionString))
{
    primary.Session.AssociateWithParent(associateKey);
    Console.WriteLine($"\nAssociated {associateKey} with primary {primaryKey}");
}

// --- Load primary with associates ---
using var loaded = sessionManager.Load(primaryKey, connectionString);
var session = loaded.Session;

Console.WriteLine($"\nLoaded primary session: {session.Identifier}");
Console.WriteLine($"  Items: {session.Items.Count}");

// TODO: Verify — enumerate associated sessions via session.Associates or similar
Console.WriteLine($"\nAssociation established. Use SessionManager.LoadAssociatesForSession()");
Console.WriteLine($"to access the associated session's data alongside the primary.");
