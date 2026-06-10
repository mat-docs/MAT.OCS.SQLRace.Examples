# Cookbook

A task-based index: find the example you need by what you want to accomplish.

## Getting Started

| How do I... | C# | Python | MATLAB |
|------------|-----|--------|--------|
| Load a session from a `.ssn2` file? | [`01-load-session-from-file.cs`](snippets/csharp/getting-started/01-load-session-from-file.cs) | [`01_load_session.py`](snippets/python/getting-started/01_load_session.py) | [`load_session.m`](snippets/matlab/getting-started/load_session.m) |
| Load a session from a database? | [`02-load-session-from-database.cs`](snippets/csharp/getting-started/02-load-session-from-database.cs) | [`01_load_session.py`](snippets/python/getting-started/01_load_session.py) | [`load_session.m`](snippets/matlab/getting-started/load_session.m) |
| Read parameter samples? | [`03-read-parameter-samples.cs`](snippets/csharp/getting-started/03-read-parameter-samples.cs) | [`02_read_parameters.py`](snippets/python/getting-started/02_read_parameters.py) | [`read_parameters.m`](snippets/matlab/getting-started/read_parameters.m) |
| Create a session and write data? | [`04-create-session-write-data.cs`](snippets/csharp/getting-started/04-create-session-write-data.cs) | — | — |
| Find sessions by metadata? | [`05-query-sessions-by-metadata.cs`](snippets/csharp/getting-started/05-query-sessions-by-metadata.cs) | [`03_query_sessions.py`](snippets/python/getting-started/03_query_sessions.py) | [`query_sessions.m`](snippets/matlab/getting-started/query_sessions.m) |
| Verify MATLAB + SQL Race setup? | — | — | [`diagnose_setup.m`](snippets/matlab/getting-started/diagnose_setup.m) |

## Data Access

| How do I... | C# | Python | MATLAB |
|------------|-----|--------|--------|
| Read samples in a time range? | [`read-samples-between.cs`](snippets/csharp/data-access/read-samples-between.cs) | [`read_samples_to_list.py`](snippets/python/data-access/read_samples_to_list.py) | [`extract_to_timetable.m`](snippets/matlab/data-access/extract_to_timetable.m) |
| Downsample / subsample data? | [`read-subsampled-data.cs`](snippets/csharp/data-access/read-subsampled-data.cs) | — | — |
| Read data backwards (reverse iteration)? | [`reverse-iteration.cs`](snippets/csharp/data-access/reverse-iteration.cs) | — | — |
| Align parameters at different rates? | [`multi-rate-alignment.cs`](snippets/csharp/data-access/multi-rate-alignment.cs) | — | — |
| Read many parameters efficiently? | [`bulk-parameter-read.cs`](snippets/csharp/data-access/bulk-parameter-read.cs) | [`multi_parameter_extract.py`](snippets/python/data-access/multi_parameter_extract.py) | [`multi_channel_read.m`](snippets/matlab/data-access/multi_channel_read.m) |
| Handle missing or invalid data? | [`data-status-filtering.cs`](snippets/csharp/data-access/data-status-filtering.cs) | — | — |
| Build timestamp arrays for GetData? | [`timestamp-array-construction.cs`](snippets/csharp/data-access/timestamp-array-construction.cs) | — | — |
| Export data to pandas? | — | [`export_to_pandas.py`](snippets/python/data-access/export_to_pandas.py) | — |
| Export data to MATLAB timetable? | — | — | [`extract_to_timetable.m`](snippets/matlab/data-access/extract_to_timetable.m) |
| Get per-segment statistics? | — | — | [`segment_statistics.m`](snippets/matlab/data-access/segment_statistics.m) |

## Session Management

| How do I... | C# | Python |
|------------|-----|--------|
| Find sessions in a date range? | [`query-with-composite-filter.cs`](snippets/csharp/session-management/query-with-composite-filter.cs) | [`03_query_sessions.py`](snippets/python/getting-started/03_query_sessions.py) |
| Read and write session metadata? | [`session-metadata-crud.cs`](snippets/csharp/session-management/session-metadata-crud.cs) | — |
| Add annotations to a session? | [`marker-management.cs`](snippets/csharp/session-management/marker-management.cs) | — |
| Work with laps and segments? | [`lap-and-segment-management.cs`](snippets/csharp/session-management/lap-and-segment-management.cs) | — |
| Define and read events? | [`event-definitions-and-data.cs`](snippets/csharp/session-management/event-definitions-and-data.cs) | [`04_extract_events.py`](snippets/python/getting-started/04_extract_events.py) |
| Associate sessions together? | [`session-association.cs`](snippets/csharp/session-management/session-association.cs) | — |
| Resolve parameter units? | [`parameter-unit-resolution.cs`](snippets/csharp/session-management/parameter-unit-resolution.cs) | — |

## Live Data

