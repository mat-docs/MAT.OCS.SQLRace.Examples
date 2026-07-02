# ─────────────────────────────────────────────────────────────
# SQL Race Example: Query Sessions
# ─────────────────────────────────────────────────────────────
#
# Demonstrates: Using QueryManager with ScalarFilter to find sessions
# Prerequisites: Python 3.8+, pythonnet, MESL.SqlRace.Domain DLL
# Input: A SQLite database with sessions
# Output: Matching session identifiers and recording times
#
# Related: snippets/python/getting-started/01_load_session.py
# Docs: https://mat-docs.github.io/Atlas.SQLRaceAPI.Documentation/api/index.html
# ─────────────────────────────────────────────────────────────

import json
import os
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

from MESL.SqlRace.Domain import Core
from MESL.SqlRace.Domain.Query import QueryManager, ScalarFilter, MatchingRule

Core.LicenceProgramName = "SQLRace"
Core.Initialize()

import tempfile
db_path = str(Path(tempfile.gettempdir()) / "sqlrace-examples.ssn2")
connection_string = f"DbEngine=SQLite;Data Source={db_path};"

# --- Create query manager and set a filter ---
query_manager = QueryManager.CreateQueryManager(connection_string)

search_term = "Example"
query_filter = ScalarFilter("Identifier", MatchingRule.Contains, search_term, False)
query_manager.Filter = query_filter

print(f"Searching for sessions matching '{search_term}'...")
print(f"{'Session Key':<40} {'Identifier':<30} {'Recorded':>22}")
print("─" * 94)

count = 0
for summary in query_manager.ExecuteQuery():
    print(f"{str(summary.Key):<40} {summary.Identifier:<30} {summary.TimeOfRecording}")
    count += 1
    if count >= 20:
        break

if count == 0:
    print("No sessions found. Run 04-create-session-write-data.cs to create some.")
else:
    print(f"\n{count} session(s) found.")
