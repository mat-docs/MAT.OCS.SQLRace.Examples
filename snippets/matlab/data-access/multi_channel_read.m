% ─────────────────────────────────────────────────────────────
% SQL Race Example: Multi-Channel Read
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Extracting multiple parameters and building a multi-column
%               timetable with proper cleanup of PDA objects
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A session with multiple parameters
% Output: Multi-column MATLAB timetable
%
% Related: snippets/matlab/data-access/extract_to_timetable.m
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
% Default: use the flight-test scenario data file bundled in the repo
dataDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');
dbPath = fullfile(dataDir, 'flight-test-manoeuvre-extraction.ssn2');
sessionGuid = '828f3c72-9573-4a74-9803-20ff993d848a';

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
    nParams = session.Parameters.Count;
    fprintf('Session has %d parameter(s)\n', nParams);

    % --- Collect all parameters ---
    paramIds = cell(nParams, 1);
    for i = 1:nParams
        paramIds{i} = char(session.Parameters.Item(i-1).Identifier);
    end

    % --- Read each parameter and store in a struct ---
    data = struct();
    timeVec = [];

    for p = 1:nParams
        paramId = paramIds{p};
        safeName = matlab.lang.makeValidName(paramId);
        pda = session.CreateParameterDataAccess(paramId);

        try
            samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);
            nSamples = samples.SampleCount;

            values = zeros(nSamples, 1);
            for i = 1:nSamples
                values(i) = samples.Data(i);
            end
            data.(safeName) = values;

            % Use first parameter's timestamps for the time axis
            if isempty(timeVec)
                tsNs = zeros(nSamples, 1, 'int64');
                for i = 1:nSamples
                    tsNs(i) = samples.Timestamp(i);
                end
                startNs = tsNs(1);
                timeVec = seconds(double(tsNs - startNs) / 1e9);
            end

            fprintf('  %s: %d samples\n', paramId, nSamples);
        catch ex
            fprintf('  %s: error — %s\n', paramId, ex.message);
        end

        pda.Dispose();
    end

    % --- Build timetable ---
    if ~isempty(timeVec)
        tt = timetable(timeVec);
        fields = fieldnames(data);
        for f = 1:numel(fields)
            tt.(fields{f}) = data.(fields{f})(1:height(tt));
        end
        fprintf('\nTimetable: %d rows x %d variables\n', height(tt), width(tt));
        disp(head(tt, 5));
    end
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
