using MAT.OCS.Core;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Infrastructure.DataPipeline;
using MESL.SqlRace.Domain.Query;
using MESL.SqlRace.Enumerators;
using SqlRace.Examples.Core;

// ─── Parse arguments ─────────────────────────────────────────
if (args.Length < 1)
{
    Console.WriteLine("Usage: dotnet run -- <source.ssn2> [--start <seconds>] [--end <seconds>]");
    Console.WriteLine();
    Console.WriteLine("  Opens the .ssn2 file, displays timing info, and lets you crop to a new file.");
    Console.WriteLine("  Times are in seconds relative to session start.");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run -- data.ssn2                     # Interactive — prompts for start/end");
    Console.WriteLine("  dotnet run -- data.ssn2 --start 5 --end 30  # Crop to 5s–30s");
    return;
}

var sourcePath = Path.GetFullPath(args[0]);
if (!File.Exists(sourcePath))
{
    Console.WriteLine($"ERROR: File not found: {sourcePath}");
    return;
}

double? argStart = null;
double? argEnd = null;

for (var i = 1; i < args.Length; i++)
{
    if (args[i] == "--start" && i + 1 < args.Length && double.TryParse(args[i + 1], out var s))
    {
        argStart = s;
        i++;
    }
    else if (args[i] == "--end" && i + 1 < args.Length && double.TryParse(args[i + 1], out var e))
    {
        argEnd = e;
        i++;
    }
}

// ─── Initialise SQL Race ─────────────────────────────────────
SqlRaceBootstrapper.Initialize();

var sourceConnectionString = ConnectionStringProvider.ForSQLiteFile(sourcePath);

// ─── Load the source session ─────────────────────────────────
var sessionManager = SessionManager.CreateSessionManager();
using var queryManager = QueryManager.CreateQueryManager(sourceConnectionString);
var summaries = queryManager.ExecuteQuery().ToList();

if (summaries.Count == 0)
{
    Console.WriteLine($"ERROR: No sessions found in '{sourcePath}'.");
    return;
}

// Use the most recent session in the file
var summary = summaries.OrderByDescending(s => s.TimeOfRecording).First();
using var sourceClient = sessionManager.Load(summary.Key, sourceConnectionString);
var sourceSession = sourceClient.Session;

var durationNs = sourceSession.EndTime - sourceSession.StartTime;
var durationSeconds = durationNs / 1_000_000_000.0;

Console.WriteLine($"Source file:     {sourcePath}");
Console.WriteLine($"Session:         {sourceSession.Identifier}");
Console.WriteLine($"Start time (ns): {sourceSession.StartTime}");
Console.WriteLine($"End time (ns):   {sourceSession.EndTime}");
Console.WriteLine($"Duration:        {durationSeconds:F3} seconds");
Console.WriteLine($"Parameters:      {sourceSession.Parameters.Count}");
Console.WriteLine();

// ─── Determine crop times ────────────────────────────────────
double cropStartSec;
double cropEndSec;

if (argStart.HasValue && argEnd.HasValue)
{
    cropStartSec = argStart.Value;
    cropEndSec = argEnd.Value;
}
else
{
    Console.Write($"Crop start (seconds from start, 0–{durationSeconds:F3}) [0]: ");
    var startInput = Console.ReadLine()?.Trim();
    cropStartSec = string.IsNullOrEmpty(startInput) ? 0 : double.Parse(startInput);

    Console.Write($"Crop end (seconds from start, 0–{durationSeconds:F3}) [{durationSeconds:F3}]: ");
    var endInput = Console.ReadLine()?.Trim();
    cropEndSec = string.IsNullOrEmpty(endInput) ? durationSeconds : double.Parse(endInput);
}

if (cropStartSec < 0 || cropEndSec > durationSeconds || cropStartSec >= cropEndSec)
{
    Console.WriteLine($"ERROR: Invalid crop range {cropStartSec:F3}s–{cropEndSec:F3}s (session is 0–{durationSeconds:F3}s).");
    return;
}

var cropStartNs = sourceSession.StartTime + (long)(cropStartSec * 1_000_000_000);
var cropEndNs = sourceSession.StartTime + (long)(cropEndSec * 1_000_000_000);

