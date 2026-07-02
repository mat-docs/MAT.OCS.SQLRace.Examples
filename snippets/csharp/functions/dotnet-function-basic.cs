// ─────────────────────────────────────────────────────────────
// SQL Race Example: .NET Function (Basic)
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Implementing IDotNetFunction to create a calculated parameter
//               that doubles an input value
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: A session with a Temperature:Sensors parameter
// Output: Doubled values from the .NET function output
//
// Related: snippets/csharp/functions/fdl-function-basic.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

// NOTE: This file shows the complete pattern for a .NET function.
// In production, the function class would be in a separate assembly
// loaded by the function manager via MEF [Export] attributes.

using System.ComponentModel.Composition;
using MESL.SqlRace.Domain;
using MESL.SqlRace.Domain.Functions;

Console.WriteLine("DoubleValueFunction defined.");
Console.WriteLine("See fdl-function-basic.cs for a complete runnable example.");
Console.WriteLine("In production, .NET functions are discovered via MEF [Export] attributes.");

// --- The function implementation ---
[Export(typeof(IDotNetFunction))]
public class DoubleValueFunction : IDotNetFunction
{
    private const string InputParam = "Temperature:Sensors";
    private const string OutputParam = "DoubledTemperature:Calculated";

    public IFunctionDefinition? Definition { get; private set; }

    public void Initialize(IFunctionManager functionManager, Session session)
    {
        // --- Create the function definition ---
        Definition = functionManager.CreateFunctionDefinition(
            DotNetFunctionConstants.DotNetFunctionTypeId,
            OutputParam,
            "Temperature doubled for testing");

        Definition.AddInputParameter(InputParam);
        Definition.SetImplementation(this);
        Definition.SetOutputParameter(OutputParam, "degC", "%5.1f", 0, 1000);

        var errors = functionManager.Build(Definition, session);
        if (errors?.Any() == true)
            throw new InvalidOperationException($"Function build failed: {string.Join(", ", errors)}");
    }

    public void Execute(
        IFunctionContext context,
        long timestamp,
        IDictionary<string, double> inputValues,
        IDictionary<string, double> outputValues)
    {
        if (inputValues.TryGetValue(InputParam, out var input))
        {
            outputValues[OutputParam] = input * 2.0;
        }
    }
}

// --- Usage (host application) ---
// NOTE: In practice, function assemblies are discovered via MEF.
// This snippet shows the manual pattern for clarity.
// var functionManager = FunctionManagerFactory.Create();
// var func = new DoubleValueFunction();
// func.Initialize(functionManager, session);
// using var pda = session.CreateParameterDataAccess("DoubledTemperature:Calculated");


