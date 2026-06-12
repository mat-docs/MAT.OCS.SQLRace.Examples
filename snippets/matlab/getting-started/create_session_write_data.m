% ─────────────────────────────────────────────────────────────
% SQL Race Example: Create Session and Write Data
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Creating a SQLite session with multi-rate parameters,
%               writing data, and reading it back to verify
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: None (creates its own .ssn2 file in the temp folder)
% Output: Session key and verified samples for Temperature (100 Hz) and
%         Pressure (50 Hz)
%
% Related: snippets/matlab/getting-started/read_parameters.m
% Docs: https://atlas.motionapplied.com/developer-resources/atlas/sql-race/examples/session-loading/
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
NET.addAssembly(fullfile(installDir, 'MAT.OCS.Core.dll'));
enumDll = fullfile(installDir, 'MESL.SqlRace.Enumerators.dll');
if exist(enumDll, 'file')
    NET.addAssembly(enumDll);
end
import MESL.SqlRace.Domain.*;
import MESL.SqlRace.Domain.Infrastructure.DataPipeline.*;
import MESL.SqlRace.Enumerators.*;
import MAT.OCS.Core.*;

NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
Core.Initialize();

% --- Connection string (writable temp file) ---
dbPath = fullfile(tempdir, 'sqlrace-matlab-examples.ssn2');
connectionString = sprintf('DbEngine=SQLite;Data Source=%s;', dbPath);
sessionKey = SessionKey.NewKey();

% --- Create a new session ---
sessionManager = SessionManager.CreateSessionManager();
clientSession = sessionManager.CreateSession(connectionString, sessionKey, ...
    'Example Session', System.DateTime.UtcNow, 'Example');

try
    session = clientSession.Session;

    % --- Build parameter configuration ---
    config = session.CreateConfiguration();
    tempChannelId = uint32(1);
    pressChannelId = uint32(2);
    tempInterval = FrequencyExtensions.ToInterval(Frequency(100, FrequencyUnit.Hz));
    pressInterval = FrequencyExtensions.ToInterval(Frequency(50, FrequencyUnit.Hz));

    config.AddConversion(RationalConversion.CreateSimple1To1Conversion('degC_conv', 'degC', '%5.2f'));
    config.AddConversion(RationalConversion.CreateSimple1To1Conversion('bar_conv', 'bar', '%5.3f'));

    config.AddChannel(Channel(tempChannelId, 'Temperature', tempInterval, ...
        DataType.Double64Bit, ChannelDataSourceType.Periodic, 'Temperature:Sensors', false));
    config.AddChannel(Channel(pressChannelId, 'Pressure', pressInterval, ...
        DataType.Double64Bit, ChannelDataSourceType.Periodic, 'Pressure:Inlet', false));

    % Parameter groups expect a List<string> of group identifiers
    thermalGroups = NET.createGeneric('System.Collections.Generic.List', {'System.String'});
    thermalGroups.Add('Thermal');
    hydraulicGroups = NET.createGeneric('System.Collections.Generic.List', {'System.String'});
    hydraulicGroups.Add('Hydraulic');
    appGroupChildren = NET.createGeneric('System.Collections.Generic.List', {'System.String'});
    appGroupChildren.Add('Thermal');
    appGroupChildren.Add('Hydraulic');

    config.AddParameterGroup(ParameterGroup('Thermal', 'Thermal'));
    config.AddParameterGroup(ParameterGroup('Hydraulic', 'Hydraulic'));
    appGroup = ApplicationGroup('Sensors', 'Sensors', appGroupChildren);
    appGroup.SupportsRda = false;
    config.AddGroup(appGroup);

    config.AddParameter(Parameter('Temperature:Sensors', 'Temperature', 'Inlet temperature sensor', ...
        500, 0, 500, 0, 0.0, uint32(0), uint32(0), 'degC_conv', thermalGroups, tempChannelId, 'Sensors'));
    config.AddParameter(Parameter('Pressure:Inlet', 'Pressure', 'Inlet pressure sensor', ...
        10, 0, 10, 0, 0.0, uint32(0), uint32(0), 'bar_conv', hydraulicGroups, pressChannelId, 'Sensors'));

    config.Commit();

    % --- Write 1 second of data ---
    startTime = int64(36e12);  % 10:00:00.000

    for i = 0:99  % 100 Hz temperature
        timestamp = startTime + int64(i) * tempInterval;
        value = 20.0 + sin(i * 0.1) * 5.0;
        session.AddChannelData(tempChannelId, timestamp, 1, System.BitConverter.GetBytes(value));
    end

    for i = 0:49  % 50 Hz pressure
        timestamp = startTime + int64(i) * pressInterval;
        value = 1.013 + sin(i * 0.05) * 0.2;
        session.AddChannelData(pressChannelId, timestamp, 1, System.BitConverter.GetBytes(value));
    end

    % --- Read back and verify ---
    tempPda = session.CreateParameterDataAccess('Temperature:Sensors');
    tempSamples = tempPda.GetSamplesBetween(startTime, startTime + 100 * tempInterval);
    pressPda = session.CreateParameterDataAccess('Pressure:Inlet');
    pressSamples = pressPda.GetSamplesBetween(startTime, startTime + 50 * pressInterval);

    fprintf('Session created: %s\n', char(sessionKey.ToString()));
    fprintf('Connection:      %s\n\n', connectionString);
    fprintf('Temperature:Sensors (100 Hz): %d samples\n', tempSamples.SampleCount);
    fprintf('  First: %.2f degC  Last: %.2f degC\n', ...
        tempSamples.Data(1), tempSamples.Data(tempSamples.SampleCount));
    fprintf('Pressure:Inlet (50 Hz): %d samples\n', pressSamples.SampleCount);
    fprintf('  First: %.3f bar  Last: %.3f bar\n', ...
        pressSamples.Data(1), pressSamples.Data(pressSamples.SampleCount));

    tempPda.Dispose();
    pressPda.Dispose();
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
