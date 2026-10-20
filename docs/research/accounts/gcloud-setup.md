# Google Cloud / Firebase setup for sign-in

The one-time console steps for [sign-in](README.md), done on 2026-10-14. The setup is to reuse the Firebase project of the owner's game Cardmaster, which already has Google sign-in and a Realtime Database. Players see "Cardmaster" on Google's consent screen. The owner accepted that, since this game is for friends. Console labels are as of October 2026, so they may drift.

**Read this when** you're setting up, or checking, the Firebase side of sign-in.

Up: [Accounts](README.md)

## The setup: reuse the Cardmaster project

All four steps are in the **Cardmaster** Firebase and Google Cloud project. Steps 1–3 each *add* this game's origin, and don't affect Cardmaster itself. Step 4 is a check: the publishing status and test users apply to both games.

1. **Firebase console → Authentication → Settings → Authorized domains:** add `jhiggins-tech.github.io`.
2. **Google Cloud console → Google Auth Platform → Clients →** the web client whose ID Cardmaster uses (its `GOOGLE_CLIENT_ID`) **→ Authorized JavaScript origins:** add `https://jhiggins-tech.github.io`. No redirect URIs are needed.
3. **Google Cloud console → APIs & Services → Credentials →** Cardmaster's browser API key (its `apiKey`): if it has **website restrictions**, add `https://jhiggins-tech.github.io/*`. If it's unrestricted, leave it.
4. **Google Cloud console → Google Auth Platform → Audience:** check the publishing status. If it's **Testing**, only the test users listed there can sign in, to either game; anyone else sees "Access blocked". Either add each friend as a test user (Google allows up to 100) or publish the app.

**No database console step.**
- Cardmaster's CI deploys the database rules from Cardmaster's own repository on every push to its `main`, so rules typed into the console would be overwritten.
- This game's rules go into that rules file instead, under their own top-level key (`pooketSlug`), with tests in its rules test suite ([draft](database-rules.md)). They arrive as a pull request for the owner to merge, and Cardmaster's CI deploys them.
- The database root stays locked, so until the rules are deployed, this game's saves are simply denied.

## Values to hand back

These are public identifiers: they're already in Cardmaster's shipped web bundle. Copy the same values Cardmaster uses (its `src/net/config.ts`) into **repository variables**, not secrets, on `pooket-slug`: Settings → Secrets and variables → Actions → **Variables** tab.

| Variable | Cardmaster's value | Used for |
|---|---|---|
| `GOOGLE_CLIENT_ID` | `GOOGLE_CLIENT_ID` | Google's sign-in button |
| `FIREBASE_API_KEY` | `FIREBASE.apiKey` | Signing in to Firebase and token refresh |
| `FIREBASE_DB_URL` | `FIREBASE.databaseURL` | Saving and loading progress |

The workflow passes each one as its own `customParameters` flag. They're kept separate because GameCI splits `customParameters` on spaces, which would break a JSON blob.

## Trade-offs of sharing

- **Players get one account across Cardmaster and this game,** with the same Firebase user ID. That's a plus if the games ever share anything.
- **Changing this game's rules means a Cardmaster pull request,** deployed by Cardmaster's CI. This game's rules section must stay in that file. If a Cardmaster change dropped it, this game's saves would be denied, not exposed (fails safe).
- **Both games' data and free-plan limits are shared:** Spark gives 1 GB stored, 10 GB a month downloaded and 100 simultaneous connections. A save here is about one small JSON record per player, written at the end of a run, so this is fine for friends.
- **Shared blast radius.** A mistake in either game's rules could affect the other game's data. Keep the data namespaced under `pooketSlug/` and covered by the rules tests.
- **Branding:** Google's consent screen and account chooser name Cardmaster.

A separate Firebase project was the other option. The owner chose to share Cardmaster's; the steps for a separate one are in [history](../history/accounts-decisions.md#alternative-a-separate-firebase-project).
