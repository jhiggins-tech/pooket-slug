# Database rules for player stats

This game's section of the shared Realtime Database rules. It covers the record each player gets, what the rules enforce, how the game should write to the record, and how the rules are tested.

**Status, 2026-10-14:** drafted as a change to Cardmaster's repository and tested against the database emulator in the cloud sandbox. **It isn't committed there yet:** this session's permission guard blocked the commit to Cardmaster, so it waits for the owner ([to-do](../todo.md)). **It also needs extending first:** the game design asks for more per-player data ([below](#the-record)). Once merged, Cardmaster's `firebase/database.rules.json` is the source of truth, and the copy below is the draft.

**Read this when** you're writing the game's save code, changing what it stores, or reviewing the rules pull request.

Up: [Accounts](README.md)

## The record

Each player has one record at `pooketSlug/players/{uid}`. It must have all five fields, and nothing else:

| Field | Meaning | Rules |
|---|---|---|
| `best` | Best score | A whole number from 0 to 1,000,000,000; never lower than the stored value |
| `runs` | Runs finished while signed in | A whole number from 0 to 1,000,000,000; never lower than stored |
| `wins` | Runs that reached the victory screen | A whole number, never more than `runs`; never lower than stored |
| `first` | When the record was made | The server time when the record is created; never changes |
| `ts` | When it was last saved | The server time, whenever it's written |

- **Only the player** can read or write their record, and nobody can list the players.
- **Nothing can be deleted,** neither the record nor any field in it.
- **The first write must be the whole record.** Later writes can change some fields.
- **The game design needs more.** Its [Building on the spike](../../../DESIGN.md#building-on-the-spike) section adds, per player: freed crew, whether co-op is open (Mission one beaten solo), career XP, cosmetics owned and worn, rank progress, and the tally per soldier and weapon. Each is a rules change in Cardmaster, so the plan is to extend or reshape this record before the first pull request, as scoped in the [refactor plan](../roadmap/refactor-plan.md#the-player-record-scoped-by-the-moscow-pass). Anything added later means another small rules change, with tests.
- **The rules can't make a score honest.** With their own token, a player can write any numbers that pass these checks. That's fine for a personal record, but don't build a leaderboard on it ([why](README.md#database-rules)).

## Writing it from the game

This is the plan for the save code. None of it is written yet.

- **On sign-in:** `GET pooketSlug/players/{uid}.json?auth=<Firebase ID token>`. It returns `null` until the first save.
- **Merging:**
  - Take whichever is higher of the cloud's `best` and the local high score.
  - Count runs and wins only while signed in. Then offline play on two devices is never counted twice.
- **First save:** `PUT` the whole record, with `first` and `ts` both set to `{".sv": "timestamp"}`.
- **Later saves:** `PATCH` `best`, `runs`, `wins` and `ts`.
- **If a save is refused** (HTTP 401), another device has probably saved since. Read the record again, re-apply this run's changes, and retry once.

## The rules section

Added after Cardmaster's `lobby` section, as a new top-level key:

```json
"pooketSlug": {
  "players": {
    "$uid": {
      ".read": "auth != null && auth.uid === $uid",
      ".write": "auth != null && auth.uid === $uid && newData.exists()",
      ".validate": "newData.hasChildren(['best', 'runs', 'wins', 'first', 'ts'])",
      "best": { ".validate": "newData.isNumber() && newData.val() >= 0 && newData.val() <= 1000000000 && newData.val() % 1 === 0 && (!data.exists() || newData.val() >= data.val())" },
      "runs": { ".validate": "newData.isNumber() && newData.val() >= 0 && newData.val() <= 1000000000 && newData.val() % 1 === 0 && (!data.exists() || newData.val() >= data.val())" },
      "wins": { ".validate": "newData.isNumber() && newData.val() >= 0 && newData.val() <= newData.parent().child('runs').val() && newData.val() % 1 === 0 && (!data.exists() || newData.val() >= data.val())" },
      "first": { ".validate": "(!data.exists() && newData.val() === now) || (data.exists() && newData.val() === data.val())" },
      "ts": { ".validate": "newData.val() === now" },
      "$other": { ".validate": false }
    }
  }
}
```

How the deletes are blocked:
- **Deleting the whole record:** the `.write` rule requires `newData.exists()`.
- **Deleting a single field:** validation doesn't run on a deleted value, but it does run on the record that contains it, which `hasChildren` then rejects.

The same change also adds a note to Cardmaster's README: the `pooketSlug` section and the extra sign-in origin belong to this game, so later rules changes keep them.

## Tests

The change adds seven tests to Cardmaster's `firebase/tests/rules.test.mjs`, plus a signed-out case. Each one talks to the emulator over the REST API, as the game will. They check that:
- the player creates their record, reads it, and saves over it with `PATCH` and `PUT`
- nobody else reads or writes it, and nobody lists the players
- the stats never go down, so an older save from another device is refused
- wins never outnumber runs
- the times are the server time, and `first` never changes
- every field is required, nothing else is allowed, and numbers must be whole and in range
- nothing can be deleted, by the player or anyone else

**Mutation check:** the rules were weakened 12 ways, one at a time. Every time, the run failed and a named test reported it. The 12 ways:
- allowing deletes
- a lower `best`
- a lower `runs`
- more wins than runs
- a changed `first`
- a client-chosen `ts`
- extra fields
- partial records
- fractions
- no upper bound
- other players reading
- other players writing

Results are in the [test log](../verification/test-log.md#part-3-sign-in).

## Running the tests in the cloud sandbox

- **The usual way doesn't work here.** Cardmaster's `npm run test:firebase` starts the emulators with the Firebase CLI, which sends every request through `HTTPS_PROXY` and ignores `NO_PROXY` (firebase-tools 15.33.0, `lib/apiv2.js`). So even its call that loads the rules into the local emulator goes to the sandbox proxy, which refuses it. The emulators then shut down with `Failed to load initial Database Emulator rules`.
- **Don't unset `HTTPS_PROXY`;** the sandbox's rules forbid it. Use [`rules-test.sh`](../verification/tools/rules-test.sh) instead. It starts the database emulator's JAR directly, loads the rules with curl (which honours `NO_PROXY`), then runs the rules tests with Node.
- **What it needs:**
  - Node 24 (`nvm install 24`; the sandbox ships Node 22)
  - Java 21 or newer
  - `npm ci --prefix firebase`, run once in the Cardmaster checkout
- **It only covers the rules tests.** Cardmaster's CI runs the hosting and sign-in tests on every push, on GitHub's runners, which have no such proxy.
