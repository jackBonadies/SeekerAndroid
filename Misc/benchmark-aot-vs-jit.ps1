<#
.SYNOPSIS
    Statup benchmark: profiled AOT vs JIT.

.DESCRIPTION
    Per arm: full clean, build + install, then benchmark.ps1.

.EXAMPLE
    .\Misc\benchmark-aot-vs-jit.ps1 -Runs 20
#>
[CmdletBinding()]
param(
    [int]    $Runs   = 10,
    [string] $Device = $(if ($env:DEV) { $env:DEV } else { 'R5CW7238Q0D' })
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root   = Split-Path $PSScriptRoot -Parent
$csproj = Join-Path $root 'Seeker\Seeker.csproj'
$tfm    = 'net10.0-android36.0'

$state = & adb -s $Device get-state
if ($LASTEXITCODE -ne 0 -or "$state".Trim() -ne 'device') { throw "Device $Device is not connected." }
if (-not (Test-Path (Join-Path $root 'Seeker\custom.aprof'))) {
    throw 'Seeker\custom.aprof is missing - the AOT arm is profiled AOT and needs it. Regenerate with FinishAotProfiling.'
}

function Invoke-Arm {
    param(
        [Parameter(Mandatory)] [string] $Label,
        [Parameter(Mandatory)] [string] $Aot,
        [Parameter(Mandatory)] [string] $Profiled
    )

    Write-Host ''
    Write-Host ('=' * 62)
    Write-Host " $Label  (RunAOTCompilation=$Aot, AndroidEnableProfiledAot=$Profiled)"
    Write-Host ('=' * 62)

    foreach ($dir in "$root\Seeker\obj\Release\$tfm", "$root\Seeker\bin\Release\$tfm") {
        if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
    }

    $buildArgs = @(
        'build', $csproj,
        '-c', 'Release',
        '-t:Install',
        "-p:Device=$Device",
        "-p:RunAOTCompilation=$Aot",
        "-p:AndroidEnableProfiledAot=$Profiled",
        '-v:m', '-nologo'
    )

    $sw = [Diagnostics.Stopwatch]::StartNew()
    & dotnet @buildArgs | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$Label build failed with exit code $LASTEXITCODE." }
    Write-Host ('build+install: {0:n0}s' -f $sw.Elapsed.TotalSeconds)

    & (Join-Path $PSScriptRoot 'benchmark.ps1') -Label $Label -Runs $Runs -Device $Device
}

$results = @(
    Invoke-Arm -Label aot -Aot true  -Profiled true
    Invoke-Arm -Label jit -Aot false -Profiled false
)

Write-Host ''
Write-Host ('=' * 62)
Write-Host " summary  ($Runs runs each, device $Device)"
Write-Host ('=' * 62)
$results | Format-Table -AutoSize
