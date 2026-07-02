using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Query;
using Microsoft.Extensions.Configuration;
using SqlRace.Examples.Core;
using BatchExporter;

// ─── Configuration ───────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var exportConfig = new ExportConfig();
configuration.GetSection("Export").Bind(exportConfig);

var connectionString = ConnectionStringProvider.Resolve(
    configuration.GetConnectionString("SqlRace"));

// ─── Initialise ──────────────────────────────────────────────
SqlRaceBootstrapper.Initialize();
Console.WriteLine("SQL Race Batch Exporter");
Console.WriteLine($"Connection:  {connectionString}");
Console.WriteLine($"Output:      {exportConfig.ResolvedOutputDirectory}");
Console.WriteLine($"Parameters:  {string.Join(", ", exportConfig.ParameterIdentifiers)}");
Console.WriteLine($"Missing:     {exportConfig.MissingDataBehavior}");
Console.WriteLine();

// ─── Query sessions ──────────────────────────────────────────
var queryManager = QueryManager.CreateQueryManager(connectionString);

// Apply date range filter if configured
if (!string.IsNullOrWhiteSpace(exportConfig.DateRangeStart) ||
    !string.IsNullOrWhiteSpace(exportConfig.DateRangeEnd))
{
    var composite = new CompositeFilter(CombineType.AND);

    if (DateTime.TryParse(exportConfig.DateRangeStart, out var startDate))
    {
        composite.Add(new ScalarFilter("TimeOfRecording", MatchingRule.GreaterThan,
            startDate.ToString("O"), ignoreCase: true));
    }

    if (DateTime.TryParse(exportConfig.DateRangeEnd, out var endDate))
    {
        composite.Add(new ScalarFilter("TimeOfRecording", MatchingRule.LessThan,
            endDate.ToString("O"), ignoreCase: true));
    }

    queryManager.Filter = composite;
    Console.WriteLine($"Date filter: {exportConfig.DateRangeStart} → {exportConfig.DateRangeEnd}");
}

var sessions = queryManager.ExecuteQuery().ToList();

if (sessions.Count == 0)
{
    Console.WriteLine("No sessions found matching the filter criteria.");
    Console.WriteLine("Tip: Run the GettingStarted or ServerListenerRecorder project to create sessions.");
    return;
}

Console.WriteLine($"Found {sessions.Count} session(s) to export.\n");

// ─── Export each session ─────────────────────────────────────
var sessionManager = SessionManager.CreateSessionManager();
var totalFiles = 0;
var totalSamples = 0L;

for (var sessionIdx = 0; sessionIdx < sessions.Count; sessionIdx++)
{
    var summary = sessions[sessionIdx];
    Console.WriteLine($"[{sessionIdx + 1}/{sessions.Count}] {summary.Identifier} ({summary.Key})");

    using var clientSession = sessionManager.Load(summary.Key, connectionString);
    var session = clientSession.Session;

    // Resolve parameter list (use configured list, or all if empty)
    var paramIds = exportConfig.ParameterIdentifiers.Length > 0
        ? exportConfig.ParameterIdentifiers
        : session.Parameters.Select(p => p.Identifier).ToArray();

    // Filter to parameters that actually exist in this session
    var availableParams = paramIds
        .Where(id => session.Parameters.Any(p => p.Identifier == id))
        .ToList();

    if (availableParams.Count == 0)
    {
        Console.WriteLine("  Skipped: no matching parameters found.");
        continue;
    }

    // Build output filename
    var safeName = string.Join("_", summary.Identifier.Split(Path.GetInvalidFileNameChars()));
    var fileName = $"{safeName}_{summary.TimeOfRecording:yyyyMMdd-HHmmss}.csv";
    var filePath = Path.Combine(exportConfig.ResolvedOutputDirectory, fileName);

    using var csv = new CsvWriter(filePath, exportConfig.CsvDelimiter,
        exportConfig.TimestampFormat, exportConfig.MissingDataBehavior);
    csv.WriteHeader("Timestamp", availableParams);

    // Read all parameters and interleave by timestamp
    // For simplicity, use the first parameter's timestamps as the time base
    var pdas = new List<ParameterDataAccessBase>();
    try
    {
        foreach (var paramId in availableParams)
            pdas.Add(session.CreateParameterDataAccess(paramId));

        // Read first parameter to get timestamp array
        var baseSamples = pdas[0].GetSamplesBetween(session.StartTime, session.EndTime);
        var allSamples = new List<ParameterValues> { baseSamples };

        for (var i = 1; i < pdas.Count; i++)
            allSamples.Add(pdas[i].GetSamplesBetween(session.StartTime, session.EndTime));

        var rowCount = baseSamples.SampleCount;
        var values = new double[availableParams.Count];
        var statuses = new DataStatusType[availableParams.Count];

        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < availableParams.Count; col++)
            {
                if (row < allSamples[col].SampleCount)
                {
                    values[col] = allSamples[col].Data[row];
                    statuses[col] = allSamples[col].DataStatus[row];
                }
                else
                {
                    values[col] = double.NaN;
                    statuses[col] = DataStatusType.Missing;
                }
            }

            csv.WriteRow(baseSamples.Timestamp[row], summary.TimeOfRecording, values, statuses);
        }

        totalSamples += csv.RowsWritten * availableParams.Count;
        totalFiles++;
        Console.WriteLine($"  Exported: {availableParams.Count} params, {csv.RowsWritten} rows → {filePath}");
    }
    finally
    {
        foreach (var pda in pdas)
            pda.Dispose();
    }
}

// ─── Summary ─────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine($"Export complete.");
Console.WriteLine($"  Sessions:  {sessions.Count}");
Console.WriteLine($"  Files:     {totalFiles}");
Console.WriteLine($"  Samples:   {totalSamples:N0}");
Console.WriteLine($"  Output:    {exportConfig.ResolvedOutputDirectory}");
