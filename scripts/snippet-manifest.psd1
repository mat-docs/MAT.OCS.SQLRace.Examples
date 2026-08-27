<#
    Which example snippets the end-to-end runner executes, and how.

    scripts/run-examples-e2e.ps1 reads this file. Keeping the list here rather
    than inline in the script means adding a snippet is a data change, and it
    puts the skips in one visible place - a snippet silently missing from the
    run is indistinguishable from one that passed.

    Name  - identifies the snippet and names its golden output file under
            tests/golden/<language>/<name>.txt
    Path  - repo-relative path to the snippet
    Args  - arguments to pass. '{fixtureKey}' and '{fixturePath}' are replaced
            with the deterministic fixture's session key and file path. Omit for
            snippets that create their own session or take no input.
    Skip  - why this snippet is not executed. Skipped snippets are listed in the
            summary so the coverage gap stays visible.

    MATLAB snippets take no arguments - they discover the session themselves
    from the fixture - so none of them declare Args.
#>
@{

    CSharp = @(
        @{ Name = 'append-multiple-sessions'; Path = 'snippets\csharp\composite-sessions\append-multiple-sessions.cs' }
        @{ Name = 'composite-with-associates'; Path = 'snippets\csharp\composite-sessions\composite-with-associates.cs' }
        @{ Name = 'whole-session-compare'; Path = 'snippets\csharp\composite-sessions\whole-session-compare.cs' }
        @{ Name = 'conversion-types'; Path = 'snippets\csharp\configuration\conversion-types.cs' }
        @{ Name = 'create-parameter-config'; Path = 'snippets\csharp\configuration\create-parameter-config.cs' }
        @{ Name = 'create-transient-parameter'; Path = 'snippets\csharp\configuration\create-transient-parameter.cs' }
        @{ Name = 'introspect-session-config'; Path = 'snippets\csharp\configuration\introspect-session-config.cs'; Args = @('{fixtureKey}') }
        @{ Name = 'bulk-parameter-read'; Path = 'snippets\csharp\data-access\bulk-parameter-read.cs'; Args = @('{fixtureKey}') }
        @{ Name = 'data-status-filtering'; Path = 'snippets\csharp\data-access\data-status-filtering.cs'; Args = @('{fixtureKey}') }
        @{ Name = 'multi-rate-alignment'; Path = 'snippets\csharp\data-access\multi-rate-alignment.cs'; Args = @('{fixtureKey}') }
        @{ Name = 'read-samples-between'; Path = 'snippets\csharp\data-access\read-samples-between.cs'; Args = @('{fixtureKey}') }
        @{ Name = 'read-subsampled-data'; Path = 'snippets\csharp\data-access\read-subsampled-data.cs'; Args = @('{fixtureKey}') }
        @{ Name = 'reverse-iteration'; Path = 'snippets\csharp\data-access\reverse-iteration.cs'; Args = @('{fixtureKey}') }
        @{ Name = 'timestamp-array-construction'; Path = 'snippets\csharp\data-access\timestamp-array-construction.cs' }
        @{ Name = 'dotnet-function-basic'; Path = 'snippets\csharp\functions\dotnet-function-basic.cs'; Skip = 'declares a function class rather than running one - needs a MEF host and the .NET Functions licence option' }
        @{ Name = 'dotnet-function-with-pda'; Path = 'snippets\csharp\functions\dotnet-function-with-pda.cs'; Skip = 'declares a function class rather than running one - needs a MEF host and the .NET Functions licence option' }
        @{ Name = 'fdl-function-basic'; Path = 'snippets\csharp\functions\fdl-function-basic.cs'; Skip = 'needs the ATLAS Functions licence option, which the fixture runner does not have' }
        @{ Name = '01-load-session-from-file'; Path = 'snippets\csharp\getting-started\01-load-session-from-file.cs'; Args = @('{fixturePath}') }
        @{ Name = '02-load-session-from-database'; Path = 'snippets\csharp\getting-started\02-load-session-from-database.cs'; Skip = 'needs a SQL Server instance, not the SQLite fixture' }
        @{ Name = '03-read-parameter-samples'; Path = 'snippets\csharp\getting-started\03-read-parameter-samples.cs'; Args = @('{fixtureKey}') }
        @{ Name = '04-create-session-write-data'; Path = 'snippets\csharp\getting-started\04-create-session-write-data.cs' }
        @{ Name = '05-query-sessions-by-metadata'; Path = 'snippets\csharp\getting-started\05-query-sessions-by-metadata.cs' }
        @{ Name = 'poll-live-samples'; Path = 'snippets\csharp\live-data\poll-live-samples.cs'; Skip = 'needs a live session from a recorder or Stream API' }
        @{ Name = 'rda-parameter-change-detection'; Path = 'snippets\csharp\live-data\rda-parameter-change-detection.cs'; Skip = 'needs a live session from a recorder or Stream API' }
        @{ Name = 'subscribe-to-data-events'; Path = 'snippets\csharp\live-data\subscribe-to-data-events.cs'; Skip = 'needs a live session from a recorder or Stream API' }
        @{ Name = 'subscribe-to-lap-events'; Path = 'snippets\csharp\live-data\subscribe-to-lap-events.cs'; Skip = 'needs a live session from a recorder or Stream API' }
        @{ Name = 'event-definitions-and-data'; Path = 'snippets\csharp\session-management\event-definitions-and-data.cs' }
        @{ Name = 'lap-and-segment-management'; Path = 'snippets\csharp\session-management\lap-and-segment-management.cs' }
        @{ Name = 'marker-management'; Path = 'snippets\csharp\session-management\marker-management.cs' }
        @{ Name = 'parameter-unit-resolution'; Path = 'snippets\csharp\session-management\parameter-unit-resolution.cs'; Args = @('{fixtureKey}') }
        @{ Name = 'query-with-composite-filter'; Path = 'snippets\csharp\session-management\query-with-composite-filter.cs' }
        @{ Name = 'session-association'; Path = 'snippets\csharp\session-management\session-association.cs' }
        @{ Name = 'session-metadata-crud'; Path = 'snippets\csharp\session-management\session-metadata-crud.cs' }
    )

    Python = @(
        @{ Name = 'export_to_pandas'; Path = 'snippets\python\data-access\export_to_pandas.py'; Args = @('{fixtureKey}') }
        @{ Name = 'multi_parameter_extract'; Path = 'snippets\python\data-access\multi_parameter_extract.py'; Args = @('{fixtureKey}') }
        @{ Name = 'read_samples_to_list'; Path = 'snippets\python\data-access\read_samples_to_list.py'; Args = @('{fixtureKey}') }
        @{ Name = '01_load_session'; Path = 'snippets\python\getting-started\01_load_session.py'; Args = @('{fixtureKey}') }
        @{ Name = '02_read_parameters'; Path = 'snippets\python\getting-started\02_read_parameters.py'; Args = @('{fixtureKey}') }
        @{ Name = '03_query_sessions'; Path = 'snippets\python\getting-started\03_query_sessions.py' }
        @{ Name = '04_extract_events'; Path = 'snippets\python\getting-started\04_extract_events.py'; Args = @('{fixtureKey}'); Skip = 'event data does not survive a session reload, so a loaded fixture session never has any - the snippet runs but verifies nothing' }
    )

    Matlab = @(
        @{ Name = 'introspect_session_config'; Path = 'snippets\matlab\configuration\introspect_session_config.m' }
        @{ Name = 'parameter_unit_resolution'; Path = 'snippets\matlab\configuration\parameter_unit_resolution.m' }
        @{ Name = 'data_status_filtering'; Path = 'snippets\matlab\data-access\data_status_filtering.m' }
        @{ Name = 'extract_to_timetable'; Path = 'snippets\matlab\data-access\extract_to_timetable.m' }
        @{ Name = 'multi_channel_read'; Path = 'snippets\matlab\data-access\multi_channel_read.m' }
        @{ Name = 'plot_parameters'; Path = 'snippets\matlab\data-access\plot_parameters.m' }
        @{ Name = 'read_samples_between'; Path = 'snippets\matlab\data-access\read_samples_between.m' }
        @{ Name = 'reverse_iteration'; Path = 'snippets\matlab\data-access\reverse_iteration.m' }
        @{ Name = 'segment_statistics'; Path = 'snippets\matlab\data-access\segment_statistics.m' }
        @{ Name = 'subsampled_read'; Path = 'snippets\matlab\data-access\subsampled_read.m' }
        @{ Name = 'read_function_output'; Path = 'snippets\matlab\functions\read_function_output.m' }
        @{ Name = 'create_session_write_data'; Path = 'snippets\matlab\getting-started\create_session_write_data.m' }
        @{ Name = 'diagnose_setup'; Path = 'snippets\matlab\getting-started\diagnose_setup.m' }
        @{ Name = 'list_parameters'; Path = 'snippets\matlab\getting-started\list_parameters.m' }
        @{ Name = 'load_session'; Path = 'snippets\matlab\getting-started\load_session.m' }
        @{ Name = 'load_session_from_database'; Path = 'snippets\matlab\getting-started\load_session_from_database.m'; Skip = 'needs a SQL Server instance, not the SQLite fixture' }
        @{ Name = 'query_sessions'; Path = 'snippets\matlab\getting-started\query_sessions.m' }
        @{ Name = 'read_parameters'; Path = 'snippets\matlab\getting-started\read_parameters.m' }
        @{ Name = 'poll_live_samples'; Path = 'snippets\matlab\live-data\poll_live_samples.m'; Skip = 'needs a live session from a recorder or Stream API' }
        @{ Name = 'server_listener'; Path = 'snippets\matlab\live-data\server_listener.m'; Skip = 'needs a live session from a recorder or Stream API' }
        @{ Name = 'event_extraction'; Path = 'snippets\matlab\session-management\event_extraction.m' }
        @{ Name = 'lap_segment_management'; Path = 'snippets\matlab\session-management\lap_segment_management.m' }
        @{ Name = 'marker_management'; Path = 'snippets\matlab\session-management\marker_management.m' }
        @{ Name = 'session_metadata'; Path = 'snippets\matlab\session-management\session_metadata.m' }
    )
}
