# StandaloneRecorder

A console application that demonstrates the pattern for connecting to an ATLAS Data Server (ADS), recording live telemetry, and simultaneously processing data with a read-transform-write loop.

## Server Listener vs Standalone Recorder

| Aspect | Server Listener | Standalone Recorder |
|--------|----------------|-------------------|
| Direction | Passive: receives data pushed to it | Active: connects to an ADS |
| Initiation | Data source pushes | This application pulls |
| Use case | Receiving from ATLAS or a test rig | Connecting to a running ADS instance |

## What this example demonstrates

1. Session creation with `SessionFactory`
2. Multi-parameter configuration with `ParameterBuilder`
3. A **read-transform-write** loop: raw telemetry → exponential moving average → derived parameter
4. Async operation with `CancellationToken` for graceful Ctrl+C shutdown
5. The pattern for detecting new sessions via `SessionManager.SessionEventOccurred`

The ADS connection code is stubbed with `// TODO: Verify` comments since it requires a running ADS instance.

## Prerequisites

- .NET 8 SDK
- MESL.SQLRace.API NuGet package
- For real ADS connection: ATLAS 10 with Server Listener enabled (Tools > SqlRace > Settings)

## Build and Run

```bash
cd projects/csharp
dotnet run --project StandaloneRecorder
```

Press **Ctrl+C** to stop.

## Configuration

Edit `appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `ConnectionStrings:SqlRace` | (empty → SQLite in temp) | SQL Race connection string |
| `Recorder:AdsHost` | `127.0.0.1` | ATLAS Data Server hostname |
| `Recorder:ListenerIpAddress` | `127.0.0.1` | Local listener IP |
| `Recorder:ListenerPort` | `6565` | Local listener port |
| `Recorder:DataSourcePath` | (empty → temp dir) | Data file directory |

## Data Flow

```
ADS (or simulation) → Raw data → Session (source parameter)
                                       ↓
                              EMA Transform (α=0.1)
                                       ↓
                              Session (derived parameter)
```

## Expected Output

```
SQL Race initialised
Session created: <guid>
  Source:  Temperature:Sensors (100 Hz)
  Derived: TemperatureSmoothed:Derived (EMA)

Recording and processing (Ctrl+C to stop)...

  Batch    1: READ raw → TRANSFORM (EMA α=0.1) → WRITE smoothed  [latest: raw=75.30, smoothed=75.03]
  Batch    2: READ raw → TRANSFORM (EMA α=0.1) → WRITE smoothed  [latest: raw=77.94, smoothed=75.32]
  ...
```
