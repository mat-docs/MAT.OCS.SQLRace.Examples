// 
// SQL Race Example: SQL Race Example: Read Subsampled Data
// 
// Demonstrates: Using GetData with a custom timestamp array to downsample
//               high-frequency data to a lower rate
// Prerequisites: MESL.SQLRace.API, a session with data (e.g., 100 Hz)
// Expected output: Values at the requested timestamps (e.g., every 100ms = 10 Hz)
// Related: timestamp-array-construction.cs, multi-rate-alignment.cs
// 

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

// --- Build a custom timestamp array at 10 Hz (100ms intervals) ---
var desiredInterval = 100_000_000L; // 100ms in nanoseconds (= 10 Hz)
var start = session.StartTime;
var end = session.EndTime;
var sampleCount = (int)((end - start) / desiredInterval);

if (sampleCount <= 0)
{
    Console.WriteLine("Session is too short for subsampling at 10 Hz.");
    return;
}

var timestamps = new long[sampleCount];
for (var i = 0; i < sampleCount; i++)
    timestamps[i] = start + i * desiredInterval;

// --- Read data at the custom timestamps ---
var samples = pda.GetData(timestamps);

Console.WriteLine($"Parameter:       {paramId}");
Console.WriteLine($"Original range:  {start} – {end} ns");
Console.WriteLine($"Subsampled to:   {sampleCount} points at 10 Hz");
Console.WriteLine();

for (var i = 0; i < Math.Min(samples.SampleCount, 15); i++)
{
    var relativeMs = (samples.Timestamp[i] - start) / 1_000_000.0;
    Console.WriteLine($"  t+{relativeMs,8:F1} ms  {samples.Data[i]:F4}");
}

if (samples.SampleCount > 15)
    Console.WriteLine($"  ... and {samples.SampleCount - 15} more");
