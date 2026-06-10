using System.ComponentModel.Composition;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.DotNet;

namespace FunctionLibrary;

/// <summary>
/// A .NET function that computes a rolling average over a configurable time window.
/// Demonstrates state management and timestamp-based windowing.
/// </summary>
/// <remarks>
/// .NET functions are discovered via MEF [Export] and initialised by the function manager.
/// The framework handles execution scheduling; the Initialize method configures the
/// function definition with input/output bindings.
/// </remarks>
[Export(typeof(IDotNetFunction))]
[Serializable]
public class RollingAverageFunction : IDotNetFunction
{
    private const string InputParameter = "Temperature:Sensors";
    private const string OutputParameter = "TemperatureAvg:Calculated";
    private const long WindowNs = 1_000_000_000L; // 1 second window

    [NonSerialized]
    private readonly Queue<(long Timestamp, double Value)> _window = new();

    /// <inheritdoc/>
    public string Name => "RollingAverage";

    /// <summary>
    /// Initialises the function definition with input/output parameter binding.
    /// The framework discovers this function via MEF and calls Initialize at load time.
    /// </summary>
    public void Initialize(IFunctionManager functionManager)
    {
        // TODO: Verify — CreateFunctionDefinition parameter and IFunctionDefinition
        // configuration API may differ across SQL Race versions.
        var definition = functionManager.CreateFunctionDefinition(
            DotNetFunctionConstants.UniqueId);

        // Configure input/output via IFunctionDefinition properties.
        definition.InputParameterIdentifiers.Add(InputParameter);

        // TODO: Verify — OutputParameterDefinitions configuration pattern.
        // Output parameter setup varies by API version; consult the SQL Race
        // function authoring guide for the installed version.

        var buildResult = functionManager.Build(definition);
        if (buildResult?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(
                $"RollingAverageFunction build failed: {string.Join("; ", buildResult.Errors)}");
        }
    }

    /// <summary>
    /// Executes the function. Called by the framework for each execution cycle.
    /// </summary>
    public void Execute(IExecutionContext context)
    {
        // TODO: Verify — IExecutionContext usage pattern for reading inputs
        // and writing outputs varies by SQL Race version.
    }
}
