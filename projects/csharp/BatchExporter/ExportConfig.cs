namespace BatchExporter;

/// <summary>
/// Configuration for the batch export process, bound from appsettings.json.
/// </summary>
public sealed class ExportConfig
{
    /// <summary>
    /// Output directory for CSV files. Empty defaults to <c>./exports/</c>.
    /// </summary>
    public string OutputDirectory { get; set; } = "";

    /// <summary>Parameter identifiers to export. Empty exports all parameters.</summary>
    public string[] ParameterIdentifiers { get; set; } = ["Temperature:Sensors", "Pressure:Inlet", "Speed:Shaft"];

    /// <summary>
    /// Filter sessions recorded on or after this date. Empty means no lower bound.
    /// </summary>
    public string DateRangeStart { get; set; } = "";

    /// <summary>
    /// Filter sessions recorded on or before this date. Empty means no upper bound.
    /// </summary>
    public string DateRangeEnd { get; set; } = "";

    /// <summary>CSV field delimiter.</summary>
    public string CsvDelimiter { get; set; } = ",";

    /// <summary>Timestamp format string for the first CSV column.</summary>
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>How to handle missing data points.</summary>
    public MissingDataBehavior MissingDataBehavior { get; set; } = MissingDataBehavior.WriteNaN;

    /// <summary>Maximum samples to read per PDA call (for chunking large sessions).</summary>
    public int MaxSamplesPerRead { get; set; } = 32767;

    /// <summary>
    /// Resolves the output directory, falling back to <c>./exports/</c> if empty.
    /// </summary>
    public string ResolvedOutputDirectory =>
        string.IsNullOrWhiteSpace(OutputDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "exports")
            : OutputDirectory;
}

/// <summary>
/// Behaviour when a parameter sample has a non-Sample data status.
/// </summary>
public enum MissingDataBehavior
{
    /// <summary>Skip the row entirely.</summary>
    Skip,

    /// <summary>Write "NaN" in place of the missing value.</summary>
    WriteNaN,

    /// <summary>Write "0" in place of the missing value.</summary>
    WriteZero
}
