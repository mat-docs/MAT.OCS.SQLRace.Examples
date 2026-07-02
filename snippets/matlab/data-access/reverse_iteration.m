% ─────────────────────────────────────────────────────────────
% SQL Race Example: Reverse Iteration
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Using GoTo and GetNextSamples with StepDirection.Reverse to
%               read data backwards from the end of a session
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data)
% Output: Samples printed in reverse chronological order
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
import MESL.SqlRace.Domain.Infrastructure.Enumerators.*;

NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();

% --- Connection and session ---
% Default: use the flight-test scenario data file bundled in the repo
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'flight-test-manoeuvre-extraction.ssn2');
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
    pda = session.CreateParameterDataAccess(paramId);
    try
        % --- Position the PDA at the end of the session ---
        pda.GoTo(session.EndTime);

        % --- Read 10 samples backwards ---
        % StepDirection lives in MESL.SqlRace.Domain.Infrastructure.Enumerators
        samples = pda.GetNextSamples(10, StepDirection.Reverse);

        fprintf('Parameter: %s\n', paramId);
        fprintf('Last %d samples (reverse order):\n', samples.SampleCount);
        fprintf('%20s  %12s\n', 'Timestamp (ns)', 'Value');
        fprintf('%s\n', repmat('-', 1, 34));

        for i = 1:samples.SampleCount
            fprintf('%20d  %12.4f\n', samples.Timestamp(i), samples.Data(i));
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
