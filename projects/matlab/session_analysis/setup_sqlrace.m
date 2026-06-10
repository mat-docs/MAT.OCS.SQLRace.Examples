function handles = setup_sqlrace(dllPath)
% SETUP_SQLRACE Initialise the SQL Race runtime and return manager handles.
%
%   handles = setup_sqlrace()
%   handles = setup_sqlrace(dllPath)
%
%   Loads the SQL Race .NET assembly, initialises the runtime, and returns
%   a struct with handles to SessionManager and FileSessionManager.
%
%   Call this once at the start of your MATLAB session.
%
%   Prerequisites:
%       - MATLAB R2023a or later (for .NET 6+ support)
%       - .NET runtime configured to Core: dotnetenv('core')
%       - Runtime config at matlabroot/bin/win64/dotnetcli_netcore.runtimeconfig.json
%         must include Microsoft.WindowsDesktop.App framework
%         (see docs/troubleshooting.md for details)
%
%   Input:
%       dllPath - (optional) Path to MESL.SqlRace.Domain.dll
%                 Default: SQLRACE_DLL_PATH env var, or ATLAS 10 default install
%
%   Output:
%       handles - struct with fields:
%           .SessionManager     - SessionManager instance
%           .FileSessionManager - FileSessionManager instance
%
%   Example:
%       dotnetenv('core');              % run once per MATLAB session
%       h = setup_sqlrace();
%       clientSession = h.SessionManager.Load(sessionKey, connectionString);

    % --- Ensure .NET Core runtime is active ---
    % SQL Race targets .NET 8; MATLAB must be configured for .NET Core.
    % If you have runtimes newer than .NET 8 installed, use:
    %   dotnetenv("core", Version="8");
    env = dotnetenv;
    if ~strcmpi(env.Runtime, 'core')
        error('setup_sqlrace:WrongRuntime', ...
            ['MATLAB is using the .NET Framework runtime.\n' ...
             'SQL Race requires .NET 8 (Core). Run the following and restart MATLAB:\n' ...
             '  dotnetenv(''core'')\n' ...
             'Check docs/troubleshooting.md for runtime configuration details.']);
    end

    if nargin < 1
        dllPath = getenv('SQLRACE_DLL_PATH');
        if isempty(dllPath)
            dllPath = 'C:\Program Files\McLaren Applied Technologies\ATLAS 10\MESL.SqlRace.Domain.dll';
        end
    end

    % Validate DLL exists
    if ~exist(dllPath, 'file')
        error('setup_sqlrace:DllNotFound', ...
            'SQL Race DLL not found at: %s\nUpdate the path to match your installation.', dllPath);
    end

    % --- Pre-load Microsoft.Win32.SystemEvents ---
    % The MATLAB-hosted .NET Core runtime may not resolve this assembly
    % automatically. It is required by SQLRace MetricsService/MidnightNotifier.
    % The recommended fix is to include Microsoft.WindowsDesktop.App in the
    % runtime config (see docs/troubleshooting.md). This pre-load is a
    % defence-in-depth fallback.
    installDir = fileparts(dllPath);
    sysEventsDll = fullfile(installDir, 'Microsoft.Win32.SystemEvents.dll');
    if exist(sysEventsDll, 'file')
        NET.addAssembly(sysEventsDll);
    end

    % Load the SQL Race assembly
    NET.addAssembly(dllPath);
    import MESL.SqlRace.Domain.*;

    % Initialise the runtime (idempotent)
    try
    NET.setStaticProperty('MESL.SqlRace.Domain.Core.LicenceProgramName', 'SQLRace');
    Core.Initialize();
        fprintf('SQL Race initialised successfully.\n');
    catch ex
        % May already be initialised — check if it's a re-init error
        if contains(ex.message, 'already')
            fprintf('SQL Race already initialised.\n');
        else
            rethrow(ex);
        end
    end

    % Create manager instances
    handles.SessionManager = SessionManager.CreateSessionManager();
    handles.FileSessionManager = FileSessionManager.CreateFileSessionManager();

    fprintf('SessionManager and FileSessionManager ready.\n');
end
