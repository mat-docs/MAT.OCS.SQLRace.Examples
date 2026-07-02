using Microsoft.Extensions.Configuration;

namespace SqlRace.Examples.Core;

/// <summary>
/// Resolves SQL Race connection strings from a configuration hierarchy:
/// <list type="number">
///   <item>Explicit parameter</item>
///   <item>Environment variable <c>SQLRACE_CONNECTION_STRING</c></item>
///   <item><c>appsettings.json</c> key <c>SqlRace:ConnectionString</c></item>
///   <item>Default SQLite file in the system temp directory</item>
/// </list>
/// </summary>
public static class ConnectionStringProvider
{
    /// <summary>
    /// The environment variable name checked for a connection string.
    /// </summary>
    public const string EnvironmentVariable = "SQLRACE_CONNECTION_STRING";

    /// <summary>
    /// The default SQLite database file name used when no other source is configured.
    /// </summary>
    public const string DefaultFileName = "sqlrace-examples.ssn2";

    /// <summary>
    /// Resolves a connection string using the configuration hierarchy.
    /// </summary>
    /// <param name="explicit">
    /// An explicit connection string. If non-null and non-empty, this is returned directly.
    /// </param>
    /// <returns>A SQL Race connection string.</returns>
    public static string Resolve(string? @explicit = null)
    {
        // 1. Explicit parameter
        if (!string.IsNullOrWhiteSpace(@explicit))
            return @explicit;

        // 2. Environment variable
        var envValue = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(envValue))
            return envValue;

        // 3. appsettings.json
        var appSettingsValue = TryReadAppSettings();
        if (!string.IsNullOrWhiteSpace(appSettingsValue))
            return appSettingsValue;

        // 4. Default SQLite
        return DefaultSQLiteConnectionString;
    }

    /// <summary>
    /// Gets a connection string for a SQLite file at the specified path.
    /// </summary>
    public static string ForSQLiteFile(string filePath) =>
        $"DbEngine=SQLite;Data Source={filePath};";

    /// <summary>
    /// Gets the default SQLite connection string using a file in the system temp directory.
    /// </summary>
    public static string DefaultSQLiteConnectionString =>
        ForSQLiteFile(Path.Combine(Path.GetTempPath(), DefaultFileName));

    /// <summary>
    /// Gets the full path to the default SQLite database file.
    /// </summary>
    public static string DefaultSQLiteFilePath =>
        Path.Combine(Path.GetTempPath(), DefaultFileName);

    private static string? TryReadAppSettings()
    {
        try
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
                return null;

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            return config["SqlRace:ConnectionString"];
        }
        catch
        {
            return null;
        }
    }
}