| How do I... | C# |
|------------|-----|
| React to new laps? | [`subscribe-to-lap-events.cs`](snippets/csharp/live-data/subscribe-to-lap-events.cs) |
| React to new data arriving? | [`subscribe-to-data-events.cs`](snippets/csharp/live-data/subscribe-to-data-events.cs) |
| Poll for the latest samples? | [`poll-live-samples.cs`](snippets/csharp/live-data/poll-live-samples.cs) |
| Detect when new parameters appear? | [`rda-parameter-change-detection.cs`](snippets/csharp/live-data/rda-parameter-change-detection.cs) |

## Functions (Calculated Channels)

| How do I... | C# |
|------------|-----|
| Create a calculated channel with FDL? | [`fdl-function-basic.cs`](snippets/csharp/functions/fdl-function-basic.cs) |
| Create a calculated channel with .NET? | [`dotnet-function-basic.cs`](snippets/csharp/functions/dotnet-function-basic.cs) |
| Use a PDA inside a .NET function? | [`dotnet-function-with-pda.cs`](snippets/csharp/functions/dotnet-function-with-pda.cs) |

## Composite Sessions

| How do I... | C# |
|------------|-----|
| Append multiple sessions? | [`append-multiple-sessions.cs`](snippets/csharp/composite-sessions/append-multiple-sessions.cs) |
| Compare whole sessions? | [`whole-session-compare.cs`](snippets/csharp/composite-sessions/whole-session-compare.cs) |
| Use composites with associates? | [`composite-with-associates.cs`](snippets/csharp/composite-sessions/composite-with-associates.cs) |

## Configuration

| How do I... | C# |
|------------|-----|
| Set up parameter configuration manually? | [`create-parameter-config.cs`](snippets/csharp/configuration/create-parameter-config.cs) |
| Create in-memory-only parameters? | [`create-transient-parameter.cs`](snippets/csharp/configuration/create-transient-parameter.cs) |
| Inspect a session's configuration? | [`introspect-session-config.cs`](snippets/csharp/configuration/introspect-session-config.cs) |
| Use different conversion types? | [`conversion-types.cs`](snippets/csharp/configuration/conversion-types.cs) |

## Projects (Tier 2)

For complete, buildable applications, see [projects/](projects/).

| How do I... | Project |
|------------|---------|
| Record live data to SQLite? | [ServerListenerRecorder](projects/csharp/ServerListenerRecorder/) |
| Connect to an ATLAS Data Server? | [StandaloneRecorder](projects/csharp/StandaloneRecorder/) |
| Export sessions to CSV? | [BatchExporter](projects/csharp/BatchExporter/) |
| Monitor a live session in real time? | [LiveDashboard](projects/csharp/LiveDashboard/) |
| Create custom .NET functions? | [FunctionLibrary](projects/csharp/FunctionLibrary/) |
| Browse and export sessions from Python? | [session_explorer](projects/python/session_explorer/) |
| Control ATLAS from Python? | [atlas_automation](projects/python/atlas_automation/) |
| Analyse sessions in MATLAB? | [session_analysis](projects/matlab/session_analysis/) |

### All C# projects

| Project | Description |
|---------|-------------|
| `SqlRace.Examples.Core` | Shared library: bootstrapping, connection strings, fluent builders |
| `GettingStarted` | Console app demonstrating the Core library API |
| `ServerListenerRecorder` | Records multi-rate telemetry to SQLite |
| `StandaloneRecorder` | ADS connection with read-transform-write loop |
| `BatchExporter` | Queries sessions and exports to CSV |
| `LiveDashboard` | Text-based real-time parameter monitor |
| `FunctionLibrary` | Three .NET function patterns (converter, rolling avg, cross-param) |

## Notebooks (Tier 3)

Interactive Jupyter notebooks for exploring SQL Race data. See [notebooks/](notebooks/).

### Core Notebooks

| How do I... | Notebook |
|------------|----------|
| Get started with SQL Race from Python? | [01-getting-started.ipynb](notebooks/01-getting-started.ipynb) |
| Explore session structure and metadata? | [02-session-exploration.ipynb](notebooks/02-session-exploration.ipynb) |
| Analyse multiple parameters together? | [03-multi-parameter-analysis.ipynb](notebooks/03-multi-parameter-analysis.ipynb) |
| Compare data across sessions? | [04-session-comparison.ipynb](notebooks/04-session-comparison.ipynb) |
| Export data to CSV, Parquet, HDF5? | [05-export-pipeline.ipynb](notebooks/05-export-pipeline.ipynb) |
| Monitor a live session? | [06-live-data-monitoring.ipynb](notebooks/06-live-data-monitoring.ipynb) |

### Industry Scenarios

| How do I... | Notebook |
|------------|----------|
| Analyse lap-by-lap telemetry? | [motorsport-lap-analysis.ipynb](notebooks/scenarios/motorsport-lap-analysis.ipynb) |
| Extract flight test manoeuvre windows? | [flight-test-manoeuvre-extraction.ipynb](notebooks/scenarios/flight-test-manoeuvre-extraction.ipynb) |
| Trend turbine performance over time? | [turbine-performance-trending.ipynb](notebooks/scenarios/turbine-performance-trending.ipynb) |
| Compare durability test cycles? | [durability-cycle-comparison.ipynb](notebooks/scenarios/durability-cycle-comparison.ipynb) |
