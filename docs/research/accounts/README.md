# Accounts: sign in with Google

Optional sign-in with a Google account, so a player's stats and progression follow them across devices and browsers. The record holds what the game design keeps per player: best scores and clears, freed crew, the co-op unlock, career XP, cosmetics, rank progress and the tally ([database rules](database-rules.md#the-record)), and there's a friends board per mission. It reuses the Firebase project of the owner's game Cardmaster ([setup](gcloud-setup.md)). 

**Status:** live. The database rules went live on 2026-10-14 (merged into Cardmaster and deployed by its CI, [database rules](database-rules.md)); the page, bridge and game code have been live since the owner confirmed them on 2026-10-15.

How it's wired:
- **Config:** CI passes the repository variables to `WebGLBuild`, which writes `pooket-config.js` beside the page. Missing values mean no sign-in, and progress stays in the browser.
- **The page** ([`pooket-auth.js`](../../../Assets/WebGLTemplates/PooketSlug/pooket-auth.js)) owns the button, the session (`pooket-slug.auth` in localStorage), and the database calls. It turns the game's profile into the record and back, and keeps the save number.
- **The game** ([`CloudAccount.cs`](../../../Assets/Scripts/Save/CloudAccount.cs)) hears `OnAccount` and `OnCloudProfile` on the `PooketAuth` object. `CloudProfileStore` saves locally and, while signed in, through `PS_Save`. `Game` merges a cloud copy into the profile and saves the result.
- **Phones held sideways** hide the page header, so the sign-in button shows only when the phone is upright.

**Read this when** you're implementing sign-in or cloud saves, or touching the page template's account area.

Up: [Research index](../README.md)

## The design

```text
page header (outside the Unity canvas)
  Google Identity Services button ──► Google ID token
        │
        ▼  POST identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key=<web API key>
  Firebase Auth: uid + 1-hour Firebase ID token + refresh token   (refresh: securetoken.googleapis.com)
        │
        ▼  REST  https://<db>.firebasedatabase.app/pooketSlug/players/<uid>.json?auth=<Firebase ID token>
  Realtime Database, with security rules keyed on auth.uid
        │
        ▼  jslib bridge + SendMessage(json)
  Unity: Profile, merged with this browser's copy (Profile.Merge)
```

Why: no server and no Firebase SDK (the free plan is enough), Firebase checks Google's token and the rules check Firebase's, and the button is a real page element, so browsers don't block its popup the way they can one started from inside the canvas. It reuses the owner's Cardmaster Firebase project, whose consent screen players see. The options weighed and the effort estimate are in [history](../history/accounts-decisions.md).

## How it fits the game

- **Page template** ([`index.html`](../../../Assets/WebGLTemplates/PooketSlug/index.html)):
  - an `#account` element in the header
  - a small `pooket-auth.js`, ported from TypeScript to plain JavaScript, that loads Google's button, exchanges tokens, refreshes them and calls the database
  - it keeps `unityInstance` in a global, and queues results that arrive before the game has loaded.
- **Bridge:** a `PooketAuth.jslib` plugin exposes `PS_AuthInit()`, `PS_Save(json)` and `PS_ShowAccount(show)` to C# (signing out is the page's own button). JavaScript replies with `SendMessage(gameObject, method, json)`, which takes one string argument.
- **Game:**
  - **Account state in the HUD:** signed in or not, and the display name.
  - **The profile:** [`Profile`](../../../Assets/Scripts/Core/Lobby/Profile.cs), in Core and tested, is written. It holds the best score and clears for each mission, whether co-op is open, the reveals already seen, career XP and the rank reached (stages beaten, which never goes down), and the first and last save. It's saved at the end of every run, except in a visit that keeps nothing: a sandbox (`?sandbox=`) or a spike (`?spike=`) plays from the saved profile and writes it nowhere, this browser or the cloud (`ProfileKeeping` in the same file, `ProfileTests`). Two copies merge by taking the better of each field, never adding clears up, which is the rule the cloud copy needs ([how](database-rules.md#writing-it-from-the-game)).
  - **The seam:** [`IProfileStore`](../../../Assets/Scripts/Core/Lobby/Profile.cs) has [`LocalProfileStore`](../../../Assets/Scripts/Save/LocalProfileStore.cs) (PlayerPrefs, which also took over the old high-score key) and `CloudProfileStore` in [`CloudAccount.cs`](../../../Assets/Scripts/Save/CloudAccount.cs). On sign-in the game loads the cloud's copy, merges and saves both.