Console.WriteLine();
Console.WriteLine($"Cropping: {cropStartSec:F3}s – {cropEndSec:F3}s ({cropEndSec - cropStartSec:F3}s)");
Console.WriteLine($"  Absolute: {cropStartNs} – {cropEndNs} ns");

// ─── Create output file ──────────────────────────────────────
var sourceDir = Path.GetDirectoryName(sourcePath) ?? ".";
var sourceNameNoExt = Path.GetFileNameWithoutExtension(sourcePath);
var destPath = Path.Combine(sourceDir, $"{sourceNameNoExt}_cropped.ssn2");

// Avoid overwriting
var counter = 1;
while (File.Exists(destPath))
{
    destPath = Path.Combine(sourceDir, $"{sourceNameNoExt}_cropped_{counter}.ssn2");
    counter++;
}

var destConnectionString = ConnectionStringProvider.ForSQLiteFile(destPath);
var destSessionKey = SessionKey.NewKey();

var croppedName = $"{sourceSession.Identifier} (cropped {cropStartSec:F1}s–{cropEndSec:F1}s)";
using var destClient = sessionManager.CreateSession(destConnectionString, destSessionKey, croppedName, summary.TimeOfRecording, "Cropped");
var destSession = destClient.Session;

Console.WriteLine($"Output file:     {destPath}");
Console.WriteLine();

// ─── Copy parameter configuration ───────────────────────────
var destConfig = destSession.CreateConfiguration();
var addedConversions = new HashSet<string>();
var addedParamGroups = new HashSet<string>();
var addedAppGroups = new HashSet<string>();
var addedChannels = new HashSet<uint>();
var channelIdMap = new Dictionary<uint, uint>(); // old ID → new safe ID
uint nextChannelId = 1;

foreach (var paramBase in sourceSession.Parameters)
{
    if (paramBase is not ChannelBasedParameter param)
    {
        Console.WriteLine($"  Skipping non-channel parameter: {paramBase.Identifier}");
        continue;
    }
    // Copy conversion if not already added
    var convName = param.ConversionFunctionName;
    if (!string.IsNullOrEmpty(convName) && addedConversions.Add(convName))
    {
        try
        {
            var sourceConversion = sourceSession.GetConversion(convName);
            if (sourceConversion is not null)
            {
                destConfig.AddConversion(sourceConversion);
            }
        }
        catch
        {
            // If we can't retrieve the conversion, create a simple 1:1 fallback
            destConfig.AddConversion(RationalConversion.CreateSimple1To1Conversion(convName, "", "%5.4f"));
        }
    }

    // Copy parameter groups
    if (param.GroupIdentifiers is not null)
    {
        foreach (var groupId in param.GroupIdentifiers)
        {
            if (addedParamGroups.Add(groupId))
            {
                destConfig.AddParameterGroup(new ParameterGroup(groupId, groupId));
            }
        }
    }

    // Copy application group
    var appGroupName = param.ApplicationName;
    if (!string.IsNullOrEmpty(appGroupName) && addedAppGroups.Add(appGroupName))
    {
        var memberGroups = sourceSession.Parameters
            .Where(p => p.ApplicationName == appGroupName && p.GroupIdentifiers is not null)
            .SelectMany(p => p.GroupIdentifiers!)
            .Distinct()
            .ToList();

        destConfig.AddGroup(new ApplicationGroup(appGroupName, appGroupName, memberGroups) { SupportsRda = false });
    }

    // Copy channels (avoid duplicates if multiple params share a channel)
    // Remap IDs to sequential values to avoid Int32 overflow in SQLite
    foreach (var channel in param.Channels)
    {
        if (addedChannels.Add(channel.Id))
        {
            var safeId = nextChannelId++;
            channelIdMap[channel.Id] = safeId;
            destConfig.AddChannel(new Channel(
                safeId,
                channel.Name,
                channel.Interval,
                channel.DataType,
                channel.DataSource,
                channel.Name,
                false));
        }
    }

    // Copy parameter with remapped channel IDs
    var remappedChannelIds = param.ChannelIds
        .Where(id => channelIdMap.ContainsKey(id))
        .Select(id => channelIdMap[id])
        .ToList();

    destConfig.AddParameter(new Parameter(
        param.Identifier,
        param.Name,
        param.Description ?? "",
        param.MaximumValue,
        param.MinimumValue,
        param.MaximumValue,
        param.MinimumValue,
        0.0,
        param.DataBitMask,
        param.ErrorBitmask,
        convName,
        param.GroupIdentifiers?.ToList() ?? new List<string>(),
        remappedChannelIds,
        appGroupName));
}

