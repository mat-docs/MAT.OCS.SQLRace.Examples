using System.ComponentModel.Composition;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.DotNet;

namespace FunctionLibrary;

/// <summary>
/// A .NET function that converts temperature from Celsius to Fahrenheit.
/// Demonstrates the basic IDotNetFunction pattern: single input, single output.
/// </summary>
/// <remarks>
/// .NET functions are discovered via MEF [Export] and initialised by the function manager.
/// The framework handles execution scheduling; the Initialize method configures the
/// function definition with input/output bindings.
/// </remarks>
[Export(typeof(IDotNetFunction))]
[Serializable]
public class UnitConverterFunction : IDotNetFunction
{
    private const string InputParameter = "Temperature:Sensors";
    private const string OutputParameter = "TemperatureFahrenheit:Calculated";

    /// <inheritdoc/>
    public string Name => "UnitConverter";

    /// <summary>
    /// Initialises the function by creating its definition and binding parameters.
    /// </summary>
    public void Initialize(IFunctionManager functionManager)
    {
        // TODO: Verify — CreateFunctionDefinition parameter and IFunctionDefinition
        // configuration API may differ across SQL Race versions.
        var definition = functionManager.CreateFunctionDefinition(
            DotNetFunctionConstants.UniqueId);

        definition.InputParameterIdentifiers.Add(InputParameter);

        // TODO: Verify — OutputParameterDefinitions configuration pattern.
        // Output parameter setup (unit: degF, format: %5.1f, range: -40 to 1000)
        // varies by API version; consult the SQL Race function authoring guide.

        var buildResult = functionManager.Build(definition);
        if (buildResult?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(
                $"UnitConverterFunction build failed: {string.Join("; ", buildResult.Errors)}");
        }
    }

    /// <summary>
    /// Executes the function. Called by the framework for each execution cycle.
    /// </summary>
    public void Execute(IExecutionContext context)
    {
        // TODO: Verify — IExecutionContext usage pattern for C-to-F conversion.
    }
}
