# ─────────────────────────────────────────────────────────────
# SQL Race Example: Extract Events
# ─────────────────────────────────────────────────────────────
#
# Demonstrates: Loading event definitions and event data from a session
# Prerequisites: Python 3.8+, pythonnet, MESL.SqlRace.Domain DLL
# Input: A session with event data (run event-definitions-and-data.cs first)
# Output: Event definitions and instances with timestamps
#
# Related: snippets/csharp/session-management/event-definitions-and-data.cs
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

    # --- Event definitions → Python dict ---
    definitions = {}
    for evt_def in session.EventDefinitions:
        definitions[evt_def.Description] = evt_def.Priority
        print(f"Definition: {evt_def.Description} (priority: {evt_def.Priority})")

    # --- Event data ---
    events = session.Events.GetEventData(session.StartTime, session.EndTime)

    print(f"\n{'Timestamp (ns)':>20}  {'Key':<5}  {'Group':<16}  {'Data'}")
    print("─" * 70)

    for evt in events:
        values = [str(evt.RawData[i]) for i in range(evt.RawData.Count)] if evt.RawData else []
        data_str = ", ".join(values)
        print(f"{evt.TimeStamp:>20}  {evt.EventDefinitionKey:<5}  {evt.GroupName:<16}  [{data_str}]")
finally:
    client_session.Dispose()
