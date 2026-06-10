// ─────────────────────────────────────────────────────────────
// SQL Race Examples: Getting Started Integration Tests
// ─────────────────────────────────────────────────────────────
//
// Integration tests that verify the GettingStarted project can
// create a session, write data, and read it back using a local
// SQLite database.
//
// These tests require the MESL.SQLRace.API NuGet package AND an ATLAS
// installation (for the SQL Race licence). They pass locally but are
// skipped in CI where no licence is available.
//
// Run: dotnet test --filter "FullyQualifiedName~GettingStarted"
// ─────────────────────────────────────────────────────────────

using Xunit;
using SqlRace.Examples.Core;
using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;

namespace SqlRace.Examples.Tests;

/// <summary>
/// Integration tests for session create → write → read cycle.
/// These require the SQL Race runtime to be available.
/// </summary>
public class GettingStartedTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public GettingStartedTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"sqlrace-test-{Guid.NewGuid():N}.ssn2");
        _connectionString = ConnectionStringProvider.ForSQLiteFile(_dbPath);
    }

    public void Dispose()
    {
        // Clean up test database
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); }
            catch { /* best effort cleanup */ }
        }
    }

    [Fact]
    public void CreateSession_ReturnsValidSession()
    {
        SqlRaceBootstrapper.Initialize();

        using var clientSession = SessionFactory.CreateSession("Test", _connectionString);
        Assert.NotNull(clientSession);
        Assert.NotNull(clientSession.Session);
        Assert.True(clientSession.Session.StartTime >= 0);
    }

    [Fact]
    public void WriteAndReadData_RoundTrips()
    {
        SqlRaceBootstrapper.Initialize();

        using var clientSession = SessionFactory.CreateSession("RoundTrip", _connectionString);
        var session = clientSession.Session;

        var result = new ParameterBuilder(session)
            .WithIdentifier("Temperature:Sensors")
            .WithFrequency(100, FrequencyUnit.Hz)
            .InApplicationGroup("Sensors")
            .InParameterGroup("Thermal")
            .WithConversion("degC")
            .Build();

        // Write data
        var startTime = 36_000_000_000_000L;
        var interval = new Frequency(100, FrequencyUnit.Hz).ToInterval();
        for (int i = 0; i < 10; i++)
        {
            session.AddChannelData(result.ChannelId, startTime + i * interval, 1, BitConverter.GetBytes(20.0 + i));
        }

        // Read data back
        using var pda = session.CreateParameterDataAccess("Temperature:Sensors");
        var samples = pda.GetSamplesBetween(startTime, startTime + 10 * interval);

        Assert.Equal(10, samples.SampleCount);
        Assert.Equal(20.0, samples.Data[0], precision: 2);
    }

    [Fact]
    public void LoadSession_WithValidKey_ReturnsSession()
    {
        SqlRaceBootstrapper.Initialize();

        // Create a session first
        using var created = SessionFactory.CreateSession("LoadTest", _connectionString);
        var sessionKey = created.Session.Key;

        // Load it back
        using var loaded = SessionFactory.LoadSession(sessionKey, _connectionString);
        Assert.NotNull(loaded);
        Assert.Equal(sessionKey, loaded.Session.Key);
    }
}
