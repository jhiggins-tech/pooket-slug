# Database rules for player progress

This game's section of the shared Realtime Database rules: each player's record, a friends leaderboard, how the game should write them, and how the rules are tested.

**Status: live** since 2026-10-14. Cardmaster's [`firebase/database.rules.json`](https://github.com/jhiggins-tech/cardmaster/blob/main/firebase/database.rules.json) is the source of truth, deployed by its CI.

**Read this when** you're changing the game's save code or what it stores, or changing the rules.

Up: [Accounts](README.md)

## The record

Each player has one record at `pooketSlug/players/{uid}`. Only they read and write it, and nobody can list the players. It holds what the design keeps per player, plus a save number:

| Field | Meaning | Rules |
|---|---|---|
| `version` | The record's format: 1 for now | Required. A whole number from 1 to 1,000; never lower |
| `rev` | The save number | Required. 1 on the first save, then exactly one more than the stored number on every save |
| `coop` | Co-op is open (Mission one beaten solo) | Required. True or false; once true, it stays true |
| `xp` | Career XP | Required. A whole number up to 1,000,000,000; never lower |
| `tokens` | Tokens from XP levels, spent on looks | Required. A whole number up to 1,000,000; it can go down |
| `rank` | Stages beaten | Required. A whole number up to 1,000; never lower |
| `name` | The player's display name | 1 to 24 characters |
| `crew/{id}` | Freed crewmates | Each one `true` |
| `seen/{id}` | Reveals already shown | Each one `true` |
| `cosmetics/owned/{item}` | Looks the player owns | Each one `true` |
| `cosmetics/worn/{slot}` | What's worn in each slot | The id of an item the player owns |
| `missions/{mission}/{setting}` | `best` and `clears`, per mission, under `normal` (the rules also allow `hard`, from a hard setting the game no longer has: it never reads or writes it) | Both required, whole numbers up to 1,000,000,000; never lower |
| `tally/{soldier}/{weapon}` | `shots`, `hits`, `dealt`, `taken` and `kills` | All five required, whole numbers up to 1,000,000,000; never lower |
| `first` | When the record was made | Required. The server time on the first save; it never changes |
| `ts` | When it was last saved | Required. The server time, on every save |

- **Ids are short:** crewmates, reveals, items, slots, missions, soldiers and weapons are at most 32 characters. Nothing else is allowed, at any level.
- **The save number stops a device that's behind from overwriting newer progress.** Every save must carry `rev` one above the stored number, so when two devices save from the same copy, the second is refused. It then reads the record again and merges ([below](#writing-it-from-the-game)). Numbers that only go up wouldn't be enough on their own: a device that's behind can still have more XP than the cloud, and its save would drop the other device's freed crew.
- **The record and its required fields can't be deleted.** Entries in its lists (crew, reveals, items owned) can be dropped, but only by the player's own up-to-date save; the game never does.
- **The rules can't make it honest.** With their own token, a player can write anything that passes these checks. That's fine for personal progress.

## The friends board

`pooketSlug/leaderboard/{mission}/{uid}` holds each player's `name`, `best` and `ts` for a mission. It's the owner's friends board, which runs on trust.
- **Any signed-in player** of the shared Firebase project can read a mission's board. Nobody can list the boards, or read one signed out.
- **Only the player** writes their entry. Their best never goes down, the name is 1 to 24 characters, `ts` is the server time, and nothing else is allowed.
- **A player can take their own entry down;** nobody else can.
- **Its scores come from players' own devices,** like the record, so it's for friends, not the public.

## Writing it from the game

