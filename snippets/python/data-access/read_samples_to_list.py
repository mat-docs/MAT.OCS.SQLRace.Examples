# ─────────────────────────────────────────────────────────────
# SQL Race Example: Read Samples to List
# ─────────────────────────────────────────────────────────────
#
# Demonstrates: Extracting parameter samples into native Python lists
# Prerequisites: Python 3.8+, pythonnet, MESL.SqlRace.Domain DLL
# Input: A session with Temperature:Sensors data
# Output: Python lists of timestamps and values
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

from MESL.SqlRace.Domain import Core, SessionManager
from MAT.OCS.Core import SessionKey

Core.LicenceProgramName = "SQLRace"
Core.Initialize()

import tempfile
db_path = str(Path(tempfile.gettempdir()) / "sqlrace-examples.ssn2")
connection_string = f"DbEngine=SQLite;Data Source={db_path};"
param_id = "Temperature:Sensors"

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
pda = None

try:
    session = client_session.Session
    if not any(p.Identifier == param_id for p in session.Parameters):
        print(f"ERROR: Parameter '{param_id}' not found in session.")
        print(f"Available: {', '.join(p.Identifier for p in session.Parameters)[:200]}")
        sys.exit(1)
    pda = session.CreateParameterDataAccess(param_id)
    samples = pda.GetSamplesBetween(session.StartTime, session.EndTime)
    if samples.SampleCount == 0:
        print(f"WARNING: No samples in session for '{param_id}'.")
        sys.exit(0)

    # --- Copy .NET arrays into native Python lists ---
    timestamps = [samples.Timestamp[i] for i in range(samples.SampleCount)]
    values = [samples.Data[i] for i in range(samples.SampleCount)]

    print(f"Extracted {len(values)} samples into Python lists")
    print(f"  Type: timestamps={type(timestamps).__name__}, values={type(values).__name__}")
    print(f"  First: t={timestamps[0]} ns, v={values[0]:.4f}")
    print(f"  Last:  t={timestamps[-1]} ns, v={values[-1]:.4f}")
    print(f"  Min:   {min(values):.4f}")
    print(f"  Max:   {max(values):.4f}")
    print(f"  Mean:  {sum(values) / len(values):.4f}")
finally:
    if pda is not None:
        pda.Dispose()
    client_session.Dispose()
