using System.ComponentModel.Composition;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.DotNet;

namespace FunctionLibrary;

/// <summary>
/// A .NET function that computes the ratio of temperature to pressure,
/// demonstrating cross-parameter calculations.
/// </summary>
/// <remarks>
/// .NET functions are discovered via MEF [Export] and initialised by the function manager.
/// The framework handles execution scheduling; the Initialize method configures the
/// function definition with input/output bindings.
/// </remarks>
[Export(typeof(IDotNetFunction))]
[Serializable]
public class StatisticsFunction : IDotNetFunction
{
    private const string InputParameter = "Temperature:Sensors";
    private const string LookupParameter = "Pressure:Inlet";
    private const string OutputParameter = "TempPressRatio:Calculated";

    /// <inheritdoc/>
    public string Name => "Statistics";

    /// <summary>
    /// Initialises the function, configuring input parameters for cross-parameter calculation.
    /// </summary>
    public void Initialize(IFunctionManager functionManager)
    {
        // TODO: Verify — CreateFunctionDefinition parameter and IFunctionDefinition
        // configuration API may differ across SQL Race versions.
        var definition = functionManager.CreateFunctionDefinition(
            DotNetFunctionConstants.UniqueId);

        definition.InputParameterIdentifiers.Add(InputParameter);
        definition.InputParameterIdentifiers.Add(LookupParameter);

        // TODO: Verify — OutputParameterDefinitions configuration pattern.
        // Output parameter setup (unit: degC/bar, format: %6.2f, range: 0 to 1000)
        // varies by API version; consult the SQL Race function authoring guide.

        var buildResult = functionManager.Build(definition);
        if (buildResult?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(
                $"StatisticsFunction build failed: {string.Join("; ", buildResult.Errors)}");
        }
    }

    /// <summary>
    /// Executes the function. Called by the framework for each execution cycle.
    /// </summary>
    public void Execute(IExecutionContext context)
    {
        // TODO: Verify — IExecutionContext usage pattern for cross-parameter ratio.
    }
}
