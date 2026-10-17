#!/usr/bin/env bash
# The scene check (tools/scenecheck, the game's own reader) against the narrator's recordings, so each line is timed
# against its clip: what the build and scene-files.yml both run before scenes go on the site. Writes the checked
# scenes, the recordings and their list to <out>. Needs .NET 8 and GH_TOKEN that can read the vendor repository.
# usage: tools/check_scenes.sh <out>
set -euo pipefail
out=${1:?output folder}
narration=${RUNNER_TEMP:-/tmp}/narration
"$(dirname "$0")"/vendor_fetch.sh narration "$narration" "narrator lines are checked for reading time only" >&2 # the check's own output is its report
dotnet run --project "$(dirname "$0")"/scenecheck -- scenes "$narration" "$out"
