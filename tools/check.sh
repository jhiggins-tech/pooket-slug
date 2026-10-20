#!/usr/bin/env bash
# The checks CI runs before a build, in one command and without Unity: compiles every assembly as Unity would for
# WebGL (Core, its tests, Net and the game), runs Fusion's IL weaver over the netcode, runs the EditMode tests, then
# the scene check (tools/scenecheck, with the narrator's recordings when they can be had) and the docs link check
# (tools/doclinks.py). CI runs the scene check too; the docs link check is only here.
# The first run downloads the Unity kit for this project's Unity version (the vendor repository's Unity kit
# workflow makes it) and the Photon SDK, both from the private vendor repository's releases, through `gh` (or curl
# with GH_TOKEN); later runs reuse them.
#
#   tools/check.sh                 everything
#   tools/check.sh compile         only the type-check
#   tools/check.sh docs            only the docs link check (no kit needed)
#   KIT=<dir> tools/check.sh       use an unpacked kit instead of the cached one
#
# Exits non-zero at the first failure; the test run's exit code is the number of failed tests.
set -euo pipefail
cd "$(dirname "$0")/.."
T=docs/research/verification/tools
VENDOR=jhiggins-tech/pooket-slug-vendor
what=${1:-all}
case $what in all|compile) ;; docs) exec python3 -I tools/doclinks.py ;; *) echo "usage: $0 [all|compile|docs]" >&2; exit 2 ;; esac

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
echo "== The scene check"
# tools/scenecheck as CI builds it with dotnet, here built and run on the kit's own .NET (as run_tests.py does).
S=$W/scenecheck; mkdir -p "$S"
RT=$(ls -d "$D"/NetCoreRuntime/shared/Microsoft.NETCore.App/* | sort -V | tail -1)
mapfile -t refs < <(find "$RT" -maxdepth 1 -name '*.dll' ! -name 'Microsoft.VisualBasic*' -printf '-r:%p\n')
mapfile -t core < <(find Assets/Scripts/Core -name '*.cs')
"$D/NetCoreRuntime/dotnet" "$D/DotNetSdkRoslyn/csc.dll" -nologo -noconfig -nostdlib -langversion:9.0 -target:exe \
  -nowarn:1701,1702,618,414,649,169,8632 -out:"$S/scenecheck.dll" "${refs[@]}" "${core[@]}" tools/scenecheck/*.cs > "$S/csc.log" ||
  { cat "$S/csc.log"; exit 1; }
echo "{\"runtimeOptions\": {\"tfm\": \"net6.0\", \"framework\": {\"name\": \"Microsoft.NETCore.App\", \"version\": \"${RT##*/}\"}}}" \
  > "$S/scenecheck.runtimeconfig.json"
voice=-
if tools/vendor_fetch.sh narration "$S/narration" "narrator lines are checked for reading time only" > /dev/null &&
  ls "$S"/narration/*.wav > /dev/null 2>&1; then voice=$S/narration; fi
"$D/NetCoreRuntime/dotnet" "$S/scenecheck.dll" scenes "$voice"
echo "== The docs link check"
python3 -I tools/doclinks.py
echo "All checks passed."
