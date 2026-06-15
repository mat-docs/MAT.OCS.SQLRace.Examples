// ============================================================================
// Fixture Generator — writes the deterministic "verified" session to disk.
// ============================================================================
// Used by the end-to-end example harness:
//   • the capability tests call VerifiedFixture.Generate(...) directly;
//   • the snippet smoke-runner runs this exe to materialise the fixture at the
//     default SQLite path the snippets read from.
//
// Usage:
//   dotnet run --project tools/FixtureGenerator -- [output.ssn2]
// If no path is given, falls back to SQLRACE_FIXTURE_PATH, then the default
// snippet path (%TEMP%/sqlrace-examples.ssn2).
// ============================================================================

using SqlRace.Examples.Core;
using SqlRace.Examples.Fixtures;

var path = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("SQLRACE_FIXTURE_PATH")
      ?? Path.Combine(Path.GetTempPath(), "sqlrace-examples.ssn2");

// Start from a clean file so generation is fully deterministic.
if (File.Exists(path))
{
    File.Delete(path);
}

var connectionString = ConnectionStringProvider.ForSQLiteFile(path);
VerifiedFixture.Generate(connectionString);

Console.WriteLine("Verified fixture written.");
Console.WriteLine($"  File:        {path}");
Console.WriteLine($"  Session key: {VerifiedFixture.SessionKeyString}");
Console.WriteLine($"  Parameters:  {VerifiedFixture.ParameterCount} " +
    $"({VerifiedFixture.TemperatureId}, {VerifiedFixture.PressureId}, {VerifiedFixture.SpeedId})");
