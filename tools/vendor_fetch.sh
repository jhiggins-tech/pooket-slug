#!/usr/bin/env bash
# Unzips <release>.zip's WAVs from the vendor repository's <release> release into <dir>, as .bytes with --bytes (so
# Unity keeps them as raw WAV bytes: the game makes the clips), leaving out sample.wav. Without the zip it warns with
# <what's missed> and carries on: the game builds without these sounds. Needs GH_TOKEN that can read the vendor repo.
# usage: tools/vendor_fetch.sh [--bytes] <release> <dir> <what's missed>
set -euo pipefail
bytes=false; [ "${1:-}" != --bytes ] || { bytes=true; shift; }
release=${1:?release}; dir=${2:?dir}; missed=${3:?what is missed without it}
zip=${RUNNER_TEMP:-/tmp}/vendor/$release.zip
mkdir -p "$dir" "$(dirname "$zip")"
if ! gh release download "$release" -R jhiggins-tech/pooket-slug-vendor -p "$release.zip" -O "$zip" --clobber; then
  echo "::warning::No $release.zip on the vendor repository's $release release: $missed."
  exit 0
fi
unzip -o -q "$zip" '*.wav' -x sample.wav -d "$dir"
if $bytes; then for f in "$dir"/*.wav; do mv "$f" "${f%.wav}.bytes"; done; fi
ls -l "$dir"
