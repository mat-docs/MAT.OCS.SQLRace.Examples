# ─────────────────────────────────────────────────────────────
# SQL Race Example: Multi-Parameter Extract
# ─────────────────────────────────────────────────────────────
#
# Demonstrates: Extracting multiple parameters into a dict of lists or a
#               multi-column DataFrame, with proper PDA disposal
# Prerequisites: Python 3.8+, pythonnet, pandas, MESL.SqlRace.Domain DLL
# Input: A session with multiple parameters
# Output: Multi-column DataFrame with all parameters
#
# Related: snippets/python/data-access/export_to_pandas.py
# Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
# ─────────────────────────────────────────────────────────────

import json
import os
import sys
import tempfile
from pathlib import Path

# --- pythonnet CoreCLR initialisation ---
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

import pandas as pd

from MESL.SqlRace.Domain import Core, SessionManager
from MAT.OCS.Core import SessionKey

Core.LicenceProgramName = "SQLRace"
Core.Initialize()

import tempfile
db_path = str(Path(tempfile.gettempdir()) / "sqlrace-examples.ssn2")
connection_string = f"DbEngine=SQLite;Data Source={db_path};"

if len(sys.argv) < 2:
    print(f"Usage: python {sys.argv[0]} <session-guid>")
    sys.exit(1)

try:
    session_key = SessionKey.Parse(sys.argv[1])
except Exception as ex:
    print(f"ERROR: '{sys.argv[1]}' is not a valid session GUID: {ex}")
    sys.exit(1)

session_manager = SessionManager.CreateSessionManager()
client_session = session_manager.Load(session_key, connection_string)
if client_session is None:
    print(f"ERROR: Session '{session_key}' not found at '{connection_string}'.")
    sys.exit(1)

try:
    session = client_session.Session
    identifiers = [p.Identifier for p in session.Parameters]
    if not identifiers:
        print("WARNING: Session has no parameters. Nothing to extract.")
        sys.exit(0)
    print(f"Session has {len(identifiers)} parameter(s): {identifiers}")

    # --- Extract each parameter into a dict ---
    all_data = {}

    for param_id in identifiers:
        pda = session.CreateParameterDataAccess(param_id)
        try:
            samples = pda.GetSamplesBetween(session.StartTime, session.EndTime)
            timestamps = [samples.Timestamp[i] for i in range(samples.SampleCount)]
            values = [samples.Data[i] for i in range(samples.SampleCount)]
            all_data[param_id] = {"timestamps": timestamps, "values": values}
            print(f"  {param_id}: {len(values)} samples")
        finally:
            pda.Dispose()

    # --- Build a multi-column DataFrame (using first parameter's timestamps) ---
    if all_data:
        first_key = identifiers[0]
        df = pd.DataFrame({"timestamp_ns": all_data[first_key]["timestamps"]})
        for param_id in identifiers:
            df[param_id] = all_data[param_id]["values"][:len(df)]

        print(f"\nDataFrame: {df.shape[0]} rows x {df.shape[1]} columns")
        print(df.head(10).to_string(index=False))
finally:
    client_session.Dispose()
