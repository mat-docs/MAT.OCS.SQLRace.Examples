% ─────────────────────────────────────────────────────────────
% SQL Race Example: Introspect Session Configuration
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Enumerating a session's parameters, conversions, and groups
%               to understand its configuration
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data)
% Output: Summary tables of parameters, application groups, parameter groups
%
% Related: snippets/matlab/configuration/parameter_unit_resolution.m
% Docs: https://atlas.motionapplied.com/developer-resources/atlas/sql-race/examples/session-loading/
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

NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();

% --- Connection and session ---
% Default: use the turbine scenario data file bundled in the repo
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'turbine-week1.ssn2');

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
    params = session.Parameters;
    nParams = params.Count;

    fprintf('Session: %s (%s)\n\n', char(session.Identifier), char(sessionKey.ToString()));

    % --- Parameters ---
    fprintf('=== Parameters (%d) ===\n', nParams);
    fprintf('%-30s %-25s %-15s\n', 'Identifier', 'Conversion', 'AppGroup');
    fprintf('%s\n', repmat('-', 1, 72));

    appGroups = strings(0);
    paramGroups = strings(0);
    for i = 1:nParams
        p = params.Item(i - 1);
        fprintf('%-30s %-25s %-15s\n', char(p.Identifier), ...
            char(p.ConversionFunctionName), char(p.ApplicationName));
        appGroups(end+1) = string(char(p.ApplicationName)); %#ok<SAGROW>
        g = p.GroupIdentifiers;
        if ~isempty(g)
            for j = 1:g.Count
                paramGroups(end+1) = string(char(g.Item(j - 1))); %#ok<SAGROW>
            end
        end
    end

    % --- Application groups ---
    fprintf('\n=== Application Groups ===\n');
    for g = unique(appGroups)'
        fprintf('  %s\n', g);
    end

    % --- Parameter groups ---
    fprintf('\n=== Parameter Groups ===\n');
    if isempty(paramGroups)
        fprintf('  (none)\n');
    else
        for g = unique(paramGroups)'
            fprintf('  %s\n', g);
        end
    end
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
