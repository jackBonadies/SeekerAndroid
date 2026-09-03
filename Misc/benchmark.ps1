<#
.SYNOPSIS
    Startup benchmark: cold-starts MainActivity N times and reports TotalTime statistics.

.DESCRIPTION
    Emits one line per run to the host and returns a summary object, so a driver
    script can collect several arms and table them.

.EXAMPLE
    .\Misc\benchmark.ps1 -Label aot -Runs 20
#>
[CmdletBinding()]
param(
    [string] $Label  = 'run',
    [int]    $Runs   = 10,
    [string] $Device = $(if ($env:DEV) { $env:DEV } else { 'R5CW7238Q0D' })
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pkg = 'com.companyname.andriodapp1'
$act = "$pkg/crc646350f44cde9e1cf7.MainActivity"

# Animation scales inflate TotalTime. Pin them off.
foreach ($setting in 'window_animation_scale', 'transition_animation_scale', 'animator_duration_scale') {
    & adb -s $Device shell settings put global $setting 0 | Out-Null
}

$times = New-Object System.Collections.Generic.List[int]
foreach ($i in 0..$Runs) {
    $out = & adb -s $Device shell am start-activity -W -S -n $act
    if ($i -eq 0) { continue }          # discard the first: dexopt / first-run warmup
    $match = $out | Select-String -Pattern '^TotalTime:\s*(\d+)'
    if ($match) {
        $t = [int]$match.Matches[0].Groups[1].Value
        $times.Add($t)
        Write-Host ('  {0,2}  {1,5} ms' -f $i, $t)
    }
    Start-Sleep -Seconds 2
}
& adb -s $Device shell am force-stop $pkg | Out-Null

if ($times.Count -eq 0) {
    throw "No TotalTime samples captured. Is $act still the right activity? (the crc64 prefix changes if the namespace does)"
}

# Turn animations back on (no side effects)
foreach ($setting in 'window_animation_scale', 'transition_animation_scale', 'animator_duration_scale') {
    & adb -s $Device shell settings put global $setting 1 | Out-Null
}

$sorted = @($times | Sort-Object)
$n      = $sorted.Count
if ($n % 2) { $median = $sorted[($n - 1) / 2] } else { $median = ($sorted[$n / 2 - 1] + $sorted[$n / 2]) / 2 }
$stat   = $times | Measure-Object -Average -Minimum -Maximum

[pscustomobject]@{
    Label  = $Label
    N      = $n
    Median = [int]$median
    Mean   = [int][math]::Round($stat.Average)
    Min    = [int]$stat.Minimum
    Max    = [int]$stat.Maximum
}
