// ─────────────────────────────────────────────────────────────
// SQL Race Example: .NET Function with PDA
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: A .NET function that creates its own ParameterDataAccess
//               inside Execute for cross-parameter lookups
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: A session with Temperature:Sensors and Pressure:Inlet parameters
// Output: Computed values published as CorrectedDensity:Functions
//
// Related: snippets/csharp/functions/dotnet-function-basic.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

// NOTE: An advanced pattern. Prefer declaring every parameter you need as an
// input (see dotnet-function-basic.cs) - the framework aligns those for you.
// Reach for a PDA only to sample a parameter off the request's own timebase.

using System.ComponentModel.Composition;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.DotNet;
using MESL.SqlRace.Enumerators;

Console.WriteLine("DensityCorrectionFunction defined (Temperature + Pressure → Density).");
Console.WriteLine("Uses a lazily-created PDA for cross-parameter lookup in Execute().");
Console.WriteLine("See dotnet-function-basic.cs for the simpler single-input pattern.");

[Export(typeof(IDotNetFunction))]
[Serializable]
public class DensityCorrectionFunction : IDotNetFunction
{
    private const string InputTemperature = "Temperature:Sensors";
    private const string LookupPressure = "Pressure:Inlet";
    private const string OutputName = "CorrectedDensity";
    private const string OutputGroup = "Functions";
    private const string OutputIdentifier = OutputName + ":" + OutputGroup;

    // A PDA belongs to one session, so it cannot travel with a serialised function.
    [NonSerialized]
    private ParameterDataAccessBase? pressurePda;

    public string Name => "DensityCorrection";

    public void Initialize(IFunctionManager functionManager)
    {
        var definition = functionManager.CreateFunctionDefinition(DotNetFunctionConstants.UniqueId);
        definition.Name = this.Name;
        definition.CalculateOverWholeSession = false;
        definition.InterpolateBetweenSamples = false;
        definition.JoinGapsAroundNull = false;
        definition.StoreInSession = false;

        // Only temperature is a declared input; pressure is read via the PDA below.
        var implementation = (DotNetFunctionImplementationDefinition)definition.ImplementationDefinition;
        implementation.Function = this;
        definition.InputParameterIdentifiers.Add(InputTemperature);

        definition.OutputParameterDefinitions.Add(new FunctionOutputParameterDefinition
        {
            Name = OutputName,
            ApplicationName = OutputGroup,
            Description = "Density corrected for temperature and pressure",
            Units = "kg/m3",
            FormatOverride = "%6.2f",
            ByteOrder = ByteOrderType.BigEndian,
            MinimumValue = "0",
            MaximumValue = "2000",
            ShowInBrowser = true,
        });

        var buildResults = functionManager.Build(definition);
        if (buildResults.Errors.Count > 0)
            throw new InvalidOperationException($"Build failed: {buildResults.Errors[0].ErrorText}");
    }

    public void Execute(IExecutionContext context)
    {
        // --- Lazily create the pressure PDA against the session being calculated ---
        this.pressurePda ??= context.Session.CreateParameterDataAccess(LookupPressure);

        var timestamps = context.FunctionInput.Timestamps;
        var temperatureIndex = context.FunctionInput.InputParameterIndexes[InputTemperature];
        var outputIndex = context.FunctionOutput.OutputParameterIndexes[OutputIdentifier];

        var temperature = context.FunctionInput.Values[temperatureIndex];
        var output = context.FunctionOutput.OutputParametersValues[outputIndex];

        // One batched read on the request's timestamps, not one read per sample.
        var pressure = this.pressurePda.GetData(timestamps);

        // Ideal gas approximation: density ∝ P / T (simplified)
        for (var i = 0; i < timestamps.Length; i++)
        {
            var pressureBar = i < pressure.SampleCount ? pressure.Data[i] : 1.013;
            var tempK = temperature[i] + 273.15;
            output[i] = pressureBar * 100_000.0 / (287.05 * tempK);
        }
    }
}