- **Config:**
  - The public identifiers reach the page through the build: `-googleClientId`, `-firebaseApiKey` and `-firebaseDbUrl`, as `customParameters`. `WebGLBuild` writes them into `pooket-config.js` beside the page (`window.POOKET_CONFIG`), which `pooket-auth.js` reads. Without all three, sign-in is off. The Photon settings are separate (`Resources/pooket-net.json`, read by `NetConfig`).
  - These are public identifiers, so they go in repository **variables**, not secrets ([values](gcloud-setup.md#values-to-hand-back)).

## Database rules

- **Where they live:** the shared project's rules are deployed from Cardmaster's repository by its CI, so this game's section, a top-level `pooketSlug` key, goes into that file through a pull request, with tests in its rules test suite.
- **The section:** the record, the board, the tests and how the game writes them are in [database rules](database-rules.md).
- **Rules can enforce:**
  - only the player reads and writes their own `pooketSlug/players/{uid}`
  - the record's shape and value ranges
  - progress that only goes up (`newData.val() >= data.val()`)
  - a save number that goes up by exactly one, so a device that's behind can't overwrite newer progress
  - server-time stamps (`ts === now`)
  - no deleting the record
- **Rules can't stop a player writing a fake score with their own token.** That's fine for personal progression, and for the friends board, which runs on trust (the owner's call). **Don't build a public leaderboard on it.** That needs server-side validation (roadmap NET-08, SCR-10).
- **Test the rules** against the Firebase emulators, with a fake Google sign-in, before publishing. In the cloud sandbox, start the database emulator directly, because the Firebase CLI's own calls to it go through the proxy ([how](database-rules.md#running-the-tests-in-the-cloud-sandbox)).

## Photon

**Photon authentication is not used.**
- Photon's Custom Authentication needs a web service that Photon calls (a Cloud Function, so the Blaze plan). Without it, Photon's user ID is unverified anyway.
- **Don't put the Firebase uid into Photon:** the partner can see it.

## Risks

- **Pooket Tabks shares this origin, so the two games share `localStorage`.**
  - Tabks keeps its session under `pooket.auth`.
  - **Prefix every key here with `pooket-slug.`**, or the two sessions could overwrite each other. The shared auth code treats a project mismatch as a dead session and signs out.
  - A cross-site scripting bug in either game would also expose both.
- **The button lives in the page, not the game.** It's hidden in fullscreen, and clicking it moves keyboard focus off the canvas (refocus afterwards).
- **Every `claude/**` branch deploys a preview** on the same origin (only the main line deploys live), and all of them use the same Firebase project and the same `localStorage`. Test accounts are real accounts there.
- **The project is shared with Cardmaster:** one account across both games, shared free-plan limits, and rules changes go through Cardmaster's repository ([trade-offs](gcloud-setup.md#trade-offs-of-sharing)).
- **Privacy:** request only the default `openid email profile`, and store no email.
- **Third-party cookies and FedCM:** opt into FedCM for the button (`use_fedcm_for_button: true`); other browsers fall back to Google's popup. The page sends no restrictive security headers (CSP or COOP), so nothing needs changing.

Sources:
- **Firebase:** [Google sign-in (web)](https://firebase.google.com/docs/auth/web/google-signin) · [Redirect best practices](https://firebase.google.com/docs/auth/web/redirect-best-practices) · [Realtime Database REST auth](https://firebase.google.com/docs/database/rest/auth) · [Rule conditions](https://firebase.google.com/docs/database/security/rules-conditions) · [API keys](https://firebase.google.com/docs/projects/api-keys) · [Pricing](https://firebase.google.com/pricing)
- **Google:** [`signInWithIdp`](https://docs.cloud.google.com/identity-platform/docs/reference/rest/v1/accounts/signInWithIdp) · [GIS client ID](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid) · [GIS JS reference](https://developers.google.com/identity/gsi/web/reference/js-reference) · [FedCM migration](https://developers.google.com/identity/gsi/web/guides/fedcm-migration)
- **Unity and Photon:** [Unity → JS](https://docs.unity3d.com/6000.3/Documentation/Manual/web-interacting-browser-unity-to-js.html) · [JS → Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/web-interacting-browser-js-to-unity.html) · [Photon custom authentication](https://doc.photonengine.com/realtime/current/connection-and-authentication/authentication/custom-authentication)
