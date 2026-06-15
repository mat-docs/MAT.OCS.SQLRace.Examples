// ─────────────────────────────────────────────────────────────
// SQL Race Examples: Function Computation Tests
// ─────────────────────────────────────────────────────────────
//
// Verifies the arithmetic of the FunctionLibrary .NET functions against hand-derived
// expected values. These test the pure compute helpers the functions' Execute() methods
// delegate to, so no SQL Race runtime/licence is required — they run everywhere.
//
// Run: dotnet test --filter "FullyQualifiedName~FunctionComputation"
// ─────────────────────────────────────────────────────────────

using Xunit;
using FunctionLibrary;

namespace SqlRace.Examples.Tests;

public class FunctionComputationTests
{
    [Theory]
    [InlineData(0.0, 32.0)]
    [InlineData(20.0, 68.0)]
    [InlineData(100.0, 212.0)]
    [InlineData(-40.0, -40.0)]
    public void UnitConverter_CelsiusToFahrenheit(double celsius, double expectedF)
    {
        Assert.Equal(expectedF, UnitConverterFunction.ToFahrenheit(celsius), precision: 9);
    }

    [Theory]
    [InlineData(20.0, 2.0, 10.0)]
    [InlineData(50.0, 5.0, 10.0)]
    [InlineData(7.0, 0.0, 0.0)] // guard: divide-by-zero returns 0
    public void Statistics_Ratio(double temperature, double pressure, double expected)
    {
        Assert.Equal(expected, StatisticsFunction.Ratio(temperature, pressure), precision: 9);
    }

    [Fact]
    public void RollingAverage_ConstantInput_IsUnchanged()
    {
        var n = 50;
        var timestamps = new long[n];
        var input = new double[n];
        var output = new double[n];
        for (var i = 0; i < n; i++)
        {
            timestamps[i] = i * 100_000_000L; // 10 Hz
            input[i] = 5.0;
        }

        RollingAverageFunction.ComputeTrailingAverage(timestamps, input, output, 1_000_000_000L);

        Assert.All(output, v => Assert.Equal(5.0, v, precision: 9));
    }

    [Fact]
    public void RollingAverage_RampInput_MatchesHandComputedWindow()
    {
        // 10 Hz samples, 1-second trailing window → up to 11 samples (indices spanning 1 s).
        var n = 12;
        var timestamps = new long[n];
        var input = new double[n];
        var output = new double[n];
        for (var i = 0; i < n; i++)
        {
            timestamps[i] = i * 100_000_000L; // 0.1 s apart
            input[i] = i;                      // ramp: value == index
        }

        RollingAverageFunction.ComputeTrailingAverage(timestamps, input, output, 1_000_000_000L);

        Assert.Equal(0.0, output[0], precision: 9);   // mean(0)
        Assert.Equal(2.5, output[5], precision: 9);    // mean(0..5)
        Assert.Equal(5.0, output[10], precision: 9);   // mean(0..10), window still 1 s
        Assert.Equal(6.0, output[11], precision: 9);   // index 0 drops out → mean(1..11)
    }
}
