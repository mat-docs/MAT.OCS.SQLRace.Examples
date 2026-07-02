# ─────────────────────────────────────────────────────────────
# SQL Race Example: Load Session
# ─────────────────────────────────────────────────────────────
#
# Demonstrates: Loading a session from a SQLite database using SessionManager
# Prerequisites: Python 3.8+, pythonnet, MESL.SqlRace.Domain DLL
# Input: A SQLite database with sessions and a session GUID
# Output: Session summary (name, start time, end time, parameter count)
#
# Related: snippets/python/getting-started/02_read_parameters.py
# Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
# ─────────────────────────────────────────────────────────────

import json
import os
import sys
import tempfile
from pathlib import Path

# --- pythonnet CoreCLR initialisation ---
# Requires WindowsDesktop runtime for Microsoft.Win32.SystemEvents dependency
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

# --- Initialise SQL Race ---
Core.LicenceProgramName = "SQLRace"
Core.Initialize()
print("SQL Race initialised")

# --- Connection string ---
db_path = str(Path(tempfile.gettempdir()) / "sqlrace-examples.ssn2")
connection_string = f"DbEngine=SQLite;Data Source={db_path};"

if len(sys.argv) < 2:
    print(f"Usage: python {sys.argv[0]} <session-guid>")
    print(f"Database: {connection_string}")
    sys.exit(1)

try:
    session_key = SessionKey.Parse(sys.argv[1])
except Exception as ex:
    print(f"ERROR: '{sys.argv[1]}' is not a valid session GUID: {ex}")
    sys.exit(1)

# --- Load the session ---
session_manager = SessionManager.CreateSessionManager()
client_session = session_manager.Load(session_key, connection_string)

if client_session is None:
    print(f"ERROR: Session '{session_key}' not found.")
    print(f"Connection: {connection_string}")
    print("Tip: Run snippets/csharp/getting-started/04-create-session-write-data.cs first to create a session.")
    sys.exit(1)

try:
    session = client_session.Session
    print(f"Session loaded: {session_key}")
    print(f"  Name:       {session.Identifier}")
    print(f"  Start time: {session.StartTime} ns")
    print(f"  End time:   {session.EndTime} ns")
    print(f"  Duration:   {(session.EndTime - session.StartTime) / 1e9:.3f} s")
    print(f"  Parameters: {session.Parameters.Count}")
    print(f"  Laps:       {session.LapCollection.Count}")
finally:
    client_session.Dispose()
    print("\nSession closed.")
