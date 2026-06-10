// 
// SQL Race Example: SQL Race Example: Query Sessions by Metadata
// 
// Demonstrates: Using QueryManager with ScalarFilter to find sessions in a
//               database, printing matching session summaries
// Prerequisites: MESL.SQLRace.API NuGet package, a database with sessions
//                (run 04-create-session-write-data.cs a few times first)
// Expected output: List of matching sessions with name and date
// Related: session-management/query-with-composite-filter.cs
// 

using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Query;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";

// --- Create a query manager ---
var queryManager = QueryManager.CreateQueryManager(connectionString);

// --- Option 1: Query all sessions (no filter) ---
Console.WriteLine("=== All sessions ===");
var allSessions = queryManager.ExecuteQuery();

var count = 0;
foreach (var summary in allSessions)
{
    Console.WriteLine($"  {summary.Key}  {summary.Identifier,-30}  {summary.TimeOfRecording:u}");
    count++;
    if (count >= 20) break;
}

Console.WriteLine($"Total: {count} session(s)");

// --- Option 2: Query with a filter ---
Console.WriteLine();
Console.WriteLine("=== Sessions matching 'Example' ===");
var filter = new ScalarFilter("Identifier", MatchingRule.Contains, "Example", ignoreCase: true);
queryManager.Filter = filter;

var filtered = queryManager.ExecuteQuery();
var filteredCount = 0;

foreach (var summary in filtered)
{
    Console.WriteLine($"  {summary.Key}  {summary.Identifier,-30}  {summary.TimeOfRecording:u}");
    filteredCount++;
}

if (filteredCount == 0)
{
    Console.WriteLine("  No sessions found matching 'Example'.");
    Console.WriteLine("  Tip: Run 04-create-session-write-data.cs to create some sessions.");
}
else
{
    Console.WriteLine($"Found: {filteredCount} session(s)");
}
