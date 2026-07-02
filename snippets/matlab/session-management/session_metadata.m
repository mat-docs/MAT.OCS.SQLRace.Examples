% ─────────────────────────────────────────────────────────────
% SQL Race Example: Session Metadata
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Creating and reading SessionDataItem entries on a session
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: None (creates its own .ssn2 file in the temp folder)
% Output: Session data items written and read back after reload
%
% Related: snippets/matlab/session-management/marker_management.m
% Docs: https://atlas.motionapplied.com/developer-resources/atlas/sql-race/examples/session-loading/
% ─────────────────────────────────────────────────────────────

% --- Configure .NET runtime (run once per MATLAB session) ---
% dotnetenv('core');  % uncomment if not already set

% --- Load assemblies and initialise ---
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
NET.addAssembly(fullfile(installDir, 'MAT.OCS.Core.dll'));
import MESL.SqlRace.Domain.*;
import MAT.OCS.Core.*;

NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();

% --- Connection string (writable temp file) ---
dbPath = fullfile(tempdir, 'sqlrace-matlab-metadata.ssn2');
if exist(dbPath, 'file'); delete(dbPath); end
connectionString = sprintf('DbEngine=SQLite;Data Source=%s;', dbPath);
sessionKey = SessionKey.NewKey();

sessionManager = SessionManager.CreateSessionManager();

% --- Create a session and add metadata items ---
client = sessionManager.CreateSession(connectionString, sessionKey, ...
    'Metadata Demo', System.DateTime.UtcNow, 'Example');
try
    session = client.Session;
    session.Items.Add(SessionDataItem('Facility', 'Wind Tunnel A'));
    session.Items.Add(SessionDataItem('Operator', 'Test Engineer 1'));
    session.Items.Add(SessionDataItem('TestObjective', 'Thermal characterisation at 80% load'));
    session.Items.Add(SessionDataItem('AmbientTemp', '22.5'));
    session.Items.Add(SessionDataItem('RunNumber', '42'));
    fprintf('Created session %s with %d metadata items\n', ...
        char(sessionKey.ToString()), session.Items.Count);
catch ex
    fprintf('Error writing metadata: %s\n', ex.message);
end
client.Dispose();

% --- Reload and read back ---
reloaded = sessionManager.Load(sessionKey, connectionString);
try
    reloadedSession = reloaded.Session;
    fprintf('\nReloaded session: %d metadata items\n', reloadedSession.Items.Count);
    fprintf('%-20s %s\n', 'Key', 'Value');
    fprintf('%s\n', repmat('-', 1, 60));
    for i = 1:reloadedSession.Items.Count
        item = reloadedSession.Items.Item(i - 1);
        fprintf('%-20s %s\n', char(item.Name), char(item.Value));
    end
catch ex
    fprintf('Error reading metadata: %s\n', ex.message);
end
reloaded.Dispose();
disp('Done.');
