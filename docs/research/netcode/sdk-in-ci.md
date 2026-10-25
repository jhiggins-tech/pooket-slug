# Getting the Photon SDK into CI

How a public repo builds with a private, license-restricted SDK. CI downloads the SDK from a private release, verifies it and unpacks it without Unity. This doc also covers what Fusion's editor scripts do on the first import in a headless build.

**Read this when** you're upgrading Fusion, adding another private SDK or paid asset, setting up a fresh clone, or the build log says the weaver didn't compile.

Up: [Netcode](README.md)

## The constraint

Photon's license forbids redistributing the SDK, and this repository is public. So the SDK must never be committed. Only the compiled game is published.

The options weighed (a private repo, a submodule, a private package, a direct download) and why a private release asset won are in [history](../history/sdk-in-ci-decisions.md).

## How it works

1. **The SDK is stored once.** A person downloads the `.unitypackage` (6.4 MB) while logged in to Photon, and attaches it to a release of the private repo `jhiggins-tech/pooket-slug-vendor`. Secrets can't hold it: GitHub caps a secret at 48 KB.
2. **CI gets a read-only token.** `VENDOR_PAT` is a fine-grained token with access to that one repo and **Contents: Read-only** ([secrets](../build-pipeline/workflow.md#secrets)). The workflow's default `GITHUB_TOKEN` can't read other private repos.
3. **The workflow fetches, verifies and unpacks it before Unity starts**, in the composite action [`.github/actions/unity-project/action.yml`](../../../.github/actions/unity-project/action.yml), which is the owner of the tag, file name and SHA-256. It fails by name when `VENDOR_PAT` is unset, downloads with `gh release download`, checks the checksum, unpacks, and restores our committed config over the package's.

4. **Unpacking needs no Unity.** A `.unitypackage` is a gzipped tar of `<guid>/pathname`, `<guid>/asset` and `<guid>/asset.meta` entries. [`tools/unpack_unitypackage.py`](../../../tools/unpack_unitypackage.py), about 50 lines of Python using `tarfile`:
   - writes each asset and its `.meta` where the editor's importer would put it, with the same GUIDs
   - refuses any path outside `Assets/` or `Packages/`
   - unpacked **241 files** (301 entries) from this package.

   Doing this before Unity starts means the editor just sees a normal project. Unity's own `-importPackage` flag would mix an import, and its recompile, into the build run.
5. **Git ignores the SDK.** `.gitignore` ignores `Assets/Photon/` except our one config file:

   ```gitignore
   /vendor/
   /Assets/Photon.meta
   /Assets/Photon/*
   !/Assets/Photon/Fusion/
   /Assets/Photon/Fusion/*
   !/Assets/Photon/Fusion/Resources/
   /Assets/Photon/Fusion/Resources/*
   !/Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion
   ```

The package also contains nested `.unitypackage` files (`FusionDemos/`, `FusionMenu/`). Unity treats them as plain files, so they're never imported or compiled.

## Our committed Fusion config

[`NetworkProjectConfig.fusion`](../../../Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion) contains only:

```json
{ "AssembliesToWeave": ["PooketSlug.Net", "Assembly-CSharp", "Assembly-CSharp-firstpass"] }
```

- **The IL weaver reads this file at compile time** to decide which assemblies to weave. It's JSON, read with `EditorJsonUtility.FromJsonOverwrite`, so every other setting takes Fusion's default.
- **The netcode is its own assembly, `PooketSlug.Net`,** so it's listed first. `Assembly-CSharp` has no network code left, and stays listed in case some arrives there.
- **We don't commit `PhotonAppSettings.asset`.** The AppId reaches the game through our own `Resources/pooket-net.json` instead (below).

## Fusion's defines and the IL weaver

Fusion's weaver (`Unity.Fusion.CodeGen`) only compiles when the `FUSION_WEAVER` define is set, and Fusion's installer only adds it once the editor has loaded. On a fresh checkout that looked like it needed a warm-up run and committed ProjectSettings. It doesn't ([the log that showed it](../history/sdk-in-ci-decisions.md#the-first-build-log)). In practice:

1. **Batch-mode Unity recompiles straight away.** When the installer changes the defines during the initial import, Unity does a *forced synchronous recompile* before running `-executeMethod`, and that recompile builds the weaver.
2. **Both compiles get woven.** The editor compile and the WebGL player compile are IL-post-processed.
3. **No ProjectSettings are committed, and there's no warm-up run.** Every build repeats the define change, which costs one extra script compile, about 7 s.
4. **The build log checks it every time.** The build script's `[PooketSlug]` line reports `IL weaver compiled: True/False` on every build.

What else the installer does:
- **It forces `PlayerSettings.runInBackground = true`.**
- **It adds defines for every build target,** Standalone and Android included, not just WebGL.
- **It can modify the package manifest mid-build.** If `Packages/manifest.json` doesn't contain the text `nuget.mono-cecil`, it calls `Client.Add("com.unity.nuget.mono-cecil@1.10.2")`, which changes packages mid-build. We list `com.unity.nuget.mono-cecil` 1.11.6, the version bundled with the editor, to prevent that.

**Engine modules.** The SDK needs engine modules the solo game didn't ([table](../build-pipeline/unity-project.md#packages-and-engine-modules), [how they were found](../verification/type-check.md#emulating-disabled-modules)).

## The AppId

- **The AppId reaches the game through a JSON file the build writes.** The workflow passes `-photonAppId ${{ secrets.PHOTON_APP_ID }}` (and optionally `-photonRegion`) in `customParameters`. The build script writes `Assets/Resources/pooket-net.json`, which `NetSession.cs` loads at runtime (`NetSession.Config`; Core's `NetConfig` holds its shape and the region rule).
- **The session copies it into Fusion's settings.** [`NetSession`](../../../Assets/Scripts/Net/NetSession.cs) copies Fusion's global app settings (when they exist), sets `AppIdFusion`, `AppVersion` and `FixedRegion`, and passes them to `StartGame` as `CustomPhotonAppSettings`.
- **Without an AppId the game still builds.** The title screen says *Online co-op is off in this build (no Photon AppId)*.

## Upgrading the SDK

1. Download the new `.unitypackage` from Photon, logged in. Read its release notes for weaver or API changes.
2. Create a new release in `pooket-slug-vendor`, for example `fusion-2.1.4`, and attach the package.
3. In the workflow, update `FUSION_TAG`, `FUSION_FILE` and `FUSION_SHA256` (`sha256sum <file>`).
4. Locally:
   1. Unpack the new package.
   2. Re-run the [type-check](../verification/type-check.md) and the [weaver harness](../verification/fusion-weaver.md).
   3. Check whether the new version needs more engine modules, then push.
5. Afterwards, check the build log for `IL weaver compiled: True` and the new `FUSION_2_x_y` define, then smoke-test co-op.

## Local development

`tools/check.sh` fetches and unpacks the SDK itself ([verification](../verification/README.md)). To play co-op from the editor, create `Assets/Resources/pooket-net.json` containing `{"appId": "<your Fusion AppId>", "region": ""}` (git ignores it). Without the SDK, the project compiles and plays solo.
