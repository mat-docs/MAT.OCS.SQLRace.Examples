// ─────────────────────────────────────────────────────────────
// SQL Race Example: Composite with Associates
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Creating a composite session that includes a primary session
//               and its associated session, reading data from both
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: None (creates its own sessions)
// Output: Data read from both primary and associated sessions via composite
//
// Related: snippets/csharp/session-management/session-association.cs
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

// --- Helper: create a session with one parameter ---
IClientSession CreateSession(string name, string paramId, uint channelId, Func<int, double> valueFunc)
{
    var key = SessionKey.NewKey();
    var client = sessionManager.CreateSession(connectionString, key, name, DateTime.UtcNow, "Example");
    var session = client.Session;

    var config = session.CreateConfiguration();
    config.AddConversion(RationalConversion.CreateSimple1To1Conversion($"conv_{channelId}", "units", "%5.2f"));
    config.AddChannel(new Channel((uint)channelId, paramId.Split(':')[0], interval, DataType.Double64Bit, ChannelDataSourceType.Periodic, paramId.Split(':')[0], false));
    config.AddParameterGroup(new ParameterGroup("Default", "Default"));
    config.AddGroup(new ApplicationGroup("Sensors", "Sensors", new List<string> { "Default" }));
    config.AddParameter(new Parameter(paramId, paramId.Split(':')[0], "", 1000, 0, 1000, 0, 0, 0u, 0u, $"conv_{channelId}", new List<string> { "Default" }, (uint)channelId, "Sensors"));
    config.Commit();

    for (var i = 0; i < 20; i++)
        session.AddChannelData(channelId, baseTime + i * interval, 1, BitConverter.GetBytes(valueFunc(i)));

    return client;
}

// --- Primary session has Temperature, associate has Pressure ---
using var primary = CreateSession("Primary Run", "Temperature:Sensors", 1u, i => 20.0 + i * 0.5);
using var associate = CreateSession("Reference Data", "Pressure:Inlet", 2u, i => 2.0 + i * 0.1);

Console.WriteLine($"Primary:   {primary.Session.Key} ({primary.Session.Parameters.Count} param)");
Console.WriteLine($"Associate: {associate.Session.Key} ({associate.Session.Parameters.Count} param)");

// --- Build composite including both ---
var compositeKey = CompositeSessionKey.NewKey();
var composite = new CompositeSession(compositeKey, "Primary + Associate",
    new[] { primary, associate }, CompositeSessionMode.Overlay);

// --- Read Temperature (from primary) and Pressure (from associate) ---
using var tempPda = composite.CreateParameterDataAccess("Temperature:Sensors");
using var pressPda = composite.CreateParameterDataAccess("Pressure:Inlet");

var tempSamples = tempPda.GetSamples(1000, composite.StartTime, composite.EndTime);
var pressSamples = pressPda.GetSamples(1000, composite.StartTime, composite.EndTime);

Console.WriteLine($"\nComposite read: {tempSamples.SampleCount} temp + {pressSamples.SampleCount} pressure\n");
Console.WriteLine($"{"Time (ms)",10}  {"Temp (degC)",12}  {"Press (bar)",12}");
Console.WriteLine(new string('─', 38));

var n = Math.Min(tempSamples.SampleCount, pressSamples.SampleCount);
for (var i = 0; i < Math.Min(n, 10); i++)
{
    var ms = (tempSamples.Timestamp[i] - baseTime) / 1_000_000.0;
    Console.WriteLine($"{ms,10:F1}  {tempSamples.Data[i],12:F2}  {pressSamples.Data[i],12:F3}");
}
