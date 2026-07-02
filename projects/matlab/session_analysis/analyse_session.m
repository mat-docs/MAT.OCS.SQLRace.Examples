%% SQL Race Session Analysis
% Loads a SQL Race session, extracts parameters into a timetable,
% computes statistics, plots traces, and saves results.

%% Configuration — select a scenario data file
% Available .ssn2 files in notebooks/scenarios/data/
scenarioDir = fullfile(fileparts(mfilename('fullpath')), '..', '..', '..', ...
    'notebooks', 'scenarios', 'data');

sessions = {
    'flight-test-manoeuvre-extraction.ssn2', '828f3c72-9573-4a74-9803-20ff993d848a';
    'motorsport-lap-analysis.ssn2',          'a8b2b3b4-5530-4825-a5d1-bda63732361e';
    'durability-cycle1.ssn2',                '53f2367d-14b6-43d1-aeaf-6e66f1b1031c';
    'durability-cycle2.ssn2',                'f8d31ef8-48ba-450b-971c-8fec68dec7f8';
    'durability-cycle3.ssn2',                '54c2a62c-08c5-4f35-9a85-8a276ec83329';
    'turbine-week1.ssn2',                    'e6d9bbc0-89a6-4754-9b4c-f7b5a6b4718b';
    'turbine-week2.ssn2',                    '7e74ba09-b54d-415e-bb25-4214300019ef';
    'turbine-week3.ssn2',                    '56d725d0-569b-4c74-b6ae-4dd497edca2d';
};

% --- Pick which file to analyse (change this index) ---
selectedIdx = 1;

dbFile = sessions{selectedIdx, 1};
sessionGuid = sessions{selectedIdx, 2};
dbPath = fullfile(scenarioDir, dbFile);
connectionString = sprintf('DbEngine=SQLite;Data Source=%s;', dbPath);
outputFile = 'session_results.mat';

fprintf('Selected: %s\n', dbFile);
if ~exist(dbPath, 'file')
    error('File not found: %s\nCheck that notebooks/scenarios/data/ contains the .ssn2 files.', dbPath);
end

%% Initialise SQL Race
h = setup_sqlrace();
import MESL.SqlRace.Domain.*;

%% Load Session
fprintf('Loading session: %s\n', sessionGuid);

sessionManager = SessionManager.CreateSessionManager();
sessionSummaries = sessionManager.FindBySessionState(SessionState.Historical, connectionString);
if sessionSummaries.Count == 0
    error('No sessions found in %s', dbPath);
end
sessionKey = sessionSummaries.Item(0).Key;
clientSession = sessionManager.Load(sessionKey, connectionString);
session = clientSession.Session;

fprintf('  Name:       %s\n', char(session.Identifier));
fprintf('  Start:      %d ns\n', session.StartTime);
fprintf('  End:        %d ns\n', session.EndTime);
fprintf('  Duration:   %.3f s\n', double(session.EndTime - session.StartTime) / 1e9);
fprintf('  Parameters: %d\n', session.Parameters.Count);

%% Extract Parameters to Timetable
nParams = session.Parameters.Count;
paramIds = cell(nParams, 1);
for i = 1:nParams
    paramIds{i} = char(session.Parameters.Item(i-1).Identifier);
end

fprintf('\nExtracting %d parameter(s)...\n', nParams);

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

        if isempty(timeVec) && nSamples > 0
            tsNs = zeros(nSamples, 1, 'int64');
            for i = 1:nSamples
                tsNs(i) = samples.Timestamp(i);
            end
            startNs = tsNs(1);
            timeVec = seconds(double(tsNs - startNs) / 1e9);
        end

        fprintf('  %s: %d samples\n', paramId, nSamples);
    catch ex
        fprintf('  %s: error - %s\n', paramId, ex.message);
    end

    pda.Dispose();
end

clientSession.Dispose();
fprintf('\nSession closed.\n');

%% Build Timetable
if isempty(timeVec)
    fprintf('No data extracted.\n');
    return;
end

tt = timetable(timeVec);
fields = fieldnames(data);
for f = 1:numel(fields)
    tt.(fields{f}) = data.(fields{f})(1:height(tt));
end

fprintf('\nTimetable: %d rows x %d variables\n', height(tt), width(tt));

%% Compute Statistics
fprintf('\n=== Statistics ===\n');
fprintf('%-30s %10s %10s %10s %10s\n', 'Parameter', 'Mean', 'Std', 'Min', 'Max');
fprintf('%s\n', repmat('-', 1, 72));

statsTable = table();
for f = 1:numel(fields)
    vals = tt.(fields{f});
    m = mean(vals, 'omitnan');
    s = std(vals, 'omitnan');
    mn = min(vals);
    mx = max(vals);
    fprintf('%-30s %10.4f %10.4f %10.4f %10.4f\n', paramIds{f}, m, s, mn, mx);

    statsTable.(fields{f}) = [m; s; mn; mx];
end
statsTable.Properties.RowNames = {'Mean', 'Std', 'Min', 'Max'};

%% Plot Parameter Traces
figure('Name', 'Session Analysis', 'NumberTitle', 'off');
nPlots = min(numel(fields), 4);  % Max 4 subplots

for f = 1:nPlots
    subplot(nPlots, 1, f);
    plot(tt.timeVec, tt.(fields{f}));
    ylabel(paramIds{f}, 'Interpreter', 'none');
    grid on;
    if f == 1
        title(sprintf('Session: %s', char(session.Identifier)), 'Interpreter', 'none');
    end
    if f == nPlots
        xlabel('Time (s)');
    end
end

%% Save Results
save(outputFile, 'tt', 'statsTable', 'paramIds');
fprintf('\nResults saved to: %s\n', outputFile);
fprintf('Done.\n');
