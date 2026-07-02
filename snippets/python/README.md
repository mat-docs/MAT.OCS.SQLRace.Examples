# Python Snippets

Python examples use [pythonnet](https://pythonnet.github.io/) with CoreCLR to call the SQL Race .NET API from Python.

## Prerequisites

- **Python 3.8+** (3.10+ recommended)
- **pythonnet** — `pip install pythonnet`
- **SQL Race .NET assemblies** — installed with ATLAS 10+ at the default location
- **pandas** (optional) — `pip install pandas` (for `export_to_pandas.py`)

## Installation

```bash
pip install pythonnet pandas
```

## CoreCLR Runtime Loading

Every snippet begins with the pythonnet CoreCLR initialisation boilerplate:

```python
import clr
from pythonnet import load
load("coreclr")  # Must be called before any CLR imports

import os
# Reads from SQLRACE_DLL_PATH env var, falls back to default ATLAS install location
SQLRACE_DLL_PATH = os.environ.get("SQLRACE_DLL_PATH", r"C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll")
clr.AddReference(SQLRACE_DLL_PATH)

from MESL.SqlRace.Domain import Core, SessionManager, SessionKey
```

## Common Gotchas

| Issue | Solution |
|-------|----------|
| `FileNotFoundException` when loading DLL | Set the `SQLRACE_DLL_PATH` environment variable to point to your ATLAS installation's `MESL.SqlRace.Domain.dll` |
| `System.BadImageFormatException` | Python bitness (32/64-bit) must match the DLL. Use 64-bit Python. |
| `RuntimeError: No runtime selected` | Call `load("coreclr")` before any `import clr` or `from System import ...` |
| `ImportError: cannot import name 'load'` | Update pythonnet: `pip install --upgrade pythonnet` (need 3.0+) |
| `.Dispose()` not called | Python `with` statements do not call .NET `IDisposable.Dispose()`. Always use `try/finally` with explicit `.Dispose()` calls. |

## Folders

| Folder | Description |
|--------|-------------|
| [getting-started/](getting-started/) | Load sessions, read parameters, query metadata, extract events |
| [data-access/](data-access/) | Extract data to Python lists, pandas DataFrames, multi-parameter extraction |
| [automation/](automation/) | ATLAS automation via COM interop |
