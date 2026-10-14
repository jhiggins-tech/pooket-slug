#!/usr/bin/env bash
# Extract Unity's compiler, reference assemblies and bundled packages from a GameCI editor image, without
# Docker: stream the image's editor layer from Docker Hub and keep only what type-checking needs.
#
# usage: extract-unity.sh <image-tag> <out-dir>        e.g. extract-unity.sh ubuntu-6000.3.26f1-webgl-3 ./unity
#        DRY_RUN=1 extract-unity.sh <image-tag> <out-dir>   (resolve the layer only, download nothing)
#
# Docker Hub allows 100 anonymous manifest requests per hour per IP, and shared CI or cloud IPs may already
# have used them up (HTTP 429). Set DOCKER_USER and DOCKER_TOKEN (a Docker Hub access token) to authenticate.
#
# The whole editor layer (~7 GB compressed) still streams through once, but only ~300 MB is written.
# Afterwards the editor data folder is <out-dir>/opt/unity/Editor/Data.
set -euo pipefail
TAG=${1:?image tag, e.g. ubuntu-6000.3.26f1-webgl-3}
OUT=${2:?output directory}
REPO=unityci/editor
REG=https://registry-1.docker.io/v2/$REPO
ACCEPT='application/vnd.oci.image.index.v1+json, application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.docker.distribution.manifest.v2+json, application/vnd.oci.image.manifest.v1+json'

token() {
  local auth=()
  [ -n "${DOCKER_USER:-}" ] && auth=(-u "$DOCKER_USER:${DOCKER_TOKEN:?DOCKER_TOKEN must be set with DOCKER_USER}")
  curl -fsS "${auth[@]}" "https://auth.docker.io/token?service=registry.docker.io&scope=repository:$REPO:pull" |
    python3 -c 'import sys, json; print(json.load(sys.stdin)["token"])'
}

TOKEN=$(token)
manifest=$(curl -fsS -H "Authorization: Bearer $TOKEN" -H "Accept: $ACCEPT" "$REG/manifests/$TAG")
# A multi-platform index points at per-platform manifests: take linux/amd64.
platform=$(python3 -c '
import json, sys
m = json.loads(sys.argv[1])
print(next((x["digest"] for x in m.get("manifests", []) if x.get("platform", {}).get("architecture") == "amd64"), ""))' "$manifest")
if [ -n "$platform" ]; then
  manifest=$(curl -fsS -H "Authorization: Bearer $TOKEN" -H "Accept: $ACCEPT" "$REG/manifests/$platform")
fi
# The editor itself is by far the largest layer.
read -r layer size < <(python3 -c '
import json, sys
l = max(json.loads(sys.argv[1])["layers"], key=lambda l: l["size"])
print(l["digest"], l["size"])' "$manifest")
echo "editor layer: $layer ($((size / 1000000)) MB compressed)"
[ "${DRY_RUN:-}" = 1 ] && exit 0

mkdir -p "$OUT"
curl -fsSL -H "Authorization: Bearer $(token)" "$REG/blobs/$layer" |
  tar -xz -C "$OUT" --wildcards \
    '*Editor/Data/Managed/*' \
    '*Editor/Data/NetStandard/*' \
    '*Editor/Data/DotNetSdkRoslyn/*' \
    '*Editor/Data/NetCoreRuntime/*' \
    '*Editor/Data/Resources/PackageManager/BuiltInPackages/*/package.json' \
    '*Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/*' \
    '*Editor/Data/Resources/PackageManager/Editor/com.unity.inputsystem-*.tgz' \
    '*Editor/Data/Resources/PackageManager/Editor/com.unity.nuget.mono-cecil-*.tgz' \
    '*WebGLSupport/BuildTools/WebGLTemplates/*'
echo "editor data: $OUT/opt/unity/Editor/Data"
