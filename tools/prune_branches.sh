#!/usr/bin/env bash
# Deletes the remote branches that are already merged into the main line, and
# leaves the rest alone.
#
#   tools/prune_branches.sh          the branches merged into the main line
#   tools/prune_branches.sh --list   just list them, and what is left
#
# The main line is claude/gallant-keller-3qzp97 (CLAUDE.md). A branch that isn't
# merged is never touched: the spikes, which are never merged as they are, and
# the work still in flight.
#
# A deleted branch's preview goes at the next deploy (tools/pages_deploy.sh prune),
# so the previews are tidied up with the branches.
set -uo pipefail

MAIN=claude/gallant-keller-3qzp97
cd "$(dirname "$0")/.." || exit 1

git fetch origin --prune --quiet

list_merged() {
  git branch -r --merged "origin/$MAIN" | grep -v HEAD | grep -v "$MAIN" | grep -v "origin/main" | sed 's/ *origin\///'
}

list_left() {
  git branch -r --no-merged "origin/$MAIN" | grep -v HEAD | grep -v "$MAIN" | grep -v "origin/main" | sed 's/ *origin\///'
}

if [ "${1:-}" = "--list" ]; then
  echo "merged into $MAIN, and ready to go:"
  list_merged
  echo
  echo "left alone (not merged):"
  list_left
  exit 0
fi

ok=0
kept=0
for b in $(list_merged); do
  if git push origin --delete "$b" >/dev/null 2>&1; then
    echo "deleted $b"
    ok=$((ok+1))
  else
    echo "kept    $b"
    kept=$((kept+1))
  fi
done
echo "---"
echo "deleted $ok, kept $kept"
echo "left alone (not merged):"
list_left
