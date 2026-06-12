% ─────────────────────────────────────────────────────────────
% SQL Race Example: Lap and Segment Management
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Creating Lap objects as test segments and reading them back
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: None (creates its own .ssn2 file in the temp folder)
% Output: Segment list with name, start time, and offset
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
dbPath = fullfile(tempdir, 'sqlrace-matlab-segments.ssn2');
if exist(dbPath, 'file'); delete(dbPath); end
connectionString = sprintf('DbEngine=SQLite;Data Source=%s;', dbPath);
sessionKey = SessionKey.NewKey();

sessionManager = SessionManager.CreateSessionManager();
clientSession = sessionManager.CreateSession(connectionString, sessionKey, ...
    'Segment Demo', System.DateTime.UtcNow, 'Example');

try
    session = clientSession.Session;
    baseTime = int64(36e12);  % 10:00:00.000

    % --- Add segments (laps) representing test phases ---
    % Lap ctor: (Int64 startTime, Int16 number, Byte triggerSource, String name, Boolean countForFastestLap)
    names   = {'Warmup', 'Steady State 1', 'Ramp Up', 'Steady State 2', 'Cooldown'};
    offsets = int64([0, 60e9, 180e9, 240e9, 360e9]);

    for i = 1:numel(names)
        lap = Lap(baseTime + offsets(i), int16(i), uint8(0), names{i}, false);
        session.LapCollection.Add(lap);
    end

    fprintf('Session %s: %d segments\n\n', ...
        char(sessionKey.ToString()), session.LapCollection.Count);
    fprintf('%3s %-20s %20s %12s\n', '#', 'Name', 'Start (ns)', 'Offset (s)');
    fprintf('%s\n', repmat('-', 1, 58));

    for i = 1:session.LapCollection.Count
        lap = session.LapCollection.Item(i - 1);
        offsetS = double(lap.StartTime - baseTime) / 1e9;
        fprintf('%3d %-20s %20d %12.0f\n', lap.Number, char(lap.Name), lap.StartTime, offsetS);
    end
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
