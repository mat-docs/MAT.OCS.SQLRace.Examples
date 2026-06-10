% ─────────────────────────────────────────────────────────────
% SQL Race Example: Read Parameters
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Creating a PDA, reading samples, converting to MATLAB arrays
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A session with Temperature:Sensors data
% Output: MATLAB double arrays of timestamps and values
%
% Related: snippets/matlab/data-access/extract_to_timetable.m
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
% Default: use the flight-test scenario data file bundled in the repo
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'flight-test-manoeuvre-extraction.ssn2');
sessionGuid = '828f3c72-9573-4a74-9803-20ff993d848a';

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

paramId = 'Temperature:Sensors';

try
    session = clientSession.Session;
    pda = session.CreateParameterDataAccess(paramId);

    try
        samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);

        % --- Convert .NET arrays to MATLAB double arrays ---
        nSamples = samples.SampleCount;
        timestamps = zeros(nSamples, 1, 'int64');
        values = zeros(nSamples, 1);

        for i = 1:nSamples
            timestamps(i) = samples.Timestamp(i);
            values(i) = samples.Data(i);
        end

        fprintf('Parameter: %s\n', paramId);
        fprintf('Samples:   %d\n', nSamples);
        fprintf('Min:       %.4f\n', min(values));
        fprintf('Max:       %.4f\n', max(values));
        fprintf('Mean:      %.4f\n', mean(values));

        % Plot if desired
        % plot(double(timestamps - timestamps(1)) / 1e9, values);
        % xlabel('Time (s)'); ylabel(paramId);
    catch ex
        fprintf('Error reading data: %s\n', ex.message);
    end

    pda.Dispose();
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
