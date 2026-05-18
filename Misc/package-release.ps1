# Builds universal + per-ABI IzzySoft APKs and stages them in .\release-apks\
# for upload to the GitHub Release.
#
# Usage (from repo root):
#   .\Misc\package-release.ps1
#   .\Misc\package-release.ps1 -Version 120

[CmdletBinding()]
param(
    [string]$Version
)

$ErrorActionPreference = "Stop"

if (-not $Version) {
    $Version = Read-Host "Seeker version (e.g. 120)"
}
if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Version is required."
}

$pkg     = "com.companyname.andriodapp1"
$csproj  = ".\Seeker\Seeker.csproj"
$config  = "Release IzzySoft"
$base    = ".\Seeker\bin\$config\net9.0-android"
$out     = ".\release-apks"

$rids = @("android-arm64", "android-arm", "android-x64", "android-x86")
$abiMap = @{
    "android-arm64" = "arm64-v8a"
    "android-arm"   = "armeabi-v7a"
    "android-x64"   = "x86_64"
    "android-x86"   = "x86"
}

New-Item -ItemType Directory -Force $out | Out-Null

Write-Host "==> Publishing universal APK" -ForegroundColor Cyan
dotnet publish -c $config -f net9.0-android $csproj
if ($LASTEXITCODE -ne 0) { throw "Universal publish failed." }

foreach ($rid in $rids) {
    Write-Host "==> Publishing $rid" -ForegroundColor Cyan
    # Wipe any prior per-RID output so a previous (broken) all-ABI build
    # doesn't leave stale native libs behind.
    $ridDir = Join-Path $base $rid
    if (Test-Path $ridDir) {
        Remove-Item -Recurse -Force $ridDir
    }
    # -p:RuntimeIdentifiers= clears the plural value set in Seeker.csproj so
    # -r actually narrows the build to a single ABI; otherwise every per-ABI
    # APK ends up containing native libs for all four ABIs.
    dotnet publish -c $config -f net9.0-android -r $rid -p:RuntimeIdentifiers= $csproj
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for $rid." }
}

Write-Host "==> Collecting APKs into $out" -ForegroundColor Cyan

$uni = Join-Path $base "$pkg-Signed.apk"
if (Test-Path $uni) {
    Copy-Item $uni (Join-Path $out "seeker$Version-universal.apk") -Force
} else {
    Write-Warning "Universal APK not found at $uni"
}

foreach ($rid in $rids) {
    $src = Join-Path $base "$rid\publish\$pkg-Signed.apk"
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $out "seeker$Version-$($abiMap[$rid]).apk") -Force
    } else {
        Write-Warning "Per-ABI APK not found for $rid at $src"
    }
}

Write-Host ""
Get-ChildItem $out | Select-Object Name, @{n="MB";e={[math]::Round($_.Length/1MB,1)}} | Format-Table -AutoSize
