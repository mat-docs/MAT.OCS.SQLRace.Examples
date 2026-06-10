using MAT.OCS.Core;
using MESL.SqlRace.Domain;

namespace BatchExporter;

/// <summary>
/// Writes parameter data to CSV format using streaming writes for efficiency.
/// </summary>
public sealed class CsvWriter : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly string _delimiter;
    private readonly string _timestampFormat;
    private readonly MissingDataBehavior _missingBehavior;

    /// <summary>
    /// Creates a new CSV writer targeting the specified file path.
    /// </summary>
    public CsvWriter(string filePath, string delimiter, string timestampFormat, MissingDataBehavior missingBehavior)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _writer = new StreamWriter(filePath, append: false, encoding: System.Text.Encoding.UTF8);
        _delimiter = delimiter;
        _timestampFormat = timestampFormat;
        _missingBehavior = missingBehavior;
    }

    /// <summary>Total rows written (excluding header).</summary>
    public long RowsWritten { get; private set; }

    /// <summary>
    /// Writes the header row with timestamp and parameter identifier columns.
    /// </summary>
    public void WriteHeader(string timestampColumnName, IReadOnlyList<string> parameterIdentifiers)
    {
        _writer.Write(timestampColumnName);
        foreach (var id in parameterIdentifiers)
        {
            _writer.Write(_delimiter);
            _writer.Write(QuoteIfNeeded(id));
        }
        _writer.WriteLine();
    }

    /// <summary>
    /// Writes a single row of data. Each parameter provides its value and status.
    /// </summary>
    /// <param name="timestamp">Nanosecond timestamp to format.</param>
    /// <param name="sessionDate">Session date for converting nanoseconds to wall time.</param>
    /// <param name="values">Value per parameter.</param>
    /// <param name="statuses">Data status per parameter.</param>
    /// <returns><c>true</c> if the row was written; <c>false</c> if skipped due to missing data.</returns>
    public bool WriteRow(long timestamp, DateTime sessionDate, double[] values, DataStatusType[] statuses)
    {
        // Check if any value is missing and we should skip
        if (_missingBehavior == MissingDataBehavior.Skip)
        {
            for (var i = 0; i < statuses.Length; i++)
            {
                if (statuses[i] != DataStatusType.Sample)
                    return false;
            }
        }

        // Format timestamp as wall-clock time
        var timeOfDay = TimeSpan.FromTicks(timestamp / 100); // ns → ticks
        var dateTime = sessionDate.Date + timeOfDay;
        _writer.Write(dateTime.ToString(_timestampFormat));

        for (var i = 0; i < values.Length; i++)
        {
            _writer.Write(_delimiter);

            if (statuses[i] != DataStatusType.Sample)
            {
                _writer.Write(_missingBehavior == MissingDataBehavior.WriteNaN ? "NaN" : "0");
            }
            else
            {
                _writer.Write(values[i].ToString("G"));
            }
        }

        _writer.WriteLine();
        RowsWritten++;
        return true;
    }

    private string QuoteIfNeeded(string value)
    {
        if (value.Contains(_delimiter) || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _writer.Flush();
        _writer.Dispose();
    }
}
