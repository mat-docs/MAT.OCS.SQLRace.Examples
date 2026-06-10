// ─────────────────────────────────────────────────────────────
// SQL Race Example: Query with Composite Filter
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: QueryManager with CompositeFilter combining multiple ScalarFilter conditions
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: A SQLite database with sessions (run 04-create-session-write-data.cs first)
// Output: Matching session identifiers and recording times
//
// Related: snippets/csharp/getting-started/05-query-sessions-by-metadata.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Query;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";

var queryManager = QueryManager.CreateQueryManager(connectionString);

// --- Build a composite filter: session type contains "Example" AND recorded after a date ---
var composite = new CompositeFilter(CombineType.AND);
composite.Add(new ScalarFilter("Identifier", MatchingRule.Contains, "Example", ignoreCase: true));
composite.Add(new ScalarFilter("TimeOfRecording", MatchingRule.GreaterThan, DateTime.UtcNow.AddDays(-30).ToString("O"), ignoreCase: true));

queryManager.Filter = composite;

Console.WriteLine("Filter: Identifier contains 'Example' AND recorded in last 30 days");
Console.WriteLine($"{"Session Key",-40} {"Identifier",-30} {"Recorded",22}");
Console.WriteLine(new string('─', 94));

var count = 0;
foreach (var summary in queryManager.ExecuteQuery())
{
    Console.WriteLine($"{summary.Key,-40} {summary.Identifier,-30} {summary.TimeOfRecording:u}");
    count++;
}

if (count == 0)
{
    Console.WriteLine("No sessions matched. Create some sessions first with 04-create-session-write-data.cs.");
}
else
{
    Console.WriteLine($"\n{count} session(s) matched.");
}
