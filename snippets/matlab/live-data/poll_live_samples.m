% ─────────────────────────────────────────────────────────────
% SQL Race Example: Poll Latest Samples
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Polling the most recent samples of a parameter by seeking to
%               the end and reading backwards — the pattern used to monitor a
%               live session
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data). For a live
%        session, point the connection string at the live database/server.
% Output: The latest samples at each poll cycle
%
% Note: Against historical data the latest values do not change between
%       polls. Against a live session, each reload picks up newly flushed data.
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
installDir = fileparts(sqlraceDll);
sysEventsDll = fullfile(installDir, 'Microsoft.Win32.SystemEvents.dll');
if exist(sysEventsDll, 'file')
    NET.addAssembly(sysEventsDll);
end
NET.addAssembly(sqlraceDll);
import MESL.SqlRace.Domain.*;
import MESL.SqlRace.Domain.Infrastructure.Enumerators.*;

NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();

% --- Connection and session ---
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'motorsport-lap-analysis.ssn2');
paramId = 'Speed:Shaft';
nPolls = 3;          % number of poll cycles
pollIntervalSec = 1; % delay between polls

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

fprintf('Polling %s every %ds (%d cycles)...\n\n', paramId, pollIntervalSec, nPolls);

for pollNo = 1:nPolls
    % --- Reload each cycle to pick up newly flushed data (live sessions) ---
    clientSession = sessionManager.Load(sessionKey, connectionString);
    try
        session = clientSession.Session;
        pda = session.CreateParameterDataAccess(paramId);
        try
            % Seek past the end, then read the most recent samples backwards
            pda.GoTo(intmax('int64'));
            samples = pda.GetNextSamples(5, StepDirection.Reverse);
            if samples.SampleCount > 0
                fprintf('  Poll %d: latest=%.2f at %d ns (%d recent)\n', ...
                    pollNo, samples.Data(1), samples.Timestamp(1), samples.SampleCount);
            else
                fprintf('  Poll %d: no data yet\n', pollNo);
            end
        catch ex
            fprintf('  Poll %d: error — %s\n', pollNo, ex.message);
        end
        pda.Dispose();
    catch ex
        fprintf('  Poll %d: load error — %s\n', pollNo, ex.message);
    end
    clientSession.Dispose();

    if pollNo < nPolls
        pause(pollIntervalSec);
    end
end

fprintf('\nPolling complete — %d polls.\n', nPolls);
