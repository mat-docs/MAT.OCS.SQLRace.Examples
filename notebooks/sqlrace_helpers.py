# ─────────────────────────────────────────────────────────────
# SQL Race Notebook Helpers
# ─────────────────────────────────────────────────────────────
#
# Shared utilities for SQL Race Jupyter notebooks.
# Import with: from sqlrace_helpers import init_sqlrace, load_session, extract_parameter
#
# Prerequisites: pythonnet, ATLAS 10 installed
# ─────────────────────────────────────────────────────────────

import json
import os
import sys
import tempfile
from pathlib import Path

import numpy as np
import pandas as pd


# ── Configuration ──────────────────────────────────────────

DEFAULT_DLL_PATH = r"C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll"
DEFAULT_DB_PATH = str(Path(tempfile.gettempdir()) / "sqlrace-examples.ssn2")


def _load_dotenv():
    """Load variables from the repo .env file if it exists (no external dependency)."""
    # Walk up from this file to find .env at the repo root
    here = Path(__file__).resolve().parent
    for parent in [here] + list(here.parents):
        env_file = parent / ".env"
        if env_file.is_file():
            with open(env_file) as f:
                for line in f:
                    line = line.strip()
                    if not line or line.startswith("#"):
                        continue
                    # Handle 'export KEY="VALUE"' or 'KEY=VALUE'
                    if line.startswith("export "):
                        line = line[7:]
                    key, _, value = line.partition("=")
                    key = key.strip()
                    value = value.strip().strip('"').strip("'")
                    if key and key not in os.environ:
                        os.environ[key] = value
            break


_load_dotenv()


def get_dll_path():
    """Return the SQL Race DLL path from environment or default."""
    return os.environ.get("SQLRACE_DLL_PATH", DEFAULT_DLL_PATH)


def get_connection_string(db_path=None):
    """Return a connection string from environment, argument, or default SQLite."""
    env = os.environ.get("SQLRACE_CONNECTION_STRING")
    if env:
        return env
    path = db_path or DEFAULT_DB_PATH
    return f"DbEngine=SQLite;Data Source={path};"


# ── Initialisation ─────────────────────────────────────────

_initialised = False


def init_sqlrace(dll_path=None):
    """Initialise the SQL Race runtime. Safe to call multiple times.

    Returns the SessionManager instance.
    """
    global _initialised

    dll = dll_path or get_dll_path()
    atlas_dir = os.path.dirname(dll)

    if not _initialised:
        runtime_cfg = os.path.join(tempfile.gettempdir(), "sqlrace_python.runtimeconfig.json")
        with open(runtime_cfg, "w") as f:
            json.dump({"runtimeOptions": {"tfm": "net8.0", "frameworks": [
                {"name": "Microsoft.NETCore.App", "version": "8.0.0"},
                {"name": "Microsoft.WindowsDesktop.App", "version": "8.0.0"}],
                "additionalProbingPaths": [atlas_dir]}}, f)

        from pythonnet import load
        load("coreclr", runtime_config=runtime_cfg)
        _initialised = True

    import clr
    clr.AddReference(dll)
    clr.AddReference(os.path.join(atlas_dir, "MAT.OCS.Core.dll"))

    from MESL.SqlRace.Domain import Core, SessionManager

    Core.LicenceProgramName = "SQLRace"
    Core.Initialize()

    return SessionManager.CreateSessionManager()


# ── Session helpers ────────────────────────────────────────

def load_session(session_manager, session_guid, connection_string=None):
    """Load a session by GUID string. Returns (client_session, session)."""
    from MAT.OCS.Core import SessionKey

    key = SessionKey.Parse(session_guid)
    cs = connection_string or get_connection_string()
    session_summary = session_manager.FindSummaryBy(key, cs)
    client_session = session_manager.Load(session_summary.key, session_summary.GetConnectionString())
    return client_session, client_session.Session


