#!/usr/bin/env bash
# Puts a WebGL build on the gh-pages branch without disturbing anything else there.
#
#   tools/pages_deploy.sh main <build dir>           the main line: the site's root (previews/ is kept)
#   tools/pages_deploy.sh preview <slug> <build dir>  any other branch: previews/<slug>/
#   tools/pages_deploy.sh files <slug|-> <dir> <name> just one folder of files, <name>/ beside the game (the root's
#                                                     for -, previews/<slug>/'s for a slug), the game left alone:
#                                                     the sandbox's setups (sandbox/), live in a couple of minutes
#   tools/pages_deploy.sh prune                       only drops the previews of deleted branches
#
# Every run also drops previews/<slug>/ for branches that no longer exist and rewrites previews/index.html.
# gh-pages is kept as one commit with no history (as force_orphan did), so a 50 MB build doesn't pile up in the
# repository on every deploy. Several branches build at once, so the push is leased against the gh-pages commit
# this run started from; if another deploy landed first, it starts over from that one.
# Runs inside a checkout of this repository whose origin can push (actions/checkout's token does).
set -euo pipefail

mode=$1
case $mode in
  main) slug=; src=$2 ;;
  preview) slug=$2; src=$3 ;;
  files) slug=${2#-}; src=$3; folder=$4 ;;
  prune) slug=; src= ;;
  *) echo "usage: $0 main <dir> | preview <slug> <dir> | files <slug|-> <dir> <name> | prune" >&2; exit 2 ;;
esac
if [ "$mode" = files ]; then
  case $folder in ''|*[!a-z0-9-]*) echo "::error::files needs a plain folder name, not '$folder'." >&2; exit 2 ;; esac
  [ -d "$src" ] || { echo "::error::$src isn't a folder." >&2; exit 1; }
else
  [ -z "$src" ] || [ -f "$src/index.html" ] || { echo "::error::$src has no index.html: nothing to deploy." >&2; exit 1; }
fi

# A branch's preview folder (tools/slug.sh has the rule).
slugify() { "$(dirname "$0")"/slug.sh; }

repo=${GITHUB_REPOSITORY:-jhiggins-tech/pooket-slug}
pages="https://${repo%%/*}.github.io/${repo#*/}"
site=$(mktemp -d)/site
git config user.name >/dev/null || git config user.name 'github-actions[bot]'
git config user.email >/dev/null || git config user.email '41898282+github-actions[bot]@users.noreply.github.com'

for attempt in 1 2 3 4 5 6; do
  git worktree remove --force "$site" 2>/dev/null || rm -rf "$site"
  git fetch --no-tags --depth=1 origin +refs/heads/gh-pages:refs/remotes/origin/gh-pages
  base=$(git rev-parse refs/remotes/origin/gh-pages)
  git worktree add --detach "$site" "$base" >/dev/null

  case $mode in
    main)
      find "$site" -mindepth 1 -maxdepth 1 ! -name .git ! -name previews -exec rm -rf {} +
      cp -R "$src"/. "$site"/ ;;
    preview)
      rm -rf "$site/previews/$slug"
      mkdir -p "$site/previews/$slug"
      cp -R "$src"/. "$site/previews/$slug"/ ;;
    files)
      dest=$site${slug:+/previews/$slug}/$folder
      rm -rf "$dest"
      mkdir -p "$dest"
      cp -R "$src"/. "$dest"/ ;;
  esac
  touch "$site/.nojekyll"
  [ -z "$slug" ] || [ "$mode" = files ] || echo "${GITHUB_SHA::7} from ${GITHUB_REF_NAME:-?}, $(date -u '+%d %b %H:%M') UTC" > "$site/previews/$slug/preview.txt"

  # Previews whose branch has gone.
  live=$(git ls-remote --heads origin | sed 's#.*refs/heads/##' | slugify)
  if [ -d "$site/previews" ]; then
    for dir in "$site"/previews/*/; do
      [ -d "$dir" ] || continue
      name=$(basename "$dir")
      if ! grep -qxF "$name" <<<"$live" && [ "$name" != "$slug" ]; then
        echo "Removing previews/$name/: its branch is gone."
        rm -rf "$dir"
      fi
    done
    # The list of previews, for whoever is looking for one.
    {
      echo '<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width">'
      echo '<title>Pooket Slug previews</title><style>body{font:16px system-ui;margin:24px;background:#111;color:#eee}a{color:#ffe860}li{margin:8px 0}</style>'
      echo "<h1>Pooket Slug previews</h1><p><a href=\"../\">The main game</a></p><ul>"
      for dir in "$site"/previews/*/; do
        [ -d "$dir" ] || continue
        name=$(basename "$dir")
        info=$(cat "$dir/preview.txt" 2>/dev/null || true)
        echo "<li><a href=\"$name/\">$name</a> <small>$info</small></li>"
      done
      echo '</ul>'
    } > "$site/previews/index.html"
  fi

  git -C "$site" add -A --sparse # a sparse checkout (the light jobs' tools-only one) would refuse paths outside it
  tree=$(git -C "$site" write-tree)
  if [ "$tree" = "$(git rev-parse "$base^{tree}")" ]; then
    echo "gh-pages already has this."
    exit 0
  fi
  what=${slug:+preview $slug}
  [ "$mode" != files ] || what="$folder/${slug:+ in preview $slug}"
  commit=$(git -C "$site" commit-tree "$tree" -m "Deploy ${GITHUB_SHA:-local} from ${GITHUB_REF_NAME:-local}${what:+ ($what)}")
  if git push --force-with-lease=refs/heads/gh-pages:"$base" origin "$commit":refs/heads/gh-pages; then
    git worktree remove --force "$site"
    if [ "$mode" = files ]; then echo "Files: $pages/${slug:+previews/$slug/}$folder/"
    elif [ -n "$slug" ]; then echo "Preview: $pages/previews/$slug/"; elif [ "$mode" = main ]; then echo "Live: $pages/"; fi
    exit 0
  fi
  echo "Another deploy landed first; starting again from it (attempt $attempt)."
  sleep $((attempt * 5))
done
echo "::error::Couldn't push to gh-pages after 6 attempts." >&2
exit 1
