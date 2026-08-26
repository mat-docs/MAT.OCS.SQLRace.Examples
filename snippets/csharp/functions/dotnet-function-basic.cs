// ─────────────────────────────────────────────────────────────
// SQL Race Example: .NET Function (Basic)
// ─────────────────────────────────────────────────────────────
//
// Demonstrates: Implementing IDotNetFunction to create a calculated parameter
//               that doubles an input value
// Prerequisites: MESL.SQLRace.API NuGet package, .NET 8
// Input: A session with a Temperature:Sensors parameter
// Output: Doubled values published as DoubledTemperature:Functions
//
// Related: snippets/csharp/functions/fdl-function-basic.cs
// Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
// ─────────────────────────────────────────────────────────────

// NOTE: This file shows the complete pattern for a .NET function. In production
// the class would live in a separate assembly, discovered via MEF [Export].

using System.ComponentModel.Composition;
using MESL.SqlRace.Domain.Functions;
using MESL.SqlRace.Domain.Functions.DotNet;
using MESL.SqlRace.Enumerators;

Console.WriteLine("DoubleValueFunction defined.");
Console.WriteLine("See fdl-function-basic.cs for a complete runnable example.");
Console.WriteLine("In production, .NET functions are discovered via MEF [Export] attributes.");

// --- The function implementation ---
[Export(typeof(IDotNetFunction))]
[Serializable]
public class DoubleValueFunction : IDotNetFunction
{
    private const string InputParameter = "Temperature:Sensors";
    private const string OutputName = "DoubledTemperature";
    private const string OutputGroup = "Functions";
    private const string OutputIdentifier = OutputName + ":" + OutputGroup;

    // Diagnostics only; keep it the same as the definition's name.
    public string Name => "DoubleValue";

    // Called once by the function manager to declare what this function
    // consumes, what it publishes, and where its code lives.
    public void Initialize(IFunctionManager functionManager)
    {
        var definition = functionManager.CreateFunctionDefinition(DotNetFunctionConstants.UniqueId);
        definition.Name = this.Name;
        definition.CalculateOverWholeSession = false;
        definition.InterpolateBetweenSamples = false;
        definition.JoinGapsAroundNull = false;
        definition.StoreInSession = false;

        // Link the definition to this implementation; without it the framework
        // has no Execute() body to invoke.
        var implementation = (DotNetFunctionImplementationDefinition)definition.ImplementationDefinition;
        implementation.Function = this;

        definition.InputParameterIdentifiers.Add(InputParameter);

        definition.OutputParameterDefinitions.Add(new FunctionOutputParameterDefinition
        {
            Name = OutputName,
            ApplicationName = OutputGroup,
            Description = "Temperature doubled for testing",
            Units = "degC",
            FormatOverride = "%5.1f",
            ByteOrder = ByteOrderType.BigEndian,
            MinimumValue = "0",
            MaximumValue = "1000",
            ShowInBrowser = true,
        });

        var buildResults = functionManager.Build(definition);
        if (buildResults.Errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Function build failed: {buildResults.Errors[0].ErrorText}");
        }
    }

    // Called once per data request with a batch of samples. Inputs and outputs
    // are aligned on the request's timebase, so they share indexing.
    public void Execute(IExecutionContext context)
    {
        var sampleCount = context.FunctionInput.Timestamps.Length;
        var inputIndex = context.FunctionInput.InputParameterIndexes[InputParameter];
        var outputIndex = context.FunctionOutput.OutputParameterIndexes[OutputIdentifier];

        var input = context.FunctionInput.Values[inputIndex];
        var output = context.FunctionOutput.OutputParametersValues[outputIndex];

        for (var i = 0; i < sampleCount; i++)
        {
            output[i] = input[i] * 2.0;
        }
    }
}

// --- Usage (host application), shown manually for clarity ---
// var functionManager = FunctionManagerFactory.Create();
// new DoubleValueFunction().Initialize(functionManager);
// using var pda = session.CreateParameterDataAccess("DoubledTemperature:Functions");
