# ─────────────────────────────────────────────────────────────
# End-to-end snippet smoke-runner.
#
# Generates the deterministic verified fixture (tools/FixtureGenerator) at the default
# SQLite path the snippets read from, then runs each runnable snippet against it and
# checks it exits cleanly and prints the expected anchor text.
#
# Requires the SQL Race runtime + licence — local / licenced-runner only, NOT GitHub CI.
# Usage:  pwsh scripts/run-examples-e2e.ps1
# ─────────────────────────────────────────────────────────────
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    $fixtureKey = "f1c0ffee-0000-4000-8000-000000000001"   # VerifiedFixture.SessionKeyString
    $fixturePath = Join-Path $env:TEMP "sqlrace-examples.ssn2"  # default path snippets read

    Write-Host "Building generator + snippet runner..." -ForegroundColor Cyan
    dotnet build tools/FixtureGenerator/FixtureGenerator.csproj -c Release -v quiet | Out-Null
    if (-not $?) { throw "FixtureGenerator build failed" }

    Write-Host "Generating verified fixture at $fixturePath" -ForegroundColor Cyan
    & "tools/FixtureGenerator/bin/Release/net8.0/FixtureGenerator.exe" $fixturePath | Out-Null
    if (-not $?) { throw "Fixture generation failed" }

    # Each snippet: args to pass, and an expected substring proving it ran against the fixture.
    $snippets = @(
        @{ Name = "01-load-session-from-file";   Path = "snippets\csharp\getting-started\01-load-session-from-file.cs"; Args = @($fixturePath); Expect = "Verified Test Session" },
        @{ Name = "03-read-parameter-samples";   Path = "snippets\csharp\getting-started\03-read-parameter-samples.cs"; Args = @($fixtureKey);  Expect = "1000" },
        @{ Name = "read-samples-between";         Path = "snippets\csharp\data-access\read-samples-between.cs";          Args = @($fixtureKey);  Expect = "Temperature:Sensors" },
        @{ Name = "bulk-parameter-read";          Path = "snippets\csharp\data-access\bulk-parameter-read.cs";           Args = @($fixtureKey);  Expect = $null },
        @{ Name = "multi-rate-alignment";         Path = "snippets\csharp\data-access\multi-rate-alignment.cs";          Args = @($fixtureKey);  Expect = $null },
        @{ Name = "reverse-iteration";            Path = "snippets\csharp\data-access\reverse-iteration.cs";             Args = @($fixtureKey);  Expect = $null },
        @{ Name = "parameter-unit-resolution";    Path = "snippets\csharp\session-management\parameter-unit-resolution.cs"; Args = @($fixtureKey); Expect = "Temperature:Sensors" }
    )

    # Preserve the SnippetRunner program (the runner mechanism overwrites it).
    $runnerProgram = "tools\SnippetRunner\Program.cs"
    $backup = Get-Content $runnerProgram -Raw

    $results = @()
    try {
        foreach ($s in $snippets) {
            Write-Host "`n===== $($s.Name) =====" -ForegroundColor Cyan
            Copy-Item $s.Path $runnerProgram -Force
            (Get-Item $runnerProgram).LastWriteTime = Get-Date

            $out = dotnet run --project tools\SnippetRunner\SnippetRunner.csproj -c Release -- @($s.Args) 2>&1 |
                Where-Object { $_ -notmatch "warning|NU1701|Restored|Determining|reachable on all platforms" -and $_ -ne "" }
            $exit = $LASTEXITCODE
            $text = ($out | Out-String)

            $ok = ($exit -eq 0) -and ($text -notmatch "ERROR:|Usage:|not found|No sessions")
            if ($ok -and $s.Expect) { $ok = $text -match [regex]::Escape($s.Expect) }

            $status = if ($ok) { "PASS" } else { "FAIL" }
            Write-Host "[$status] exit=$exit" -ForegroundColor $(if ($ok) { "Green" } else { "Red" })
            $out | Select-Object -First 4 | ForEach-Object { Write-Host "  $_" }
            $results += [pscustomobject]@{ Snippet = $s.Name; Status = $status }
        }
    }
    finally {
        Set-Content $runnerProgram -Value $backup -NoNewline
    }

    Write-Host "`n=== Summary ===" -ForegroundColor Yellow
    $results | Format-Table -AutoSize | Out-String | Write-Host
    $failed = @($results | Where-Object { $_.Status -ne "PASS" }).Count
    if ($failed -gt 0) { Write-Host "$failed snippet(s) failed." -ForegroundColor Red; exit 1 }
    Write-Host "All snippets passed against the verified fixture." -ForegroundColor Green
}
finally {
    Pop-Location
}
