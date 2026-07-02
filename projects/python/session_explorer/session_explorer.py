#!/usr/bin/env python3
"""
SQL Race Session Explorer
=========================

An interactive command-line tool for browsing SQL Race sessions and
exporting parameter data to CSV or pandas DataFrame (.pkl).

Usage:
    python session_explorer.py [--connection-string CS] [--output-dir DIR] [--format csv|pkl]

Prerequisites:
    pip install pythonnet pandas
"""

import argparse
import os
import sys
import tempfile
from pathlib import Path

# ─── pythonnet CoreCLR initialisation ─────────────────────────
import json

SQLRACE_DLL_PATH = os.environ.get("SQLRACE_DLL_PATH", r"C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll")
ATLAS_DIR = os.path.dirname(SQLRACE_DLL_PATH)

_runtime_cfg = os.path.join(tempfile.gettempdir(), "sqlrace_python.runtimeconfig.json")
with open(_runtime_cfg, "w") as _f:
    json.dump({"runtimeOptions": {"tfm": "net8.0", "frameworks": [
        {"name": "Microsoft.NETCore.App", "version": "8.0.0"},
        {"name": "Microsoft.WindowsDesktop.App", "version": "8.0.0"}],
        "additionalProbingPaths": [ATLAS_DIR]}}, _f)

from pythonnet import load
load("coreclr", runtime_config=_runtime_cfg)

import clr
clr.AddReference(SQLRACE_DLL_PATH)
clr.AddReference(os.path.join(ATLAS_DIR, "MAT.OCS.Core.dll"))

from MESL.SqlRace.Domain import Core, SessionManager
from MESL.SqlRace.Domain.Query import QueryManager
from MAT.OCS.Core import SessionKey, DataStatusType

import pandas as pd


def initialise():
    """Initialise the SQL Race runtime."""
    Core.LicenceProgramName = "SQLRace"
    Core.Initialize()


def get_connection_string(explicit=None):
    """Resolve connection string from argument, env var, or default."""
    if explicit:
        return explicit
    env = os.environ.get("SQLRACE_CONNECTION_STRING")
    if env:
        return env
    db_path = str(Path(tempfile.gettempdir()) / "sqlrace-examples.ssn2")
    return f"DbEngine=SQLite;Data Source={db_path};"


def list_sessions(connection_string):
    """Query and return all sessions as a list of summaries."""
    query_manager = QueryManager.CreateQueryManager(connection_string)
    sessions = []
    for summary in query_manager.ExecuteQuery():
        sessions.append(summary)
    return sessions


def show_session_details(session):
    """Print detailed information about a loaded session."""
    print(f"\n  Identifier: {session.Identifier}")
    print(f"  Start:      {session.StartTime} ns")
    print(f"  End:        {session.EndTime} ns")
    print(f"  Duration:   {(session.EndTime - session.StartTime) / 1e9:.3f} s")
    print(f"  Parameters: {session.Parameters.Count}")
    print(f"  Laps:       {session.LapCollection.Count}")

    if session.Parameters.Count > 0:
        print("\n  Parameters:")
        for i in range(session.Parameters.Count):
            p = session.Parameters[i]
            print(f"    [{i}] {p.Identifier}")

    if session.Items.Count > 0:
        print("\n  Metadata:")
        for item in session.Items:
            print(f"    {item.Name}: {item.Value}")


def export_parameters(session, param_ids, output_path, fmt, session_name):
    """Export selected parameters to CSV or pickle."""
    data = {"timestamp_ns": None}

    for param_id in param_ids:
        pda = session.CreateParameterDataAccess(param_id)
        try:
            samples = pda.GetSamplesBetween(session.StartTime, session.EndTime)
            timestamps = [samples.Timestamp[i] for i in range(samples.SampleCount)]
            values = [samples.Data[i] for i in range(samples.SampleCount)]

            if data["timestamp_ns"] is None:
                data["timestamp_ns"] = timestamps
            data[param_id] = values[:len(data["timestamp_ns"])]
            print(f"    {param_id}: {len(values)} samples")
        finally:
            pda.Dispose()

    if data["timestamp_ns"] is None:
        print("  No data to export.")
        return

    df = pd.DataFrame(data)

    os.makedirs(output_path, exist_ok=True)
    safe_name = "".join(c if c.isalnum() or c in "-_" else "_" for c in session_name)
    ext = "csv" if fmt == "csv" else "pkl"
    file_path = os.path.join(output_path, f"{safe_name}.{ext}")

    if fmt == "csv":
        df.to_csv(file_path, index=False)
    else:
        df.to_pickle(file_path)

    print(f"  Exported to: {file_path} ({len(df)} rows)")


def main():
    parser = argparse.ArgumentParser(description="SQL Race Session Explorer")
    parser.add_argument("--connection-string", default=None, help="SQL Race connection string")
    parser.add_argument("--output-dir", default="./exports", help="Output directory")
    parser.add_argument("--format", choices=["csv", "pkl"], default="csv", help="Export format")
    args = parser.parse_args()

    initialise()
    connection_string = get_connection_string(args.connection_string)
    print(f"Connection: {connection_string}\n")

    # ─── List sessions ────────────────────────────────────────
    sessions = list_sessions(connection_string)

    if not sessions:
        print("No sessions found. Create some with the C# examples first.")
        return

    print(f"{'#':>3}  {'Identifier':<35} {'Recorded':>22}")
    print("─" * 62)
    for i, s in enumerate(sessions):
        print(f"{i:>3}  {s.Identifier:<35} {s.TimeOfRecording}")

    # ─── Select a session ─────────────────────────────────────
    print()
    choice = input(f"Select session [0-{len(sessions)-1}]: ").strip()
    if not choice.isdigit() or int(choice) >= len(sessions):
        print("Invalid selection.")
        return

    selected = sessions[int(choice)]
    session_manager = SessionManager.CreateSessionManager()
    client_session = session_manager.Load(selected.Key, connection_string)

    try:
        session = client_session.Session
        show_session_details(session)

        if session.Parameters.Count == 0:
            print("\nNo parameters to export.")
            return

        # ─── Select parameters ────────────────────────────────
        print(f"\nExport which parameters? (comma-separated indices, or 'all')")
        param_choice = input("> ").strip()

        if param_choice.lower() == "all":
            param_ids = [session.Parameters[i].Identifier for i in range(session.Parameters.Count)]
        else:
            indices = [int(x.strip()) for x in param_choice.split(",") if x.strip().isdigit()]
            param_ids = [session.Parameters[i].Identifier for i in indices if i < session.Parameters.Count]

        if not param_ids:
            print("No parameters selected.")
            return

        print(f"\nExporting {len(param_ids)} parameter(s)...")
        export_parameters(session, param_ids, args.output_dir, args.format, selected.Identifier)

    finally:
        client_session.Dispose()

    print("\nDone.")


if __name__ == "__main__":
    main()
