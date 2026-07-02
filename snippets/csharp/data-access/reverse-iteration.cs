// 
// SQL Race Example: SQL Race Example: Reverse Iteration
// 
// Demonstrates: Using GoTo and GetNextSamples with StepDirection.Reverse to
//               read data backwards from the end of a session
// Prerequisites: MESL.SQLRace.API, a session with data
// Expected output: Samples printed in reverse chronological order
// Related: read-samples-between.cs, bulk-parameter-read.cs
// 

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.Enumerators;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <session-guid> [parameter-identifier]");
    return;
}

SessionKey sessionKey;
try { sessionKey = SessionKey.Parse(args[0]); }
catch (Exception ex) { Console.WriteLine($"ERROR: '{args[0]}' is not a valid GUID. {ex.Message}"); return; }
var paramId = args.Length > 1 ? args[1] : "Temperature:Sensors";

var sessionManager = SessionManager.CreateSessionManager();
IClientSession clientSession;
try { clientSession = sessionManager.Load(sessionKey, connectionString); }
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Could not load session '{sessionKey}': {ex.Message}");
    return;
}
using (clientSession)
{
var session = clientSession.Session;

if (!session.Parameters.Any(p => p.Identifier == paramId))
{
    Console.WriteLine($"ERROR: Parameter '{paramId}' not found in session.");
    Console.WriteLine($"Available: {string.Join(", ", session.Parameters.Take(10).Select(p => p.Identifier))}");
    return;
}

using var pda = session.CreateParameterDataAccess(paramId);

// --- Position the PDA at the end of the session ---
pda.GoTo(session.EndTime);

// --- Read 10 samples backwards ---
var samples = pda.GetNextSamples(10, StepDirection.Reverse);

Console.WriteLine($"Parameter: {paramId}");
Console.WriteLine($"Last {samples.SampleCount} samples (reverse order):");
Console.WriteLine($"{"Timestamp (ns)",20}  {"Value",12}");
Console.WriteLine(new string('-', 34));

for (var i = 0; i < samples.SampleCount; i++)
{
    Console.WriteLine($"{samples.Timestamp[i],20}  {samples.Data[i],12:F4}");
}
}
