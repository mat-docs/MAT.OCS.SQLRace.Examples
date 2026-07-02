namespace ServerListenerRecorder;

/// <summary>
/// Configuration for the server listener recorder, bound from appsettings.json.
/// </summary>
public sealed class RecorderConfig
{
    /// <summary>IP address to listen on for incoming telemetry data.</summary>
    public string ListenerIpAddress { get; set; } = "127.0.0.1";

    /// <summary>Port to listen on for incoming telemetry data.</summary>
    public int ListenerPort { get; set; } = 6565;

    /// <summary>
    /// Path to the data source directory. Empty defaults to the system temp directory.
    /// </summary>
    public string DataSourcePath { get; set; } = "";

    /// <summary>Session identifier prefix for recorded sessions.</summary>
    public string SessionIdentifier { get; set; } = "ServerListener-Demo";

    /// <summary>Database engine: "SQLite" or "SqlServer".</summary>
    public string DbEngine { get; set; } = "SQLite";

    /// <summary>
    /// Resolves the data source path, falling back to temp directory if empty.
    /// </summary>
    public string ResolvedDataSourcePath =>
        string.IsNullOrWhiteSpace(DataSourcePath)
            ? Path.GetTempPath()
            : DataSourcePath;
}