destConfig.Commit();
Console.WriteLine($"Configuration copied: {sourceSession.Parameters.Count} parameter(s)");

// ─── Copy sample data within crop window ─────────────────────
var totalSamples = 0L;

foreach (var paramBase in sourceSession.Parameters)
{
    if (paramBase is not ChannelBasedParameter param) continue;
    var sourceChannelId = param.ChannelIds.FirstOrDefault();
    if (sourceChannelId == 0 || !channelIdMap.TryGetValue(sourceChannelId, out var destChannelId)) continue;

    using var pda = sourceSession.CreateParameterDataAccess(param.Identifier);
    var samples = pda.GetSamplesBetween(cropStartNs, cropEndNs);

    if (samples.SampleCount == 0)
    {
        Console.WriteLine($"  {param.Identifier,-35} 0 samples (no data in range)");
        continue;
    }

    for (var i = 0; i < samples.SampleCount; i++)
    {
        if (samples.DataStatus[i] != DataStatusType.Sample)
            continue;

        destSession.AddChannelData(
            destChannelId,
            samples.Timestamp[i],
            1,
            BitConverter.GetBytes(samples.Data[i]));
    }

    totalSamples += samples.SampleCount;
    Console.WriteLine($"  {param.Identifier,-35} {samples.SampleCount,8} samples copied");
}

// ─── Copy laps within crop window ────────────────────────────
var lapscopied = 0;
foreach (var lap in sourceSession.LapCollection)
{
    if (lap.StartTime >= cropStartNs && lap.StartTime <= cropEndNs)
    {
        destSession.LapCollection.Add(new Lap(lap.StartTime, lap.Number, 0, lap.Name ?? "", lap.CountForFastestLap));
        lapscopied++;
    }
}

// Ensure at least 1 lap exists — SQL Race sessions must always have a lap
if (lapscopied == 0)
{
    destSession.LapCollection.Add(new Lap(cropStartNs, 1, 0, "Lap 1", false));
    lapscopied = 1;
    Console.WriteLine("  (No source laps in range — added default lap at crop start)");
}

// ─── Copy markers within crop window ─────────────────────────
var markersCopied = 0;
foreach (var marker in sourceSession.Markers)
{
    var mStart = marker.StartTimestamp ?? 0L;
    var mEnd = marker.EndTimestamp ?? 0L;
    // Include markers that overlap with the crop window
    if (mStart <= cropEndNs && mEnd >= cropStartNs)
    {
        var clampedStart = Math.Max(mStart, cropStartNs);
        var clampedEnd = Math.Min(mEnd, cropEndNs);
        destSession.Markers.Add(new Marker(clampedStart, clampedEnd, marker.Label ?? "", marker.Label ?? "", marker.Description ?? "", Guid.NewGuid().ToString()));
        markersCopied++;
    }
}

// ─── Copy session items ──────────────────────────────────────
var itemsCopied = 0;
foreach (var item in sourceSession.Items)
{
    destSession.Items.Add(new SessionDataItem(item.Name, item.Value?.ToString() ?? ""));
    itemsCopied++;
}

// ─── Copy constants ──────────────────────────────────────────
var constantsCopied = 0;
foreach (var constant in sourceSession.Constants)
{
    destSession.Constants.Add(new Constant(constant.Name, constant.Value?.ToString() ?? "", constant.Description ?? "", constant.Units ?? "", constant.Format ?? ""));
    constantsCopied++;
}

// ─── Summary ─────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Crop complete.");
Console.WriteLine($"  Source:     {sourcePath}");
Console.WriteLine($"  Output:     {destPath}");
Console.WriteLine($"  Duration:   {cropEndSec - cropStartSec:F3}s (was {durationSeconds:F3}s)");
Console.WriteLine($"  Parameters: {sourceSession.Parameters.Count}");
Console.WriteLine($"  Samples:    {totalSamples:N0}");
Console.WriteLine($"  Segments:   {lapscopied}");
Console.WriteLine($"  Markers:    {markersCopied}");
Console.WriteLine($"  Items:      {itemsCopied}");
Console.WriteLine($"  Constants:  {constantsCopied}");
