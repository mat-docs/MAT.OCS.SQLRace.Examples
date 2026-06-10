# Notebooks (Tier 3)

Interactive Jupyter notebooks for exploring SQL Race session data with narrative, code, and visualisation.

## Prerequisites

- Python 3.8+
- ATLAS 10 installed (for SQL Race .NET assemblies)
- Jupyter: `pip install -r requirements.txt`

## Setup

```bash
cd notebooks
pip install -r requirements.txt
jupyter notebook
```

### Configuration

Set environment variables to customise the connection:

| Variable | Default | Description |
|----------|---------|-------------|
| `SQLRACE_DLL_PATH` | `C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll` | Path to SQL Race DLL |
| `SQLRACE_CONNECTION_STRING` | SQLite in temp directory | Full connection string |

## Core Notebooks

| # | Notebook | Description |
|---|----------|-------------|
| 1 | [01-getting-started.ipynb](01-getting-started.ipynb) | First steps: initialise, load a session, read samples |
| 2 | [02-session-exploration.ipynb](02-session-exploration.ipynb) | Browse session structure, parameters, laps, metadata |
| 3 | [03-multi-parameter-analysis.ipynb](03-multi-parameter-analysis.ipynb) | Read, align, and plot multiple parameters |
| 4 | [04-session-comparison.ipynb](04-session-comparison.ipynb) | Compare data across two sessions |
| 5 | [05-export-pipeline.ipynb](05-export-pipeline.ipynb) | Export session data to CSV, Parquet, HDF5 |
| 6 | [06-live-data-monitoring.ipynb](06-live-data-monitoring.ipynb) | Poll and display live session data |

## Industry Scenarios

| Notebook | Industry | Description |
|----------|----------|-------------|
| [motorsport-lap-analysis.ipynb](scenarios/motorsport-lap-analysis.ipynb) | Motorsport | Lap-by-lap telemetry comparison |
| [flight-test-manoeuvre-extraction.ipynb](scenarios/flight-test-manoeuvre-extraction.ipynb) | Aerospace | Extract manoeuvre windows from flight test data |
| [turbine-performance-trending.ipynb](scenarios/turbine-performance-trending.ipynb) | Energy | Long-term turbine efficiency trending |
| [durability-cycle-comparison.ipynb](scenarios/durability-cycle-comparison.ipynb) | Automotive | Compare stress cycles across durability runs |

## Shared Helpers

The `sqlrace_helpers.py` module provides common utilities:

- `init_sqlrace()` — Initialise runtime, return SessionManager
- `load_session()` — Load a session by GUID
- `extract_parameter()` / `extract_parameters()` — Extract data to pandas
- `extract_to_timetable()` — Extract with timedelta index
- `get_laps()` — Lap info as DataFrame
- `plot_parameters()` — Quick subplot visualisation
