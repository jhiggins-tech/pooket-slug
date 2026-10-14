#!/usr/bin/env bash
# Compile the Input System package's runtime sources into Unity.InputSystem.dll, so game code that uses the
# Input System can be type-checked without Unity. The package ships inside the editor image as
# Resources/PackageManager/Editor/com.unity.inputsystem-<version>.tgz (extract-unity.sh pulls it out).
#
# usage: build-inputsystem.sh <editor-data-dir> <out-dir> [package.tgz | extracted package dir]
set -euo pipefail
D=${1:?Editor/Data directory}
OUT=${2:?output directory}
SRC=${3:-$(ls "$D"/Resources/PackageManager/Editor/com.unity.inputsystem-*.tgz | tail -1)}
mkdir -p "$OUT/inputsystem"
if [ -d "$SRC" ]; then P=$SRC; else tar -xzf "$SRC" -C "$OUT/inputsystem"; P=$OUT/inputsystem/package; fi

REFS=(-r:"$D/NetStandard/ref/2.1.0/netstandard.dll")
for f in "$D"/Managed/UnityEngine/UnityEngine.*.dll; do REFS+=(-r:"$f"); done
DEFS="UNITY_WEBGL;ENABLE_INPUT_SYSTEM;UNITY_INPUT_SYSTEM_PLATFORM_SCROLL_DELTA;UNITY_INPUT_SYSTEM_INPUT_MODULE_SCROLL_DELTA"
for y in 2019 2020 2021 2022 2023; do for m in 1 2 3 4; do DEFS+=";UNITY_${y}_${m}_OR_NEWER"; done; done
for m in 0 1 2 3; do DEFS+=";UNITY_6000_${m}_OR_NEWER"; done

# Runtime sources only: no Editor folders, and not the InputForUI plugin, which uses Unity-internal APIs.
find "$P/InputSystem" -name '*.cs' -not -path '*/Editor/*' -not -path '*/InputForUI/*' > "$OUT/inputsystem/files.txt"
"$D/NetCoreRuntime/dotnet" "$D/DotNetSdkRoslyn/csc.dll" -nologo -noconfig -nostdlib -langversion:9.0 \
  -target:library -unsafe -nowarn:1701,1702,0618,0649,0414,0169,0067 "-define:$DEFS" "${REFS[@]}" \
  -out:"$OUT/Unity.InputSystem.dll" @"$OUT/inputsystem/files.txt"
echo "$OUT/Unity.InputSystem.dll ($(wc -l < "$OUT/inputsystem/files.txt") source files)"
