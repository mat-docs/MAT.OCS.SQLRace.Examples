namespace StandaloneRecorder;

/// <summary>
/// Configuration for the standalone recorder, bound from appsettings.json.
/// </summary>
public sealed class RecorderConfig
{
    /// <summary>ATLAS Data Server hostname or IP address.</summary>
    public string AdsHost { get; set; } = "127.0.0.1";

    /// <summary>IP address for the local listener endpoint.</summary>
    public string ListenerIpAddress { get; set; } = "127.0.0.1";

    /// <summary>Port for the local listener endpoint.</summary>
    public int ListenerPort { get; set; } = 6565;

    /// <summary>
    /// Path to the data source directory. Empty defaults to the system temp directory.
    /// </summary>
    public string DataSourcePath { get; set; } = "";

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
