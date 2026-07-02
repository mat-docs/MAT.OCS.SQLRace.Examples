// ─────────────────────────────────────────────────────────────
// SQL Race Example: Introspect Session Configuration
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Enumerating a session's parameters, channels, conversions,
//               and groups to understand its configuration
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: A session with configured parameters (run 04-create-session-write-data.cs first)
// Output: Summary tables of all configuration objects
//
// Related: snippets/csharp/configuration/create-parameter-config.cs
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
    Console.WriteLine("Tip: Run 04-create-session-write-data.cs first.");
    return;
}

var sessionKey = SessionKey.Parse(args[0]);
var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.Load(sessionKey, connectionString);
var session = clientSession.Session;

Console.WriteLine($"Session: {session.Identifier} ({sessionKey})\n");

// --- Parameters ---
Console.WriteLine($"=== Parameters ({session.Parameters.Count}) ===");
Console.WriteLine($"{"Identifier",-30} {"Conversion",-25} {"AppGroup",-15}");
Console.WriteLine(new string('─', 72));

foreach (var param in session.Parameters)
{
    Console.WriteLine($"{param.Identifier,-30} {param.ConversionFunctionName,-25} {param.ApplicationName,-15}");
}

// --- Application Groups and Parameter Groups ---
Console.WriteLine($"\n=== Application Groups ===");
var appGroups = session.Parameters.Select(p => p.ApplicationName).Distinct();
foreach (var group in appGroups)
    Console.WriteLine($"  {group}");

Console.WriteLine($"\n=== Parameter Groups ===");
var paramGroups = session.Parameters.SelectMany(p => p.GroupIdentifiers ?? Enumerable.Empty<string>()).Distinct();
foreach (var group in paramGroups)
    Console.WriteLine($"  {group}");
