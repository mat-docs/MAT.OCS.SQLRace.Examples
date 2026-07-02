# FunctionLibrary

A class library providing reusable custom .NET functions for SQL Race. These functions create calculated parameters that appear alongside recorded data in ATLAS.

## What are .NET Functions?

SQL Race supports two types of calculated channels:
- **FDL functions** — simple formulas written in FDL syntax (see `snippets/csharp/functions/fdl-function-basic.cs`)
- **.NET functions** — full C# classes implementing `IDotNetFunction`, enabling complex calculations, state management, and cross-parameter lookups

## Functions in this library

| Class | Description | Pattern |
|-------|-------------|---------|
| `UnitConverterFunction` | Celsius → Fahrenheit | Basic: single input, single output |
| `RollingAverageFunction` | Time-windowed rolling average | Stateful: maintains a sample queue across Execute calls |
| `StatisticsFunction` | Temperature/Pressure ratio | Advanced: uses a PDA inside Execute for cross-parameter lookup |

## How .NET Functions Work

1. The function class implements `IDotNetFunction` and is decorated with `[Export(typeof(IDotNetFunction))]`
2. `Initialize` is called once: creates the function definition, binds inputs/outputs, and builds
3. `Execute` is called per sample: reads input values, computes, writes output values
4. The function's output parameter appears in the session's parameter list

## Deployment

To use these functions in ATLAS:

1. Build the library: `dotnet build -c Release`
2. Copy the output DLL to the ATLAS functions directory
3. Restart ATLAS — the functions are discovered via MEF

```bash
dotnet build -c Release
# Copy bin/Release/net8.0/FunctionLibrary.dll to ATLAS functions directory
```

## Building

```bash
cd projects/csharp
dotnet build FunctionLibrary
```

Note: This is a class library, not a console app — it does not run standalone. See `snippets/csharp/functions/` for runnable examples.

## Function Modes

- **Instantaneous**: Execute is called for each sample timestamp independently (default)
- **LeadingEdge**: Execute is called only when new data arrives at the leading edge

All functions in this library use instantaneous mode.

## Debugging

To debug a .NET function:
1. Set breakpoints in the function code
2. Attach the debugger to the ATLAS process
3. Load a session that triggers the function
4. Execute will be called for each sample

Alternatively, write unit tests that create a session, add the function, write data, and verify the output via a PDA.
