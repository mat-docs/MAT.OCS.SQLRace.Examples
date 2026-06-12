% ─────────────────────────────────────────────────────────────
% SQL Race Example: Server Listener (Live Recording)
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Configuring a SQL Race server listener, discovering live
%               sessions on an ATLAS Data Server, and reading the latest data
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL,
%                AND a reachable SQL Server recorder with a live session.
%                Run dotnetenv('core') once before using this snippet.
% Input: Recorder connection details (edit the variables below)
% Output: Live session discovery and a short live-data read loop
%
% Note: This snippet REQUIRES a live ATLAS Data Server / SQL Server recorder.
%       It cannot run against the bundled SQLite scenario data. Edit the
%       connection details below for your environment before running.
%
% Related: snippets/matlab/live-data/poll_live_samples.m
% Docs: https://atlas.motionapplied.com/developer-resources/atlas/sql-race/examples/live-data/
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
import MESL.SqlRace.Domain.Infrastructure.Enumerators.*;
import MESL.SqlRace.Domain.Remoting.Server.*;
import MAT.OCS.Core.*;

NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();

% --- Recorder / server configuration (edit for your environment) ---
recorderDbEngine = 'SQLServer';
databaseName     = 'SQLRACE01';
recorderDataServer = 'MACHINE\LOCAL';
serverListenerIp   = '127.0.0.1';
serverListenerPort = 6566;
parameterId        = 'vCar:Chassis';

connStr = sprintf('server=%s;Initial Catalog=%s;Trusted_Connection=True;', ...
    recorderDataServer, databaseName);

% --- Configure the server listener endpoint ---
ipAddr = System.Net.IPAddress.Parse(serverListenerIp);
endPoint = System.Net.IPEndPoint(ipAddr, int32(serverListenerPort));
Core.ConfigureServer(true, endPoint);

recordersConfiguration = RecordersConfiguration.GetRecordersConfiguration();
recordersConfiguration.AddConfiguration(System.Guid.NewGuid(), recorderDbEngine, ...
    recorderDataServer, recorderDataServer, connStr, false);

sessionManager = SessionManager.CreateSessionManager();

% --- Find live sessions ---
activeSessions = sessionManager.FindBySessionState(SessionState.Live, connStr);
fprintf('Found %d live session(s).\n', activeSessions.Count);

if activeSessions.Count == 0
    fprintf('No live sessions. Start a recording on the server and re-run.\n');
    recordersConfiguration.RemoveConfiguration();
    return;
end

% --- Load the most recent live session ---
summary = activeSessions.Item(activeSessions.Count - 1);
fprintf('Loading live session: %s\n', char(summary.Identifier));
clientSession = sessionManager.Load(summary.Key, summary.GetConnectionString());

try
    liveSession = clientSession.Session;
    liveSession.LoadConfiguration();

    if liveSession.Parameters.Count == 0
        fprintf('Session contains no parameters.\n');
    else
        pda = liveSession.CreateParameterDataAccess(parameterId);
        try
            % Follow the live edge: read forward from the current end
            pda.GoTo(liveSession.EndTime);
            for iter = 1:10
                samples = pda.GetNextSamples(int32(50), StepDirection.Forward);
                if samples.SampleCount > 0
                    fprintf('Iter %d: %d new sample(s), latest=%.3f\n', ...
                        iter, samples.SampleCount, samples.Data(samples.SampleCount));
                else
                    fprintf('Iter %d: waiting for data...\n', iter);
                end
                pause(1);
            end
        catch ex
            fprintf('Error reading live data: %s\n', ex.message);
        end
        pda.Dispose();
    end
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Close();
recordersConfiguration.RemoveConfiguration();
disp('Done.');
