$tests = @(
    @{ Name = "snippet 03 - invalid GUID"; Path = "snippets\csharp\getting-started\03-read-parameter-samples.cs"; Args = @("not-a-guid") },
    @{ Name = "snippet 03 - missing session"; Path = "snippets\csharp\getting-started\03-read-parameter-samples.cs"; Args = @("00000000-0000-0000-0000-000000000099") },
    @{ Name = "read-samples-between - missing session"; Path = "snippets\csharp\data-access\read-samples-between.cs"; Args = @("00000000-0000-0000-0000-000000000099") },
    @{ Name = "bulk-parameter-read - missing session"; Path = "snippets\csharp\data-access\bulk-parameter-read.cs"; Args = @("00000000-0000-0000-0000-000000000099") },
    @{ Name = "reverse-iteration - missing session"; Path = "snippets\csharp\data-access\reverse-iteration.cs"; Args = @("00000000-0000-0000-0000-000000000099") },
    @{ Name = "reverse-iteration - missing param"; Path = "snippets\csharp\data-access\reverse-iteration.cs"; Args = @("c06dea71-8673-434d-a930-25e4ee89e159", "Nope:NotReal") },
    @{ Name = "parameter-unit-resolution - missing session"; Path = "snippets\csharp\session-management\parameter-unit-resolution.cs"; Args = @("00000000-0000-0000-0000-000000000099") }
)

foreach ($t in $tests) {
    Write-Host ""
    Write-Host "===== $($t.Name) =====" -ForegroundColor Cyan
    Copy-Item $t.Path tools\SnippetRunner\Program.cs -Force
    (Get-Item tools\SnippetRunner\Program.cs).LastWriteTime = Get-Date
    $out = dotnet run --project tools\SnippetRunner\SnippetRunner.csproj -- @($t.Args) 2>&1 | Where-Object { $_ -notmatch "warning|NU1701|Restored|Determining" -and "$_".Trim() -ne "" }
    $out | ForEach-Object { Write-Host "  $_" }
    if ($out -match "Unhandled exception|System\.|^\s*at ") {
        Write-Host "  [LEAKY EXCEPTION]" -ForegroundColor Red
    }
}
