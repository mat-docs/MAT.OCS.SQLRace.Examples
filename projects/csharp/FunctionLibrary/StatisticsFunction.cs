using System.ComponentModel.Composition;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.DotNet;
using MESL.SqlRace.Enumerators;

namespace FunctionLibrary;

/// <summary>
/// A .NET function that computes the ratio of temperature to pressure,
/// demonstrating cross-parameter calculations over multiple inputs.
/// </summary>
/// <remarks>
/// .NET functions are discovered via MEF [Export] and initialised by the function manager.
/// Initialize configures the function definition (inputs, output parameter and the link
/// back to this implementation); Execute is called once per data request to compute values.
/// </remarks>
[Export(typeof(IDotNetFunction))]
[Serializable]
public class StatisticsFunction : IDotNetFunction
{
    private const string InputTemperature = "Temperature:Sensors";
    private const string InputPressure = "Pressure:Inlet";
    private const string OutputName = "TempPressRatio";
    private const string OutputGroup = "Calculated";
    private const string OutputIdentifier = OutputName + ":" + OutputGroup;

    /// <inheritdoc/>
    public string Name => "Statistics";

    /// <summary>
    /// Creates the function definition, binds both input parameters, declares the output
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

        definition.InputParameterIdentifiers.Add(InputTemperature);
        definition.InputParameterIdentifiers.Add(InputPressure);

        definition.OutputParameterDefinitions.Add(new FunctionOutputParameterDefinition
        {
            Name = OutputName,
            ApplicationName = OutputGroup,
            Description = "Ratio of temperature to inlet pressure",
            Units = "degC/bar",
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
                $"StatisticsFunction build failed: {buildResult.Errors.FirstOrDefault()?.ErrorText}");
        }
    }

    /// <summary>
    /// Computes temperature / pressure for each sample. The two inputs are aligned on the
    /// request's timebase, so they share the same sample count and indexing.
    /// </summary>
    public void Execute(IExecutionContext context)
    {
        var sampleCount = context.FunctionInput.Timestamps.Length;
        var temperatureIndex = context.FunctionInput.InputParameterIndexes[InputTemperature];
        var pressureIndex = context.FunctionInput.InputParameterIndexes[InputPressure];
        var outputIndex = context.FunctionOutput.OutputParameterIndexes[OutputIdentifier];

        var temperature = context.FunctionInput.Values[temperatureIndex];
        var pressure = context.FunctionInput.Values[pressureIndex];
        var output = context.FunctionOutput.OutputParametersValues[outputIndex];

        for (var i = 0; i < sampleCount; i++)
        {
            output[i] = Ratio(temperature[i], pressure[i]);
        }
    }

    /// <summary>Temperature-to-pressure ratio; returns 0 when pressure is zero.</summary>
    public static double Ratio(double temperature, double pressure) =>
        pressure != 0.0 ? temperature / pressure : 0.0;
}
