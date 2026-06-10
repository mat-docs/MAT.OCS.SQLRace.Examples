// ─────────────────────────────────────────────────────────────
// SQL Race Example: .NET Function with PDA
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: A .NET function that creates its own ParameterDataAccess
//               inside Execute for cross-parameter lookups
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: A session with Temperature:Sensors and Pressure:Inlet parameters
// Output: Computed values using data from an additional parameter
//
// Related: snippets/csharp/functions/dotnet-function-basic.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

// NOTE: This is an advanced pattern. The function reads an additional parameter
// via its own PDA to perform cross-parameter calculations (e.g., density
// correction using both temperature and pressure).

using System.ComponentModel.Composition;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Functions;

Console.WriteLine("DensityCorrectionFunction defined (Temperature + Pressure → Density).");
Console.WriteLine("Uses lazy-initialised PDA for cross-parameter lookup in Execute().");
Console.WriteLine("See dotnet-function-basic.cs for the simpler single-input pattern.");

[Export(typeof(IDotNetFunction))]
public class DensityCorrectionFunction : IDotNetFunction
{
    private const string InputTemp = "Temperature:Sensors";
    private const string LookupPressure = "Pressure:Inlet";
    private const string OutputParam = "CorrectedDensity:Calculated";

    [NonSerialized]
    private ParameterDataAccessBase? _pressurePda;
    private Session? _session;

    public IFunctionDefinition? Definition { get; private set; }

    public void Initialize(IFunctionManager functionManager, Session session)
    {
        _session = session;

        Definition = functionManager.CreateFunctionDefinition(
            DotNetFunctionConstants.DotNetFunctionTypeId,
            OutputParam,
            "Density corrected for temperature and pressure");

        Definition.AddInputParameter(InputTemp);
        Definition.SetImplementation(this);
        Definition.SetOutputParameter(OutputParam, "kg/m3", "%6.2f", 0, 2000);

        var errors = functionManager.Build(Definition, session);
        if (errors?.Any() == true)
            throw new InvalidOperationException($"Build failed: {string.Join(", ", errors)}");
    }

    public void Execute(
        IFunctionContext context,
        long timestamp,
        IDictionary<string, double> inputValues,
        IDictionary<string, double> outputValues)
    {
        // --- Lazy-initialise the pressure PDA ---
        _pressurePda ??= _session!.CreateParameterDataAccess(LookupPressure);

        if (!inputValues.TryGetValue(InputTemp, out var tempC))
            return;

        // --- Read pressure at this timestamp ---
        var pressureSamples = _pressurePda.GetData(new[] { timestamp });
        var pressureBar = pressureSamples.SampleCount > 0 ? pressureSamples.Data[0] : 1.013;

        // Ideal gas approximation: density ∝ P / T (simplified)
        var tempK = tempC + 273.15;
        var density = (pressureBar * 100_000) / (287.05 * tempK);
        outputValues[OutputParam] = density;
    }
}


