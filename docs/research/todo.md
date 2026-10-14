# To-do

The working checklist: who does what, in what order, and what each item waits on. Tick items off here as they're done. Longer-term ideas live in [next steps](next-steps.md) and the [roadmap](roadmap/README.md).

**Read this when** you're picking up work, or want to know what's blocking what.

Up: [Research index](AGENTS.md)

_Last updated: 2026-10-14._

## Owner (jhiggins-tech)

- [x] **1. Open the Cardmaster Firebase project to this game's origin** (domain added 2026-10-14; confirm the OAuth origin, key and audience items below if sign-in fails) ([gcloud-setup.md](accounts/gcloud-setup.md#plan-reuse-the-cardmaster-project)). Players will see "Cardmaster" on Google's consent screen, which is accepted.
  - [ ] Firebase → Authentication → Settings → Authorized domains: add `jhiggins-tech.github.io`.
  - [ ] Google Cloud → Google Auth Platform → Clients → Cardmaster's web client → Authorized JavaScript origins: add `https://jhiggins-tech.github.io`.
  - [ ] Google Cloud → Credentials → Cardmaster's browser API key: if it has website restrictions, add `https://jhiggins-tech.github.io/*`.
  - [ ] Google Cloud → Google Auth Platform → Audience: if the publishing status is Testing, add friends as test users or publish the app. Otherwise only listed test users can sign in.
