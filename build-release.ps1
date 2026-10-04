# Build self-contained single-file releases of the FFVI Pixel Remaster Save Editor
# for Windows (win-x64) and Linux (linux-x64, e.g. Steam Deck).
# Output per RID: publish\Ffvi.SaveTool-<rid>\ plus publish\Ffvi.SaveTool-<rid>-YYYYMMDD.zip
# No .NET install required to run the result.
#
# Usage: from the project root, run:
#   .\build-release.ps1
#
# If PowerShell complains about execution policy, run once:
#   Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned

$ErrorActionPreference = "Stop"
$root  = $PSScriptRoot
$proj  = Join-Path $root "src\Ffvi.SaveTool.App\Ffvi.SaveTool.App.csproj"
$stamp = Get-Date -Format "yyyyMMdd"

foreach ($rid in "win-x64", "linux-x64") {
    $out = Join-Path $root "publish\Ffvi.SaveTool-$rid"
    Write-Host "Publishing $proj ($rid)" -ForegroundColor Cyan

    if (Test-Path $out) { Remove-Item -Recurse -Force $out }

    dotnet publish $proj `
        -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=embedded `
        -o $out

    if ($LASTEXITCODE -ne 0) {
        Write-Host "dotnet publish failed for $rid (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Remove-Item "$out\*.pdb"  # native lib symbols (~100 MB), not needed by users
    Copy-Item (Join-Path $root "README.md") $out
    Copy-Item (Join-Path $root "LICENSE")   $out

    $zipPath = Join-Path $root "publish\Ffvi.SaveTool-$rid-$stamp.zip"
    if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
    Compress-Archive -Path $out -DestinationPath $zipPath

    $folderMB = [Math]::Round((Get-ChildItem $out -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
    $zipMB    = [Math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host ""
    Write-Host "Build complete ($rid):" -ForegroundColor Green
    Write-Host "  dir: $out ($folderMB MB)"
    Write-Host "  zip: $zipPath ($zipMB MB)"
}
