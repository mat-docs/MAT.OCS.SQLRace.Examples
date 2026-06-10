% ─────────────────────────────────────────────────────────────
% SQL Race Example: Segment Statistics
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: GetLapStatistics() for per-segment aggregate statistics
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A session with segments (laps) and a numeric parameter
% Output: Per-segment min, max, and mean printed to command window
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
import MESL.SqlRace.Enumerators.*;

NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();

% --- Connection and session ---
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

paramId = 'Temperature:Sensors';

try
    session = clientSession.Session;
    pda = session.CreateParameterDataAccess(paramId);

    try
        laps = session.LapCollection;
        fprintf('Parameter: %s\n', paramId);
        fprintf('Segments:  %d\n\n', laps.Count);

        % --- Compute statistics for each segment ---
        % MATLAB can't resolve .NET enum values directly; use Enum.Parse
        asm = NET.addAssembly(sqlraceDll);
        statOptionType = asm.AssemblyHandle.GetType('MESL.SqlRace.Domain.Infrastructure.DataPipeline.StatisticOption');
        statAll = System.Enum.Parse(statOptionType, 'All');

        for i = 1:laps.Count
            lap = laps.Item(i - 1);  % .NET uses zero-based indexing
            stats = pda.GetLapStatistics(lap, false, statAll);

            fprintf('Segment %d — Min: %.4f, Max: %.4f, Mean: %.4f\n', ...
                i, ...
                double(stats.MinimumValue), ...
                double(stats.MaximumValue), ...
                double(stats.MeanValue));
        end
    catch ex
        fprintf('Error computing statistics: %s\n', ex.message);
    end

    pda.Dispose();
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
