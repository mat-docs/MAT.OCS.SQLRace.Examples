# MATLAB Snippets

MATLAB examples use `NET.addAssembly` to call the SQL Race .NET API directly from MATLAB.

## Prerequisites

- **MATLAB R2019b+** (R2023b+ recommended for .NET 8 support)
- **SQL Race .NET assemblies** — installed with ATLAS 10+ at the default location
- .NET runtime must be configured in MATLAB. See [MATLAB .NET documentation](https://mathworks.com/help/matlab/matlab_external/call-net-from-matlab.html).

## Setup

Every snippet begins with the assembly loading boilerplate:

```matlab
% Reads from SQLRACE_DLL_PATH env var, falls back to default ATLAS install location
sqlraceDll = getenv('SQLRACE_DLL_PATH');
if isempty(sqlraceDll)
    sqlraceDll = 'C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll';
end
NET.addAssembly(sqlraceDll);

% Import namespaces
import MESL.SqlRace.Domain.*;
```

## Common Issues

| Issue | Solution |
|-------|----------|
| `Could not load file or assembly` | Set the `SQLRACE_DLL_PATH` environment variable to point to your ATLAS installation's `MESL.SqlRace.Domain.dll` |
| `No appropriate method found` | Check argument types — MATLAB may need explicit casting |
| Wrong array types | Use `int64()` for timestamps, `double()` for values |
| Memory issues with large datasets | Extract data in chunks; clear .NET objects when done |

## Folders

| Folder | Description |
|--------|-------------|
| [getting-started/](getting-started/) | Load sessions, read parameters, query metadata, verify setup |
| [data-access/](data-access/) | Extract data to MATLAB timetables and arrays, per-segment statistics |
