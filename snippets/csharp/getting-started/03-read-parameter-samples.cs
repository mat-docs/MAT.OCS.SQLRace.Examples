// 
// SQL Race Example: SQL Race Example: Read Parameter Samples
// 
// Demonstrates: Creating a ParameterDataAccess and reading samples between
//               start and end time, printing timestamped values
// Prerequisites: MESL.SQLRace.API NuGet package, a session with data
//                (run 04-create-session-write-data.cs first)
// Expected output: Timestamped parameter values
// Related: 04-create-session-write-data.cs, data-access/read-samples-between.cs
// 

using MAT.OCS.Core;
using MESL.SqlRace.Domain;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

// --- Load a session (pass file path or session key as argument) ---
var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";
var parameterIdentifier = "Temperature:Sensors";

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <session-guid>");
    Console.WriteLine("Tip: Run 04-create-session-write-data.cs first to create a session with data.");
    return;
}

SessionKey sessionKey;
try { sessionKey = SessionKey.Parse(args[0]); }
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
    Console.WriteLine($"Connection: {connectionString}");
    Console.WriteLine("Tip: Run 04-create-session-write-data.cs first and re-run with the printed GUID.");
    return;
}
using (clientSession)
{
    var session = clientSession.Session;

// --- Check parameter exists ---
var parameter = session.Parameters
    .FirstOrDefault(p => p.Identifier == parameterIdentifier);

if (parameter is null)
{
    Console.WriteLine($"Parameter '{parameterIdentifier}' not found in session.");
    Console.WriteLine($"Available parameters: {string.Join(", ", session.Parameters.Select(p => p.Identifier))}");
    return;
}

// --- Create a PDA and read samples ---
using var pda = session.CreateParameterDataAccess(parameterIdentifier);
var samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

Console.WriteLine($"Parameter: {parameterIdentifier}");
Console.WriteLine($"Samples:   {samples.SampleCount}");
Console.WriteLine($"{"Timestamp (ns)",20}  {"Value",12}");
Console.WriteLine(new string('-', 34));

for (var i = 0; i < Math.Min(samples.SampleCount, 20); i++)
{
    Console.WriteLine($"{samples.Timestamp[i],20}  {samples.Data[i],12:F4}");
}

if (samples.SampleCount > 20)
    Console.WriteLine($"  ... and {samples.SampleCount - 20} more samples");
}
