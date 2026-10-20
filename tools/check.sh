#!/usr/bin/env bash
# The checks CI runs before a build, in one command and without Unity: compiles every assembly as Unity would for
# WebGL (Core, its tests, Net and the game), runs Fusion's IL weaver over the netcode, and runs the EditMode tests.
# The first run downloads the Unity kit for this project's Unity version (the vendor repository's Unity kit
# workflow makes it) and the Photon SDK, both from the private vendor repository's releases, through `gh` (or curl
# with GH_TOKEN); later runs reuse them.
#
#   tools/check.sh                 everything
#   tools/check.sh compile         only the type-check
#   KIT=<dir> tools/check.sh       use an unpacked kit instead of the cached one
#
# Exits non-zero at the first failure; the test run's exit code is the number of failed tests.
set -euo pipefail
cd "$(dirname "$0")/.."
T=docs/research/verification/tools
VENDOR=jhiggins-tech/pooket-slug-vendor
what=${1:-all}
case $what in all|compile) ;; *) echo "usage: $0 [all|compile]" >&2; exit 2 ;; esac

version=$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)
cache=${XDG_CACHE_HOME:-$HOME/.cache}/pooket-slug
W=${W:-$cache/check}

# One release asset from the vendor repository: gh where it's signed in (cloud sessions are), else curl + GH_TOKEN.
fetch() { # <tag> <file> <dest>
  local api=repos/$VENDOR/releases/tags/$1 id
  if command -v gh >/dev/null && id=$(gh api "$api" --jq ".assets[] | select(.name == \"$2\") | .id" 2>/dev/null) && [ -n "$id" ]; then
    gh api -H 'Accept: application/octet-stream' "repos/$VENDOR/releases/assets/$id" > "$3"
  else
    : "${GH_TOKEN:?gh could not reach $VENDOR: sign gh in, or set GH_TOKEN to a token that can read it}"
    id=$(curl -fsSL -H "Authorization: Bearer $GH_TOKEN" "https://api.github.com/$api" |
      python3 -c 'import json,sys; print(next(a["id"] for a in json.load(sys.stdin)["assets"] if a["name"] == sys.argv[1]))' "$2")
    curl -fsSL -H "Authorization: Bearer $GH_TOKEN" -H 'Accept: application/octet-stream' \
      "https://api.github.com/repos/$VENDOR/releases/assets/$id" -o "$3"
  fi
}

# The Unity kit for this Unity version.
K=${KIT:-$cache/unity-kit-$version}
if [ ! -f "$K/VERSION" ]; then
  echo "Downloading the Unity kit for $version (once)..."
  mkdir -p "$cache"; rm -rf "$K.part"; mkdir -p "$K.part"
  fetch "unity-kit-$version" "unity-kit-$version.tar.gz" "$cache/kit.tar.gz" || {
    echo "::error::No Unity kit for $version: run the vendor repository's Unity kit workflow with version $version." >&2
    exit 1; }
  tar -xzf "$cache/kit.tar.gz" -C "$K.part" && rm "$cache/kit.tar.gz" && mv "$K.part" "$K"
fi
D=$K/Data

# The Photon SDK, as CI unpacks it (the tag, file and checksum come from CI's unity-project action, so they never disagree).
if ! find Assets/Photon -name '*.dll' -print -quit 2>/dev/null | grep -q .; then
  wf=.github/actions/unity-project/action.yml
  get() { sed -n "s/^ *$1: //p" "$wf" | head -1; }
  tag=$(get FUSION_TAG); file=$(get FUSION_FILE); sum=$(get FUSION_SHA256)
  echo "Downloading the Photon SDK ($file, once)..."
  mkdir -p "$cache"
  [ -f "$cache/$file" ] || fetch "$tag" "$file" "$cache/$file"
  echo "$sum  $cache/$file" | sha256sum -c --quiet -
  python3 -I tools/unpack_unitypackage.py "$cache/$file" .
  git checkout -- Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion # the committed config wins
fi

rm -rf "$W"; mkdir -p "$W"
echo "== Compile (Core, its tests, Net and the game)"
python3 -I $T/compile_unity.py --editor-data "$D" --project . --out "$W/out" \
  --cecil-dir "$K/package" --inputsystem "$K/is/Unity.InputSystem.dll" --ugui "$K/ugui/UnityEngine.UI.dll" \
  --nunit "$K/ext-nunit/package/net40/unity-custom/nunit.framework.dll" all
[ "$what" = compile ] && exit 0
echo "== Fusion's weaver"
$T/weave.sh "$D" "$PWD" "$W/out" "$K/package" "$W/weave"
echo "== EditMode tests"
python3 -I $T/run_tests.py --editor-data "$D" --project . --core "$W/out/game/PooketSlug.Core.dll" \
  --nunit "$K/nunit/lib/netstandard2.0" --nunitlite "$K/nunitlite/lib/netstandard2.0" --out "$W/tests"
echo "All checks passed."
