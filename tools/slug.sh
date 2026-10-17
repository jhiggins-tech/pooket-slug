#!/usr/bin/env bash
# A branch's preview folder: its name without claude/, lower case, anything but letters, digits and - as -.
# The one copy of the rule: the workflows and pages_deploy.sh all use it.
#   tools/slug.sh <branch>     one name
#   ... | tools/slug.sh        a name per line
if [ $# -gt 0 ]; then printf '%s\n' "$@"; else cat; fi | sed -e 's#^claude/##' -e 's/[^A-Za-z0-9-]/-/g' | tr 'A-Z' 'a-z'
