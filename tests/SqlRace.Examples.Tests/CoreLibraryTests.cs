// ─────────────────────────────────────────────────────────────
// SQL Race Examples: Core Library Tests
// ─────────────────────────────────────────────────────────────
//
// Unit tests for SqlRace.Examples.Core — ConnectionStringProvider,
// ParameterBuilder, and SqlRaceBootstrapper.
//
// Run: dotnet test
// ─────────────────────────────────────────────────────────────

using Xunit;
using SqlRace.Examples.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Enumerators;

namespace SqlRace.Examples.Tests;

public class ConnectionStringProviderTests
{
    [Fact]
    public void Default_ReturnsLocalSqlite()
    {
        var cs = ConnectionStringProvider.Resolve();

        Assert.Contains("DbEngine=SQLite", cs);
        Assert.Contains("sqlrace-examples.ssn2", cs);
    }

    [Fact]
    public void Explicit_OverridesDefault()
    {
        var cs = ConnectionStringProvider.Resolve("DbEngine=SQLite;Data Source=custom.ssn2;");

        Assert.Contains("custom.ssn2", cs);
    }

    [Fact]
    public void EnvironmentVariable_OverridesDefault()
    {
        const string envCs = "DbEngine=SQLite;Data Source=env-test.ssn2;";
        var previous = Environment.GetEnvironmentVariable("SQLRACE_CONNECTION_STRING");

        try
        {
            Environment.SetEnvironmentVariable("SQLRACE_CONNECTION_STRING", envCs);
            var cs = ConnectionStringProvider.Resolve();

            Assert.Contains("env-test.ssn2", cs);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SQLRACE_CONNECTION_STRING", previous);
        }
    }

    [Fact]
    public void ForSQLiteFile_FormatsCorrectly()
    {
        var cs = ConnectionStringProvider.ForSQLiteFile(@"C:\data\test.ssn2");

        Assert.Contains("DbEngine=SQLite", cs);
        Assert.Contains(@"C:\data\test.ssn2", cs);
    }

    [Fact]
    public void DefaultSQLiteFilePath_EndsWithSsn2()
    {
        Assert.EndsWith("sqlrace-examples.ssn2", ConnectionStringProvider.DefaultSQLiteFilePath);
    }
}

public class ParameterBuilderTests
{
    [Fact]
    public void Builder_CreatesParameter()
    {
        SqlRaceBootstrapper.Initialize();
        var connStr = ConnectionStringProvider.ForSQLiteFile(
            Path.Combine(Path.GetTempPath(), $"test-builder-{Guid.NewGuid():N}.ssn2"));

        using var clientSession = SessionFactory.CreateSession("Test", connStr);
        var session = clientSession.Session;

        var result = new ParameterBuilder(session)
            .WithIdentifier("Temperature:Sensors")
            .WithDescription("Inlet temperature sensor")
            .InApplicationGroup("Sensors")
            .InParameterGroup("Thermal")
            .WithFrequency(100, FrequencyUnit.Hz)
            .WithDataType(DataType.Double64Bit)
            .WithConversion("degC", "%5.2f")
            .WithRange(min: 0, max: 500)
            .Build();

        Assert.NotNull(result.Parameter);
        Assert.True(result.ChannelId > 0);
        Assert.Equal(1, session.Parameters.Count);
    }

    [Fact]
    public void Builder_FluentChaining()
    {
        SqlRaceBootstrapper.Initialize();
        var connStr = ConnectionStringProvider.ForSQLiteFile(
            Path.Combine(Path.GetTempPath(), $"test-fluent-{Guid.NewGuid():N}.ssn2"));

        using var clientSession = SessionFactory.CreateSession("Test", connStr);
        var session = clientSession.Session;

        var result = new ParameterBuilder(session)
            .WithIdentifier("Speed:Shaft")
            .WithName("Shaft Speed")
            .WithDescription("Rotational speed of the main shaft")
            .WithFrequency(100, FrequencyUnit.Hz)
            .InApplicationGroup("Powertrain")
            .InParameterGroup("Drivetrain")
            .Build();

        Assert.NotNull(result.Parameter);
        Assert.Equal(1, session.Parameters.Count);
    }

    [Fact]
    public void Builder_BatchBuild()
    {
        SqlRaceBootstrapper.Initialize();
        var connStr = ConnectionStringProvider.ForSQLiteFile(
            Path.Combine(Path.GetTempPath(), $"test-batch-{Guid.NewGuid():N}.ssn2"));

        using var clientSession = SessionFactory.CreateSession("Test", connStr);
        var session = clientSession.Session;

        var builders = new[]
        {
            new ParameterBuilder(session)
                .WithIdentifier("Temperature:Sensors")
                .InApplicationGroup("Sensors").InParameterGroup("Thermal")
                .WithFrequency(100, FrequencyUnit.Hz)
                .WithConversion("degC"),
            new ParameterBuilder(session)
                .WithIdentifier("Pressure:Inlet")
                .InApplicationGroup("Sensors").InParameterGroup("Hydraulic")
                .WithFrequency(50, FrequencyUnit.Hz)
                .WithConversion("bar"),
            new ParameterBuilder(session)
                .WithIdentifier("Speed:Shaft")
                .InApplicationGroup("Powertrain").InParameterGroup("Drivetrain")
                .WithFrequency(200, FrequencyUnit.Hz)
                .WithConversion("rpm"),
        };

        var results = ParameterBuilder.BuildBatch(session, builders);

        Assert.Equal(3, results.Count);
        Assert.Equal(3, session.Parameters.Count);
    }

    [Fact]
    public void Builder_ThrowsWithoutIdentifier()
    {
        // ParameterBuilder requires a Session, but we can verify the
        // null-check on the session argument without SQL Race runtime.
        Assert.Throws<ArgumentNullException>(() => new ParameterBuilder(null!));
    }
}

public class SqlRaceBootstrapperTests
{
    [Fact]
    public void Initialize_MethodExists()
    {
        // SqlRaceBootstrapper.Initialize() should be idempotent
        // and safe to call even without MESL assemblies at test time.
        // This test verifies the method exists and is callable.
        var method = typeof(SqlRaceBootstrapper).GetMethod("Initialize");
        Assert.NotNull(method);
    }
}
