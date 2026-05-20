# Builds universal + per-ABI IzzySoft APKs and stages them in .\release-apks\
# for upload to the GitHub Release.
#
# Usage (from repo root):
#   .\Misc\package-release.ps1
#   .\Misc\package-release.ps1 -Version 120

[CmdletBinding()]
param(
    [string]$Version,
    [string]$KeystorePath,
    [string]$KeyAlias,
    [string]$KeyPass,
    [string]$StorePass,
    [switch]$NoGitMetadata
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

# --- Signing overrides (param > env var > csproj default) -----------------
if (-not $KeystorePath) { $KeystorePath = $env:ANDROID_KEYSTORE_PATH }
if (-not $KeyAlias)     { $KeyAlias     = $env:ANDROID_KEY_ALIAS }
if (-not $KeyPass)      { $KeyPass      = $env:ANDROID_KEY_PASSWORD }
if (-not $StorePass)    { $StorePass    = $env:ANDROID_KEYSTORE_PASSWORD }

# --- Git-derived metadata for deterministic builds ------------------------
$gitSha          = ""
$sourceDateEpoch = ""
if (-not $NoGitMetadata) {
    try {
        $gitSha = (& git rev-parse HEAD 2>$null).Trim()
        $sourceDateEpoch = (& git log -1 --pretty=%ct 2>$null).Trim()
    } catch {
        Write-Warning "git metadata unavailable — proceeding without SourceRevisionId / SOURCE_DATE_EPOCH"
    }
}
if ($sourceDateEpoch) {
    $env:SOURCE_DATE_EPOCH = $sourceDateEpoch
    Write-Host "SOURCE_DATE_EPOCH=$sourceDateEpoch (HEAD commit time)"
}
if ($gitSha) {
    Write-Host "SourceRevisionId=$gitSha"
}

# --- Build the shared MSBuild argument list -------------------------------
$commonArgs = @(
    "-p:ContinuousIntegrationBuild=true",
    "-p:Deterministic=true"
)
if ($gitSha) {
    $commonArgs += "-p:SourceRevisionId=$gitSha"
}
if ($KeystorePath) { $commonArgs += "-p:AndroidSigningKeyStore=$KeystorePath" }
if ($KeyAlias)     { $commonArgs += "-p:AndroidSigningKeyAlias=$KeyAlias" }
if ($KeyPass)      { $commonArgs += "-p:AndroidSigningKeyPass=$KeyPass" }
if ($StorePass)    { $commonArgs += "-p:AndroidSigningStorePass=$StorePass" }

New-Item -ItemType Directory -Force $out | Out-Null

Write-Host "==> Publishing universal APK" -ForegroundColor Cyan
dotnet publish -c $config -f net9.0-android @commonArgs $csproj
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
    dotnet publish -c $config -f net9.0-android -r $rid -p:RuntimeIdentifiers= @commonArgs $csproj
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
