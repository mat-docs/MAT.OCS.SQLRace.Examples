# LiveDashboard

A console application that connects to a SQL Race session and displays a continuously updating text-based dashboard of parameter values.

## Use Case

Real-time monitoring of live sessions during testing — useful trackside, in a control room, or at a test rig. The text-based approach works over SSH and doesn't require a GUI.

## Prerequisites

- .NET 8 SDK
- MESL.SQLRace.API NuGet package
- A database with at least one session (run GettingStarted first)

For live monitoring, run ServerListenerRecorder in one terminal and LiveDashboard in another.

## Build and Run

```bash
cd projects/csharp
dotnet run --project LiveDashboard
```

Press **Ctrl+C** to stop.

## Configuration

Edit `appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `ConnectionStrings:SqlRace` | (empty → SQLite in temp) | SQL Race connection string |
| `Dashboard:PollingIntervalMs` | `2000` | Refresh interval in milliseconds |
| `Dashboard:SampleWindowSize` | `100` | Number of recent samples for min/max/mean |
| `Dashboard:ParameterIdentifiers` | `[...]` | Parameters to display |

## Architecture

- **Program.cs** — Entry point, session loading, polling loop
- **DashboardRenderer.cs** — Console rendering with in-place updates via `Console.SetCursorPosition`

## Polling vs Event-Driven

This example uses polling for simplicity and compatibility with both live and historical sessions. For event-driven patterns (subscribing to new data as it arrives), see the live-data snippets:
- `snippets/csharp/live-data/subscribe-to-lap-events.cs`
- `snippets/csharp/live-data/subscribe-to-data-events.cs`

## Adapting for a GUI Application

The `DashboardRenderer` can be replaced with any UI framework. The polling loop pattern translates directly to:
- **WPF**: `DispatcherTimer` with data binding
- **Avalonia**: `DispatcherTimer` or reactive `Observable.Interval`
- **Blazor**: `Timer` with `InvokeAsync(StateHasChanged)`

## Expected Output

```
╔══════════════════════════════════════════════════════════════════╗
║  SQL Race Live Dashboard                    [Polling: 2000ms]   ║
║  Session: ServerListener-Demo-20260312      [Data]              ║
╠══════════════════════════════════════════════════════════════════╣
║  Parameter                       Latest        Min        Max   ║
║  ────────────────────────────────────────────────────────────── ║
║  Temperature:Sensors              87.30      62.10      94.50   ║
║  Pressure:Inlet                    2.41       2.10       2.89   ║
║  Speed:Shaft                    1420.00    1380.00    1510.00   ║
╠══════════════════════════════════════════════════════════════════╣
║  Last updated: 14:30:45.123                 Press Ctrl+C to exit║
╚══════════════════════════════════════════════════════════════════╝
```
