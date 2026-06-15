// ─────────────────────────────────────────────────────────────
// SQL Race Example: Parameter Unit Resolution
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Resolving the engineering unit for a parameter via its conversion
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: A session with configured parameters (run 04-create-session-write-data.cs first)
// Output: Parameter details with resolved units
//
// Related: snippets/csharp/configuration/conversion-types.cs
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
    Console.WriteLine("Tip: Run 04-create-session-write-data.cs first to create a session.");
    return;
}

SessionKey sessionKey;
try
{
    sessionKey = SessionKey.Parse(args[0]);
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: '{args[0]}' is not a valid session GUID. {ex.Message}");
    return;
}

var sessionManager = SessionManager.CreateSessionManager();
IClientSession clientSession;
try { clientSession = sessionManager.Load(sessionKey, connectionString); }
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Could not load session '{sessionKey}': {ex.Message}");
    Console.WriteLine("Tip: Run 04-create-session-write-data.cs first, then pass the printed GUID.");
    return;
}
using (clientSession)
{
var session = clientSession.Session;

Console.WriteLine($"Session: {session.Identifier} ({session.Parameters.Count} parameters)\n");
Console.WriteLine($"{"Identifier",-30} {"Conversion",-25} {"Unit",-10} {"Format",-10} {"Range"}");
Console.WriteLine(new string('─', 95));

foreach (var param in session.Parameters)
{
    // --- Resolve unit from the conversion ---
    var conversionName = param.ConversionFunctionName;
    var unit = "(unknown)";
    var format = "";

    try
    {
        var conversion = session.GetConversion(conversionName);
        if (conversion is not null)
        {
            unit = conversion.Units ?? "(none)";
            format = conversion.FormatString ?? "";
        }
    }
    catch (Exception ex)
    {
        unit = "(error)";
        Console.Error.WriteLine($"WARNING: Failed to resolve conversion '{conversionName}' for {param.Identifier}: {ex.Message}");
    }

    Console.WriteLine($"{param.Identifier,-30} {conversionName,-25} {unit,-10} {format,-10} [{param.MinimumValue:F1}, {param.MaximumValue:F1}]");
}
}
