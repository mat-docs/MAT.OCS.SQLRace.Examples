# ServerListenerRecorder

A console application that records telemetry data to a local SQLite database, demonstrating the pattern used with the SQL Race Server Listener.

## What is a Server Listener?

In production deployments, SQL Race can receive live telemetry data pushed from ATLAS or another data source via a Server Listener. The listener accepts data over a network socket and writes it to a SQL Race session in real time.

This example simulates that pattern by generating sine wave data and writing it to a session at realistic intervals.

## When to use this pattern

- Receiving telemetry from a test rig or vehicle
- Recording data pushed from ATLAS to a local database
- Building a data acquisition pipeline that writes to SQL Race

## Prerequisites

- .NET 8 SDK
- MESL.SQLRace.API NuGet package

## Build and Run

```bash
cd projects/csharp
dotnet run --project ServerListenerRecorder
```

Press **Ctrl+C** to stop recording gracefully.

## Configuration

Edit `appsettings.json` or set environment variables:

| Setting | Default | Description |
|---------|---------|-------------|
| `ConnectionStrings:SqlRace` | (empty → SQLite in temp) | SQL Race connection string |
| `Recorder:ListenerIpAddress` | `127.0.0.1` | IP to listen on |
| `Recorder:ListenerPort` | `6565` | Port to listen on |
| `Recorder:DataSourcePath` | (empty → temp dir) | Directory for data files |
| `Recorder:SessionIdentifier` | `ServerListener-Demo` | Session name prefix |
| `Recorder:DbEngine` | `SQLite` | Database engine |

## Network Notes

When using a real Server Listener:
- The listener port in this application and the Server Listener port in ATLAS (Tools > SqlRace > Settings) **must be different**
- Ensure your firewall allows inbound connections on the configured port
- For remote connections, use the machine's network IP instead of `127.0.0.1`

## Architecture

```
Program.cs          → Entry point, config, cancellation, main loop
RecorderConfig.cs   → Configuration POCO bound from appsettings.json
Core library        → SqlRaceBootstrapper, SessionFactory, ParameterBuilder
```

## Expected Output

```
SQL Race initialised
Connection: DbEngine=SQLite;Data Source=/tmp/sqlrace-examples.ssn2;
Listener:   127.0.0.1:6565
Session created: <guid>
  Name: ServerListener-Demo-20260312-143022
  Parameters: 3 configured

Recording simulated data (Ctrl+C to stop)...

  Batch 1: Temp=10 Press=5 Speed=20 samples (total: 35)
  Batch 2: Temp=10 Press=5 Speed=20 samples (total: 70)
  ...
^C
Shutdown requested...
Recording stopped. Total batches: 42
Session closed.
```
