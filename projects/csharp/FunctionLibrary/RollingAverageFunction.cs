using System.ComponentModel.Composition;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.DotNet;
using MESL.SqlRace.Enumerators;

namespace FunctionLibrary;

/// <summary>
/// A .NET function that computes a rolling (trailing) average over a fixed time window.
/// Demonstrates timestamp-based windowing across a batch of input samples.
/// </summary>
/// <remarks>
/// .NET functions are discovered via MEF [Export] and initialised by the function manager.
/// Initialize configures the function definition (input, output parameter and the link
/// back to this implementation); Execute is called once per data request to compute values.
/// </remarks>
[Export(typeof(IDotNetFunction))]
[Serializable]
public class RollingAverageFunction : IDotNetFunction
{
    private const string InputParameter = "Temperature:Sensors";
    private const string OutputName = "TemperatureAvg";
    private const string OutputGroup = "Calculated";
    private const string OutputIdentifier = OutputName + ":" + OutputGroup;
    private const long WindowNs = 1_000_000_000L; // 1-second trailing window (nanoseconds)

    /// <inheritdoc/>
    public string Name => "RollingAverage";

    /// <summary>
    /// Creates the function definition, binds the input parameter, declares the output
    /// parameter, and links the definition to this implementation so the framework
    /// calls <see cref="Execute"/>.
    /// </summary>
    public void Initialize(IFunctionManager functionManager)
    {
        var definition = functionManager.CreateFunctionDefinition(DotNetFunctionConstants.UniqueId);
        definition.Name = Name;
        definition.CalculateOverWholeSession = false;
        definition.InterpolateBetweenSamples = false;
        definition.JoinGapsAroundNull = false;
        definition.StoreInSession = false;

        // Link the definition to this .NET implementation; without this the framework
        // has no Execute() body to invoke.
        var implementation = (DotNetFunctionImplementationDefinition)definition.ImplementationDefinition;
        implementation.Function = this;

        definition.InputParameterIdentifiers.Add(InputParameter);

        definition.OutputParameterDefinitions.Add(new FunctionOutputParameterDefinition
        {
            Name = OutputName,
            ApplicationName = OutputGroup,
            Description = "1-second rolling average of temperature",
            Units = "degC",
            FormatOverride = "%6.2f",
            ByteOrder = ByteOrderType.BigEndian,
            MinimumValue = "0",
            MaximumValue = "1000",
            ShowInBrowser = true,
        });

        var buildResult = functionManager.Build(definition);
        if (buildResult.Errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"RollingAverageFunction build failed: {buildResult.Errors.FirstOrDefault()?.ErrorText}");
        }
    }

    /// <summary>
    /// For each sample, averages all input samples within the trailing <see cref="WindowNs"/>
    /// window. Uses a two-pointer sweep so the whole batch is processed in a single pass.
    /// </summary>
    public void Execute(IExecutionContext context)
    {
        var timestamps = context.FunctionInput.Timestamps;
        var sampleCount = timestamps.Length;
        var inputIndex = context.FunctionInput.InputParameterIndexes[InputParameter];
        var outputIndex = context.FunctionOutput.OutputParameterIndexes[OutputIdentifier];

        var input = context.FunctionInput.Values[inputIndex];
        var output = context.FunctionOutput.OutputParametersValues[outputIndex];

        ComputeTrailingAverage(timestamps, input, output, WindowNs);
    }

    /// <summary>
    /// Writes into <paramref name="output"/> the trailing-window average of <paramref name="input"/>:
    /// each element is the mean of all samples within <paramref name="windowNs"/> before (and
    /// including) its timestamp. Single-pass two-pointer sweep.
    /// </summary>
    public static void ComputeTrailingAverage(long[] timestamps, double[] input, double[] output, long windowNs)
    {
        var windowStart = 0;
        var windowSum = 0.0;
        for (var i = 0; i < timestamps.Length; i++)
        {
            windowSum += input[i];

            // Drop samples that have fallen outside the trailing time window.
            while (timestamps[i] - timestamps[windowStart] > windowNs)
            {
                windowSum -= input[windowStart];
                windowStart++;
            }

            output[i] = windowSum / (i - windowStart + 1);
        }
    }
}
