# ATLAS Automation

Demonstrates controlling ATLAS 10 via the Automation API from Python.

## What is the Automation API?

ATLAS 10 exposes an automation interface that allows external applications to:
- Launch and connect to ATLAS instances
- Load SQL Race sessions into workbook sets
- Control the cursor position and timebase
- Query session and display information

## Prerequisites

- Python 3.8+ (64-bit)
- ATLAS 10 installed
- SQL Race .NET assemblies

## Setup

```bash
cd projects/python/atlas_automation
python -m venv .venv
source .venv/bin/activate  # or .venv\Scripts\activate on Windows
pip install -r requirements.txt
```

Set the `SQLRACE_DLL_PATH` and `ATLAS_EXE_PATH` environment variables to match your installation, or edit the defaults at the top of `atlas_controller.py`.

## Usage

```bash
python atlas_controller.py <session-guid> [--connection-string CS]
```

## Current Status

The Automation API calls are **stubbed with TODO comments** since they require a running ATLAS instance and the exact API signatures vary by ATLAS version. The structure demonstrates the correct pattern:

1. Launch ATLAS (or connect to a running instance)
2. Connect via `ApplicationServiceClient`
3. Load a session
4. Control the cursor
5. Disconnect

## Integration with SQL Race

This tool uses the SQL Race API to verify the session exists before attempting to load it in ATLAS. This two-step pattern (verify in SQL Race, then load in ATLAS) is the recommended approach.
