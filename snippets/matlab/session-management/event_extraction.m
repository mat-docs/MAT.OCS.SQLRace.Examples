% ─────────────────────────────────────────────────────────────
% SQL Race Example: Event Extraction
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Enumerating EventDefinitions and reading event instances
%               per segment with GetEventData / GetDisplayText
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data)
% Output: Event definitions and a per-segment breakdown of event instances
%
% Note: Event *creation* happens in the recording/import pipeline. This
%       snippet reads events; it reports zero events if the session has none.
%
% Related: snippets/matlab/session-management/marker_management.m
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
% Default: use the motorsport scenario data file (multiple segments)
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
    fprintf('Session: %s\n', char(session.Identifier));

    % --- List event definitions (the catalogue of possible event types) ---
    defs = session.EventDefinitions;
    fprintf('Event definitions: %d\n', defs.Count);
    for i = 1:defs.Count
        def = defs.Item(i - 1);
        fprintf('  [%s] %s (priority %s)\n', ...
            char(def.EventDefinitionId), char(def.Description), char(def.Priority.ToString()));
    end

    % --- Read event instances per segment ---
    laps = session.LapCollection;
    eventCollection = session.Events;
    fprintf('\nReading events across %d segment(s):\n', laps.Count);
    fprintf('%-18s %12s\n', 'Segment', 'Events');
    fprintf('%s\n', repmat('-', 1, 32));

    totalEvents = 0;
    for n = 1:laps.Count
        lap = laps.Item(n - 1);
        lapEnd = lap.EndTime;
        if lapEnd.HasValue
            endTs = lapEnd.Value;
        else
            endTs = session.EndTime;
        end
        events = eventCollection.GetEventData(lap.StartTime, endTs);
        fprintf('%-18s %12d\n', char(lap.Name), events.Count);
        totalEvents = totalEvents + double(events.Count);

        % Show the first few events in this segment, if any
        for j = 1:min(events.Count, 5)
            evt = events.Item(j - 1);
            statusText = char(eventCollection.GetDisplayText(evt));
            fprintf('    t=%d ns  def=%s  %s\n', ...
                evt.TimeStamp, char(string(evt.EventDefinitionKey)), statusText);
        end
    end

    fprintf('\nTotal events: %d\n', totalEvents);
    if totalEvents == 0
        fprintf('(This session contains no event instances — events are written by\n');
        fprintf(' the recording/import pipeline, e.g. ATLAS or a ServerListener recorder.)\n');
    end
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
