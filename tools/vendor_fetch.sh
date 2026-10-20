#!/usr/bin/env bash
# Fetches a vendor sound the way vendor.lock pins it: the named file from the vendor repository's `pinned` release,
# checked against its SHA-256, then its WAVs (all of a zip's but sample.wav, or the one WAV) into <dir>, as .bytes with
# --bytes (so Unity keeps them as raw WAV bytes: the game makes the clips). A file that doesn't match its hash is always
# an error. One that can't be fetched is an error with VENDOR_STRICT=true (the main line: the live game never ships
# without its sounds); anywhere else it warns with <what's missed> and the game builds without them.
# Needs GH_TOKEN that can read the vendor repository. To take a new take, pin it: tools/vendor_lock.sh.
# usage: tools/vendor_fetch.sh [--bytes] <release> <dir> <what's missed>
set -euo pipefail
bytes=false; [ "${1:-}" != --bytes ] || { bytes=true; shift; }
release=${1:?release}; dir=${2:?dir}; missed=${3:?what is missed without it}
lock=$(dirname "$0")/../vendor.lock
read -r name sum < <(awk -v r="$release" '$1 == r { print $2, $3 }' "$lock") ||
  { echo "::error::$release isn't in vendor.lock: pin it with tools/vendor_lock.sh $release."; exit 1; }
file=${RUNNER_TEMP:-/tmp}/vendor/$name
mkdir -p "$dir" "$(dirname "$file")"
download() { # through the REST API, as gh's own release download needs GraphQL, which cloud sessions can't reach
  local api=repos/jhiggins-tech/pooket-slug-vendor/releases id
  id=$(gh api "$api/tags/pinned" --jq ".assets[] | select(.name == \"$name\") | .id") && [ -n "$id" ] &&
    gh api -H 'Accept: application/octet-stream' "$api/assets/$id" > "$file.part" && mv "$file.part" "$file"
}
if [ ! -f "$file" ] && ! download; then
  if [ "${VENDOR_STRICT:-false}" = true ]; then
    echo "::error::Couldn't fetch $name ($release) from the vendor repository's pinned release, so $missed. The main line never builds without it: check VENDOR_PAT and the release."
    exit 1
  fi
  echo "::warning::Couldn't fetch $name ($release) from the vendor repository's pinned release: $missed."
  exit 0
fi
echo "$sum  $file" | sha256sum -c --quiet - ||
  { echo "::error::$name doesn't match its hash in vendor.lock."; rm -f "$file"; exit 1; }
case $name in
  *.zip) unzip -o -q "$file" '*.wav' -x sample.wav -d "$dir" ;;
  *.wav) cp "$file" "$dir/${name%-*}.wav" ;;
esac
if $bytes; then for f in "$dir"/*.wav; do mv "$f" "${f%.wav}.bytes"; done; fi
ls -l "$dir"
