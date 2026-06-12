% ─────────────────────────────────────────────────────────────
% SQL Race Example: Parameter Unit Resolution
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Resolving the engineering unit for each parameter via its
%               conversion (Session.GetConversion)
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data)
% Output: Parameter details with resolved units, format, and range
%
% Related: snippets/matlab/configuration/introspect_session_config.m
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

    fprintf('Session: %s (%d parameters)\n\n', char(session.Identifier), params.Count);
    fprintf('%-30s %-25s %-10s %-10s %s\n', ...
        'Identifier', 'Conversion', 'Unit', 'Format', 'Range');
    fprintf('%s\n', repmat('-', 1, 95));

    for i = 1:params.Count
        p = params.Item(i - 1);
        conversionName = char(p.ConversionFunctionName);
        unit = '(unknown)';
        fmt = '';

        % --- Resolve unit from the conversion ---
        try
            conversion = session.GetConversion(conversionName);
            if ~isempty(conversion)
                u = conversion.Units;
                if ~isempty(u); unit = char(u); else; unit = '(none)'; end
                f = conversion.FormatString;
                if ~isempty(f); fmt = char(f); end
            end
        catch
            unit = '(error)';
        end

        fprintf('%-30s %-25s %-10s %-10s [%.1f, %.1f]\n', ...
            char(p.Identifier), conversionName, unit, fmt, ...
            p.MinimumValue, p.MaximumValue);
    end
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
