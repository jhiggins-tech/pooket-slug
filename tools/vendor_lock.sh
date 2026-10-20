#!/usr/bin/env bash
# Pins a vendor sound in vendor.lock: works out the name and SHA-256 of what the vendor repository's <release> release
# holds now (its <release>.zip, or one WAV from it) and writes that line, so builds fetch exactly that file. The file
# itself is copied to the vendor repository's `pinned` release by its Pin workflow (its pin/pins.txt lists what to
# pin; a push there runs it): pin there first, then run this on the game item's branch. A new take reaches the game
# only this way, when that branch is approved. Needs gh that can read the vendor repository.
#   tools/vendor_lock.sh <release>             the whole zip (announcer, narration, orchestra)
#   tools/vendor_lock.sh <release> <file.wav>  one WAV from the zip (start-music title.wav, den-music den.wav)
set -euo pipefail
cd "$(dirname "$0")/.."
VENDOR=jhiggins-tech/pooket-slug-vendor
release=${1:?release}; member=${2:-}
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT

asset() { # <tag> <name>: the asset's id, or nothing
  gh api "repos/$VENDOR/releases/tags/$1" --jq ".assets[] | select(.name == \"$2\") | .id" 2>/dev/null || true
}

id=$(asset "$release" "$release.zip")
[ -n "$id" ] || { echo "No $release.zip on the vendor repository's $release release." >&2; exit 1; }
gh api -H 'Accept: application/octet-stream' "repos/$VENDOR/releases/assets/$id" > "$tmp/$release.zip"
if [ -n "$member" ]; then
  unzip -q -j "$tmp/$release.zip" "$member" -d "$tmp/out"
  file=$tmp/out/$member; base=${member%.*}; ext=${member##*.}
else
  file=$tmp/$release.zip; base=$release; ext=zip
fi
sum=$(sha256sum "$file" | cut -d' ' -f1)
name=$base-${sum:0:12}.$ext

line="$release $name $sum"
if grep -q "^$release " vendor.lock; then
  sed -i "s|^$release .*|$line|" vendor.lock
else
  echo "$line" >> vendor.lock
fi
[ -n "$(asset pinned "$name")" ] && echo "Pinned $release: $name" ||
  echo "Locked $release to $name, which isn't on the vendor repository's pinned release yet: run its Pin workflow." >&2
