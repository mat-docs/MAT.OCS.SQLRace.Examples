# ─────────────────────────────────────────────────────────────
# End-to-end snippet runner: C#, Python and MATLAB.
#
# Generates the deterministic fixture (tools/FixtureGenerator), runs every
# runnable snippet from scripts/snippet-manifest.psd1 against it, and compares
# each snippet's output against a checked-in golden file under tests/golden/.
#
# Why golden files rather than "did it exit 0": an API change can leave a
# snippet compiling and exiting cleanly while quietly returning different data.
# Comparing normalised output catches that. It also means adding a snippet costs
# a manifest line rather than a hand-authored assertion.
#
# Python and MATLAB matter disproportionately here. They bind to SQL Race by
# name at runtime, so a rename that a C# compiler would catch fails silently for
# them - running them is the only way to find out.
#
# Requires the SQL Race runtime + licence - local / licenced-runner only, NOT
# GitHub CI. The licence-free compile check is in tests/SqlRace.Examples.Tests
# (SnippetCompilationTests).
#
# Usage:
#   ./scripts/run-examples-e2e.ps1
#   ./scripts/run-examples-e2e.ps1 -Language CSharp
#   ./scripts/run-examples-e2e.ps1 -Filter '*read*'
#   ./scripts/run-examples-e2e.ps1 -SqlRaceApiVersion <candidate-version>
#   ./scripts/run-examples-e2e.ps1 -UpdateGolden        # after an intended change
# ─────────────────────────────────────────────────────────────
[CmdletBinding()]
param (
    [ValidateSet('All', 'CSharp', 'Python', 'Matlab')]
    [string] $Language = 'All',

    # Wildcard over snippet names, for iterating on one snippet.
    [string] $Filter = '*',

    # Rewrite golden files from this run's output instead of comparing. Review
    # the resulting diff - it will happily bake in a regression.
    [switch] $UpdateGolden,

    # Pin the MESL.SQLRace.API version to validate a candidate build before it
    # ships. Empty means whatever the projects resolve normally.
    [string] $SqlRaceApiVersion,

    # Extra NuGet source, normally a folder holding a .nupkg that has not been
    # published anywhere - the package a CI build just produced, for instance.
    # Passed as RestoreAdditionalProjectSources rather than added with
    # "dotnet nuget add source", because the repo nuget.config starts with
    # <clear /> and would discard a source added at user level.
    [string] $AdditionalPackageSource,

    # Hard limit per snippet. Nothing here is supposed to wait on anything, so
    # a snippet still running after this has hung and is killed. Generous
    # because a cold MATLAB start alone is most of a minute.
    [int] $SnippetTimeoutSeconds = 180,

    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo

try {
    $fixtureKey = 'f1c0ffee-0000-4000-8000-000000000001'   # VerifiedFixture.SessionKeyString
    $fixturePath = Join-Path $env:TEMP 'sqlrace-examples.ssn2'
    $goldenRoot = Join-Path $repo 'tests\golden'
    $manifest = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'snippet-manifest.psd1')

    $buildArgs = @('-c', 'Release', '-v', 'quiet')
    if ($SqlRaceApiVersion) { $buildArgs += "-p:SqlRaceApiVersion=$SqlRaceApiVersion" }
    if ($AdditionalPackageSource) {
        $buildArgs += "-p:RestoreAdditionalProjectSources=$AdditionalPackageSource"
    }

    # ── Normalisation ────────────────────────────────────────
    #
    # Snippets print values that legitimately differ between runs. Without this
    # every golden comparison would fail on the first newly generated GUID.
    function ConvertTo-ComparableOutput {
        param ([string] $Text)

        if (-not $Text) { return '' }

        $normalised = $Text -replace "`r`n", "`n"

        # Volatile values, most specific pattern first.
        $normalised = $normalised -replace '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}', '<guid>'
        $normalised = $normalised -replace '\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(\.\d+)?', '<datetime>'
        # Single-digit month and day, and an AM/PM suffix: pythonnet renders
        # DateTime with the current culture, so 9/3/2026 10:00:00 AM has to
        # normalise too or the golden only holds for the day it was written.
        $normalised = $normalised -replace '\d{1,2}/\d{1,2}/\d{4} \d{1,2}:\d{2}:\d{2}( [AP]M)?', '<datetime>'
        # Windows paths, including the per-user temp directory the fixture lives in.
        $normalised = $normalised -replace '[A-Za-z]:\\[^\s"'']*', '<path>'
        # Elapsed times and sizes: "12.345 s", "1234 ms", "4.2 MB".
        $normalised = $normalised -replace '\d+(\.\d+)?\s*(ms|s\b|KB|MB|GB)', '<measure>'

        # Trailing whitespace and blank lines at either end are noise.
        $lines = $normalised -split "`n" | ForEach-Object { $_.TrimEnd() }
        return (($lines -join "`n").Trim())
    }

    # Build noise that says nothing about the snippet's behaviour.
    $noisePattern = 'warning|NU1701|Restored|Determining|reachable on all platforms|' +
                    'Build succeeded|Time Elapsed|^\s*\d+ Warning|^\s*\d+ Error'

    function Remove-BuildNoise {
        param ([string[]] $Lines)
        return $Lines | Where-Object { $_ -notmatch $noisePattern -and $_.Trim() -ne '' }
    }

    # ── Fixture ──────────────────────────────────────────────
    if (-not $SkipBuild) {
        Write-Host 'Building fixture generator...' -ForegroundColor Cyan
        & dotnet build tools/FixtureGenerator/FixtureGenerator.csproj @buildArgs | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'FixtureGenerator build failed' }
    }

    # Regenerated before every snippet, not once per run. Several snippets create
    # their own sessions in the same SQLite file the read-only snippets query, so
    # a single fixture would accumulate state: 01-load-session-from-file reports
    # "Sessions found: 1" on its own and "Sessions found: 10" after the session
    # creating snippets have run. Golden output has to be independent of the
    # order snippets execute in, and of whether a previous run left anything
    # behind.
    function Reset-Fixture {
        # SQLite handles are not always released the instant a snippet process
        # exits, so deleting and regenerating can lose a race with the previous
        # snippet. Retry rather than failing the whole run on a transient lock.
        for ($attempt = 1; $attempt -le 5; $attempt++) {
            try {
                if (Test-Path $fixturePath) { Remove-Item $fixturePath -Force -ErrorAction Stop }
                & 'tools/FixtureGenerator/bin/Release/net8.0/FixtureGenerator.exe' $fixturePath | Out-Null
                if ($LASTEXITCODE -eq 0) { return }
            }
            catch {
                if ($attempt -eq 5) { throw "Could not reset the fixture: $($_.Exception.Message)" }
            }
            Start-Sleep -Milliseconds (200 * $attempt)
        }
        throw "Fixture generation failed after 5 attempts (file locked by a previous snippet?)"
    }

    Write-Host "Verified fixture at $fixturePath" -ForegroundColor Cyan
    Reset-Fixture

    # Runs an external command with a hard timeout, capturing both streams.
    #
    # Needed because a snippet can hang rather than fail - one waiting on live
    # data that never arrives, or a MATLAB session that stops for input. Without
    # a timeout that wedges the entire run, and because the hung process keeps a
    # handle on the fixture, every subsequent snippet fails too. Nothing is
    # supposed to sit and wait here, so a snippet still running after the
    # timeout is a failure by definition.
    function Invoke-WithTimeout {
        param (
            [Parameter(Mandatory)][string] $FilePath,
            [string[]] $ArgumentList = @(),
            [string] $WorkingDirectory = $repo,
            [int] $TimeoutSeconds = $SnippetTimeoutSeconds
        )

        $stdout = [System.IO.Path]::GetTempFileName()
        $stderr = [System.IO.Path]::GetTempFileName()

        try {
            $startArgs = @{
                FilePath               = $FilePath
                WorkingDirectory       = $WorkingDirectory
                RedirectStandardOutput = $stdout
                RedirectStandardError  = $stderr
                NoNewWindow            = $true
                PassThru               = $true
            }
            if ($ArgumentList.Count -gt 0) { $startArgs.ArgumentList = $ArgumentList }

            $process = Start-Process @startArgs

            # Touching Handle caches it. Without this, Start-Process -PassThru
            # hands back an object that releases the native handle on exit and
            # ExitCode reads back empty - every snippet then looks like a
            # failure with no exit code to explain it.
            $null = $process.Handle

            if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
                # Kill the tree: dotnet run spawns the snippet as a child, and
                # killing only the parent would leave the child holding the
                # fixture open.
                try { $process.Kill($true) } catch { }
                $process.WaitForExit(5000) | Out-Null
                return [pscustomobject]@{
                    ExitCode = -1
                    TimedOut = $true
                    Lines    = @("Timed out after $TimeoutSeconds seconds.")
                }
            }

            # The timed WaitForExit overload can return before the exit code is
            # cached on the object, leaving ExitCode empty. The parameterless
            # overload also waits for the redirected streams to be flushed, so
            # this both populates ExitCode and guarantees the files are complete.
            $process.WaitForExit()

            $lines = @()
            foreach ($file in $stdout, $stderr) {
                if ((Get-Item $file).Length -gt 0) {
                    # The snippets write UTF-8 (box-drawing characters in table
                    # rules, en dashes in ranges). Get-Content defaults to the
                    # ANSI codepage on Windows PowerShell, which would mangle
                    # them into mojibake and fail every golden comparison.
                    $lines += (Get-Content $file -Encoding UTF8 -ErrorAction SilentlyContinue)
                }
            }

            return [pscustomobject]@{
                ExitCode = $process.ExitCode
                TimedOut = $false
                Lines    = $lines
            }
        }
        finally {
            Remove-Item $stdout, $stderr -Force -ErrorAction SilentlyContinue
        }
    }

    # ── C# runner project ────────────────────────────────────
    #
    # tools/SnippetRunner compiles whichever file its SnippetFile property points
    # at, so a snippet runs without anything in the working tree changing. The
    # previous version of this script copied each snippet over Program.cs and
    # restored it in a finally block, which left the repo dirty for the duration
    # and lost the original if the run was interrupted.
    $runnerProject = 'tools\SnippetRunner\SnippetRunner.csproj'

    function Initialize-SnippetRunner {
        # Build once with the default Program.cs: this resolves the package and
        # populates the output folder that SQLRACE_DLL_PATH points at, so all
        # three languages exercise the same assemblies.
        Write-Host 'Preparing snippet runner...' -ForegroundColor Cyan
        & dotnet build $runnerProject @buildArgs | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw ("SnippetRunner build failed. If this is a NuGet 401, MESL.SQLRace.API is " +
                   "referenced as Version=`"*`" and NuGet cannot resolve a floating version " +
                   "without reaching the feed: set NUGET_AUTH_USERNAME and NUGET_AUTH_TOKEN " +
                   "(see .env.example), or pass -SqlRaceApiVersion to pin a version already " +
                   "in the local NuGet cache.")
        }

        $domain = Join-Path $repo 'tools\SnippetRunner\bin\Release\net8.0\MESL.SqlRace.Domain.dll'
        if (-not (Test-Path $domain)) { throw "MESL.SqlRace.Domain.dll not found at $domain" }
        return $domain
    }

    function Invoke-CSharpSnippet {
        param ([hashtable] $Snippet, [string[]] $Arguments)

        # SnippetFile must be absolute - MSBuild resolves relative Compile items
        # against the project directory, not the working directory.
        $snippetFile = (Resolve-Path $Snippet.Path).Path

        $dotnetArgs = @('run', '--project', $runnerProject) + $buildArgs +
                      @("-p:SnippetFile=$snippetFile", '--') + $Arguments

        return Invoke-WithTimeout -FilePath 'dotnet' -ArgumentList $dotnetArgs
    }

    # A NuGet-restored build keeps platform-specific assets under runtimes/<rid>/
    # and relies on the app's deps.json to pick the right ones. Python and MATLAB
    # host the CLR themselves, so there is no deps.json for SQL Race: the loader
    # falls back to the flat directory, finds the placeholder System.Data.SqlClient
    # ("not supported on this platform") and cannot find SQLite.Interop.dll at all.
    #
    # Flattening the Windows assets over a copy of the output gives those hosts the
    # same layout an ATLAS installation has. This is not only a harness concern - a
    # customer pointing pythonnet or MATLAB at a NuGet-restored SQL Race hits
    # exactly this, while one pointing at an ATLAS install does not.
    function New-FlatRuntimeDirectory {
        param ([Parameter(Mandatory)][string] $BuildOutputDir)

        $flatDir = Join-Path $env:TEMP 'sqlrace-flat-runtime'
        if (Test-Path $flatDir) { Remove-Item $flatDir -Recurse -Force }
        New-Item -ItemType Directory -Path $flatDir -Force | Out-Null

        Copy-Item (Join-Path $BuildOutputDir '*') $flatDir -Recurse -Force

        # Overlay Windows assets last so they win over the cross-platform
        # placeholders sitting in the output root.
        $overlays = @(
            'runtimes\win\lib\netstandard2.0'
            'runtimes\win\lib\netcoreapp2.0'
            'runtimes\win\lib\netcoreapp2.1'
            'runtimes\win\lib\netcoreapp3.0'
            'runtimes\win-x64\native'
        )
        foreach ($overlay in $overlays) {
            $source = Join-Path $BuildOutputDir $overlay
            if (Test-Path $source) {
                Copy-Item (Join-Path $source '*') $flatDir -Force -ErrorAction SilentlyContinue
            }
        }

        return $flatDir
    }

    function Invoke-PythonSnippet {
        param ([hashtable] $Snippet, [string[]] $Arguments)

        # Python defaults stdout to the console codepage on Windows (cp1252),
        # and the snippets print box-drawing characters in their table rules -
        # without this they die with UnicodeEncodeError before reaching any SQL
        # Race call, which would look like an API failure but is not one.
        $previousEncoding = $env:PYTHONIOENCODING
        $env:PYTHONIOENCODING = 'utf-8'
        try {
            return Invoke-WithTimeout -FilePath 'python' `
                -ArgumentList (@((Resolve-Path $Snippet.Path).Path) + $Arguments)
        }
        finally {
            $env:PYTHONIOENCODING = $previousEncoding
        }
    }

