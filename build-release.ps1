#requires -Version 5.1
$ErrorActionPreference = 'Stop'

# --- Config ---
$Version    = '1.0.0'
$Rid        = 'win-x64'      # change to 'win-arm64' for ARM builds
$Config     = 'Release'
$AppProject = 'src/PasswordKeeper.App/PasswordKeeper.App.csproj'

# --- Paths ---
$root       = $PSScriptRoot
$publishDir = Join-Path $root "src/PasswordKeeper.App/bin/$Config/net9.0-windows/$Rid/publish"
$outDir     = Join-Path $root 'dist'
$zipName    = "PasswordKeeper-$Version-$Rid.zip"
$zipPath    = Join-Path $outDir $zipName

Write-Host "==> Cleaning previous publish output"
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
if (Test-Path $zipPath)   { Remove-Item -Force $zipPath }

Write-Host "==> Publishing $AppProject ($Rid, self-contained, single-file)"
& dotnet publish $AppProject `
    -c $Config `
    -r $Rid `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=embedded `
    -p:Version=$Version `
    --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

Write-Host "==> Creating zip $zipPath"
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force

$exe = Get-ChildItem -Path $publishDir -Filter 'PasswordKeeper.App.exe' | Select-Object -First 1
$zip = Get-Item $zipPath
Write-Host ""
Write-Host "Done."
Write-Host ("  exe : {0}  ({1:N1} MB)" -f $exe.FullName, ($exe.Length / 1MB))
Write-Host ("  zip : {0}  ({1:N1} MB)" -f $zip.FullName, ($zip.Length / 1MB))
