using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;
using SqlRace.Examples.Core;

namespace SqlRace.Examples.Fixtures;

/// <summary>
/// Generates a deterministic SQL Race session with analytically-defined data, so that
/// example outputs can be checked against known-correct expected values.
///
/// This is the "verified input" for the end-to-end example tests: every sample is produced
/// by a closed-form formula, so the expected output of any example reading this session can
/// be derived by hand (and is exposed here as constants the tests assert against).
/// </summary>
public static class VerifiedFixture
{
    // ── Session identity ───────────────────────────────────────
    /// <summary>Fixed session key so examples that take a GUID can be pointed at the fixture.</summary>
    public const string SessionKeyString = "f1c0ffee-0000-4000-8000-000000000001";
    // Contains "Example" so the query snippets, which filter on
    // Identifier contains "Example", have something to match.
    public const string SessionName = "Verified Example Session";

    /// <summary>Session start in nanoseconds-of-day (10:00:00.000 → 36,000 s).</summary>
    public const long StartTimeNs = 36_000_000_000_000L;
    public const int DurationSeconds = 10;

    // ── Temperature:Sensors @ 100 Hz — linear ramp ─────────────
    //     value(i) = TempBase + TempSlope * i
    public const string TemperatureId = "Temperature:Sensors";
    public const int TemperatureHz = 100;
    public const double TempBase = 20.0;
    public const double TempSlope = 0.1;
    public const int TemperatureSampleCount = TemperatureHz * DurationSeconds; // 1000

    // ── Pressure:Inlet @ 100 Hz — constant ─────────────────────
    public const string PressureId = "Pressure:Inlet";
    public const int PressureHz = 100;
    public const double PressureConstant = 2.0;
    public const int PressureSampleCount = PressureHz * DurationSeconds; // 1000

    // ── Speed:Shaft @ 50 Hz — linear ramp ──────────────────────
    //     value(i) = SpeedSlope * i
    public const string SpeedId = "Speed:Shaft";
    public const int SpeedHz = 50;
    public const double SpeedSlope = 0.2;
    public const int SpeedSampleCount = SpeedHz * DurationSeconds; // 500

    public const int ParameterCount = 3;

    // NOTE: a freshly-created SQLite session always reloads with exactly one (default) lap.
    // Laps added via LapCollection.Add do not survive a reload in this API path, so the
    // verified fixture does not rely on laps. (Pre-existing .ssn2 files may contain laps
    // written by other tooling.)
    public const int LapCountOnReload = 1;

    /// <summary>The fixed session key as a parsed <see cref="SessionKey"/>.</summary>
    public static SessionKey Key => SessionKey.Parse(SessionKeyString);

    /// <summary>Expected value of the temperature ramp at sample <paramref name="i"/>.</summary>
    public static double Temperature(int i) => TempBase + TempSlope * i;

    /// <summary>Expected value of the speed ramp at sample <paramref name="i"/>.</summary>
    public static double Speed(int i) => SpeedSlope * i;

    /// <summary>
    /// (Re)creates the verified session in the database at <paramref name="connectionString"/>.
    /// Any existing session with the fixed key is removed first so generation is idempotent.
    /// </summary>
    public static void Generate(string connectionString)
    {
        SqlRaceBootstrapper.Initialize();

        var sessionManager = SessionManager.CreateSessionManager();

        // Only the 10:00:00 time-of-day matters - it is what makes
        // Session.StartTime equal StartTimeNs. The date is today so that
        // TimeOfRecording falls inside the "last 30 days" window the query
        // snippets filter on; a fixed date made them match nothing.
        var sessionDate = DateTime.UtcNow.Date.AddHours(10);

        using var clientSession = sessionManager.CreateSession(
            connectionString, Key, SessionName, sessionDate, "Example");
        var session = clientSession.Session;

        // Build all three parameters in a single configuration commit. (Separate Build()
        // calls each create and commit their own configuration, and only the first
        // survives a reload — BuildBatch is the correct API for multiple parameters.)
        var built = ParameterBuilder.BuildBatch(session, new[]
        {
            new ParameterBuilder(session)
                .WithIdentifier(TemperatureId)
                .WithDescription("Inlet temperature sensor (verified ramp)")
                .InApplicationGroup("Sensors")
                .InParameterGroup("Thermal")
                .WithFrequency(TemperatureHz, FrequencyUnit.Hz)
                .WithDataType(DataType.Double64Bit)
                .WithConversion("degC", "%5.2f")
                .WithRange(min: 0, max: 500),
            new ParameterBuilder(session)
                .WithIdentifier(PressureId)
                .WithDescription("Inlet pressure sensor (verified constant)")
                .InApplicationGroup("Sensors")
                .InParameterGroup("Hydraulic")
                .WithFrequency(PressureHz, FrequencyUnit.Hz)
                .WithDataType(DataType.Double64Bit)
                .WithConversion("bar", "%5.3f")
                .WithRange(min: 0, max: 10),
            new ParameterBuilder(session)
                .WithIdentifier(SpeedId)
                .WithDescription("Shaft speed (verified ramp)")
                .InApplicationGroup("Driveline")
                .InParameterGroup("Rotation")
                .WithFrequency(SpeedHz, FrequencyUnit.Hz)
                .WithDataType(DataType.Double64Bit)
                .WithConversion("rad/s", "%6.2f")
                .WithRange(min: 0, max: 200),
        });

        WriteRamp(session, built[0].ChannelId, TemperatureHz, TemperatureSampleCount, Temperature);
        WriteRamp(session, built[1].ChannelId, PressureHz, PressureSampleCount, _ => PressureConstant);
        WriteRamp(session, built[2].ChannelId, SpeedHz, SpeedSampleCount, Speed);

        // Finalise the recording so all data and metadata are persisted to disk.
        session.Flush();
        session.EndData();
    }

    private static void WriteRamp(Session session, uint channelId, int hz, int sampleCount, Func<int, double> value)
    {
        var interval = new Frequency(hz, FrequencyUnit.Hz).ToInterval();
        for (var i = 0; i < sampleCount; i++)
        {
            var timestamp = StartTimeNs + i * interval;
            session.AddChannelData(channelId, timestamp, 1, BitConverter.GetBytes(value(i)));
        }
    }
}