# Preflight before the MATLAB leg. A MATLAB startup costs the better part of a
    # minute, so without this a missing install or an unavailable licence seat
    # produces twenty-odd identical failures over quarter of an hour instead of
    # one clear message in forty seconds.
    #
    # Licensing is a network checkout: <matlabroot>/licenses/network.lic points at
    # the FlexLM server, or MLM_LICENSE_FILE overrides it as port@host. Note that
    # FlexLM needs two ports open - lmgrd (27000 by default) and the MLM vendor
    # daemon, which is randomly assigned unless the server pins it with
    # "port=" on its DAEMON line.
    function Test-MatlabAvailable {
        $matlab = Get-Command matlab -ErrorAction SilentlyContinue
        if (-not $matlab) {
            Write-Host 'MATLAB not found on PATH - skipping the MATLAB leg.' -ForegroundColor Yellow
            Write-Host '  Install MATLAB, or run with -Language CSharp or -Language Python.' -ForegroundColor Yellow
            return $false
        }

        Write-Host "Checking out a MATLAB licence ($($matlab.Source))..." -ForegroundColor Cyan
        $previousPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $probe = & matlab -batch "fprintf('MATLAB %s licensed\n', version)" 2>&1 |
                ForEach-Object { $_.ToString() }
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousPreference
        }

        if ($exitCode -ne 0) {
            Write-Host 'MATLAB is installed but would not start - skipping the MATLAB leg.' -ForegroundColor Yellow
            $probe | Select-Object -First 6 | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
            Write-Host "  MLM_LICENSE_FILE = $($env:MLM_LICENSE_FILE)" -ForegroundColor Yellow
            Write-Host '  A licence manager error here usually means the agent cannot reach' -ForegroundColor Yellow
            Write-Host '  the FlexLM server, or no concurrent seat is free.' -ForegroundColor Yellow
            return $false
        }

        Write-Host "  $($probe | Where-Object { $_ -match 'licensed' } | Select-Object -First 1)" -ForegroundColor DarkGray
        return $true
    }

