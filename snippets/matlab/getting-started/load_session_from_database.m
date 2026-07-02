% ─────────────────────────────────────────────────────────────
% SQL Race Example: Load Session from a SQL Server Database
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Connecting to a SQL Server SQL Race database (rather than a
%               local SQLite .ssn2 file), listing sessions, and loading one
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL,
%                AND access to a SQL Server instance hosting a SQL Race
%                database (e.g. SQLRACE01).
%                Run dotnetenv('core') once before using this snippet.
% Input: A SQL Server connection string (edit the variables below, or set
%        the SQLRACE_CONNECTION_STRING environment variable)
% Output: A list of sessions and a summary of the most recent one
%
% Related: snippets/matlab/getting-started/load_session.m
% Docs: https://atlas.motionapplied.com/developer-resources/atlas/sql-race/examples/session-loading/
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

% --- SQL Server connection string ---
% Unlike SQLite (which uses a "DbEngine=SQLite;..." string), SQL Race passes
% SQL Server strings straight to SqlClient — so use a standard SQL Server
% connection string with NO DbEngine prefix. Integrated Security uses the
% current Windows account (Trusted_Connection=True has the same effect).
connectionString = getenv('SQLRACE_CONNECTION_STRING');
if isempty(connectionString)
    dataServer     = 'SERVER_NAME';   % SQL Server instance
    initialCatalog = 'DATABASE_NAME';            % SQL Race database
    connectionString = sprintf(['Data Source=%s;Initial Catalog=%s;' ...
        'Integrated Security=True;'], dataServer, initialCatalog);
end

fprintf('Connecting to: %s\n', connectionString);

sessionManager = SessionManager.CreateSessionManager();

% --- List historical sessions on the server ---
try
    sessions = sessionManager.FindBySessionState(SessionState.Historical, connectionString);
catch ex
    fprintf('ERROR: could not query the database: %s\n', ex.message);
    fprintf(['Tips: check the server name/instance, that the SQLRACE01 catalog\n' ...
             '      exists, and that your Windows account has access.\n']);
    return;
end

fprintf('Found %d historical session(s).\n\n', sessions.Count);
if sessions.Count == 0
    disp('No sessions to load.');
    return;
end

nShow = min(sessions.Count, 10);
fprintf('%-40s %-30s %s\n', 'Session Key', 'Identifier', 'Recorded');
fprintf('%s\n', repmat('-', 1, 94));
for i = 1:nShow
    s = sessions.Item(i - 1);
    fprintf('%-40s %-30s %s\n', ...
        char(s.Key.ToString()), char(s.Identifier), char(s.TimeOfRecording.ToString()));
end

% --- Load the most recent session and print a summary ---
sessionKey = sessions.Item(0).Key;
clientSession = sessionManager.Load(sessionKey, connectionString);
try
    session = clientSession.Session;
    fprintf('\nLoaded: %s\n', char(session.Identifier));
    fprintf('  Start time: %d ns\n', session.StartTime);
    fprintf('  End time:   %d ns\n', session.EndTime);
    fprintf('  Duration:   %.3f s\n', double(session.EndTime - session.StartTime) / 1e9);
    fprintf('  Parameters: %d\n', session.Parameters.Count);
    fprintf('  Laps:       %d\n', session.LapCollection.Count);
catch ex
    fprintf('Error reading session: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
