#!/usr/bin/env bash
# Runs Cardmaster's Realtime Database rules tests (firebase/tests/rules.test.mjs), which cover this
# game's pooketSlug section, against the database emulator from inside the cloud sandbox.
#
# Usage: rules-test.sh <cardmaster checkout>
# Needs Node 24 and Java 21 or newer, and `npm ci --prefix firebase` run once in the checkout.
#
# Cardmaster's own `npm run test:firebase` starts the emulators with the Firebase CLI. The CLI sends
# every request through HTTPS_PROXY and ignores NO_PROXY, so in the sandbox its call that loads the
# rules into the local emulator reaches the proxy and is refused. This script starts the emulator's
# JAR directly, loads the rules with curl (which honours NO_PROXY) and runs the rules tests. Hosting
# and sign-in tests aren't covered here; Cardmaster's CI runs them on every push.
set -euo pipefail
repo=${1:?usage: rules-test.sh <cardmaster checkout>}
ns=demo-cardmaster-default-rtdb # the namespace rules.test.mjs uses

jar() { ls ~/.cache/firebase/emulators/firebase-database-emulator-v*.jar 2>/dev/null | sort -V | tail -1; }
[ -n "$(jar)" ] || (cd "$repo/firebase" && npx firebase setup:emulators:database >/dev/null)

tmp=$(mktemp -d)
java -jar "$(jar)" --host 127.0.0.1 --port 9000 >"$tmp/emulator.log" 2>&1 &
pid=$!
trap 'kill $pid 2>/dev/null; wait $pid 2>/dev/null || true; rm -rf "$tmp"' EXIT
for _ in $(seq 60); do curl -s -o /dev/null "http://127.0.0.1:9000/.json?ns=$ns" && break; sleep 0.5; done

# The same call the CLI makes to load the rules.
code=$(curl -s -o "$tmp/load.out" -w '%{http_code}' -X PUT -H 'Authorization: Bearer owner' \
  --data-binary @"$repo/firebase/database.rules.json" "http://127.0.0.1:9000/.settings/rules.json?ns=$ns")
if [ "$code" != 200 ]; then
  echo "The emulator refused the rules (HTTP $code):" >&2
  cat "$tmp/load.out" >&2
  exit 1
fi

cd "$repo/firebase"
node --test tests/rules.test.mjs
