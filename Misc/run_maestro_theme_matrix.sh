#!/usr/bin/env bash
#
# Run the Maestro capture flow in both device appearances.
#
# WHY THIS SCRIPT EXISTS
# ----------------------
# The "Follow system" theme only switches when the *device* appearance switches, so a
# flow that exercises it needs a way to change the device appearance. Two independent
# mechanisms are available, and this script provides the external one:
#
#   1. In-flow Maestro commands `setDarkMode: enabled|disabled`, `assertDarkMode` and
#      `assertLightMode`. These are real, current Maestro commands - they are listed in
#      the official command index (https://docs.maestro.dev/llms.txt) and are parsed by
#      `YamlFluentCommand` in Maestro's own source
#      (maestro-orchestra/.../yaml/YamlFluentCommand.kt, `val setDarkMode`,
#      `val assertDarkMode`, `val assertLightMode`). MaestroTests/follow_system_theme.yaml
#      uses them; MaestroTests/schema/maestro_commands.json is the checked-in copy of that
#      vocabulary and `Misc/test_ui_contracts.py` validates every flow against it.
#
#   2. The external Android precondition below, `adb shell cmd uimode night yes|no`.
#      This needs no Maestro feature at all, so it also works with older Maestro builds
#      and it keeps the appearance out of the flow. Use it when the installed Maestro
#      does not have the dark-mode commands, or when the appearance must be set once for
#      a whole matrix of flows.
#
# Both produce the same device state. This script uses (2) and then runs the normal
# flows, so the theme matrix never depends on an in-flow command being available.
#
# USAGE
#   Misc/run_maestro_theme_matrix.sh [-s SERIAL] [-f FLOW] [-o OUTDIR]
#
#   -s SERIAL   device serial, forwarded to both adb (-s) and Maestro (--device)
#               (default: the only attached device)
#   -f FLOW     flow to run in each appearance (default: MaestroTests/screenshots_core.yaml)
#   -o OUTDIR   Maestro output/screenshot directory (default: maestro-theme-matrix)
#
# Requirements: adb on PATH (Android platform-tools), `maestro` on PATH.
# This script is not executed in CI here (no device on this host); it is the documented,
# reproducible way to produce the light/dark screenshots on a machine that has one.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SERIAL=""
FLOW="$REPO_ROOT/MaestroTests/screenshots_core.yaml"
OUTDIR="maestro-theme-matrix"

while getopts "s:f:o:h" opt; do
    case "$opt" in
        s) SERIAL="$OPTARG" ;;
        f) FLOW="$OPTARG" ;;
        o) OUTDIR="$OPTARG" ;;
        h) sed -n '1,60p' "${BASH_SOURCE[0]}"; exit 0 ;;
        *) echo "unknown option" >&2; exit 2 ;;
    esac
done

ADB=(adb)
MAESTRO=(maestro test)
if [[ -n "$SERIAL" ]]; then
    ADB=(adb -s "$SERIAL")
    MAESTRO=(maestro test --device "$SERIAL")
fi

command -v adb >/dev/null 2>&1 || { echo "adb not found on PATH" >&2; exit 1; }
command -v maestro >/dev/null 2>&1 || { echo "maestro CLI not found on PATH" >&2; exit 1; }
[[ -f "$FLOW" ]] || { echo "flow not found: $FLOW" >&2; exit 1; }

if ! "${ADB[@]}" get-state >/dev/null 2>&1; then
    echo "no device/emulator reachable through adb" >&2
    exit 1
fi

read_night_mode() {
    # `cmd uimode night` prints e.g. "Night mode: yes" / "Night mode: no" / "Night mode: auto"
    "${ADB[@]}" shell cmd uimode night | tr -d '\r' | awk -F': *' '{print tolower($2)}'
}

set_night_mode() {
    local mode="$1"      # yes | no | auto
    "${ADB[@]}" shell cmd uimode night "$mode" >/dev/null
    local observed
    observed="$(read_night_mode)"
    if [[ "$mode" != "auto" && "$observed" != "$mode" ]]; then
        echo "failed to set night mode to '$mode' (device reports '$observed')" >&2
        exit 1
    fi
    echo "device night mode: $observed"
}

ORIGINAL_NIGHT_MODE="$(read_night_mode || true)"
restore() {
    if [[ -n "${ORIGINAL_NIGHT_MODE:-}" ]]; then
        set_night_mode "$ORIGINAL_NIGHT_MODE" || true
    fi
}
trap restore EXIT

mkdir -p "$OUTDIR"

# Dark first, then light, so a failing first pass does not leave the device light and a
# half-written light run behind.
set_night_mode yes
echo "== dark appearance: $FLOW"
"${MAESTRO[@]}" --test-output-dir "$OUTDIR/dark" \
    -e IS_NIGHT=true -e PREFIX=1 "$FLOW"

set_night_mode no
echo "== light appearance: $FLOW"
"${MAESTRO[@]}" --test-output-dir "$OUTDIR/light" \
    -e IS_NIGHT=false -e PREFIX=0 "$FLOW"

echo "screenshots and reports: $OUTDIR"
