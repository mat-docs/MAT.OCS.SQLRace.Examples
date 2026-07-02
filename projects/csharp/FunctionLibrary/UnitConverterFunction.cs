using System.ComponentModel.Composition;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.DotNet;
using MESL.SqlRace.Enumerators;

namespace FunctionLibrary;

/// <summary>
/// A .NET function that converts temperature from Celsius to Fahrenheit.
/// Demonstrates the basic IDotNetFunction pattern: single input, single output.
/// </summary>
/// <remarks>
/// .NET functions are discovered via MEF [Export] and initialised by the function manager.
/// Initialize configures the function definition (inputs, output parameter and the link
/// back to this implementation); Execute is called once per data request to compute values.
/// </remarks>
[Export(typeof(IDotNetFunction))]
[Serializable]
public class UnitConverterFunction : IDotNetFunction
{
    private const string InputParameter = "Temperature:Sensors";
    private const string OutputName = "TemperatureFahrenheit";
    private const string OutputGroup = "Calculated";
    private const string OutputIdentifier = OutputName + ":" + OutputGroup;

    /// <inheritdoc/>
    public string Name => "UnitConverter";

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
            Description = "Temperature converted to Fahrenheit",
            Units = "degF",
            FormatOverride = "%5.1f",
            ByteOrder = ByteOrderType.BigEndian,
            MinimumValue = "-40",
            MaximumValue = "1000",
            ShowInBrowser = true,
        });

        var buildResult = functionManager.Build(definition);
        if (buildResult.Errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"UnitConverterFunction build failed: {buildResult.Errors.FirstOrDefault()?.ErrorText}");
        }
    }

    /// <summary>
    /// Converts each Celsius input sample to Fahrenheit. Called once per data request
    /// with a batch of samples in <see cref="IExecutionContext.FunctionInput"/>.
    /// </summary>
    public void Execute(IExecutionContext context)
    {
        var sampleCount = context.FunctionInput.Timestamps.Length;
        var inputIndex = context.FunctionInput.InputParameterIndexes[InputParameter];
        var outputIndex = context.FunctionOutput.OutputParameterIndexes[OutputIdentifier];

        var input = context.FunctionInput.Values[inputIndex];
        var output = context.FunctionOutput.OutputParametersValues[outputIndex];

        for (var i = 0; i < sampleCount; i++)
        {
            output[i] = ToFahrenheit(input[i]);
        }
    }

    /// <summary>Converts a Celsius value to Fahrenheit.</summary>
    public static double ToFahrenheit(double celsius) => celsius * 9.0 / 5.0 + 32.0;
}
