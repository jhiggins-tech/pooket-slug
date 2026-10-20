# Accounts: decisions and estimates

This is a record of the choices made when sign-in was planned (14 Oct 2026), not instructions. What is built is in [Accounts](../accounts/README.md) and [Firebase setup](../accounts/gcloud-setup.md).

Up: [Research index](../README.md)

## Why this design

- **No server and no Firebase SDK.** It fits Firebase's free Spark plan, needs no billing account, and needs no deploy pipeline for the game itself.
- **Firebase does the verification.** It checks Google's token, and the database rules check Firebase's token, so there's no token-checking code of our own.
- **No popup blocking.** Google's button is a real page element that handles its own click, so browsers don't block it. A sign-in popup started from inside the Unity canvas arrives a frame late through Unity's input queue, and browsers (Safari especially) may block it.
- **It's a proven pattern.** The owner's other web games already use it, and Pooket Tabks runs it live on this same origin (`jhiggins-tech.github.io`).
- **It reuses existing infrastructure.** Cardmaster's Firebase project already has Google sign-in and a Realtime Database, so this game only adds its origin there and keeps its data under its own key, `pooketSlug/`. The cost is that players see "Cardmaster" on Google's consent screen, which the owner accepted ([trade-offs](../accounts/gcloud-setup.md#trade-offs-of-sharing)).


## Options considered

| Option | Verdict |
|---|---|
| **Google button + Firebase Auth and Realtime Database over REST** | **Chosen.** Effort S–M; free plan; no server |
| Firebase JS SDK, `signInWithPopup` + Firestore | Workable, but off Firebase Hosting it must use the popup (the redirect flow breaks where third-party storage is blocked), and the popup must be started by a real click on a page element |
| Google button + Cloud Run or Functions + Firestore | Needs the paid Blaze plan and a deploy pipeline, and still can't prove scores honest, because the game computes them in the browser |


## Effort estimate

**M overall, about 2 to 3 days (the estimate; it is built):**

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


## Alternative: a separate Firebase project

If this game ever needs its own name on the consent screen or its own quotas:

1. **Firebase console → Create a project** (for example `pooket-slug`). Use the Spark plan, with Analytics off.
2. **Project settings → Your apps → Add app → Web** (skip Hosting). Note the `apiKey`.
3. **Authentication → Sign-in method → Google → Enable.** Set the public name and a support email, and copy the Web client ID.
4. **Authentication → Settings → Authorized domains:** add `jhiggins-tech.github.io`.
5. **Realtime Database → Create database**, in **Locked mode** and a region near players. Copy the URL.
6. **Realtime Database → Rules:** publish this game's rules. Or deploy them from CI with a `FIREBASE_SERVICE_ACCOUNT` secret.
7. **Google Auth Platform → Clients → "Web client (auto created by Google Service)" → Authorized JavaScript origins:** add `https://jhiggins-tech.github.io`.
8. **Google Auth Platform:** set Branding to the app name. Audience: External, left in Testing, with each player added as a test user. Add no scopes.
9. **Credentials → "Browser key (auto created by Firebase)":** restrict it to `https://jhiggins-tech.github.io/*`, and to the Identity Toolkit and Token Service APIs.

Then hand back the same three variables, from the new project.