How the save code works, in [`CloudAccount.cs`](../../../Assets/Scripts/Save/CloudAccount.cs) and the page's `pooket-auth.js`. The [`Profile`](../../../Assets/Scripts/Core/Lobby/Profile.cs) record and its merge are in Core, with a local store behind `IProfileStore`.
- **On sign-in:** `GET pooketSlug/players/{uid}.json?auth=<Firebase ID token>`. It returns `null` until the first save. Keep its `rev`.
- **Merging:** take the better of each, as `Profile.Merge` does: the higher numbers, both copies' list entries, co-op if either has it, and the earlier `first`. Clears take the higher count, not the sum, since the same clears may be on both copies.
- **First save:** `PUT` the whole record, with `rev` 1 and with `first` and `ts` both set to `{".sv": "timestamp"}`.
- **Later saves:** `PATCH` the record with what changed, plus `rev` one above the last one read or written and `ts` as the server time. A write below the record, such as to one mission, is refused: it can't carry the save number.
- **If a save is refused** (HTTP 401), another device has probably saved since. Read the record again, merge, and retry once with the new save number.
- **The board:** after a run that beats a mission's best, `PUT pooketSlug/leaderboard/{mission}/{uid}` with the name, the best and `ts`.
- **The ids already exist:** missions (`mission-1`) and reveals (`coop`) come from `Missions` and `Reveals`, and a mission's record goes under `normal`.

## The rules section

The section is in [Cardmaster's rules file](https://github.com/jhiggins-tech/cardmaster/blob/main/firebase/database.rules.json), as a new top-level `pooketSlug` key after `lobby`. Nothing else in the file changes. How it works:
- **Write access is granted in two places:** on a player's record, to that player, never to delete it; and on a board entry, to its player. Everything else is `.validate`.
- **Deletes:** the record's `.write` needs `newData.exists()`. A required field can't be deleted either: validation runs on the record that holds it, and `hasChildren` then fails.
- **The save number:** the record's own `.validate` compares `newData.child('rev')` with `data.child('rev')`. It runs on every write that changes the record, a `PATCH` of one field included.
- **Maps** must be objects (`hasChildren()`), so a plain value can't skip the checks on their entries.
- **A worn item** must be under `owned` in the new data: `newData.parent().parent().child('owned').child(newData.val()).exists()`.

## Tests

The tests include signed-out cases for both paths. Each talks to the emulator over the REST API, as the game will. They check that:
- the player creates their record, reads it, and saves over it with `PATCH` and `PUT`
- nobody else reads or writes it, and nobody lists the players
- each save goes one up: a save from a device that's behind, one that skips ahead, and a write below the record are all refused
- progress never goes down, but tokens can be spent
- the times are the server time, and `first` never changes
- every required field is there and nothing unknown is, with whole numbers in range
- lists hold ids marked `true`, and maps keep their shapes
- a worn cosmetic is one the player owns
- the record and its required fields can't be deleted
- on the board, a player posts their best and any signed-in player reads it
- only the player writes their entry, and their best never goes down
- an entry is a name, a whole best and the server time
- a player can take their own entry down, and nobody else can

A mutation check run when the rules were written (weaken each rule, a named test must fail) is in [history](../history/rules-mutation-check.md); redo it when you change a rule.

## Running the tests in the cloud sandbox

- **The usual way doesn't work here.** Cardmaster's `npm run test:firebase` starts the emulators with the Firebase CLI, which sends every request through `HTTPS_PROXY` and ignores `NO_PROXY` (firebase-tools 15.33.0, `lib/apiv2.js`). So even its call that loads the rules into the local emulator goes to the sandbox proxy, which refuses it. The emulators then shut down with `Failed to load initial Database Emulator rules`.
- **Don't unset `HTTPS_PROXY`;** the sandbox's rules forbid it. Use [`rules-test.sh`](../verification/tools/rules-test.sh) instead. It starts the database emulator's JAR directly, loads the rules with curl (which honours `NO_PROXY`), then runs the rules tests with Node.
- **What it needs:**
  - Node 24 for `npm ci` (Cardmaster requires it; `nvm install 24`). The tests themselves also ran on the sandbox's Node 22.
  - Java 21 or newer
  - `npm ci --prefix firebase`, run once in the Cardmaster checkout
- **It only covers the rules tests.** Cardmaster's CI runs the hosting and sign-in tests on every push, on GitHub's runners, which have no such proxy.
