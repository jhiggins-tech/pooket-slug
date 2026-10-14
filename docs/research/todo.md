# To-do

The working checklist: who does what, in what order, and what each item waits on. Tick items off here as they're done. Longer-term ideas live in [next steps](next-steps.md) and the [roadmap](roadmap/README.md).

**Read this when** you're picking up work, or want to know what's blocking what.

Up: [Research index](AGENTS.md)

_Last updated: 2026-10-14._

## Owner (jhiggins-tech)

- [ ] **1. Open the Cardmaster Firebase project to this game's origin** ([gcloud-setup.md](accounts/gcloud-setup.md#plan-reuse-the-cardmaster-project)). Players will see "Cardmaster" on Google's consent screen, which is accepted.
  - [ ] Firebase → Authentication → Settings → Authorized domains: add `jhiggins-tech.github.io`.
  - [ ] Google Cloud → Google Auth Platform → Clients → Cardmaster's web client → Authorized JavaScript origins: add `https://jhiggins-tech.github.io`.
  - [ ] Google Cloud → Credentials → Cardmaster's browser API key: if it has website restrictions, add `https://jhiggins-tech.github.io/*`.
  - [ ] Google Cloud → Google Auth Platform → Audience: if the publishing status is Testing, add friends as test users or publish the app. Otherwise only listed test users can sign in.
- [ ] **2. Add the repository variables** on `pooket-slug` (Settings → Secrets and variables → Actions → **Variables**): `GOOGLE_CLIENT_ID`, `FIREBASE_API_KEY` and `FIREBASE_DB_URL`. Copy Cardmaster's values from its `src/net/config.ts`. *Unblocks C1.*
- [x] **2b. Add the `cardmaster` repository to this session.** Done 2026-10-14.
- [ ] **2c. Let this session commit and push to `cardmaster`,** once the record covers the game design (see C1). The [rules change](accounts/database-rules.md) is drafted and tested in this session's copy, but the session's permission guard blocked the commit: the change is to another repository, and its rules deploy to the live Firebase project. When it's ready, either:
  - tell Claude to commit and push it, and whether to open the pull request (*unblocks the rules part of C1*), or
  - apply the change yourself.

  You merge the pull request, and Cardmaster's CI deploys the rules.