- [x] **2. Add the repository variables** on `pooket-slug`. Done 2026-10-14. (Settings → Secrets and variables → Actions → **Variables**): `GOOGLE_CLIENT_ID`, `FIREBASE_API_KEY` and `FIREBASE_DB_URL`. Copy Cardmaster's values from its `src/net/config.ts`. *Unblocks C1.*
- [x] **2b. Add the `cardmaster` repository to this session.** Done 2026-10-14.
- [x] **2c. Let this session commit and push to `cardmaster`.** Given 2026-10-14. The [rules](accounts/database-rules.md) are on Cardmaster's branch `claude/gallant-keller-3qzp97`, and its CI passed.
- [x] **2d. Merge [jhiggins-tech/cardmaster#11](https://github.com/jhiggins-tech/cardmaster/pull/11),** the rules pull request. Merged 2026-10-14, and Cardmaster's CI deployed the rules.
- [x] **3. Sort the feature list.** Done 2026-10-14: the `moscow` column is filled in [feature-candidates.csv](roadmap/feature-candidates.csv).
- [ ] **4. Sign in for real** once the sign-in build is live, to confirm it end to end. *Needs C1.*
- [ ] **5. Optional: play the untested co-op paths** in two visible windows: the host leaving mid-game, quick match, a mistyped code, a third player joining ([steps](netcode/sessions.md#testing-by-hand)). CI now checks the host leaving on every build (C3c), so three paths are left.
- [x] **6. Answer the refactor plan's open points.** Done 2026-10-14 ([decisions](roadmap/refactor-plan.md#decisions-2026-10-14)): a friends leaderboard, branches keep deploying, two-player tests automated in CI, classic guns covered by the kits, uGUI and assembly definitions accepted.
- [x] **7. Sign off the four design documents** in [docs/design](../design/README.md). All four signed off on 2026-10-14; player mechanics after one revision.
- [x] **8. Give the go-ahead for the refactor.** Given 2026-10-14.

## Claude

C2, the refactor, is done. C1's code is written and checked locally; it goes live with its first CI build.

- [ ] **C1. "Sign in with Google" spike** ([plan](accounts/README.md)). *Code written 2026-10-14; waits for CI and owner 4.*
  - [x] Database rules for Cardmaster: a `pooketSlug` section in its rules file, with tests in its rules test suite ([database rules](accounts/database-rules.md)).
    - The player's record, as the [refactor plan](roadmap/refactor-plan.md#the-player-record-scoped-by-the-moscow-pass) scoped it, and the friends board. Only the player reads and writes their record; the shape is validated, progress only goes up, a save number refuses saves from a device that's behind, and nothing required can be deleted.
    - 30 of 30 rules tests pass, and all 32 deliberate weakenings were caught. Cardmaster's CI passed on the branch.
    - Live since 2026-10-14: merged as [jhiggins-tech/cardmaster#11](https://github.com/jhiggins-tech/cardmaster/pull/11) and deployed by Cardmaster's CI.
  - [x] Page: `pooket-auth.js` (Google button, token exchange and refresh, database calls, and turning the game's profile into the record and back), an `#account` slot in the header, and messages queued until the game has loaded. Storage key `pooket-slug.auth`.
  - [x] Bridge: `PooketAuth.jslib` (`PS_AuthInit`, `PS_Save`, `PS_SignOut`) and the `PooketAuth` receiver, `CloudAccount`.
  - [x] Game: `CloudProfileStore` saves to this browser and, when signed in, the cloud; the cloud's copy is merged in on sign-in and after a refused save. The title screen says who's signed in.
  - [x] Build: `-googleClientId` / `-firebaseApiKey` / `-firebaseDbUrl` from the repository variables. `WebGLBuild` writes them to `pooket-config.js` next to the page, so `WriteNetConfig` is untouched.
  - [ ] Not yet: the friends board, and XP, rank, crew, cosmetics and the tally (the game doesn't have them yet; the first save writes zeros, and later saves leave them alone).
  - [ ] Verify:
    - [x] compile locally (no errors or warnings, each input backend)
    - [x] drive the page against the database emulator with the live rules ([`auth-page.mjs`](verification/tools/auth-page.mjs): 22 of 22)
    - [ ] get the CI build green
    - the rules are live (merged and deployed 2026-10-14)
  - [ ] Docs: [accounts](accounts/README.md) and the [test log](verification/test-log.md).
- [x] **C2. Refactor pass** to prepare the build and netcode for the chosen features. *Done 2026-10-14.*
  - [x] Map the Must and Should rows to the [refactor candidates](roadmap/refactor-candidates.md), and send you a short plan before changing code. Sent 2026-10-14: [refactor plan](roadmap/refactor-plan.md).
  - [x] Refactor in small commits. Each one is type-checked, weave-checked and smoke-tested. Anything that touches netcode gets a two-player test.
    - All four phases landed on 2026-10-14, each step checked locally and in CI ([progress](roadmap/refactor-plan.md#the-plan), [test log](verification/test-log.md#part-4-refactor)). What waits for its feature is listed with each step: boss and tank network objects, the rest of per-player state, the lobby features, character select, settings, and the cloud profile.
  - [x] Update [architecture](netcode/architecture.md) and the refactor candidates as items land.
- [ ] **C3. Loose ends** (small; any time):
  - [ ] C3a. Make connection failures say *Couldn't reach Photon* instead of `DISCONNECTED (ERROR)` ([sessions](netcode/sessions.md#when-things-fail)).
  - [x] C3b. Fix the Photon region to **AU** (NET-05, from the MoSCoW pass), with four-letter room codes. Done 2026-10-14 in `NetSession` and `InviteCode` ([regions and room codes](netcode/sessions.md#regions-and-room-codes)). Older `ABCD-EU` codes still parse, and the CI co-op test reads either form.
  - [x] C3c. Run the two-browser co-op test in CI after each deploy. Done 2026-10-14 as step 4 of the [refactor plan](roadmap/refactor-plan.md): it passed on its first run, host migration included ([test log](verification/test-log.md#part-2-photon-fusion-co-op)). Of owner 5's paths, it covers the host leaving.

## Done recently

- [x] GameCI → WebGL → GitHub Pages pipeline, live. Solo game played by the owner.
- [x] Photon Fusion 2 co-op: built in CI from the private SDK release, and played live by the owner on 2026-10-14.
- [x] Research docs and reference guide (`docs/research/`). `LEARNED-LESSONS.md` retired.
- [x] Feature candidates CSV (176 rows) and draft refactor candidates.
- [x] "Sign in with Google" research and setup checklist.
