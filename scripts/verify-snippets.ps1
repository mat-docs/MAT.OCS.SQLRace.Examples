param([string]$Guid = "c06dea71-8673-434d-a930-25e4ee89e159")

$snippets = @(
    @{ Name = "01-load-session-from-file"; Path = "snippets\csharp\getting-started\01-load-session-from-file.cs"; Args = @() },
    @{ Name = "03-read-parameter-samples"; Path = "snippets\csharp\getting-started\03-read-parameter-samples.cs"; Args = @($Guid) },
    @{ Name = "read-samples-between"; Path = "snippets\csharp\data-access\read-samples-between.cs"; Args = @($Guid) },
    @{ Name = "bulk-parameter-read"; Path = "snippets\csharp\data-access\bulk-parameter-read.cs"; Args = @($Guid) },
    @{ Name = "multi-rate-alignment"; Path = "snippets\csharp\data-access\multi-rate-alignment.cs"; Args = @($Guid) },
    @{ Name = "reverse-iteration"; Path = "snippets\csharp\data-access\reverse-iteration.cs"; Args = @($Guid) },
    @{ Name = "parameter-unit-resolution"; Path = "snippets\csharp\session-management\parameter-unit-resolution.cs"; Args = @($Guid) },
    @{ Name = "session-metadata-crud"; Path = "snippets\csharp\session-management\session-metadata-crud.cs"; Args = @() }
)

$results = @()
foreach ($s in $snippets) {
    Write-Host ""
    Write-Host "===== $($s.Name) =====" -ForegroundColor Cyan
    Copy-Item $s.Path tools\SnippetRunner\Program.cs -Force
    (Get-Item tools\SnippetRunner\Program.cs).LastWriteTime = Get-Date
    $out = dotnet run --project tools\SnippetRunner\SnippetRunner.csproj -- @($s.Args) 2>&1 | Where-Object { $_ -notmatch "warning|NU1701|Restored|Determining" -and $_ -ne "" }
    $exitCode = $LASTEXITCODE
    $status = if ($exitCode -eq 0) { "PASS" } else { "FAIL($exitCode)" }
    Write-Host "[$status]" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })
    $out | Select-Object -First 3 | ForEach-Object { Write-Host "  $_" }
    if (($out | Measure-Object).Count -gt 6) { Write-Host "  ..." }
    $out | Select-Object -Last 3 | ForEach-Object { Write-Host "  $_" }
    $results += [pscustomobject]@{ Name = $s.Name; Status = $status }
}
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Yellow
$results | Format-Table -AutoSize
