# Accounts: "Sign in with Google" spike

Plan for identifying players with their Google account and keeping their stats and progression (best score, missions, unlocks) across devices and browsers. **Status, 2026-10-14:** researched. The plan is to reuse the Firebase project of the owner's game Cardmaster ([setup](gcloud-setup.md)). It's waiting for three console changes and the repository variables. Nothing is implemented yet. Roadmap rows SOC-01 and SOC-02, which then unlock SCR-07…09.

**Read this when** you're implementing sign-in or cloud saves, or touching the page template's account area.

Up: [Research index](../AGENTS.md)

## Recommendation: Google button + Firebase over REST, no server

```text
page header (outside the Unity canvas)
  Google Identity Services button ──► Google ID token
        │
        ▼  POST identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key=<web API key>
  Firebase Auth: uid + 1-hour Firebase ID token + refresh token   (refresh: securetoken.googleapis.com)
        │
        ▼  REST  https://<db>.firebasedatabase.app/players/<uid>.json?auth=<Firebase ID token>
  Realtime Database, with security rules keyed on auth.uid
        │
        ▼  jslib bridge + SendMessage(json)
  Unity: Progress { best, missions, unlocks }, merged with the local high score
```

Why this design:
- **No server and no Firebase SDK.** It fits Firebase's free Spark plan, needs no billing account, and needs no deploy pipeline for the game itself.
- **Firebase does the verification.** It checks Google's token, and the database rules check Firebase's token, so there's no token-checking code of our own.
- **No popup blocking.** Google's button is a real page element that handles its own click, so browsers don't block it. A sign-in popup started from inside the Unity canvas arrives a frame late through Unity's input queue, and browsers (Safari especially) may block it.
- **It's a proven pattern.** The owner's other web games already use it, and Pooket Tabks runs it live on this same origin (`jhiggins-tech.github.io`).
- **It reuses existing infrastructure.** Cardmaster's Firebase project already has Google sign-in and a Realtime Database, so this game only adds its origin there and keeps its data under its own key, `pooketSlug/`. The cost is that players see "Cardmaster" on Google's consent screen, which the owner accepted ([trade-offs](gcloud-setup.md#trade-offs-of-sharing)).

Options considered:

| Option | Verdict |
|---|---|
| **Google button + Firebase Auth and Realtime Database over REST** | **Chosen.** Effort S–M; free plan; no server |
| Firebase JS SDK, `signInWithPopup` + Firestore | Workable, but off Firebase Hosting it must use the popup (the redirect flow breaks where third-party storage is blocked), and the popup must be started by a real click on a page element |
| Google button + Cloud Run or Functions + Firestore | Needs the paid Blaze plan and a deploy pipeline, and still can't prove scores honest, because the game computes them in the browser |

## How it fits the game

- **Page template** ([`index.html`](../../../Assets/WebGLTemplates/PooketSlug/index.html)):
  - an `#account` element in the header
  - a small `pooket-auth.js`, ported from TypeScript to plain JavaScript, that loads Google's button, exchanges tokens, refreshes them and calls the database
  - it keeps `unityInstance` in a global, and queues results that arrive before the game has loaded.
- **Bridge:** a `PooketAuth.jslib` plugin exposes `PS_AuthInit(config)`, `PS_Save(json)` and `PS_SignOut()` to C#. JavaScript replies with `SendMessage(gameObject, method, json)`, which takes one string argument.
- **Game:**
  - **Account state in the HUD:** signed in or not, and the display name.
  - **Progress:** a `Progress` model saved at `pooketSlug/players/{uid}` at the end of a run, merged with the existing local high score.
  - **The seam:** a save interface (`ISaveStore`) with local and cloud implementations ([refactor candidate 13](../roadmap/refactor-candidates.md)).
- **Config:**
  - The public identifiers reach the game through the build: `-googleClientId`, `-firebaseApiKey`, `-firebaseDbUrl`, and so on, as `customParameters`. `WebGLBuild` writes them into `Resources/pooket-net.json`, and `NetConfig` reads them.
  - `WriteNetConfig` currently returns early when there's no `-photonAppId`, so it has to be decoupled.
  - These are public identifiers, so they go in repository **variables**, not secrets ([values](gcloud-setup.md#values-to-hand-back)).

## Database rules

- **Where they live:** the shared project's rules are deployed from Cardmaster's repository by its CI, so this game's section, a top-level `pooketSlug` key, goes into that file through a pull request, with tests in its rules test suite.
- **Rules can enforce:**
  - only the owner reads and writes `pooketSlug/players/{uid}`
  - the record's shape and value ranges
  - a best score that only goes up (`newData.val() >= data.val()`)
  - server-time stamps (`ts === now`)
  - no deletes
- **Rules can't stop a player writing a fake score with their own token.** That's fine for personal progression. **Don't build a public leaderboard on it.** That needs server-side validation (roadmap NET-08, SCR-10).
- **Test the rules** against the Firebase emulators, with a fake Google sign-in, before publishing. That runs fine in the cloud sandbox: Java and npm are available.

## Photon

**Skip Photon authentication for this spike.**
- Photon's Custom Authentication needs a web service that Photon calls (a Cloud Function, so the Blaze plan). Without it, Photon's user ID is unverified anyway.
- **Don't put the Firebase uid into Photon:** the partner can see it.

## Risks

- **Pooket Tabks shares this origin, so the two games share `localStorage`.**
  - Tabks keeps its session under `pooket.auth`.
  - **Prefix every key here with `pooket-slug.`**, or the two sessions could overwrite each other. The shared auth code treats a project mismatch as a dead session and signs out.
  - A cross-site scripting bug in either game would also expose both.
- **The button lives in the page, not the game.** It's hidden in fullscreen, and clicking it moves keyboard focus off the canvas (refocus afterwards).
- **Every `claude/**` build deploys live** and uses the same Firebase project. That's fine for a spike.
- **The project is shared with Cardmaster:** one account across both games, shared free-plan limits, and rules changes go through Cardmaster's repository ([trade-offs](gcloud-setup.md#trade-offs-of-sharing)).
- **Privacy:** request only the default `openid email profile`, and store no email.
- **Third-party cookies and FedCM:** opt into FedCM for the button (`use_fedcm_for_button: true`); other browsers fall back to Google's popup. The page sends no restrictive security headers (CSP or COOP), so nothing needs changing.

## Effort

**M overall, about 2–3 days:**

| Part | Effort |
|---|---|
| Page and JavaScript bridge | S–M |
| Unity side: account state, progress model and merge, HUD, save at end of run | S–M |
| Database rules | S, or M with emulator tests and a CI deploy of the rules |
| CI (pass the new variables) | S |
| Testing | M |

What can be tested where:
- **In the cloud sandbox:** the rules and the bridge, against the emulators and in headless Chromium.
- **CI only:** the Unity build.
- **By a person:** a real Google sign-in.

Sources:
- **Firebase:** [Google sign-in (web)](https://firebase.google.com/docs/auth/web/google-signin) · [Redirect best practices](https://firebase.google.com/docs/auth/web/redirect-best-practices) · [Realtime Database REST auth](https://firebase.google.com/docs/database/rest/auth) · [Rule conditions](https://firebase.google.com/docs/database/security/rules-conditions) · [API keys](https://firebase.google.com/docs/projects/api-keys) · [Pricing](https://firebase.google.com/pricing)
- **Google:** [`signInWithIdp`](https://docs.cloud.google.com/identity-platform/docs/reference/rest/v1/accounts/signInWithIdp) · [GIS client ID](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid) · [GIS JS reference](https://developers.google.com/identity/gsi/web/reference/js-reference) · [FedCM migration](https://developers.google.com/identity/gsi/web/guides/fedcm-migration)
- **Unity and Photon:** [Unity → JS](https://docs.unity3d.com/6000.3/Documentation/Manual/web-interacting-browser-unity-to-js.html) · [JS → Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/web-interacting-browser-js-to-unity.html) · [Photon custom authentication](https://doc.photonengine.com/realtime/current/connection-and-authentication/authentication/custom-authentication)