def session_summary(session):
    """Print a summary of the session."""
    duration_s = (session.EndTime - session.StartTime) / 1e9
    print(f"Session:    {session.Identifier}")
    print(f"Start:      {session.StartTime} ns")
    print(f"End:        {session.EndTime} ns")
    print(f"Duration:   {duration_s:.3f} s")
    print(f"Parameters: {session.Parameters.Count}")
    print(f"Laps:       {session.LapCollection.Count}")


def list_parameters(session):
    """Return a list of parameter identifier strings."""
    return [str(session.Parameters[i].Identifier)
            for i in range(session.Parameters.Count)]


# ── Data extraction ────────────────────────────────────────

def extract_parameter(session, identifier, start_time=None, end_time=None):
    """Extract a single parameter as a pandas Series with nanosecond timestamps.

    Args:
        session: The SQL Race session object.
        identifier: Parameter identifier string.
        start_time: Start time in ns (defaults to session start).
        end_time: End time in ns (defaults to session end).

    Returns:
        pandas.Series with Int64 index (nanoseconds) and float values.
    """
    t0 = start_time if start_time is not None else session.StartTime
    t1 = end_time if end_time is not None else session.EndTime

    pda = session.CreateParameterDataAccess(identifier)
    try:
        samples = pda.GetSamplesBetween(t0, t1)
        n = samples.SampleCount

        timestamps = np.array([int(samples.Timestamp[i]) for i in range(n)], dtype=np.int64)
        values = np.array([float(samples.Data[i]) for i in range(n)], dtype=np.float64)

        return pd.Series(values, index=timestamps, name=identifier)
    finally:
        pda.Dispose()


def extract_parameters(session, identifiers, start_time=None, end_time=None):
    """Extract multiple parameters into a pandas DataFrame.

    Each parameter becomes a column. Rows are aligned by timestamp using
    an outer join, so parameters at different rates will have NaN gaps.

    Args:
        session: The SQL Race session object.
        identifiers: List of parameter identifier strings.
        start_time: Start time in ns (defaults to session start).
        end_time: End time in ns (defaults to session end).

    Returns:
        pandas.DataFrame with Int64 index (nanoseconds).
    """
    series_list = []
    for ident in identifiers:
        s = extract_parameter(session, ident, start_time, end_time)
        series_list.append(s)

    if not series_list:
        return pd.DataFrame()

    df = pd.concat(series_list, axis=1, join="outer")
    df.index.name = "timestamp_ns"
    return df


def extract_to_timetable(session, identifiers, start_time=None, end_time=None):
    """Extract parameters into a DataFrame with a timedelta index (seconds from start).

    Convenience wrapper around extract_parameters that converts nanosecond
    timestamps to seconds relative to the first sample.
    """
    df = extract_parameters(session, identifiers, start_time, end_time)
    if df.empty:
        return df

    t0 = df.index[0]
    df.index = pd.to_timedelta((df.index - t0) / 1e9, unit="s")
    df.index.name = "time"
    return df


# ── Lap helpers ────────────────────────────────────────────

def get_laps(session):
    """Return lap information as a pandas DataFrame."""
    laps = session.LapCollection
    records = []
    for i in range(laps.Count):
        lap = laps[i]
        records.append({
            "number": int(lap.Number),
            "start_time_ns": int(lap.StartTime),
            "name": str(lap.Name),
        })
    return pd.DataFrame(records)


# ── Plotting helpers ───────────────────────────────────────

def plot_parameters(df, title="Parameter Traces", figsize=(12, 6)):
    """Plot each column as a subplot. Returns the matplotlib figure."""
    import matplotlib.pyplot as plt

    n = len(df.columns)
    fig, axes = plt.subplots(n, 1, figsize=(figsize[0], figsize[1] * n / 2),
                             sharex=True, squeeze=False)

    for i, col in enumerate(df.columns):
        ax = axes[i, 0]
        ax.plot(df.index.total_seconds() if hasattr(df.index, "total_seconds") else df.index,
                df[col], linewidth=0.8)
        ax.set_ylabel(col)
        ax.grid(True, alpha=0.3)

    axes[0, 0].set_title(title)
    axes[-1, 0].set_xlabel("Time (s)")
    fig.tight_layout()
    return fig
