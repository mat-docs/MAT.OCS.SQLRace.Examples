// ─────────────────────────────────────────────────────────────
// SQL Race Example: Data Status Filtering
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Checking DataStatusType on returned samples to identify and
//               handle missing or invalid data points
// Prerequisites: MESL.SQLRace.API, a session with data
// Expected output: Per-sample status breakdown and filtered valid-only values
// Related: read-samples-between.cs, bulk-parameter-read.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

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

var sessionKey = SessionKey.Parse(args[0]);
var paramId = args.Length > 1 ? args[1] : "Temperature:Sensors";

var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.Load(sessionKey, connectionString);
var session = clientSession.Session;

using var pda = session.CreateParameterDataAccess(paramId);
var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

// --- Count samples by status ---
var statusCounts = new Dictionary<DataStatusType, int>();
for (var i = 0; i < samples.SampleCount; i++)
{
    var status = samples.DataStatus[i];
    statusCounts.TryGetValue(status, out var count);
    statusCounts[status] = count + 1;
}

Console.WriteLine($"Parameter: {paramId}");
Console.WriteLine($"Total samples: {samples.SampleCount}");
Console.WriteLine();
Console.WriteLine("Status breakdown:");
foreach (var (status, count) in statusCounts)
{
    var pct = 100.0 * count / samples.SampleCount;
    Console.WriteLine($"  {status,-15} {count,6} ({pct:F1}%)");
}

// --- Filter to valid samples only ---
Console.WriteLine();
Console.WriteLine("Valid samples (DataStatusType.Sample):");

var validCount = 0;
for (var i = 0; i < samples.SampleCount; i++)
{
    if (samples.DataStatus[i] != DataStatusType.Sample)
        continue;

    if (validCount < 10)
    {
        var ms = (samples.Timestamp[i] - session.StartTime) / 1_000_000.0;
        Console.WriteLine($"  t+{ms,8:F3} ms  {samples.Data[i]:F4}");
    }
    validCount++;
}

Console.WriteLine($"Total valid: {validCount} / {samples.SampleCount}");
