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
- [ ] **2. Add the repository variables** on `pooket-slug` (Settings → Secrets and variables → Actions → **Variables**): `GOOGLE_CLIENT_ID`, `FIREBASE_API_KEY` and `FIREBASE_DB_URL`. Copy Cardmaster's values from its `src/net/config.ts`. *Unblocks C1.*
- [ ] **2b. Give this session push access to the `cardmaster` repository** when C1 starts, so the rules pull request can be opened. Or apply the rules change yourself. You merge it; Cardmaster's CI deploys it.
- [ ] **3. Sort the feature list.** Fill the `moscow` column (Must / Should / Could / Won't) in [feature-candidates.csv](roadmap/feature-candidates.csv) and send it back. *Unblocks C2.*
- [ ] **4. Sign in for real** once the sign-in build is live, to confirm it end to end. *Needs C1.*
- [ ] **5. Optional: play the untested co-op paths** in two visible windows: the host leaving mid-game, quick match, a mistyped code, a third player joining ([steps](netcode/sessions.md#testing-by-hand)). C3c would automate this instead.

## Claude

Whichever input arrives first starts: the variables start C1, the sorted CSV starts C2.

- [ ] **C1. "Sign in with Google" spike** ([plan](accounts/README.md)). *Waits for owner 2.*
  - [ ] Database rules, as a pull request to Cardmaster: a `pooketSlug/players/{uid}` section in its rules file, where only the owner reads and writes, the shape is validated, the best score only goes up, and there are no deletes. Add tests to its rules test suite (emulators, fake Google sign-in). *Needs owner 2b.*
  - [ ] Page: `pooket-auth.js` (Google button, token exchange and refresh, database calls), an `#account` slot in the header, and results queued until the game has loaded. Storage keys prefixed `pooket-slug.`.
  - [ ] Bridge: a `PooketAuth.jslib` plugin (`PS_AuthInit`, `PS_Save`, `PS_SignOut`) plus a `SendMessage` receiver in C#.
  - [ ] Game: an `ISaveStore` with local and cloud implementations; a `Progress` model (best, missions, unlocks) merged with the local high score; "signed in as …" in the HUD; save at the end of a run.
  - [ ] Build: pass `-googleClientId` / `-firebaseApiKey` / `-firebaseDbUrl` from the repository variables, and stop `WriteNetConfig` depending on the Photon AppId.
  - [ ] Verify:
    - compile and weave locally
    - run the emulator tests
    - drive the page bridge in a headless browser
    - get the CI build green
    - the rules go live when you merge the Cardmaster pull request
  - [ ] Docs: [accounts](accounts/README.md) and the [test log](verification/test-log.md).
- [ ] **C2. Refactor pass** to prepare the build and netcode for the chosen features. *Waits for owner 3.*
  - [ ] Map the Must and Should rows to the [refactor candidates](roadmap/refactor-candidates.md), and send you a short plan before changing code.
  - [ ] Refactor in small commits. Each one is type-checked, weave-checked and smoke-tested. Anything that touches netcode gets a two-player test.
  - [ ] Update [architecture](netcode/architecture.md) and the refactor candidates as items land.
- [ ] **C3. Loose ends** (small; any time):
  - [ ] C3a. Make connection failures say *Couldn't reach Photon* instead of `DISCONNECTED (ERROR)` ([sessions](netcode/sessions.md#when-things-fail)).
  - [ ] C3b. Optionally, pin a quick-match region with a `PHOTON_REGION` variable passed as `-photonRegion` ([quick match](netcode/sessions.md#quick-match)).
  - [ ] C3c. Run the two-browser co-op test in CI after each deploy. GitHub's runners can open WebSockets, so this would cover owner 5 automatically ([roadmap OPS-02](roadmap/feature-candidates.csv)).

## Done recently

- [x] GameCI → WebGL → GitHub Pages pipeline, live. Solo game played by the owner.
- [x] Photon Fusion 2 co-op: built in CI from the private SDK release, and played live by the owner on 2026-10-14.
- [x] Research docs and reference guide (`docs/research/`). `LEARNED-LESSONS.md` retired.
- [x] Feature candidates CSV (176 rows) and draft refactor candidates.
- [x] "Sign in with Google" research and setup checklist.
