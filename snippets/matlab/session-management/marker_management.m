% ─────────────────────────────────────────────────────────────
% SQL Race Example: Marker Management
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Creating Marker objects with time ranges and annotations
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: None (creates its own .ssn2 file in the temp folder)
% Output: Markers with labels and durations printed to the command window
%
% Related: snippets/matlab/session-management/lap_segment_management.m
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
dbPath = fullfile(tempdir, 'sqlrace-matlab-markers.ssn2');
if exist(dbPath, 'file'); delete(dbPath); end
connectionString = sprintf('DbEngine=SQLite;Data Source=%s;', dbPath);
sessionKey = SessionKey.NewKey();

sessionManager = SessionManager.CreateSessionManager();
clientSession = sessionManager.CreateSession(connectionString, sessionKey, ...
    'Marker Demo', System.DateTime.UtcNow, 'Example');

try
    session = clientSession.Session;
    baseTime = int64(36e12);  % 10:00:00.000

    % --- Add markers representing annotated time regions ---
    % 6-argument ctor: (startTimestamp, endTimestamp, label, markerType, description, guid)
    anomaly = Marker(baseTime, baseTime + int64(2e9), ...
        'Anomaly', 'Anomaly', 'Unexpected temperature spike detected', char(System.Guid.NewGuid().ToString()));
    roi = Marker(baseTime + int64(5e9), baseTime + int64(8e9), ...
        'RegionOfInterest', 'RegionOfInterest', 'Steady-state operating window', char(System.Guid.NewGuid().ToString()));
    transient = Marker(baseTime + int64(10e9), baseTime + int64(12e9), ...
        'Transient', 'Transient', 'Load step response period', char(System.Guid.NewGuid().ToString()));

    session.Markers.Add(anomaly);
    session.Markers.Add(roi);
    session.Markers.Add(transient);

    fprintf('Session %s: %d markers added\n\n', ...
        char(sessionKey.ToString()), session.Markers.Count);

    for i = 1:session.Markers.Count
        marker = session.Markers.Item(i - 1);
        startNs = int64(0); endNs = int64(0);
        if marker.StartTimestamp.HasValue; startNs = marker.StartTimestamp.Value; end
        if marker.EndTimestamp.HasValue;   endNs   = marker.EndTimestamp.Value;   end
        durationS = double(endNs - startNs) / 1e9;
        fprintf('  [%s] %d-%d ns (%.1fs)\n', char(marker.Label), startNs, endNs, durationS);
        fprintf('    Type: %s\n', char(marker.MarkerType));
        fprintf('    Description: %s\n\n', char(marker.Description));
    end
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
