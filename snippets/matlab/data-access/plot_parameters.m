% ─────────────────────────────────────────────────────────────
% SQL Race Example: Plot Multiple Parameters
% ─────────────────────────────────────────────────────────────
%
% Demonstrates: Extracting the time-series for a set of parameters and
%               plotting them on a shared time axis (one stacked subplot per
%               parameter, so different value ranges stay readable)
% Prerequisites: MATLAB R2023a+, .NET 8 runtime, MESL.SqlRace.Domain DLL
%                Run dotnetenv('core') once before using this snippet.
% Input: A .ssn2 SQLite database file (defaults to scenario data) and a list
%        of parameter identifiers to plot
% Output: A figure window with one stacked subplot per parameter
%
% Related: snippets/matlab/data-access/multi_channel_read.m
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

% Parameters to plot (edit for your session; see list_parameters.m to discover them)
parameterIds = {'Pressure:Inlet', 'Temperature:Sensors', 'RPM:Turbine'};

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
    startNs = session.StartTime;

    % --- Extract each parameter's time-series into MATLAB arrays ---
    series = struct('id', {}, 't', {}, 'v', {});
    for k = 1:numel(parameterIds)
        paramId = parameterIds{k};
        pda = session.CreateParameterDataAccess(paramId);
        try
            samples = pda.GetSamplesBetween(session.StartTime, session.EndTime);
            n = samples.SampleCount;
            t = zeros(n, 1);
            v = zeros(n, 1);
            for i = 1:n
                t(i) = double(samples.Timestamp(i) - startNs) / 1e9;  % seconds
                v(i) = samples.Data(i);
            end
            series(end+1) = struct('id', paramId, 't', t, 'v', v); %#ok<SAGROW>
            fprintf('%-25s %d samples\n', paramId, n);
        catch ex
            fprintf('%-25s skipped — %s\n', paramId, ex.message);
        end
        pda.Dispose();
    end

    if isempty(series)
        error('No data extracted for the requested parameters.');
    end

    % --- Plot: one stacked subplot per parameter, shared x-axis ---
    fig = figure('Position', [100, 100, 900, 250 * numel(series)]);
    tl = tiledlayout(fig, numel(series), 1, 'TileSpacing', 'compact', 'Padding', 'compact');
    title(tl, sprintf('%s', char(session.Identifier)), 'Interpreter', 'none');

    ax = gobjects(numel(series), 1);
    for k = 1:numel(series)
        ax(k) = nexttile(tl);
        plot(ax(k), series(k).t, series(k).v, 'LineWidth', 1);
        ylabel(ax(k), series(k).id, 'Interpreter', 'none');
        grid(ax(k), 'on');
    end
    xlabel(ax(end), 'Time (s)');
    linkaxes(ax, 'x');

    fprintf('\nDisplayed %d parameter(s).\n', numel(series));
catch ex
    fprintf('Error: %s\n', ex.message);
end

clientSession.Dispose();
disp('Done.');
