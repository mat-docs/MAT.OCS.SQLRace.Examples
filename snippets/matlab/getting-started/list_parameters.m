% ─────────────────────────────────────────────────────────────
% SQL Race Example: List All Parameters
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Enumerating every parameter in a session and building a
%               MATLAB table catalogue (identifier, name, group, conversion,
%               range, sample count), then exporting it to CSV
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data)
% Output: A printed parameter catalogue and a CSV written to the temp folder
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
% Default: use the turbine scenario data file bundled in the repo
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'turbine-week1.ssn2');

% Set to true to count samples per parameter (slower on large sessions)
countSamples = true;

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

    fprintf('Session: %s\n', char(session.Identifier));
    fprintf('Parameters: %d\n\n', nParams);

    % --- Pre-allocate columns for the catalogue table ---
    Identifier  = strings(nParams, 1);
    Name        = strings(nParams, 1);
    Description = strings(nParams, 1);
    AppGroup    = strings(nParams, 1);
    Groups      = strings(nParams, 1);
    Conversion  = strings(nParams, 1);
    MinValue    = zeros(nParams, 1);
    MaxValue    = zeros(nParams, 1);
    Samples     = zeros(nParams, 1);

    for i = 1:nParams
        p = params.Item(i - 1);
        Identifier(i)  = string(char(p.Identifier));
        Name(i)        = string(char(p.Name));
        Description(i) = string(char(p.Description));
        AppGroup(i)    = string(char(p.ApplicationName));
        Conversion(i)  = string(char(p.ConversionFunctionName));
        MinValue(i)    = p.MinimumValue;
        MaxValue(i)    = p.MaximumValue;

        % Join the parameter group identifiers
        g = p.GroupIdentifiers;
        if ~isempty(g) && g.Count > 0
            gnames = strings(1, g.Count);
            for j = 1:g.Count
                gnames(j) = string(char(g.Item(j - 1)));
            end
            Groups(i) = strjoin(gnames, '; ');
        else
            Groups(i) = "";
        end

        % Optionally count samples across the whole session
        if countSamples
            pda = session.CreateParameterDataAccess(char(p.Identifier));
            try
                s = pda.GetSamplesBetween(session.StartTime, session.EndTime);
                Samples(i) = double(s.SampleCount);
            catch
                Samples(i) = NaN;
            end
            pda.Dispose();
        else
            Samples(i) = NaN;
        end
    end

    % --- Build and display the catalogue table ---
    catalogue = table(Identifier, Name, AppGroup, Groups, Conversion, ...
        MinValue, MaxValue, Samples);
    disp(catalogue);

    % --- Export to CSV ---
    csvPath = fullfile(tempdir, 'sqlrace-parameters.csv');
    writetable(catalogue, csvPath);
    fprintf('Catalogue written to: %s\n', csvPath);
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
