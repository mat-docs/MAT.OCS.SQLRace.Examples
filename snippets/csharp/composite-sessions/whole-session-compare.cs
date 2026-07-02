// ─────────────────────────────────────────────────────────────
// SQL Race Example: Whole Session Compare
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Using CompositeSessionContainer with WholeSession compare mode
//               to align and read data from two sessions side by side
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own sessions)
// Output: Aligned data from both sessions printed side by side
//
// Related: snippets/csharp/composite-sessions/append-multiple-sessions.cs
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
var baseTime = 36_000_000_000_000L;
var interval = new Frequency(10, FrequencyUnit.Hz).ToInterval();

// --- Helper: create a test session ---
IClientSession CreateSession(string name, Func<int, double> valueFunc)
{
    var key = SessionKey.NewKey();
    var client = sessionManager.CreateSession(connectionString, key, name, DateTime.UtcNow, "Example");
    var session = client.Session;

    var config = session.CreateConfiguration();
    config.AddConversion(RationalConversion.CreateSimple1To1Conversion("degC_conv", "degC", "%5.1f"));
    config.AddChannel(new Channel(1, "Temp", interval, DataType.Double64Bit, ChannelDataSourceType.Periodic, "Temp", false));
    config.AddParameterGroup(new ParameterGroup("Thermal", "Thermal"));
    config.AddGroup(new ApplicationGroup("Sensors", "Sensors", new List<string> { "Thermal" }));
    config.AddParameter(new Parameter("Temperature:Sensors", "Temp", "", 500, 0, 500, 0, 0, 0u, 0u, "degC_conv", new List<string> { "Thermal" }, 1u, "Sensors"));
    config.Commit();

    for (var i = 0; i < 20; i++)
        session.AddChannelData(1, baseTime + i * interval, 1, BitConverter.GetBytes(valueFunc(i)));

    return client;
}

using var baseline = CreateSession("Baseline Run", i => 20.0 + i * 0.3);
using var testRun = CreateSession("Test Run", i => 22.0 + i * 0.4);

// --- Create composite sessions for comparison (Overlay mode) ---
var compositeA = new CompositeSession(CompositeSessionKey.NewKey(), "Baseline",
    baseline, CompositeSessionMode.Overlay);

var compositeB = new CompositeSession(CompositeSessionKey.NewKey(), "Test",
    testRun, CompositeSessionMode.Overlay);

var containerKey = new CompositeSessionContainerKey(Guid.NewGuid());
var container = new CompositeSessionContainer(containerKey, "Comparison",
    new List<ICompositeSession> { compositeA, compositeB });

Console.WriteLine("Whole-session comparison: Baseline vs Test Run\n");
Console.WriteLine($"{"Time (ms)",10}  {"Baseline",10}  {"Test Run",10}  {"Delta",10}");
Console.WriteLine(new string('─', 44));

// --- Read from both and compare ---
using var pdaA = compositeA.CreateParameterDataAccess("Temperature:Sensors");
using var pdaB = compositeB.CreateParameterDataAccess("Temperature:Sensors");
var samplesA = pdaA.GetSamples(1000, compositeA.StartTime, compositeA.EndTime);
var samplesB = pdaB.GetSamples(1000, compositeB.StartTime, compositeB.EndTime);

var count = Math.Min(samplesA.SampleCount, samplesB.SampleCount);
for (var i = 0; i < Math.Min(count, 15); i++)
{
    var ms = (samplesA.Timestamp[i] - baseTime) / 1_000_000.0;
    var delta = samplesB.Data[i] - samplesA.Data[i];
    Console.WriteLine($"{ms,10:F1}  {samplesA.Data[i],10:F2}  {samplesB.Data[i],10:F2}  {delta,+10:F2}");
}
