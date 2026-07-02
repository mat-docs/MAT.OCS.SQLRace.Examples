# Session Explorer

An interactive command-line tool for browsing SQL Race sessions and exporting parameter data.

## Prerequisites

- Python 3.8+ (64-bit)
- SQL Race .NET assemblies (installed with ATLAS 10)

## Setup

```bash
cd projects/python/session_explorer
python -m venv .venv
source .venv/bin/activate  # or .venv\Scripts\activate on Windows
pip install -r requirements.txt
```

Edit `SQLRACE_DLL_PATH` at the top of `session_explorer.py` to match your ATLAS installation path.

## Usage

```bash
# Interactive mode (default SQLite database)
python session_explorer.py

# Specify connection string
python session_explorer.py --connection-string "DbEngine=SQLite;Data Source=/path/to/data.ssn2;"

# Export to pickle format
python session_explorer.py --format pkl --output-dir ./my-exports
```

## Workflow

1. The tool lists all sessions in the database
2. You select a session by number
3. It shows session details (parameters, metadata, laps)
4. You select parameters to export (by index or "all")
5. Data is exported to CSV or pickle format

## Command-Line Options

| Option | Default | Description |
|--------|---------|-------------|
| `--connection-string` | (env var or default SQLite) | SQL Race connection string |
| `--output-dir` | `./exports` | Output directory for exported files |
| `--format` | `csv` | Export format: `csv` or `pkl` |

## Expected Output

```
Connection: DbEngine=SQLite;Data Source=/tmp/sqlrace-examples.ssn2;

  #  Identifier                          Recorded
──────────────────────────────────────────────────────────────
  0  GettingStarted Demo                 2026-03-12 14:30:22
  1  ServerListener-Demo-20260312        2026-03-12 14:31:05

Select session [0-1]: 0

  Name:       GettingStarted Demo
  Parameters: 2
  ...

Export which parameters? (comma-separated indices, or 'all')
> all

Exporting 2 parameter(s)...
    Temperature:Sensors: 100 samples
    Pressure:Inlet: 50 samples
  Exported to: ./exports/GettingStarted_Demo.csv (100 rows)

Done.
```