# Preflight for the Python leg. Without it, a machine with no Python produces
    # one identical "The system cannot find the file specified" per snippet and
    # nothing that says Python is simply not installed - which is exactly how it
    # first showed up on a build agent.
    #
    # Returns $null when Python is usable, otherwise the reason it is not.
    function Test-PythonAvailable {
        $python = Get-Command python -ErrorAction SilentlyContinue
        if (-not $python) {
            Write-Host 'Python not found on PATH - skipping the Python leg.' -ForegroundColor Yellow
            Write-Host '  Install Python 3, then: pip install pythonnet cffi pandas' -ForegroundColor Yellow
            return 'Python is not installed on this machine'
        }

        # pythonnet needs clr_loader, which needs cffi. A missing cffi fails
        # inside pythonnet.load() with a runtime error that reads like a SQL Race
        # fault rather than a missing dependency.
        $previousPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $probe = & python -c "import clr_loader, cffi" 2>&1 | ForEach-Object { $_.ToString() }
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousPreference
        }

        if ($exitCode -ne 0) {
            Write-Host "Python is installed ($($python.Source)) but pythonnet is not usable - skipping the Python leg." -ForegroundColor Yellow
            $probe | Select-Object -First 3 | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
            Write-Host '  Fix with: pip install pythonnet cffi pandas' -ForegroundColor Yellow
            return 'pythonnet or its dependencies are missing'
        }

        Write-Host "Python ready ($($python.Source))" -ForegroundColor DarkGray
        return $null
    }

    function Invoke-MatlabSnippet {
        param ([hashtable] $Snippet, [string[]] $Arguments)

        # -batch runs headless, returns a non-zero exit code on an uncaught
        # error, and needs an absolute path since it does not inherit our cwd
        # semantics for run().
        $full = (Resolve-Path $Snippet.Path).Path
        return Invoke-WithTimeout -FilePath 'matlab' -ArgumentList @('-batch', "run('$full')")
    }

    $runners = @{
        CSharp = ${function:Invoke-CSharpSnippet}
        Python = ${function:Invoke-PythonSnippet}
        Matlab = ${function:Invoke-MatlabSnippet}
    }

    $languages = if ($Language -eq 'All') { @('CSharp', 'Python', 'Matlab') } else { @($Language) }

    # Only prepare the C# runner if we need it - it is also what supplies
    # SQLRACE_DLL_PATH for the other two languages.
    $sqlRaceDllPath = Initialize-SnippetRunner

    # The C# leg runs through dotnet, which resolves assets from deps.json. Python
    # and MATLAB need the flattened layout instead - but the same build either way,
    # so all three languages exercise identical assemblies.
    $flatRuntimeDir = New-FlatRuntimeDirectory -BuildOutputDir (Split-Path $sqlRaceDllPath -Parent)
    $env:SQLRACE_DLL_PATH = Join-Path $flatRuntimeDir 'MESL.SqlRace.Domain.dll'
    $env:PATH = "$flatRuntimeDir;$env:PATH"
    Write-Host "SQLRACE_DLL_PATH = $($env:SQLRACE_DLL_PATH)" -ForegroundColor DarkGray

    $results = @()

    foreach ($lang in $languages) {
        $snippets = @($manifest[$lang] | Where-Object { $_.Name -like $Filter })
        if ($snippets.Count -eq 0) { continue }

        if ($lang -eq 'Matlab' -and -not (Test-MatlabAvailable)) {
            foreach ($snippet in $snippets) {
                $results += [pscustomobject]@{
                    Language = 'Matlab'; Snippet = $snippet.Name; Status = 'SKIP'
                    Detail   = 'MATLAB unavailable or unlicensed on this machine'
                }
            }
            continue
        }

        if ($lang -eq 'Python') {
            $reason = Test-PythonAvailable
            if ($reason) {
                foreach ($snippet in $snippets) {
                    $results += [pscustomobject]@{
                        Language = 'Python'; Snippet = $snippet.Name; Status = 'SKIP'; Detail = $reason
                    }
                }
                continue
            }
        }

        Write-Host ""
        Write-Host "########## $lang ##########" -ForegroundColor Yellow

        $goldenDir = Join-Path $goldenRoot $lang
        if ($UpdateGolden -and -not (Test-Path $goldenDir)) {
            New-Item -ItemType Directory -Path $goldenDir -Force | Out-Null
        }

        foreach ($snippet in $snippets) {
            if ($snippet.Skip) {
                Write-Host ("[SKIP] {0} - {1}" -f $snippet.Name, $snippet.Skip) -ForegroundColor DarkGray
                $results += [pscustomobject]@{
                    Language = $lang; Snippet = $snippet.Name; Status = 'SKIP'; Detail = $snippet.Skip
                }
                continue
            }

            $arguments = @()
            foreach ($argument in @($snippet.Args)) {
                if ($null -eq $argument) { continue }
                $arguments += $argument.Replace('{fixtureKey}', $fixtureKey).Replace('{fixturePath}', $fixturePath)
            }

            Write-Host ("`n===== {0}/{1} =====" -f $lang, $snippet.Name) -ForegroundColor Cyan

            Reset-Fixture

            # Under Windows PowerShell 5.1, redirecting a native command's stderr
            # turns each line into an ErrorRecord, which $ErrorActionPreference
            # = 'Stop' promotes to a terminating error. A Python traceback or a
            # MATLAB warning would then abort the run instead of being captured
            # as the snippet's output. $ErrorActionPreference is dynamically
            # scoped, so relaxing it here covers the runner functions too.
            $previousPreference = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            try {
                $run = & $runners[$lang] $snippet $arguments
            }
            catch {
                Write-Host "[FAIL] runner threw: $($_.Exception.Message)" -ForegroundColor Red
                $results += [pscustomobject]@{
                    Language = $lang; Snippet = $snippet.Name; Status = 'FAIL'; Detail = $_.Exception.Message
                }
                continue
            }
            finally {
                $ErrorActionPreference = $previousPreference
            }

            $meaningful = Remove-BuildNoise -Lines $run.Lines
            $actual = ConvertTo-ComparableOutput (($meaningful | Out-String))

            $status = 'PASS'
            $detail = ''

            if ($run.TimedOut) {
                # Distinct from a plain failure: the snippet is hung, not wrong.
                # Named separately so nobody goes looking for a data mismatch.
                $status = 'TIMEOUT'
                $detail = "hung and was killed after $SnippetTimeoutSeconds seconds"
            }
            elseif ($run.ExitCode -ne 0) {
                $status = 'FAIL'
                $detail = "exit code $($run.ExitCode)"
            }
            elseif ($actual -match 'ERROR:|Unhandled exception|Traceback \(most recent call last\)') {
                $status = 'FAIL'
                $detail = 'snippet reported an error'
            }
            else {
                $goldenPath = Join-Path $goldenDir "$($snippet.Name).txt"

                if ($UpdateGolden) {
                    $actual | Set-Content $goldenPath -Encoding utf8
                    $status = 'GOLDEN'
                    $detail = 'written'
                }
                elseif (-not (Test-Path $goldenPath)) {
                    # Not a pass: an absent golden means nothing was verified.
                    $status = 'NOGOLD'
                    $detail = 'no golden file - run with -UpdateGolden'
                }
                else {
                    $expected = ConvertTo-ComparableOutput (Get-Content $goldenPath -Raw)
                    if ($expected -ne $actual) {
                        $status = 'DIFF'
                        $detail = 'output differs from golden'
                    }
                }
            }

            $colour = switch ($status) {
                'PASS'   { 'Green' }
                'GOLDEN' { 'Cyan' }
                'NOGOLD' { 'Yellow' }
                default  { 'Red' }
            }
            Write-Host "[$status] $detail" -ForegroundColor $colour
            $meaningful | Select-Object -First 4 | ForEach-Object { Write-Host "  $_" }

            if ($status -eq 'DIFF') {
                # Show the first few differing lines - the whole point is to make
                # the behaviour change legible, not just flag that one happened.
                $diff = Compare-Object ($expected -split "`n") ($actual -split "`n") |
                    Select-Object -First 6
                Write-Host '  --- differences (< golden, > this run) ---' -ForegroundColor Red
                $diff | ForEach-Object { Write-Host "  $($_.SideIndicator) $($_.InputObject)" -ForegroundColor Red }
            }

            $results += [pscustomobject]@{
                Language = $lang; Snippet = $snippet.Name; Status = $status; Detail = $detail
            }
        }
    }

    # ── Summary ──────────────────────────────────────────────
    Write-Host ""
    Write-Host '=== Summary ===' -ForegroundColor Yellow
    $results | Group-Object Language, Status |
        Sort-Object Name |
        ForEach-Object { "{0,-22} {1}" -f $_.Name, $_.Count } |
        Write-Host

    $bad = @($results | Where-Object { $_.Status -in @('FAIL', 'DIFF', 'TIMEOUT') })
    $ungolden = @($results | Where-Object { $_.Status -eq 'NOGOLD' })
    $skipped = @($results | Where-Object { $_.Status -eq 'SKIP' })

    if ($skipped.Count -gt 0) {
        Write-Host ""
        Write-Host "$($skipped.Count) snippet(s) not executed:" -ForegroundColor DarkGray
        $skipped | ForEach-Object { Write-Host "  $($_.Language)/$($_.Snippet): $($_.Detail)" -ForegroundColor DarkGray }
    }

    if ($ungolden.Count -gt 0) {
        Write-Host ""
        Write-Host "$($ungolden.Count) snippet(s) ran but were not verified (no golden file)." -ForegroundColor Yellow
        $ungolden | ForEach-Object { Write-Host "  $($_.Language)/$($_.Snippet)" -ForegroundColor Yellow }
    }

    if ($bad.Count -gt 0) {
        Write-Host ""
        Write-Host "$($bad.Count) snippet(s) failed:" -ForegroundColor Red
        $bad | ForEach-Object { Write-Host "  $($_.Language)/$($_.Snippet): $($_.Detail)" -ForegroundColor Red }
        exit 1
    }

    Write-Host ""
    Write-Host 'All executed snippets matched their golden output.' -ForegroundColor Green
}
finally {
    Pop-Location
}
