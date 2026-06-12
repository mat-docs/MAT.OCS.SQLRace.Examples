# MATLAB Snippets

MATLAB examples use `NET.addAssembly` to call the SQL Race .NET API directly from MATLAB.

## Prerequisites

- **MATLAB R2019b+** (R2023b+ recommended for .NET 8 support)
- **SQL Race .NET assemblies** — installed with ATLAS 10+ at the default location
- .NET runtime must be configured in MATLAB. See [MATLAB .NET documentation](https://mathworks.com/help/matlab/matlab_external/call-net-from-matlab.html).

## Setup

Every snippet begins with the assembly loading boilerplate:

```matlab
% Reads from SQLRACE_DLL_PATH env var, falls back to default ATLAS install location
sqlraceDll = getenv('SQLRACE_DLL_PATH');
if isempty(sqlraceDll)
    sqlraceDll = 'C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll';
end
NET.addAssembly(sqlraceDll);

% Import namespaces
import MESL.SqlRace.Domain.*;
```

## Common Issues

| Issue | Solution |
|-------|----------|
| `Could not load file or assembly` | Set the `SQLRACE_DLL_PATH` environment variable to point to your ATLAS installation's `MESL.SqlRace.Domain.dll` |
| `No appropriate method found` | Check argument types — MATLAB may need explicit casting |
| Wrong array types | Use `int64()` for timestamps, `double()` for values |
| Memory issues with large datasets | Extract data in chunks; clear .NET objects when done |

## Connecting to a database

| Backend | Connection string |
|---------|-------------------|
| SQLite (`.ssn2` file) | `DbEngine=SQLite;Data Source=<path-to-file>.ssn2;` |
| SQL Server | `Data Source=<server\instance>;Initial Catalog=<database>;Integrated Security=True;` |

> **SQL Server has no `DbEngine=` prefix.** SQL Race passes SQL Server connection strings
> straight to `SqlClient`, so use a standard SQL Server string. Adding `DbEngine=SQLServer;`
> raises `Keyword not supported: 'dbengine'`. See
> [`getting-started/load_session_from_database.m`](getting-started/load_session_from_database.m).

## Additional assemblies

The getting-started and session-management examples that **create** sessions (rather than
just read them) need two more assemblies alongside `MESL.SqlRace.Domain.dll`:

```matlab
NET.addAssembly(fullfile(installDir, 'MAT.OCS.Core.dll'));            % SessionKey
NET.addAssembly(fullfile(installDir, 'MESL.SqlRace.Enumerators.dll')); % DataType, ChannelDataSourceType, ...
```

Each snippet loads exactly what it needs in its setup block.

## Folders

| Folder | Description |
|--------|-------------|
| [getting-started/](getting-started/) | Load sessions, read parameters, query metadata, verify setup, create a session and write data |
| [data-access/](data-access/) | Read samples in a time range, subsample, reverse iteration, status filtering, timetables, per-segment statistics |
| [session-management/](session-management/) | Session metadata, markers, laps/segments, event extraction |
| [configuration/](configuration/) | Introspect session configuration, resolve parameter units |
| [functions/](functions/) | Read calculated-channel (function) output |
| [live-data/](live-data/) | Poll the latest samples; server-listener live recording |

## Snippets

| File | Demonstrates |
|------|-------------|
| [getting-started/load_session.m](getting-started/load_session.m) | Load a session from SQLite |
| [getting-started/load_session_from_database.m](getting-started/load_session_from_database.m) | Connect to a SQL Server SQL Race database and load a session |
| [getting-started/read_parameters.m](getting-started/read_parameters.m) | Read samples into MATLAB arrays |
| [getting-started/list_parameters.m](getting-started/list_parameters.m) | Catalogue every parameter (metadata + sample counts) and export to CSV |
| [getting-started/query_sessions.m](getting-started/query_sessions.m) | Query sessions with a `ScalarFilter` |
| [getting-started/create_session_write_data.m](getting-started/create_session_write_data.m) | Create a session with multi-rate parameters and write/read data |
| [getting-started/diagnose_setup.m](getting-started/diagnose_setup.m) | Step-by-step setup verification |
| [data-access/read_samples_between.m](data-access/read_samples_between.m) | `GetSamplesBetween` over a time range |
| [data-access/subsampled_read.m](data-access/subsampled_read.m) | `GetData` with a custom timestamp array |
| [data-access/reverse_iteration.m](data-access/reverse_iteration.m) | `GoTo` + `GetNextSamples(StepDirection.Reverse)` |
| [data-access/data_status_filtering.m](data-access/data_status_filtering.m) | Filter samples by `DataStatus` |
| [data-access/extract_to_timetable.m](data-access/extract_to_timetable.m) | Build a MATLAB timetable |
| [data-access/multi_channel_read.m](data-access/multi_channel_read.m) | Read multiple parameters together |
| [data-access/segment_statistics.m](data-access/segment_statistics.m) | Per-segment `GetLapStatistics` |
| [data-access/plot_parameters.m](data-access/plot_parameters.m) | Extract a set of parameters and plot them on a shared time axis |
| [session-management/session_metadata.m](session-management/session_metadata.m) | Write/read `SessionDataItem` metadata |
| [session-management/marker_management.m](session-management/marker_management.m) | Create and read `Marker` annotations |
| [session-management/lap_segment_management.m](session-management/lap_segment_management.m) | Create `Lap` segments |
| [session-management/event_extraction.m](session-management/event_extraction.m) | Enumerate event definitions and read events per segment |
| [configuration/introspect_session_config.m](configuration/introspect_session_config.m) | Enumerate parameters, groups, conversions |
| [configuration/parameter_unit_resolution.m](configuration/parameter_unit_resolution.m) | Resolve units via `Session.GetConversion` |
| [functions/read_function_output.m](functions/read_function_output.m) | Read calculated-channel (`:Functions`) output |
| [live-data/poll_live_samples.m](live-data/poll_live_samples.m) | Poll the most recent samples |
| [live-data/server_listener.m](live-data/server_listener.m) | Discover live sessions and follow live data (requires a live server) |

> **Live data:** `server_listener.m` requires a reachable SQL Server recorder with a live
> session and cannot run against the bundled SQLite scenario data. `poll_live_samples.m`
> demonstrates the polling pattern and runs against any session.
