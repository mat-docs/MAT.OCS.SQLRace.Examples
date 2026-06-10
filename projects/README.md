# Projects (Tier 2)

Complete, buildable solutions demonstrating production-quality patterns with the SQL Race API.

## C#

All C# projects are in the `SqlRace.Examples.sln` solution. Build all at once:

```bash
cd projects/csharp
dotnet build SqlRace.Examples.sln
```

| Project | Type | Description |
|---------|------|-------------|
| [SqlRace.Examples.Core](csharp/SqlRace.Examples.Core/) | Library | Shared utilities: bootstrapping, connection strings, fluent parameter builder |
| [GettingStarted](csharp/GettingStarted/) | Console | Demonstrates the Core library API: create session, add parameters, write/read data |
| [ServerListenerRecorder](csharp/ServerListenerRecorder/) | Console | Records telemetry data to SQLite with multi-rate parameters (100/50/200 Hz) |
| [StandaloneRecorder](csharp/StandaloneRecorder/) | Console | ADS connection pattern with read-transform-write loop (EMA smoothing) |
| [BatchExporter](csharp/BatchExporter/) | Console | Queries sessions and exports parameter data to CSV files |
| [LiveDashboard](csharp/LiveDashboard/) | Console | Text-based real-time dashboard with polling and live statistics |
| [FunctionLibrary](csharp/FunctionLibrary/) | Library | Three .NET function patterns: unit converter, rolling average, cross-parameter statistics |

## Python

| Project | Description |
|---------|-------------|
| [session_explorer](python/session_explorer/) | Interactive CLI for browsing sessions and exporting to CSV/pickle |
| [atlas_automation](python/atlas_automation/) | ATLAS Automation API controller (connection pattern, stubbed) |

## MATLAB

| Project | Description |
|---------|-------------|
| [session_analysis](matlab/session_analysis/) | Full analysis workflow: load, extract, statistics, plot, save to .mat |
