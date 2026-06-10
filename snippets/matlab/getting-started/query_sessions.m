% ─────────────────────────────────────────────────────────────
% SQL Race Example: Query Sessions
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Using QueryManager with ScalarFilter from MATLAB
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A SQLite database with sessions
% Output: Matching session identifiers and recording times
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
import MESL.SqlRace.Domain.Query.*;

NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();

% --- Connection string ---
% Default: use the flight-test scenario data file bundled in the repo
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'flight-test-manoeuvre-extraction.ssn2');

if ~exist(dbPath, 'file')
    error('File not found: %s\nCheck that notebooks/scenarios/data/ contains the .ssn2 files.', dbPath);
end
connectionString = sprintf('DbEngine=SQLite;Data Source=%s;', dbPath);

% --- Create query manager with filter ---
qm = QueryManager.CreateQueryManager(connectionString);

searchTerm = 'Example';
queryFilter = ScalarFilter('Identifier', MatchingRule.Contains, searchTerm, false);
qm.Filter = queryFilter;

fprintf('Searching for sessions matching "%s"...\n', searchTerm);
fprintf('%-40s %-30s %s\n', 'Session Key', 'Identifier', 'Recorded');
fprintf('%s\n', repmat('-', 1, 94));

queryResult = qm.ExecuteQuery();

% --- Enumerate results via reflection (required for .NET Core interop) ---
t = queryResult.GetType();
ienumIface = t.GetInterface('System.Collections.IEnumerable');
miGetEnum = ienumIface.GetMethod('GetEnumerator');
emptyArgs = NET.createArray('System.Object', 0);
en = miGetEnum.Invoke(queryResult, emptyArgs);

ienumeratorT = System.Type.GetType('System.Collections.IEnumerator');
miMoveNext = ienumeratorT.GetMethod('MoveNext');
propCurrent = ienumeratorT.GetProperty('Current');
miGetCurrent = propCurrent.GetGetMethod();

count = 0;
while miMoveNext.Invoke(en, emptyArgs)
    summary = miGetCurrent.Invoke(en, emptyArgs);
    fprintf('%-40s %-30s %s\n', ...
        char(summary.Key.ToString()), ...
        char(summary.Identifier), ...
        char(summary.TimeOfRecording.ToString()));
    count = count + 1;
    if count >= 20
        break;
    end
end

if count == 0
    disp('No sessions found. Run 04-create-session-write-data.cs to create some.');
else
    fprintf('\n%d session(s) found.\n', count);
end
