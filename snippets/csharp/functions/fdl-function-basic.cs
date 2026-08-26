// ─────────────────────────────────────────────────────────────
// SQL Race Example: FDL Function (Basic)
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Building an FDL function (Celsius to Fahrenheit) and reading
//               its output parameter back through a PDA
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8, the "ATLAS Functions"
//                licence option, a session with a Temperature:Sensors parameter
// Input: A session GUID
// Output: Converted values read from the FDL function's output parameter
//
// Related: snippets/csharp/functions/dotnet-function-basic.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.Fdl;
using MESL.SqlRace.Functions.Interfaces.Enums;

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

// --- Define the FDL function: Celsius → Fahrenheit ---
// An output parameter's identifier is "<Name>:<ApplicationName>", so this
// function publishes "TemperatureFahrenheit:Functions".
const string FunctionName = "TemperatureFahrenheit";
const string OutputGroup = "Functions";
const string OutputIdentifier = $"{FunctionName}:{OutputGroup}";
const string FdlCode = "return ($Temperature:Sensors * 1.8 + 32);";

var functionManager = FunctionManagerFactory.Create();

var definition = functionManager.CreateFunctionDefinition(FdlFunctionConstants.UniqueId);
definition.Name = FunctionName;
definition.FunctionMode = FunctionMode.Instantaneous;

// The FDL source lives on the implementation definition, which the function
// manager creates alongside the definition itself.
var implementation = (IFdlFunctionImplementationDefinition)definition.ImplementationDefinition;
implementation.FunctionCode = FdlCode;

// FDL functions require exactly one output parameter - the value they return.
definition.OutputParameterDefinitions.Add(
    FunctionOutputParameterDefinition.Create(FunctionName, OutputGroup, "Temperature in Fahrenheit")
        .Units("degF")
        .FormatOverride("%5.1f")
        .MinimumValue("-40.0")
        .MaximumValue("1000.0"));

var buildResults = functionManager.Build(definition);

if (buildResults.Errors.Count > 0)
{
    Console.WriteLine("FDL build errors:");
    foreach (var error in buildResults.Errors)
        Console.WriteLine($"  {error.ErrorText}");
    return;
}

Console.WriteLine($"FDL function built: {FdlCode}");

// --- Read the function output ---
// Load the session *after* the build: SQL Race adds a built function's output
// parameters to sessions as they load.
var sessionManager = SessionManager.CreateSessionManager();
using var clientSession = sessionManager.Load(sessionKey, connectionString);
var session = clientSession.Session;

using var pda = session.CreateParameterDataAccess(OutputIdentifier);
var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

Console.WriteLine($"Output: {samples.SampleCount} samples of {OutputIdentifier}\n");

for (var i = 0; i < Math.Min(samples.SampleCount, 10); i++)
{
    Console.WriteLine($"  {samples.Timestamp[i],20} ns  {samples.Data[i]:F1} degF");
}

if (samples.SampleCount > 10)
    Console.WriteLine($"  ... and {samples.SampleCount - 10} more");
