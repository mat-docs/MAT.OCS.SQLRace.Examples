// ─────────────────────────────────────────────────────────────
// SQL Race Example: Append Multiple Sessions
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Creating a CompositeSession from two sequential sessions and
//               reading data that spans both sessions seamlessly
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own sessions)
// Output: Samples read across the composite session boundary
//
// Related: snippets/csharp/composite-sessions/whole-session-compare.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;

Core.LicenceProgramName = "SQLRace";
Core.Initialize();

var connectionString = $"DbEngine=SQLite;Data Source={Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2")};";
var sessionManager = SessionManager.CreateSessionManager();

// --- Helper: create a session with Temperature data ---
IClientSession CreateTestSession(string name, long startTime, int sampleCount)
{
    var key = SessionKey.NewKey();
    var client = sessionManager.CreateSession(connectionString, key, name, DateTime.UtcNow, "Example");
    if (client is null)
        throw new InvalidOperationException($"Failed to create session '{name}' at '{connectionString}'. Check the file is writable and not locked.");
    var session = client.Session;

    var config = session.CreateConfiguration();
    var interval = new Frequency(10, FrequencyUnit.Hz).ToInterval();
    config.AddConversion(RationalConversion.CreateSimple1To1Conversion("degC_conv", "degC", "%5.1f"));
    config.AddChannel(new Channel(1, "Temperature", interval, DataType.Double64Bit, ChannelDataSourceType.Periodic, "Temperature", false));
    config.AddParameterGroup(new ParameterGroup("Thermal", "Thermal"));
    config.AddGroup(new ApplicationGroup("Sensors", "Sensors", new List<string> { "Thermal" }));
    config.AddParameter(new Parameter("Temperature:Sensors", "Temperature", "", 500, 0, 500, 0, 0, 0u, 0u, "degC_conv", new List<string> { "Thermal" }, 1u, "Sensors"));
    config.Commit();

    for (var i = 0; i < sampleCount; i++)
    {
        var ts = startTime + i * interval;
        session.AddChannelData(1, ts, 1, BitConverter.GetBytes(20.0 + i * 0.5));
    }

    Console.WriteLine($"  {name}: {sampleCount} samples from {startTime} ns");
    return client;
}

// --- Create two sequential sessions ---
Console.WriteLine("Creating sessions:");
var baseTime = 36_000_000_000_000L;
using var session1 = CreateTestSession("Run Part 1", baseTime, 50);
using var session2 = CreateTestSession("Run Part 2", baseTime + 5_000_000_000L, 50);

// --- Build a composite session in Append mode ---
var compositeKey = CompositeSessionKey.NewKey();
var composite = new CompositeSession(compositeKey, "Appended Run",
    new[] { session1, session2 }, CompositeSessionMode.Append);

Console.WriteLine($"\nComposite: {composite.StartTime} – {composite.EndTime} ns");

// --- Read across the boundary ---
using var pda = composite.CreateParameterDataAccess("Temperature:Sensors");
var samples = pda.GetSamples(1000, composite.StartTime, composite.EndTime);

Console.WriteLine($"Read {samples.SampleCount} samples across both sessions");

for (var i = 0; i < Math.Min(samples.SampleCount, 10); i++)
{
    var ms = (samples.Timestamp[i] - baseTime) / 1_000_000.0;
    Console.WriteLine($"  t+{ms,8:F1} ms  {samples.Data[i]:F1} degC");
}

if (samples.SampleCount > 10)
    Console.WriteLine($"  ... and {samples.SampleCount - 10} more");
