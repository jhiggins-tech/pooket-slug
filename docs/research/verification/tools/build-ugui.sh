#!/usr/bin/env bash
# Compile uGUI's runtime sources into UnityEngine.UI.dll, so game code that builds its UI with uGUI can be
# type-checked without Unity. uGUI ships inside the editor as a built-in package
# (Resources/PackageManager/BuiltInPackages/com.unity.ugui; extract-unity.sh pulls it out). Its TextMeshPro half
# isn't built: the game doesn't use it.
#
# usage: build-ugui.sh <editor-data-dir> <out-dir> [com.unity.ugui package dir]
set -euo pipefail
D=${1:?Editor/Data directory}
OUT=${2:?output directory}
P=${3:-$D/Resources/PackageManager/BuiltInPackages/com.unity.ugui}
[ -d "$P/Runtime/UGUI" ] || { echo "no uGUI sources in $P (re-run extract-unity.sh)"; exit 1; }
mkdir -p "$OUT"

REFS=(-r:"$D/NetStandard/ref/2.1.0/netstandard.dll")
for f in "$D"/Managed/UnityEngine/UnityEngine.*.dll; do REFS+=(-r:"$f"); done
# what UnityEngine.UI.asmdef's versionDefines turn on for this project's modules and packages
DEFS="UNITY_WEBGL;PLATFORM_WEBGL;ENABLE_INPUT_SYSTEM;ENABLE_LEGACY_INPUT_MANAGER;PACKAGE_PHYSICS;PACKAGE_PHYSICS2D;PACKAGE_ANIMATION;PACKAGE_UITOOLKIT;PACKAGE_INPUTSYSTEM"
for y in 2019 2020 2021 2022 2023; do for m in 1 2 3 4; do DEFS+=";UNITY_${y}_${m}_OR_NEWER"; done; done
for m in 0 1 2 3; do DEFS+=";UNITY_6000_${m}_OR_NEWER"; done

# Paths relative to the package: the editor's own path contains "/Editor/".
(cd "$P/Runtime/UGUI" && find . -name '*.cs' -not -path './*Editor*/*' | sed "s|^\.|$P/Runtime/UGUI|") > "$OUT/ugui-files.txt"
"$D/NetCoreRuntime/dotnet" "$D/DotNetSdkRoslyn/csc.dll" -nologo -noconfig -nostdlib -langversion:9.0 \
  -target:library -nowarn:1701,1702,0618,0649,0414,0169,0067 "-define:$DEFS" "${REFS[@]}" \
  -out:"$OUT/UnityEngine.UI.dll" @"$OUT/ugui-files.txt"
echo "$OUT/UnityEngine.UI.dll ($(wc -l < "$OUT/ugui-files.txt") source files)"
