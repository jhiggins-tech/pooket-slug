# Google Cloud / Firebase setup for sign-in

The one-time console steps for the [sign-in spike](README.md). Following them creates everything on Firebase's **free Spark plan**, with no billing account. Console labels are as of October 2026, so they may drift.

**Read this when** you're setting up, or checking, the Firebase project behind sign-in.

Up: [Accounts](README.md)

## Steps

1. **Firebase console → Create a project**, for example `pooket-slug`.
   - This also creates the Google Cloud project; there's nothing to link.
   - Turn Google Analytics off, and stay on the Spark plan.
   - Turning products on below enables their APIs, so nothing needs enabling by hand.
   - Use a new project rather than one that belongs to another game: another game's CI may overwrite its database rules, and its consent screen carries that game's name.
   - Sharing one project with Pooket Tabks would give players one account across both games, but it means coordinating database rules. Decide that later.
2. **Project settings → Your apps → Add app → Web.** Skip Hosting. Note the **`apiKey`**.
3. **Security → Authentication → Sign-in method → Google → Enable.**
   - Set the public-facing name ("Pooket Slug") and a support email.
   - Under *Web SDK configuration*, copy the **Web client ID**.
4. **Authentication → Settings → Authorized domains:** add `jhiggins-tech.github.io`.
5. **Databases & Storage → Realtime Database → Create database.**
   - Pick a region close to your players, for example `asia-southeast1` or `us-central1`.
   - Start in **Locked mode**.
   - Copy the **database URL**.
6. **Realtime Database → Rules:** paste the spike's `database.rules.json` once it's written, then **Publish**. Until then, locked mode denies everything, which is safe.
7. **Google Cloud console → Google Auth Platform → Clients → "Web client (auto created by Google Service)" → Authorized JavaScript origins:**
   - add `https://jhiggins-tech.github.io`
   - optionally, for local testing, add `http://localhost` and `http://localhost:<port>`.
   - No redirect URIs are needed. Use this auto-created client rather than making a new one.
8. **Google Auth Platform:**
   - **Branding:** app name ("Pooket Slug") and support email.
   - **Audience:** External. Leaving it in *Testing* is fine, because only the basic `openid email profile` scopes are used.
   - **Data access:** add no scopes.
   - The app name and logo only appear on Google's consent screen after brand verification. Skip that for the spike.
9. **Google Cloud console → APIs & Services → Credentials → "Browser key (auto created by Firebase)":**
   - **Application restriction:** websites, `https://jhiggins-tech.github.io/*`
   - **API restrictions:** Identity Toolkit API and Token Service API. Database calls authenticate with the user's token (`?auth=`), not this key.

## Values to hand back

These are public identifiers, so add them as repository **variables**, not secrets: Settings → Secrets and variables → Actions → **Variables** tab.

| Variable | From step | Used for |
|---|---|---|
| `GOOGLE_CLIENT_ID` | 3 (Web client ID) | Google's sign-in button |
| `FIREBASE_API_KEY` | 2 (`apiKey`) | `signInWithIdp` and token refresh |
| `FIREBASE_DB_URL` | 5 (database URL) | Saving and loading progress |
| `FIREBASE_PROJECT_ID` | 1 (optional) | Diagnostics |

The workflow will pass each one as its own `customParameters` flag. They're kept separate because GameCI splits `customParameters` on spaces, which would break a JSON blob.

The only secret comes later, and only if CI deploys the database rules: `FIREBASE_SERVICE_ACCOUNT`.
