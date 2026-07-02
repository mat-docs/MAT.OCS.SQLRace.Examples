% ─────────────────────────────────────────────────────────────
% SQL Race Example: Read Samples Between
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Using GetSamplesBetween to read data in a specific time range
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data)
% Output: Sample count and the first samples within the requested range
%
% Related: snippets/matlab/data-access/reverse_iteration.m
% Docs: https://atlas.motionapplied.com/developer-resources/atlas/sql-race/examples/parameter-data-access/
% ─────────────────────────────────────────────────────────────

% --- Configure .NET runtime (run once per MATLAB session) ---
% dotnetenv('core');  % uncomment if not already set

% --- Load assembly and initialise ---
sqlraceDll = getenv('SQLRACE_DLL_PATH');
if isempty(sqlraceDll)
    sqlraceDll = 'C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll';
end

% Pre-load Microsoft.Win32.SystemEvents — required when the runtime config
% does not include Microsoft.WindowsDesktop.App (see docs/troubleshooting.md)
installDir = fileparts(sqlraceDll);
sysEventsDll = fullfile(installDir, 'Microsoft.Win32.SystemEvents.dll');
if exist(sysEventsDll, 'file')
    NET.addAssembly(sysEventsDll);
end

NET.addAssembly(sqlraceDll);
import MESL.SqlRace.Domain.*;

NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();

% --- Connection and session ---
% Default: use the motorsport scenario data file bundled in the repo
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'motorsport-lap-analysis.ssn2');
paramId = 'Temperature:Sensors';

if ~exist(dbPath, 'file')
    error('File not found: %s\nCheck that notebooks/scenarios/data/ contains the .ssn2 files.', dbPath);
end
connectionString = sprintf('DbEngine=SQLite;Data Source=%s;', dbPath);

sessionManager = SessionManager.CreateSessionManager();
sessions = sessionManager.FindBySessionState(SessionState.Historical, connectionString);
if sessions.Count == 0
    error('No sessions found in %s', dbPath);
end
sessionKey = sessions.Item(0).Key;
clientSession = sessionManager.Load(sessionKey, connectionString);

try
    session = clientSession.Session;

    % --- Define a time range: first 500 ms of data ---
    rangeStart = session.StartTime;
    rangeEnd = session.StartTime + int64(500e6);  % 0.5 s in nanoseconds

    pda = session.CreateParameterDataAccess(paramId);
    try
        samples = pda.GetSamplesBetween(rangeStart, rangeEnd);

        fprintf('Parameter: %s\n', paramId);
        fprintf('Range:     %d - %d ns (%.3f s)\n', ...
            rangeStart, rangeEnd, double(rangeEnd - rangeStart) / 1e9);
        fprintf('Samples:   %d\n', samples.SampleCount);

        nShow = min(samples.SampleCount, 10);
        for i = 1:nShow
            relMs = double(samples.Timestamp(i) - rangeStart) / 1e6;
            fprintf('  t+%8.3f ms  %.4f\n', relMs, samples.Data(i));
        end
        if samples.SampleCount > 10
            fprintf('  ... and %d more\n', samples.SampleCount - 10);
        end
    catch ex
        fprintf('Error reading data: %s\n', ex.message);
    end
    pda.Dispose();
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
