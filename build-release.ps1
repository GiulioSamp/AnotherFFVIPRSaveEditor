# Build a self-contained single-file release of the FFVI Pixel Remaster Save Editor.
# Output: publish\Ffvi.SaveTool\Ffvi.SaveTool.Gui.exe (~50 MB, no .NET install required)
# Plus a timestamped zip alongside.
#
# Usage: from the project root, run:
#   .\build-release.ps1
#
# If PowerShell complains about execution policy, run once:
#   Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$out  = Join-Path $root "publish\Ffvi.SaveTool"
$proj = Join-Path $root "src\Ffvi.SaveTool.Gui\Ffvi.SaveTool.Gui.csproj"

Write-Host "Publishing $proj" -ForegroundColor Cyan

if (Test-Path $out) { Remove-Item -Recurse -Force $out }

dotnet publish $proj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=embedded `
    -o $out

if ($LASTEXITCODE -ne 0) {
    Write-Host "dotnet publish failed (exit $LASTEXITCODE)" -ForegroundColor Red
    exit $LASTEXITCODE
}

Copy-Item (Join-Path $root "README.md") $out
Copy-Item (Join-Path $root "LICENSE")   $out

$stamp   = Get-Date -Format "yyyyMMdd"
$zipPath = Join-Path $root "publish\Ffvi.SaveTool-$stamp.zip"

if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
Compress-Archive -Path $out -DestinationPath $zipPath

$exe = Join-Path $out "Ffvi.SaveTool.Gui.exe"
$exeMB = [Math]::Round((Get-Item $exe).Length / 1MB, 1)
$zipMB = [Math]::Round((Get-Item $zipPath).Length / 1MB, 1)

Write-Host ""
Write-Host "Build complete:" -ForegroundColor Green
Write-Host "  exe: $exe ($exeMB MB)"
Write-Host "  zip: $zipPath ($zipMB MB)"
