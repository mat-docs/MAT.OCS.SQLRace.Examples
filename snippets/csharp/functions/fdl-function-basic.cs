// ─────────────────────────────────────────────────────────────
// SQL Race Example: FDL Function (Basic)
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Creating an FDL function (Celsius to Fahrenheit), building it,
//               and consuming the output via a PDA
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8, a session with data
// Input: A session with a Temperature:Sensors parameter
// Output: Converted values read from the FDL function output
//
// Related: snippets/csharp/functions/dotnet-function-basic.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Functions;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <session-guid>");
    Console.WriteLine("Tip: Run 04-create-session-write-data.cs first to create a session.");
    return;
}

var sessionKey = SessionKey.Parse(args[0]);
var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.Load(sessionKey, connectionString);
var session = clientSession.Session;

// --- Create the FDL function: Celsius → Fahrenheit ---
var functionManager = FunctionManagerFactory.Create();
var fdlCode = "return ($Temperature:Sensors * 1.8 + 32)";
var outputIdentifier = "TemperatureFahrenheit:Calculated";

var funcDef = functionManager.CreateFunctionDefinition(
    FdlFunctionConstants.FdlFunctionTypeId,
    outputIdentifier,
    "Temperature in Fahrenheit");

funcDef.SetFdlCode(fdlCode);
funcDef.SetOutputParameter(outputIdentifier, "degF", "%5.1f", -40, 1000);

var errors = functionManager.Build(funcDef, session);

if (errors is not null && errors.Any())
{
    Console.WriteLine("FDL build errors:");
    foreach (var error in errors)
        Console.WriteLine($"  {error}");
    return;
}

Console.WriteLine($"FDL function built: {fdlCode}");

// --- Read the function output ---
using var pda = session.CreateParameterDataAccess(outputIdentifier);
var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

Console.WriteLine($"Output: {samples.SampleCount} samples of {outputIdentifier}\n");

for (var i = 0; i < Math.Min(samples.SampleCount, 10); i++)
{
    Console.WriteLine($"  {samples.Timestamp[i],20} ns  {samples.Data[i]:F1} degF");
}

if (samples.SampleCount > 10)
    Console.WriteLine($"  ... and {samples.SampleCount - 10} more");
