<img src="images/ATLAS_logo_GR.png" alt="ATLAS" width="260">

# SQL Race API Examples

Examples for the [SQL Race API](https://github.com/mat-docs/packages) — a .NET library for reading and writing high-frequency time-series data stored in SQLite (`.ssn2`) and SQL Server databases.

SQL Race is used across **motorsport, aerospace, automotive, energy, and defence** for recording and analysing sensor data from test sessions.

## Quick Start

```bash
# 1. Create a new .NET 8 console project
dotnet new console -n MyFirstSession
cd MyFirstSession

# 2. Add the SQL Race NuGet package
dotnet nuget add source https://nuget.pkg.github.com/mat-docs/index.json --name mat-docs
dotnet add package MESL.SQLRace.API

# 3. Copy a snippet and run
# Copy the contents of snippets/csharp/getting-started/01-load-session-from-file.cs
# into Program.cs, then:
dotnet run
```

> **No server required.** All getting-started snippets default to a local SQLite file — no SQL Server, no configuration.

## I want to...

| Task | Language | Example |
|------|----------|---------|
| Load a session from a `.ssn2` file | C# | [`01-load-session-from-file.cs`](snippets/csharp/getting-started/01-load-session-from-file.cs) |
| Load a session from a database | C# | [`02-load-session-from-database.cs`](snippets/csharp/getting-started/02-load-session-from-database.cs) |
| Read parameter samples | C# | [`03-read-parameter-samples.cs`](snippets/csharp/getting-started/03-read-parameter-samples.cs) |
| Create a session and write data | C# | [`04-create-session-write-data.cs`](snippets/csharp/getting-started/04-create-session-write-data.cs) |
| Query sessions by metadata | C# | [`05-query-sessions-by-metadata.cs`](snippets/csharp/getting-started/05-query-sessions-by-metadata.cs) |
| Read data in a time range | C# | [`read-samples-between.cs`](snippets/csharp/data-access/read-samples-between.cs) |
| Downsample data | C# | [`read-subsampled-data.cs`](snippets/csharp/data-access/read-subsampled-data.cs) |
| Read data in reverse | C# | [`reverse-iteration.cs`](snippets/csharp/data-access/reverse-iteration.cs) |
| Align multi-rate parameters | C# | [`multi-rate-alignment.cs`](snippets/csharp/data-access/multi-rate-alignment.cs) |
| Read many parameters efficiently | C# | [`bulk-parameter-read.cs`](snippets/csharp/data-access/bulk-parameter-read.cs) |
| Handle missing data | C# | [`data-status-filtering.cs`](snippets/csharp/data-access/data-status-filtering.cs) |
| Build timestamp arrays | C# | [`timestamp-array-construction.cs`](snippets/csharp/data-access/timestamp-array-construction.cs) |
| Load a session | Python | [`01_load_session.py`](snippets/python/getting-started/01_load_session.py) |
| Load a session | MATLAB | [`load_session.m`](snippets/matlab/getting-started/load_session.m) |
| Connect to a SQL Server database | MATLAB | [`load_session_from_database.m`](snippets/matlab/getting-started/load_session_from_database.m) |
| List all parameters in a session | MATLAB | [`list_parameters.m`](snippets/matlab/getting-started/list_parameters.m) |
| Create a session and write data | MATLAB | [`create_session_write_data.m`](snippets/matlab/getting-started/create_session_write_data.m) |
| Read data in a time range | MATLAB | [`read_samples_between.m`](snippets/matlab/data-access/read_samples_between.m) |
| Plot a set of parameters | MATLAB | [`plot_parameters.m`](snippets/matlab/data-access/plot_parameters.m) |
| Work with markers, laps, events, metadata | MATLAB | [`session-management/`](snippets/matlab/session-management/) |
| Inspect session configuration and units | MATLAB | [`configuration/`](snippets/matlab/configuration/) |
| Poll the latest live samples | MATLAB | [`poll_live_samples.m`](snippets/matlab/live-data/poll_live_samples.m) |

See [COOKBOOK.md](COOKBOOK.md) for a complete task-based index.

## Repository Map

```
sql-race/
├── snippets/          Tier 1 — Single-file, copy-pasteable examples (C#, Python, MATLAB)
├── projects/          Tier 2 — Complete, buildable projects with shared Core library
├── notebooks/         Tier 3 — Interactive Jupyter notebooks with narrative and plots
├── docs/              Guides: getting started, connection strings, troubleshooting
├── tests/             Compilation and integration tests
└── .github/           CI workflows and issue templates
```

### Tiers explained

| Tier | What | Who it's for |
|------|------|-------------|
| **Snippets** (`snippets/`) | Single-file, self-contained examples. Copy, paste, run. | Developers evaluating the API or looking for a quick recipe. |
| **Projects** (`projects/`) | Complete, buildable solutions with a shared Core library. | Teams building production applications. |
| **Notebooks** (`notebooks/`) | Interactive Jupyter notebooks with narrative and visualisation. | Data scientists and analysts exploring session data. |

## Language Support

| Language | Snippets | Projects | Notebooks |
|----------|----------|----------|-----------|
| C# (.NET 8) | Yes | Yes | — |
| Python (pythonnet) | Yes | Yes | Yes |
| MATLAB | Yes | Yes | — |

## Industry Applicability

SQL Race uses generic concepts that map to any domain:

| SQL Race | Motorsport | Aerospace | Energy | Automotive |
|----------|-----------|-----------|--------|------------|
| Session | Race / Practice | Flight / Sortie | Test Run | Drive Cycle |
| Lap | Lap | Test Point | Regime | Segment |
| Parameter | Channel | Measurement | Tag | Signal |

See [CONCEPTS.md](CONCEPTS.md) for a complete mapping.

## Prerequisites

- **.NET 8 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **MESL.SQLRace.API** NuGet package from the [MA GitHub Packages feed](https://nuget.pkg.github.com/mat-docs/index.json)
- **ATLAS 11+** (optional) — required only for live data and server-connected scenarios

See [docs/getting-started.md](docs/getting-started.md) for detailed setup instructions.

## Contributing

See [.github/CONTRIBUTING.md](.github/CONTRIBUTING.md) for guidelines.

## License

See [LICENSE](LICENSE) for details.
