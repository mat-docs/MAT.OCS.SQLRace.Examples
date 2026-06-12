% ─────────────────────────────────────────────────────────────
% SQL Race Example: Data Status Filtering
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Checking DataStatus on returned samples to identify and
%               handle missing or invalid data points
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data)
% Output: Per-status breakdown and filtered valid-only values
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
% Default: use the motorsport scenario data file bundled in the repo
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'motorsport-lap-analysis.ssn2');
paramId = 'Speed:Shaft';

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
    pda = session.CreateParameterDataAccess(paramId);
    try
        samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);
        n = samples.SampleCount;

        % --- Count samples by status ---
        % DataStatus values are .NET enums; convert to char for tallying.
        statusNames = strings(n, 1);
        for i = 1:n
            statusNames(i) = string(char(samples.DataStatus(i).ToString()));
        end
        [uniqueStatuses, ~, idx] = unique(statusNames);
        counts = accumarray(idx, 1);

        fprintf('Parameter: %s\n', paramId);
        fprintf('Total samples: %d\n\n', n);
        fprintf('Status breakdown:\n');
        for s = 1:numel(uniqueStatuses)
            pct = 100 * counts(s) / n;
            fprintf('  %-15s %6d (%.1f%%)\n', uniqueStatuses(s), counts(s), pct);
        end

        % --- Filter to valid samples only (DataStatusType.Sample) ---
        fprintf('\nValid samples (Sample status):\n');
        validMask = (statusNames == "Sample");
        validIdx = find(validMask);
        for k = 1:min(numel(validIdx), 10)
            i = validIdx(k);
            ms = double(samples.Timestamp(i) - session.StartTime) / 1e6;
            fprintf('  t+%8.3f ms  %.4f\n', ms, samples.Data(i));
        end
        fprintf('Total valid: %d / %d\n', numel(validIdx), n);
    catch ex
        fprintf('Error reading data: %s\n', ex.message);
    end
    pda.Dispose();
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
