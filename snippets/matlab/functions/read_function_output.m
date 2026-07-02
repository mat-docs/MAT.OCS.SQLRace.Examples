% ─────────────────────────────────────────────────────────────
% SQL Race Example: Read Function (Calculated Channel) Output
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Locating calculated-channel (function) parameters in the
%               ":Functions" application group and reading their output the
%               same way as any other parameter
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data)
% Output: List of function parameters and a sample of their output values
%
% Note: Function definitions are authored in ATLAS or via the .NET
%       FunctionManager API. Once built, their output is read like any
%       parameter — that is what this snippet shows. It reports gracefully
%       if the session has no function parameters.
%
% Related: snippets/matlab/getting-started/read_parameters.m
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

NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();

% --- Connection and session ---
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'motorsport-lap-analysis.ssn2');

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

    % --- Find function parameters (calculated channels live in :Functions) ---
    funcIds = strings(0);
    for i = 1:params.Count
        id = string(char(params.Item(i - 1).Identifier));
        if endsWith(id, ':Functions')
            funcIds(end+1) = id; %#ok<SAGROW>
        end
    end

    fprintf('Session: %s\n', char(session.Identifier));
    fprintf('Function parameters found: %d\n', numel(funcIds));

    if isempty(funcIds)
        fprintf(['\nNo function parameters in this session.\n' ...
                 'Author a function in ATLAS (or via the .NET FunctionManager API),\n' ...
                 'then re-run: its output appears as a "<name>:Functions" parameter\n' ...
                 'and is read exactly like the example below.\n']);
    else
        for k = 1:numel(funcIds)
            paramId = char(funcIds(k));
            pda = session.CreateParameterDataAccess(paramId);
            try
                samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);
                fprintf('\n%s: %d samples\n', paramId, samples.SampleCount);
                for i = 1:min(samples.SampleCount, 5)
                    fprintf('  %20d ns  %.4f\n', samples.Timestamp(i), samples.Data(i));
                end
            catch ex
                fprintf('  Error reading %s: %s\n', paramId, ex.message);
            end
            pda.Dispose();
        end
    end
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
