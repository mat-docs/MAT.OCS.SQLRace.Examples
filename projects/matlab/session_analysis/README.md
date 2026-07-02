# Session Analysis (MATLAB)

A MATLAB workflow for loading SQL Race sessions, extracting parameters, computing statistics, plotting traces, and saving results.

## Prerequisites

- MATLAB R2019b+ (R2023b+ recommended for .NET 8 support)
- SQL Race .NET assemblies (installed with ATLAS 10)

## Setup

1. Open MATLAB
2. Navigate to this directory: `cd projects/matlab/session_analysis`
3. Edit `SQLRACE_DLL_PATH` in `setup_sqlrace.m` to match your ATLAS installation

## Usage

1. Edit the configuration section at the top of `analyse_session.m`:
   - Set `sessionGuid` to your session's GUID
   - Optionally change `dbPath` and `connectionString`

2. Run:
   ```matlab
   analyse_session
   ```

## Files

| File | Description |
|------|-------------|
| `setup_sqlrace.m` | Function: loads .NET assembly, initialises runtime, returns manager handles |
| `analyse_session.m` | Script: full analysis workflow (load → extract → statistics → plot → save) |

## Workflow

1. `setup_sqlrace()` handles all .NET assembly loading and Core initialisation
2. Session is loaded via SessionManager
3. All parameters are extracted into MATLAB arrays
4. Data is assembled into a MATLAB `timetable`
5. Statistics (mean, std, min, max) are computed per parameter
6. Parameter traces are plotted
7. Results are saved to `session_results.mat`

## Output

- **Console**: session summary, per-parameter statistics table
- **Figure**: parameter trace plots (up to 4 subplots)
- **File**: `session_results.mat` containing `tt` (timetable), `statsTable`, `paramIds`

## Adapting

To analyse different parameters or sessions:
- Change `sessionGuid` to target a different session
- The script automatically extracts all parameters in the session
- For selective extraction, filter `paramIds` before the extraction loop
