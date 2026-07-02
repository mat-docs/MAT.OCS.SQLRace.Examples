// 
// SQL Race Example: SQL Race Example: Multi-Rate Alignment
// 
// Demonstrates: Reading two parameters at different native frequencies and
//               aligning them to a common timestamp array using GetData
// Prerequisites: MESL.SQLRace.API, a session with two parameters at different rates
// Expected output: Aligned values for both parameters at uniform timestamps
// Related: read-subsampled-data.cs, timestamp-array-construction.cs
// 

using MAT.OCS.Core;
using MESL.SqlRace.Domain;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <session-guid>");
    return;
}

SessionKey sessionKey;
try { sessionKey = SessionKey.Parse(args[0]); }
catch (Exception ex) { Console.WriteLine($"ERROR: '{args[0]}' is not a valid GUID. {ex.Message}"); return; }

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

// --- Two parameters (potentially at different native rates) ---
var paramA = "Temperature:Sensors";
var paramB = "Pressure:Inlet";

var available = session.Parameters.Select(p => p.Identifier).ToHashSet();
var missing = new[] { paramA, paramB }.Where(p => !available.Contains(p)).ToList();
if (missing.Count > 0)
{
    Console.WriteLine($"ERROR: Parameter(s) not in session: {string.Join(", ", missing)}");
    Console.WriteLine($"Available: {string.Join(", ", available.Take(10))}");
    return;
}

using var pdaA = session.CreateParameterDataAccess(paramA);
using var pdaB = session.CreateParameterDataAccess(paramB);

// --- Build a common timestamp array at 10 Hz ---
var alignedInterval = 100_000_000L; // 100ms
var start = session.StartTime;
var end = session.EndTime;
var count = (int)((end - start) / alignedInterval);

if (count <= 0)
{
    Console.WriteLine("Session too short for alignment at 10 Hz.");
    return;
}

var timestamps = new long[count];
for (var i = 0; i < count; i++)
    timestamps[i] = start + i * alignedInterval;

// --- Read both parameters at the common timestamps ---
var samplesA = pdaA.GetData(timestamps);
var samplesB = pdaB.GetData(timestamps);

Console.WriteLine($"Aligned {paramA} and {paramB} to {count} points at 10 Hz");
Console.WriteLine($"{"Time (ms)",10}  {paramA,18}  {paramB,18}");
Console.WriteLine(new string('-', 50));

for (var i = 0; i < Math.Min(count, 15); i++)
{
    var ms = (timestamps[i] - start) / 1_000_000.0;
    Console.WriteLine($"{ms,10:F1}  {samplesA.Data[i],18:F4}  {samplesB.Data[i],18:F4}");
}

if (count > 15)
    Console.WriteLine($"  ... and {count - 15} more rows");
}
