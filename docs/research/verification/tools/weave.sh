#!/usr/bin/env bash
# Run Photon Fusion's IL weaver over one of the game's compiled assemblies outside Unity, the way Unity's
# compilation pipeline runs ILPostProcessors. Run compile_unity.py first (it writes the DLL, PDB, reference
# list and defines this needs). See ../fusion-weaver.md.
#
# usage: weave.sh <editor-data-dir> <project> <compile-out-dir> <cecil-dir> <work-dir>
#        DUMP="PlayerNet WorldNet" weave.sh …   also prints what the weaver generated for those types
#        ASSEMBLY=Assembly-CSharp weave.sh …     weaves another assembly (default: PooketSlug.Net, the netcode)
set -euo pipefail
D=${1:?Editor/Data directory}; P=${2:?project root}; C=${3:?compile_unity.py --out directory}
CECIL=${4:?directory with Mono.Cecil*.dll}; W=${5:?work directory}
HERE=$(cd "$(dirname "$0")" && pwd)
DOTNET="$D/NetCoreRuntime/dotnet"; CSC="$D/DotNetSdkRoslyn/csc.dll"
RT=$(ls -d "$D"/NetCoreRuntime/shared/Microsoft.NETCore.App/* | tail -1)   # the editor's bundled .NET runtime
mkdir -p "$W"

REFS=()
for f in "$RT"/*.dll; do case $(basename "$f") in Microsoft.VisualBasic*) ;; *) REFS+=(-r:"$f") ;; esac; done
runtimeconfig() {
  printf '{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"%s"}}}\n' "$(basename "$RT")" > "$1"
}

# 1. Build the harness. Unity.CompilationPipeline.Common.dll must sit next to it: it is resolved when Main is
#    compiled, before the harness can install its assembly resolver.
"$DOTNET" "$CSC" -nologo -noconfig -nostdlib -target:exe -nowarn:1701,1702 "${REFS[@]}" \
  -r:"$D/Managed/Unity.CompilationPipeline.Common.dll" -out:"$W/WeaveHarness.dll" "$HERE/WeaveHarness.cs"
runtimeconfig "$W/WeaveHarness.runtimeconfig.json"
cp "$D/Managed/Unity.CompilationPipeline.Common.dll" "$W/"

# 2. Weave. The working directory must be the project root: the weaver finds Assets/**/*.fusion from there.
G=$C/game
A=${ASSEMBLY:-PooketSlug.Net}
PROBE="$C/editor;$P/Assets/Photon/Fusion/Assemblies;$CECIL;$D/Managed;$D/Managed/UnityEngine;$P/Assets/Photon/PhotonLibs/netstandard2.0/release"
(cd "$P" && "$DOTNET" "$W/WeaveHarness.dll" "$P" "$C/editor/Unity.Fusion.CodeGen.dll" \
  "$G/$A.dll" "$G/$A.pdb" "$W/$A.woven.dll" \
  "$G/$A.refs.txt" "$(cat "$G/$A.defines.txt")" "$PROBE")

# 3. Optional: show what the weaver generated.
if [ -n "${DUMP:-}" ]; then
  "$DOTNET" "$CSC" -nologo -noconfig -nostdlib -target:exe -nowarn:1701,1702 "${REFS[@]}" \
    -r:"$CECIL/Mono.Cecil.dll" -out:"$W/WeaveDump.dll" "$HERE/WeaveDump.cs"
  runtimeconfig "$W/WeaveDump.runtimeconfig.json"
  cp "$CECIL/Mono.Cecil.dll" "$W/"
  # shellcheck disable=SC2086
  "$DOTNET" "$W/WeaveDump.dll" "$W/$A.woven.dll" $DUMP
fi
