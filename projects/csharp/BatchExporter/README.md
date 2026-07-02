# BatchExporter

A console application that queries for sessions matching criteria and exports parameter data from each session to CSV files.

## Use Case

This is the most common integration pattern for non-real-time analysis: extract data from SQL Race into CSV for consumption by Python, R, Excel, or other external tools.

## Prerequisites

- .NET 8 SDK
- MESL.SQLRace.API NuGet package
- A database with sessions (run GettingStarted or ServerListenerRecorder first)

## Build and Run

```bash
cd projects/csharp
dotnet run --project BatchExporter
```

## Configuration

Edit `appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `ConnectionStrings:SqlRace` | (empty → SQLite in temp) | SQL Race connection string |
| `Export:OutputDirectory` | (empty → `./exports/`) | Output directory for CSV files |
| `Export:ParameterIdentifiers` | `["Temperature:Sensors", ...]` | Parameters to export (empty = all) |
| `Export:DateRangeStart` | (empty) | Filter: sessions recorded after this date |
| `Export:DateRangeEnd` | (empty) | Filter: sessions recorded before this date |
| `Export:CsvDelimiter` | `,` | Field delimiter |
| `Export:TimestampFormat` | `yyyy-MM-dd HH:mm:ss.fff` | Timestamp column format |
| `Export:MissingDataBehavior` | `WriteNaN` | How to handle missing data: `Skip`, `WriteNaN`, `WriteZero` |
| `Export:MaxSamplesPerRead` | `32767` | Chunk size for reading large sessions |

## Output Format

Each session produces one CSV file named `{Identifier}_{Date}.csv`:

```csv
Timestamp,Temperature:Sensors,Pressure:Inlet,Speed:Shaft
2026-03-12 10:00:00.000,75.00,3.50,3000
2026-03-12 10:00:00.010,75.30,3.52,3015
...
```

## Handling Large Sessions

For sessions with millions of samples, the `MaxSamplesPerRead` setting controls the chunk size passed to `GetSamplesBetween`. The CSV writer uses streaming writes to avoid building the entire output in memory.

## Extending

To add other output formats (Parquet, HDF5, JSON):
1. Create a new writer class implementing the same interface as `CsvWriter`
2. Add a format selection option to `ExportConfig`
3. Instantiate the appropriate writer in `Program.cs`

## Expected Output

```
SQL Race Batch Exporter
Connection:  DbEngine=SQLite;Data Source=/tmp/sqlrace-examples.ssn2;
Output:      /path/to/exports
Parameters:  Temperature:Sensors, Pressure:Inlet, Speed:Shaft

Found 3 session(s) to export.

[1/3] ServerListener-Demo-20260312-143022 (<guid>)
  Exported: 3 params, 4200 rows → exports/ServerListener-Demo_20260312-143022.csv
[2/3] GettingStarted Demo (<guid>)
  Exported: 2 params, 100 rows → exports/GettingStarted-Demo_20260312-120000.csv
...

Export complete.
  Sessions:  3
  Files:     3
  Samples:   12,900
  Output:    /path/to/exports
```
