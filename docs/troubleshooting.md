# Troubleshooting

Common issues and solutions when working with the SQL Race API.

## "Could not load file or assembly 'MESL.SqlRace.Domain'"

The SQL Race assemblies are not in the expected location. Check that:
- The `MESL.SQLRace.API` NuGet package is referenced in your project
- You have run `dotnet restore`
- The GitHub Packages NuGet source is configured (see [getting-started.md](getting-started.md))

## "LicenceProgramName must be set before Initialize"

You must set `Core.LicenceProgramName` before calling `Core.Initialize()`:

```csharp
Core.LicenceProgramName = "SQLRace";
Core.Initialize();
```

In MATLAB with .NET Core, direct static property assignment and static method calls do not work with short names. Use `NET.setStaticProperty` and `NET.invokeStaticMethod` instead:

```matlab
NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
NET.invokeStaticMethod('MESL.SqlRace.Domain.Core', 'Initialize');

sessionKey = NET.invokeStaticMethod('MESL.SqlRace.Domain.SessionKey', 'Parse', guid);
sessionManager = NET.invokeStaticMethod('MESL.SqlRace.Domain.SessionManager', 'CreateSessionManager');
```

Note: `import MESL.SqlRace.Domain.*` does not reliably expose short names under .NET Core. Avoid it — use fully qualified names with `NET.invokeStaticMethod` for all static calls. Instance methods on returned objects (e.g. `session.CreateParameterDataAccess(...)`) work normally.

## "Session not found" when loading from database

- Verify the session key (GUID) is correct
- Check that your connection string points to the right database
- Ensure the database exists and contains sessions

## SQLite "database is locked"

SQLite files support only one writer at a time. Ensure no other process (including ATLAS) has the file open for writing.

## MATLAB + .NET 8: Assembly resolution failures

**Bug 90001** — When using SQL Race from MATLAB with .NET 8, assemblies such as `Microsoft.Win32.SystemEvents` or `System.Runtime` may fail to resolve. This is because MATLAB's default .NET runtime is .NET Framework, and even when switched to .NET Core the runtime config may not include all required frameworks.

**Symptoms:**
- `Could not load file or assembly 'System.Runtime, Version=8.0.0.0'` — MATLAB is still using the .NET Framework runtime
- `Could not load file or assembly 'Microsoft.Win32.SystemEvents'` during `Core.Initialize()` or session load — the .NET Core runtime config is missing the `Microsoft.WindowsDesktop.App` framework

### Step 1: Switch MATLAB to .NET Core

Requires **MATLAB R2023a or later** (which supports .NET 6+). Run once per MATLAB installation:

```matlab
dotnetenv('core')
```

If you have .NET runtimes newer than .NET 8 installed, pin to version 8:

```matlab
dotnetenv("core", Version="8");
```

Verify with:

```matlab
e = dotnetenv;
disp(e)
```

### Step 2: Configure the runtime to include required frameworks

Edit the runtime configuration file at:

```
<matlabroot>/bin/win64/dotnetcli_netcore.runtimeconfig.json
```

Replace its contents with (or merge into your existing config):

```json
{
  "runtimeOptions": {
    "rollForward": "Minor",
    "tfm": "net8.0",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "8.0.0"
      },
      {
        "name": "Microsoft.WindowsDesktop.App",
        "version": "8.0.0"
      },
      {
        "name": "Microsoft.AspNetCore.App",
        "version": "8.0.0"
      }
    ],
    "configProperties": {
      "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": true
    }
  }
}
```

The key entry is **`Microsoft.WindowsDesktop.App`** — this framework contains `Microsoft.Win32.SystemEvents` and other Windows desktop assemblies that SQL Race depends on (via `MetricsService → MidnightNotifier`). With this config in place, MATLAB loads the correct assemblies automatically.

### Fallback: Pre-load individual DLLs

If you cannot modify the runtime config, explicitly load `Microsoft.Win32.SystemEvents.dll` before initialising SQL Race:

```matlab
installDir = fileparts(sqlraceDll);
NET.addAssembly(fullfile(installDir, 'Microsoft.Win32.SystemEvents.dll'));
```

All MATLAB snippets and the `setup_sqlrace.m` helper in this repository include this fallback.

See the [MATLAB documentation on calling .NET](https://mathworks.com/help/matlab/calling-net-from-matlab.html) for more details on .NET interop configuration.

## Timestamps seem wrong

SQL Race timestamps are **nanoseconds since midnight**, not Unix timestamps. A value of `36000000000000` means 10:00:00.000 (10 hours after midnight).

See [CONCEPTS.md](../CONCEPTS.md#time-model) for conversion formulas.
