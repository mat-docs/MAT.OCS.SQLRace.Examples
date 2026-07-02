// ─────────────────────────────────────────────────────────────
// SQL Race Example: Marker Management
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Creating Marker objects with time ranges and MarkerLabel annotations
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own session)
// Output: Markers with labels printed to console
//
// Related: snippets/csharp/session-management/lap-and-segment-management.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";
var sessionKey = SessionKey.NewKey();

var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.CreateSession(connectionString, sessionKey, "Marker Demo", DateTime.UtcNow, "Example");
var session = clientSession.Session;

var baseTime = 36_000_000_000_000L; // 10:00:00.000

// --- Add markers representing annotated time regions ---
var anomaly = new Marker(baseTime, baseTime + 2_000_000_000L, "Anomaly", "Anomaly", "Unexpected temperature spike detected", Guid.NewGuid().ToString());
var roi = new Marker(baseTime + 5_000_000_000L, baseTime + 8_000_000_000L, "RegionOfInterest", "RegionOfInterest", "Steady-state operating window", Guid.NewGuid().ToString());
var transient = new Marker(baseTime + 10_000_000_000L, baseTime + 12_000_000_000L, "Transient", "Transient", "Load step response period", Guid.NewGuid().ToString());

session.Markers.Add(anomaly);
session.Markers.Add(roi);
session.Markers.Add(transient);

Console.WriteLine($"Session {sessionKey}: {session.Markers.Count} markers added\n");

foreach (var marker in session.Markers)
{
    var startNs = marker.StartTimestamp ?? 0;
    var endNs = marker.EndTimestamp ?? 0;
    var durationS = (endNs - startNs) / 1_000_000_000.0;
    Console.WriteLine($"  [{marker.Label}] {startNs}–{endNs} ns ({durationS:F1}s)");
    Console.WriteLine($"    Type: {marker.MarkerType}");
    Console.WriteLine($"    Description: {marker.Description}");
    Console.WriteLine();
}
