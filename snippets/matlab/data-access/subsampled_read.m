% ─────────────────────────────────────────────────────────────
% SQL Race Example: Subsampled Read
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Using GetData with a custom timestamp array to downsample
%               high-frequency data to a lower rate
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data)
% Output: Values at the requested timestamps (every 100 ms = 10 Hz)
%
% Related: snippets/matlab/data-access/read_samples_between.m
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
% Default: use the turbine scenario data file bundled in the repo
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'turbine-week1.ssn2');
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

    % --- Build a custom timestamp array at 10 Hz (100 ms intervals) ---
    desiredInterval = int64(100e6);  % 100 ms in nanoseconds (= 10 Hz)
    startNs = session.StartTime;
    endNs = session.EndTime;
    sampleCount = double(idivide(endNs - startNs, desiredInterval));

    if sampleCount <= 0
        error('Session is too short for subsampling at 10 Hz.');
    end

    % Build a .NET int64 array of the desired timestamps
    timestamps = NET.createArray('System.Int64', sampleCount);
    for i = 1:sampleCount
        timestamps(i) = startNs + int64(i - 1) * desiredInterval;
    end

    pda = session.CreateParameterDataAccess(paramId);
    try
        % --- Read data at the custom timestamps ---
        samples = pda.GetData(timestamps);

        fprintf('Parameter:       %s\n', paramId);
        fprintf('Original range:  %d - %d ns\n', startNs, endNs);
        fprintf('Subsampled to:   %d points at 10 Hz\n\n', sampleCount);

        nShow = min(samples.SampleCount, 15);
        for i = 1:nShow
            relMs = double(samples.Timestamp(i) - startNs) / 1e6;
            fprintf('  t+%8.1f ms  %.4f\n', relMs, samples.Data(i));
        end
        if samples.SampleCount > 15
            fprintf('  ... and %d more\n', samples.SampleCount - 15);
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