- [x] **3. Sort the feature list.** Done 2026-10-14: the `moscow` column is filled in [feature-candidates.csv](roadmap/feature-candidates.csv).
- [ ] **4. Sign in for real** once the sign-in build is live, to confirm it end to end. *Needs C1.*
- [ ] **5. Optional: play the untested co-op paths** in two visible windows: the host leaving mid-game, quick match, a mistyped code, a third player joining ([steps](netcode/sessions.md#testing-by-hand)). CI now checks the host leaving on every build (C3c), so three paths are left.
- [x] **6. Answer the refactor plan's open points.** Done 2026-10-14 ([decisions](roadmap/refactor-plan.md#decisions-2026-10-14)): a friends leaderboard, branches keep deploying, two-player tests automated in CI, classic guns covered by the kits, uGUI and assembly definitions accepted.
- [x] **7. Sign off the four design documents** in [docs/design](../design/README.md). All four signed off on 2026-10-14; player mechanics after one revision.
- [x] **8. Give the go-ahead for the refactor.** Given 2026-10-14.

## Claude

C2, the refactor, is done. The repository variables (owner 2) start the rest of C1.

- [ ] **C1. "Sign in with Google" spike** ([plan](accounts/README.md)). *Waits for owner 2.*
  - [ ] Database rules, as a pull request to Cardmaster: a `pooketSlug/players/{uid}` section in its rules file, where only the player reads and writes, the shape is validated, the stats only go up, and there are no deletes. Add tests to its rules test suite.
    - Drafted 2026-10-14 ([database rules](accounts/database-rules.md)). It passes 24 of 24 rules tests, and all 12 deliberate weakenings were caught.
    - **Extend it before committing.** The [game design](../../DESIGN.md#building-on-the-spike) also needs, per player: freed crew, whether co-op is open (Mission one beaten solo), career XP, cosmetics owned and worn, rank progress, and the tally per soldier and weapon. Scoped from the MoSCoW pass in the [refactor plan](roadmap/refactor-plan.md#the-player-record-scoped-by-the-moscow-pass). Extend it once you approve that.
    - *Commit, push and pull request need owner 2c.*
  - [ ] Page: `pooket-auth.js` (Google button, token exchange and refresh, database calls), an `#account` slot in the header, and results queued until the game has loaded. Storage keys prefixed `pooket-slug.`.
  - [ ] Bridge: a `PooketAuth.jslib` plugin (`PS_AuthInit`, `PS_Save`, `PS_SignOut`) plus a `SendMessage` receiver in C#.
  - [ ] Game: a cloud `IProfileStore` that loads, merges and saves the `Profile` ([how](accounts/database-rules.md#writing-it-from-the-game)); "signed in as …" in the HUD. The profile, its merge, the local store and saving at the end of a run are done (refactor plan step 15).
  - [ ] Build: pass `-googleClientId` / `-firebaseApiKey` / `-firebaseDbUrl` from the repository variables, and stop `WriteNetConfig` depending on the Photon AppId.
  - [ ] Verify:
    - compile and weave locally
    - run the emulator tests ([`rules-test.sh`](verification/tools/rules-test.sh) in the sandbox)
    - drive the page bridge in a headless browser
    - get the CI build green
    - the rules go live when you merge the Cardmaster pull request
  - [ ] Docs: [accounts](accounts/README.md) and the [test log](verification/test-log.md).
- [x] **C2. Refactor pass** to prepare the build and netcode for the chosen features. *Done 2026-10-14.*
  - [x] Map the Must and Should rows to the [refactor candidates](roadmap/refactor-candidates.md), and send you a short plan before changing code. Sent 2026-10-14: [refactor plan](roadmap/refactor-plan.md).
  - [x] Refactor in small commits. Each one is type-checked, weave-checked and smoke-tested. Anything that touches netcode gets a two-player test.
    - All four phases landed on 2026-10-14, each step checked locally and in CI ([progress](roadmap/refactor-plan.md#the-plan), [test log](verification/test-log.md#part-4-refactor)). What waits for its feature is listed with each step: boss and tank network objects, the rest of per-player state, the lobby features, character select, settings, and the cloud profile.
  - [x] Update [architecture](netcode/architecture.md) and the refactor candidates as items land.
- [ ] **C3. Loose ends** (small; any time):
  - [ ] C3a. Make connection failures say *Couldn't reach Photon* instead of `DISCONNECTED (ERROR)` ([sessions](netcode/sessions.md#when-things-fail)).
  - [ ] C3b. Fix the Photon region to **AU** (NET-05, from the MoSCoW pass), with four-letter room codes. It goes in `NetSession`, which the refactor's step 12 split out ([quick match](netcode/sessions.md#quick-match)). The CI co-op test reads the code from the log, so its pattern changes too.
  - [x] C3c. Run the two-browser co-op test in CI after each deploy. Done 2026-10-14 as step 4 of the [refactor plan](roadmap/refactor-plan.md): it passed on its first run, host migration included ([test log](verification/test-log.md#part-2-photon-fusion-co-op)). Of owner 5's paths, it covers the host leaving.

## Done recently

- [x] GameCI → WebGL → GitHub Pages pipeline, live. Solo game played by the owner.
- [x] Photon Fusion 2 co-op: built in CI from the private SDK release, and played live by the owner on 2026-10-14.
- [x] Research docs and reference guide (`docs/research/`). `LEARNED-LESSONS.md` retired.
- [x] Feature candidates CSV (176 rows) and draft refactor candidates.
- [x] "Sign in with Google" research and setup checklist.
