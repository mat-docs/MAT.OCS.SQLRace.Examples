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
#   ./scripts/run-examples-e2e.ps1 -SqlRaceApiVersion 2.1.26212.6-ci
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
        $normalised = $normalised -replace '\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}', '<datetime>'
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
        if (Test-Path $fixturePath) { Remove-Item $fixturePath -Force }
        & 'tools/FixtureGenerator/bin/Release/net8.0/FixtureGenerator.exe' $fixturePath | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Fixture generation failed' }
    }

    Write-Host "Verified fixture at $fixturePath" -ForegroundColor Cyan
    Reset-Fixture

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

        $output = & dotnet run --project $runnerProject @buildArgs `
            "-p:SnippetFile=$snippetFile" -- @Arguments 2>&1 |
            ForEach-Object { $_.ToString() }
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Lines = $output }
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
            $output = & python $Snippet.Path @Arguments 2>&1 | ForEach-Object { $_.ToString() }
            return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Lines = $output }
        }
        finally {
            $env:PYTHONIOENCODING = $previousEncoding
        }
    }

    function Invoke-MatlabSnippet {
        param ([hashtable] $Snippet, [string[]] $Arguments)

        # -batch runs headless, returns a non-zero exit code on an uncaught
        # error, and needs an absolute path since it does not inherit our cwd
        # semantics for run().
        $full = (Resolve-Path $Snippet.Path).Path
        $output = & matlab -batch "run('$full')" 2>&1 | ForEach-Object { $_.ToString() }
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Lines = $output }
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

            if ($run.ExitCode -ne 0) {
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

    $bad = @($results | Where-Object { $_.Status -in @('FAIL', 'DIFF') })
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
