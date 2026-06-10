<img src="../images/ATLAS_logo_GR.png" alt="ATLAS" width="200">

# Snippets (Tier 1)

Single-file, self-contained examples you can copy-paste into your own project.

Each snippet:
- Is fully self-contained with all `using` statements and a `Main` method
- References only `MESL.SQLRace.API` — no other dependencies
- Defaults to a local SQLite connection (no configuration needed)
- Is under 80 lines
- Includes a header comment explaining what it does and expected output

## Languages

- [C#](csharp/) — comprehensive coverage of all SQL Race features
- [Python](python/) — key scenarios using pythonnet
- [MATLAB](matlab/) — key scenarios using .NET interop

## Complete Index

### C# — Getting Started

| File | Description |
|------|-------------|
| [`01-load-session-from-file.cs`](csharp/getting-started/01-load-session-from-file.cs) | Load an .ssn2 file, print session summary |
| [`02-load-session-from-database.cs`](csharp/getting-started/02-load-session-from-database.cs) | Load a session from SQLite by key |
| [`03-read-parameter-samples.cs`](csharp/getting-started/03-read-parameter-samples.cs) | Create PDA, read samples, print values |
| [`04-create-session-write-data.cs`](csharp/getting-started/04-create-session-write-data.cs) | Create session, write data, read back to verify |
| [`05-query-sessions-by-metadata.cs`](csharp/getting-started/05-query-sessions-by-metadata.cs) | Query sessions with ScalarFilter |

### C# — Data Access

| File | Description |
|------|-------------|
| [`read-samples-between.cs`](csharp/data-access/read-samples-between.cs) | GetSamplesBetween for a time range |
| [`read-subsampled-data.cs`](csharp/data-access/read-subsampled-data.cs) | GetData with custom timestamp array for downsampling |
| [`reverse-iteration.cs`](csharp/data-access/reverse-iteration.cs) | GetNextSamples with StepDirection.Reverse |
| [`multi-rate-alignment.cs`](csharp/data-access/multi-rate-alignment.cs) | Align two parameters to common timestamps |
| [`bulk-parameter-read.cs`](csharp/data-access/bulk-parameter-read.cs) | Read all parameters with PDA lifecycle management |
| [`data-status-filtering.cs`](csharp/data-access/data-status-filtering.cs) | Check DataStatusType for missing data |
| [`timestamp-array-construction.cs`](csharp/data-access/timestamp-array-construction.cs) | Uniform, offset, and log-spaced timestamp patterns |

### C# — Session Management

| File | Description |
|------|-------------|
| [`query-with-composite-filter.cs`](csharp/session-management/query-with-composite-filter.cs) | CompositeFilter with CombineType.AND |
| [`session-metadata-crud.cs`](csharp/session-management/session-metadata-crud.cs) | Create/reload/read SessionDataItem entries |
| [`marker-management.cs`](csharp/session-management/marker-management.cs) | Marker with MarkerLabel annotations |
| [`lap-and-segment-management.cs`](csharp/session-management/lap-and-segment-management.cs) | Lap objects as named test segments |
| [`event-definitions-and-data.cs`](csharp/session-management/event-definitions-and-data.cs) | EventDefinition, AddEventData, GetEventData |
| [`session-association.cs`](csharp/session-management/session-association.cs) | Associate sessions and load with associates |
| [`parameter-unit-resolution.cs`](csharp/session-management/parameter-unit-resolution.cs) | Resolve units from conversion metadata |

### C# — Live Data

| File | Description |
|------|-------------|
| [`subscribe-to-lap-events.cs`](csharp/live-data/subscribe-to-lap-events.cs) | LapStarted handler with async/cancellation |
| [`subscribe-to-data-events.cs`](csharp/live-data/subscribe-to-data-events.cs) | EventDataAdded handler with clean unsubscription |
| [`poll-live-samples.cs`](csharp/live-data/poll-live-samples.cs) | GoTo/GetNextSamples polling loop with Task.Delay |
| [`rda-parameter-change-detection.cs`](csharp/live-data/rda-parameter-change-detection.cs) | Wait for parameter to appear with TaskCompletionSource |

### C# — Functions

| File | Description |
|------|-------------|
| [`fdl-function-basic.cs`](csharp/functions/fdl-function-basic.cs) | FDL Celsius-to-Fahrenheit with build/consume lifecycle |
| [`dotnet-function-basic.cs`](csharp/functions/dotnet-function-basic.cs) | IDotNetFunction with MEF Export |
| [`dotnet-function-with-pda.cs`](csharp/functions/dotnet-function-with-pda.cs) | .NET function with lazy PDA for cross-parameter lookup |

### C# — Composite Sessions

| File | Description |
|------|-------------|
| [`append-multiple-sessions.cs`](csharp/composite-sessions/append-multiple-sessions.cs) | Sequential sessions with cross-boundary reads |
| [`whole-session-compare.cs`](csharp/composite-sessions/whole-session-compare.cs) | CompositeSessionContainer with WholeSession compare |
| [`composite-with-associates.cs`](csharp/composite-sessions/composite-with-associates.cs) | Primary + associate sessions unified via composite |

### C# — Configuration

| File | Description |
|------|-------------|
| [`create-parameter-config.cs`](csharp/configuration/create-parameter-config.cs) | Full manual step-by-step parameter setup |
| [`create-transient-parameter.cs`](csharp/configuration/create-transient-parameter.cs) | In-memory-only parameters with CreateTransientConfiguration |
| [`introspect-session-config.cs`](csharp/configuration/introspect-session-config.cs) | Enumerate parameters, channels, groups, conversions |
| [`conversion-types.cs`](csharp/configuration/conversion-types.cs) | RationalConversion vs TableConversion |

### Python — Getting Started

| File | Description |
|------|-------------|
| [`01_load_session.py`](python/getting-started/01_load_session.py) | Load session with CoreCLR pythonnet boilerplate |
| [`02_read_parameters.py`](python/getting-started/02_read_parameters.py) | PDA read with Python iteration pattern |
| [`03_query_sessions.py`](python/getting-started/03_query_sessions.py) | QueryManager with ScalarFilter |
| [`04_extract_events.py`](python/getting-started/04_extract_events.py) | Event definitions and data extraction |

### Python — Data Access

| File | Description |
|------|-------------|
| [`read_samples_to_list.py`](python/data-access/read_samples_to_list.py) | Extract to native Python lists |
| [`export_to_pandas.py`](python/data-access/export_to_pandas.py) | DataFrame with timestamp conversion |
| [`multi_parameter_extract.py`](python/data-access/multi_parameter_extract.py) | Multi-column DataFrame from all parameters |

### MATLAB — Getting Started

| File | Description |
|------|-------------|
| [`load_session.m`](matlab/getting-started/load_session.m) | Load session with NET.addAssembly boilerplate |
| [`read_parameters.m`](matlab/getting-started/read_parameters.m) | PDA read with conversion to MATLAB arrays |
| [`query_sessions.m`](matlab/getting-started/query_sessions.m) | QueryManager with ScalarFilter |

### MATLAB — Data Access

| File | Description |
|------|-------------|
| [`extract_to_timetable.m`](matlab/data-access/extract_to_timetable.m) | Convert to MATLAB timetable with duration time axis |
| [`multi_channel_read.m`](matlab/data-access/multi_channel_read.m) | Multi-column timetable from all parameters |
