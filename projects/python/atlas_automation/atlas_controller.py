#!/usr/bin/env python3
"""
ATLAS Automation Controller
============================

Demonstrates controlling ATLAS 10 via the Automation API:
launching ATLAS, loading a session, and controlling the cursor.

Usage:
    python atlas_controller.py <session-guid> [--connection-string CS]

Prerequisites:
    pip install pythonnet
    ATLAS 10 must be installed.
"""

import argparse
import subprocess
import sys
import time
import threading
import os
import tempfile
from pathlib import Path

# ─── pythonnet CoreCLR initialisation ─────────────────────────
import json

SQLRACE_DLL_PATH = os.environ.get("SQLRACE_DLL_PATH", r"C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll")
ATLAS_EXE_PATH = os.environ.get("ATLAS_EXE_PATH", r"C:\Program Files\Motion Applied Technologies\ATLAS 10\ATLAS.exe")
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


class AtlasController:
    """Controls an ATLAS 10 instance via the Automation API."""

    def __init__(self):
        self._lock = threading.Lock()
        self._process = None
        # TODO: Verify — ApplicationServiceClient API for ATLAS Automation
        self._client = None

    def launch_atlas(self):
        """Launch ATLAS 10 as a subprocess."""
        if not os.path.exists(ATLAS_EXE_PATH):
            print(f"ATLAS not found at: {ATLAS_EXE_PATH}")
            print("Update ATLAS_EXE_PATH to match your installation.")
            return False

        print(f"Launching ATLAS from: {ATLAS_EXE_PATH}")
        self._process = subprocess.Popen([ATLAS_EXE_PATH])
        print(f"  PID: {self._process.pid}")

        # Wait for ATLAS to be ready
        print("  Waiting for ATLAS to initialise...")
        time.sleep(10)  # TODO: Replace with proper readiness check
        return True

    def connect(self):
        """Connect to ATLAS via the Automation API."""
        with self._lock:
            # TODO: Verify — ApplicationServiceClient connection API
            # self._client = ApplicationServiceClient()
            # self._client.Connect()
            print("  Connected to ATLAS Automation API")
            print("  (Connection is stubbed — see TODO comments for real API)")

    def load_session(self, session_key, connection_string):
        """Load a SQL Race session into ATLAS."""
        with self._lock:
            # TODO: Verify — API for loading a session into a workbook set
            # self._client.LoadSession(session_key, connection_string)
            print(f"  Loading session: {session_key}")
            print(f"  Connection: {connection_string}")
            print("  (Session loading is stubbed — see TODO comments)")

    def set_cursor(self, timestamp_ns):
        """Move the ATLAS cursor to a specific timestamp."""
        with self._lock:
            # TODO: Verify — API for cursor control
            # self._client.SetCursorPosition(timestamp_ns)
            time_s = timestamp_ns / 1e9
            print(f"  Cursor set to: {timestamp_ns} ns ({time_s:.3f} s from midnight)")
            print("  (Cursor control is stubbed — see TODO comments)")

    def get_session_info(self):
        """Get information about the currently loaded session."""
        with self._lock:
            # TODO: Verify — API for session/timebase information
            print("  Session info request (stubbed)")
            return None

    def close(self):
        """Disconnect from ATLAS."""
        with self._lock:
            if self._client is not None:
                # self._client.Disconnect()
                self._client = None
            print("  Disconnected from ATLAS")


def main():
    parser = argparse.ArgumentParser(description="ATLAS Automation Controller")
    parser.add_argument("session_guid", help="Session GUID to load")
    parser.add_argument("--connection-string", default=None, help="SQL Race connection string")
    args = parser.parse_args()

    # Resolve connection string
    connection_string = args.connection_string
    if not connection_string:
        connection_string = os.environ.get("SQLRACE_CONNECTION_STRING")
    if not connection_string:
        db_path = str(Path(tempfile.gettempdir()) / "sqlrace-examples.ssn2")
        connection_string = f"DbEngine=SQLite;Data Source={db_path};"

    Core.LicenceProgramName = "SQLRace"
    Core.Initialize()

    # Verify session exists
    session_key = SessionKey.Parse(args.session_guid)
    session_manager = SessionManager.CreateSessionManager()
    client_session = session_manager.Load(session_key, connection_string)
    session = client_session.Session

    print(f"Session verified: {session.Identifier}")
    print(f"  Start: {session.StartTime} ns")
    print(f"  End:   {session.EndTime} ns")
    print(f"  Parameters: {session.Parameters.Count}")

    client_session.Dispose()

    # Control ATLAS
    controller = AtlasController()

    print("\n--- ATLAS Automation Demo ---")
    print("(Automation API calls are stubbed with TODO comments)")
    print()

    # In production, these would control a real ATLAS instance:
    # controller.launch_atlas()
    # controller.connect()
    # controller.load_session(args.session_guid, connection_string)
    # controller.set_cursor(session.StartTime)
    # controller.get_session_info()
    # controller.close()

    controller.connect()
    controller.load_session(args.session_guid, connection_string)

    midpoint = (session.StartTime + session.EndTime) // 2
    controller.set_cursor(midpoint)
    controller.get_session_info()
    controller.close()

    print("\nDone.")


if __name__ == "__main__":
    main()
