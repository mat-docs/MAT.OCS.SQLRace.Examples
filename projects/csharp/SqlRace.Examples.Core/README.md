# SqlRace.Examples.Core

Shared library providing common utilities for SQL Race example projects.

## Components

| Class | Purpose |
|-------|---------|
| `SqlRaceBootstrapper` | Idempotent SQL Race runtime initialisation |
| `ConnectionStringProvider` | Resolves connection strings from config hierarchy |
| `SessionFactory` | Creates and loads sessions with error handling |
| `ParameterBuilder` | Fluent builder for parameter configuration |

## Usage

```csharp
using SqlRace.Examples.Core;

SqlRaceBootstrapper.Initialize();

var connectionString = ConnectionStringProvider.Resolve();

using var client = SessionFactory.CreateSession(connectionString, "My Session");
var session = client.Session;

new ParameterBuilder(session)
    .WithIdentifier("Temperature:Sensors")
    .WithDescription("Inlet temperature")
    .InApplicationGroup("Sensors")
    .InParameterGroup("Thermal")
    .WithFrequency(100, FrequencyUnit.Hz)
    .WithDataType(DataType.Double64Bit)
    .WithConversion("degC", "%5.2f")
    .WithRange(min: 0, max: 500)
    .Build();
```
