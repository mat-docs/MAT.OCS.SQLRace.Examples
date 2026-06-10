// 
// SQL Race Example: SQL Race Example: Timestamp Array Construction
// 
// Demonstrates: Helper patterns for creating timestamp arrays to use with
//               GetData calls — uniform, non-uniform, and from session laps
// Prerequisites: MESL.SQLRace.API (no session data required for the helpers)
// Expected output: Example timestamp arrays with explanations
// Related: read-subsampled-data.cs, multi-rate-alignment.cs
// 

using MESL.SqlRace.Domain;

// --- Pattern 1: Uniform timestamps at a given frequency ---
static long[] UniformTimestamps(long start, long end, double frequencyHz)
{
    var intervalNs = (long)(1_000_000_000.0 / frequencyHz);
    var count = (int)((end - start) / intervalNs);
    var timestamps = new long[count];
    for (var i = 0; i < count; i++)
        timestamps[i] = start + i * intervalNs;
    return timestamps;
}

// --- Pattern 2: Timestamps at specific time offsets (seconds from start) ---
static long[] TimestampsFromOffsets(long startNs, params double[] offsetsInSeconds)
{
    return offsetsInSeconds
        .Select(s => startNs + (long)(s * 1_000_000_000))
        .ToArray();
}

// --- Pattern 3: Logarithmic spacing (useful for frequency response) ---
static long[] LogSpacedTimestamps(long start, long end, int count)
{
    var rangeNs = (double)(end - start);
    var timestamps = new long[count];
    for (var i = 0; i < count; i++)
    {
        var fraction = Math.Pow(10, (double)i / (count - 1) * Math.Log10(rangeNs)) / rangeNs;
        timestamps[i] = start + (long)(fraction * rangeNs);
    }
    return timestamps;
}

// --- Demo ---
var sessionStart = 36_000_000_000_000L; // 10:00:00.000
var sessionEnd = 36_001_000_000_000L;   // 10:00:01.000 (1 second)

Console.WriteLine("=== Pattern 1: Uniform at 10 Hz ===");
var uniform = UniformTimestamps(sessionStart, sessionEnd, 10);
Console.WriteLine($"Count: {uniform.Length}");
foreach (var t in uniform.Take(5))
    Console.WriteLine($"  {t} ns  (t+{(t - sessionStart) / 1e6:F1} ms)");
Console.WriteLine($"  ...");

Console.WriteLine();
Console.WriteLine("=== Pattern 2: Specific offsets (0s, 0.25s, 0.5s, 0.75s, 1.0s) ===");
var offsets = TimestampsFromOffsets(sessionStart, 0, 0.25, 0.5, 0.75, 1.0);
Console.WriteLine($"Count: {offsets.Length}");
foreach (var t in offsets)
    Console.WriteLine($"  {t} ns  (t+{(t - sessionStart) / 1e6:F1} ms)");

Console.WriteLine();
Console.WriteLine("=== Pattern 3: Log-spaced (20 points) ===");
var logSpaced = LogSpacedTimestamps(sessionStart, sessionEnd, 20);
Console.WriteLine($"Count: {logSpaced.Length}");
foreach (var t in logSpaced.Take(5))
    Console.WriteLine($"  {t} ns  (t+{(t - sessionStart) / 1e6:F3} ms)");
Console.WriteLine($"  ...");
Console.WriteLine($"  {logSpaced[^1]} ns  (t+{(logSpaced[^1] - sessionStart) / 1e6:F3} ms)");
