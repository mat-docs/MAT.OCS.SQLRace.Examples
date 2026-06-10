% ─────────────────────────────────────────────────────────────
% SQL Race Example: Load Session
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Loading a session from SQLite using SessionManager
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data)
% Output: Session summary printed to command window
%
% Related: snippets/matlab/getting-started/read_parameters.m
% Docs: https://atlas.motionapplied.com/developer-resources/atlas/sql-race/examples/session-loading/
% ─────────────────────────────────────────────────────────────

% --- Configure .NET runtime (run once per MATLAB session) ---
% dotnetenv('core');  % uncomment if not already set

% --- Load the SQL Race assembly ---
% Set SQLRACE_DLL_PATH environment variable to override the default
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

% --- Initialise SQL Race ---
NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();
disp('SQL Race initialised');

% --- Connection string ---
% Default: use the flight-test scenario data file bundled in the repo
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'flight-test-manoeuvre-extraction.ssn2');
sessionGuid = '828f3c72-9573-4a74-9803-20ff993d848a';

if ~exist(dbPath, 'file')
    error('File not found: %s\nCheck that notebooks/scenarios/data/ contains the .ssn2 files.', dbPath);
end
connectionString = sprintf('DbEngine=SQLite;Data Source=%s;', dbPath);

% --- Load the session ---
sessionManager = SessionManager.CreateSessionManager();
sessions = sessionManager.FindBySessionState(SessionState.Historical, connectionString);
if sessions.Count == 0
    error('No sessions found in %s', dbPath);
end
sessionKey = sessions.Item(0).Key;
clientSession = sessionManager.Load(sessionKey, connectionString);

try
    session = clientSession.Session;

    fprintf('Session loaded: %s\n', sessionGuid);
    fprintf('  Identifier: %s\n', char(session.Identifier));
    fprintf('  Start time: %d ns\n', session.StartTime);
    fprintf('  End time:   %d ns\n', session.EndTime);
    fprintf('  Duration:   %.3f s\n', double(session.EndTime - session.StartTime) / 1e9);
    fprintf('  Parameters: %d\n', session.Parameters.Count);
    fprintf('  Laps:       %d\n', session.LapCollection.Count);
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Session closed.');
