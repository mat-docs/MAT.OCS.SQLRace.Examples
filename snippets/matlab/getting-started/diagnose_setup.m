% ─────────────────────────────────────────────────────────────
% SQL Race Example: Diagnose Setup
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Step-by-step verification of MATLAB + SQL Race integration
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: None (uses bundled scenario data)
% Output: Pass/fail status for each setup step, with diagnostic details
%
% Related: snippets/matlab/getting-started/load_session.m
% Docs: https://atlas.motionapplied.com/developer-resources/atlas/sql-race/examples/session-loading/
% ─────────────────────────────────────────────────────────────

fprintf('\n=== SQL Race MATLAB Setup Diagnostics ===\n\n');

% --- Step 1: Check .NET runtime ---
fprintf('[1/5] Checking .NET runtime... ');
try
    ne = dotnetenv();
    if ne.Runtime == "core" || ne.Status == "loaded"
        fprintf('OK (Runtime: %s, Status: %s)\n', char(ne.Runtime), char(ne.Status));
    else
        fprintf('NOT LOADED — run dotnetenv(''core'') and restart MATLAB\n');
        return;
    end
catch ex
    fprintf('FAILED — %s\n', ex.message);
    return;
end

% --- Step 2: Load assembly ---
fprintf('[2/5] Loading SQL Race assembly... ');
sqlraceDll = getenv('SQLRACE_DLL_PATH');
if isempty(sqlraceDll)
    sqlraceDll = 'C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll';
end
if ~exist(sqlraceDll, 'file')
    fprintf('FAILED — DLL not found: %s\n', sqlraceDll);
    fprintf('       Set SQLRACE_DLL_PATH or install ATLAS 10.\n');
    return;
end
try
    installDir = fileparts(sqlraceDll);
    sysEventsDll = fullfile(installDir, 'Microsoft.Win32.SystemEvents.dll');
    if exist(sysEventsDll, 'file')
        NET.addAssembly(sysEventsDll);
    end
    NET.addAssembly(sqlraceDll);
    import MESL.SqlRace.Domain.*;
    fprintf('OK (%s)\n', sqlraceDll);
catch ex
    fprintf('FAILED\n');
    printDiagException(ex);
    return;
end

% --- Step 3: Initialise Core ---
fprintf('[3/5] Initialising Core... ');
try
    NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
    Core.Initialize();
    fprintf('OK\n');
catch ex
    fprintf('FAILED\n');
    printDiagException(ex);
    return;
end

% --- Step 4: Load a test session ---
fprintf('[4/5] Loading test session... ');
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'flight-test-manoeuvre-extraction.ssn2');
sessionGuid = '828f3c72-9573-4a74-9803-20ff993d848a';

if ~exist(dbPath, 'file')
    fprintf('SKIPPED — test data not found at %s\n', dbPath);
else
    try
        connStr = sprintf('DbEngine=SQLite;Data Source=%s;', dbPath);
        sessionManager = SessionManager.CreateSessionManager();
        sessions = sessionManager.FindBySessionState(SessionState.Historical, connStr);
        sessionKey = sessions.Item(0).Key;
        clientSession = sessionManager.Load(sessionKey, connStr);
        session = clientSession.Session;
        fprintf('OK (Parameters: %d, Segments: %d)\n', ...
            session.Parameters.Count, session.LapCollection.Count);
    catch ex
        fprintf('FAILED\n');
        printDiagException(ex);
        clientSession = [];
    end
end

% --- Step 5: Create PDA and read data ---
if exist('session', 'var')
    fprintf('[5/5] Reading parameter data... ');
    try
        pda = session.CreateParameterDataAccess('Temperature:Sensors');
        samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);
        fprintf('OK (%d samples)\n', samples.SampleCount);
        pda.Dispose();
    catch ex
        fprintf('FAILED\n');
        printDiagException(ex);
    end
    clientSession.Dispose();
else
    fprintf('[5/5] Reading parameter data... SKIPPED (no session)\n');
end

fprintf('\n=== Diagnostics complete ===\n');

% --- Helper: print .NET exception chain ---
function printDiagException(ME)
    fprintf('       MATLAB: %s\n', ME.message);
    try
        if isprop(ME, 'ExceptionObject') && ~isempty(ME.ExceptionObject)
            ex = ME.ExceptionObject;
            depth = 0;
            while ~isempty(ex)
                fprintf('       .NET[%d]: [%s] %s\n', depth, ...
                    char(ex.GetType().FullName), char(ex.Message));
                try ex = ex.InnerException; catch; ex = []; end
                depth = depth + 1;
            end
        end
    catch
    end
end
