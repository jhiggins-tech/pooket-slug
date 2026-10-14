# Getting the Photon SDK into CI

How a public repo builds with a private, license-restricted SDK. CI downloads the SDK from a private release, verifies it and unpacks it without Unity. This doc also covers what Fusion's editor scripts do on the first import in a headless build.

**Read this when** you're upgrading Fusion, adding another private SDK or paid asset, setting up a fresh clone, or the build log says the weaver didn't compile.

Up: [Netcode](README.md)

## The constraint

Photon's license forbids redistributing the SDK ([licensing](choosing-a-stack.md#free-tier-and-licensing)), and this repository is public. So the SDK must never be committed. Only the compiled game is published.

## Options considered

| Option | Verdict |
|---|---|
| Make the repo private and commit the SDK (Photon's normal setup) | Pages from a private repo needs a paid plan, or a separate public "site" repo |
| A private git submodule at `Assets/Photon/` | Works, but Fusion's per-project config would live in the private repo |
| A private Unity package (GameCI supports `gitPrivateToken`) | Fusion expects to live in `Assets/Photon` and write its config there, and packages are read-only |
| CI downloads from Photon directly | **Blocked.** The download URL returned 403 from Photon's bot protection, and downloads need a login |
| Ask Photon for an exception | Slow and uncertain |
| **A private release asset, fetched with a token** | **Chosen.** The SDK isn't in any branch's history, there's one fetch step, and a checksum pins the exact file |

## How it works

1. **The SDK is stored once.** A person downloads `Photon-Fusion-2.1.3-Stable-2390.unitypackage` (6.4 MB) while logged in to Photon, and attaches it to release `fusion-2.1.3` of the private repo `jhiggins-tech/pooket-slug-vendor`. Secrets can't hold it: GitHub caps a secret at 48 KB.
2. **CI gets a read-only token.** `VENDOR_PAT` is a fine-grained token with access to that one repo and **Contents: Read-only** ([secrets](../build-pipeline/workflow.md#secrets)). The workflow's default `GITHUB_TOKEN` can't read other private repos.
3. **The workflow fetches, verifies and unpacks it before Unity starts:**

   ```yaml
   - name: Fetch Photon Fusion SDK
     env:
       GH_TOKEN: ${{ secrets.VENDOR_PAT }}
       FUSION_TAG: fusion-2.1.3
       FUSION_FILE: Photon-Fusion-2.1.3-Stable-2390.unitypackage
       FUSION_SHA256: 375e21bc771ce5f7cea942b5f804e7c28c8f4d858f7e9aab96725214564bc52e
     run: |
       if [ -z "$GH_TOKEN" ]; then echo "::error::Repository secret VENDOR_PAT is not set …"; exit 1; fi
       gh release download "$FUSION_TAG" -R jhiggins-tech/pooket-slug-vendor -p "$FUSION_FILE" -D vendor
       echo "$FUSION_SHA256  vendor/$FUSION_FILE" | sha256sum -c -
       python3 -I tools/unpack_unitypackage.py "vendor/$FUSION_FILE" .
       git checkout -- Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion   # our config wins
   ```

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
{ "AssembliesToWeave": ["Assembly-CSharp", "Assembly-CSharp-firstpass"] }
```

- **The IL weaver reads this file at compile time** to decide which assemblies to weave. It's JSON, read with `EditorJsonUtility.FromJsonOverwrite`, so every other setting takes Fusion's default.
- **If the networking code moves into its own assembly definition** ([roadmap](../roadmap/refactor-candidates.md)), add that assembly's name here.
- **We don't commit `PhotonAppSettings.asset`.** The AppId reaches the game through our own `Resources/pooket-net.json` instead (below).

## Fusion's defines and the IL weaver

**The expected problem.**
- Fusion's weaver, `Unity.Fusion.CodeGen`, only compiles when the `FUSION_WEAVER` scripting define is set.
- Fusion's `[InitializeOnLoad]` installer only adds that define after the editor has loaded and compiled once.
- On a fresh CI checkout, ProjectSettings are regenerated with no defines. So the first compile would build no weaver, and networked properties and RPCs would ship un-woven.
- We planned a warm-up run, then committing the ProjectSettings it generated.

**What actually happens:** none of that was needed. The first build log (run 38531767207) shows:

```text
[ScriptCompilation] Requested script compilation because: Define symbols changed
[FusionEditor/Installer] Updating scripting define symbols for 'WebGL' | added: FUSION_WEAVER, FUSION2, FUSION_2,
    FUSION_2_1, FUSION_2_1_3, FUSION_2_OR_NEWER, FUSION_2_0_OR_NEWER, FUSION_2_1_OR_NEWER, FUSION_LOGLEVEL_INFO
Csc Library/Bee/artifacts/2000b0aE.dag/Unity.Fusion.CodeGen.dll
ILPostProcess Library/Bee/artifacts/2000b0aE.dag/post-processed/Assembly-CSharp.dll (+pdb)
Reloading assemblies after forced synchronous recompile.
…                                              (then -executeMethod runs our build method)
ILPostProcess Library/Bee/artifacts/2000b0aP.dag/post-processed/Assembly-CSharp.dll (+pdb)   ← WebGL player compile
[PooketSlug] Fusion SDK present: True; IL weaver compiled: True; co-op code compiled: True; WebGL defines: '…'
```

So:
1. **Batch-mode Unity recompiles straight away.** When the installer changes the defines during the initial import, Unity does a *forced synchronous recompile* before running `-executeMethod`, and that recompile builds the weaver.
2. **Both compiles get woven.** The editor compile and the WebGL player compile are IL-post-processed.
3. **The project stays code-only.** There's no ProjectSettings commit and no warm-up run. Every build repeats the define change, which costs one extra script compile, about 7 s.
4. **The build log checks it every time.** The build script's `[PooketSlug]` line reports `IL weaver compiled: True/False` on every build.

What else the installer does:
- **It forces `PlayerSettings.runInBackground = true`.**
- **It adds defines for every build target,** Standalone and Android included, not just WebGL.
- **It can modify the package manifest mid-build.** If `Packages/manifest.json` doesn't contain the text `nuget.mono-cecil`, it calls `Client.Add("com.unity.nuget.mono-cecil@1.10.2")`, which changes packages mid-build. We list `com.unity.nuget.mono-cecil` 1.11.6, the version bundled with the editor, to prevent that.

**Engine modules.** The SDK needs engine modules the solo game didn't: `ai`, `animation`, `imageconversion`, `physics`, `physics2d` and `unitywebrequest`. We found each one by compiling the SDK locally against only the enabled modules ([table](../build-pipeline/unity-project.md#packages-and-engine-modules), [method](../verification/type-check.md#emulating-disabled-modules)).

## The AppId

- **The AppId reaches the game through a JSON file the build writes.** The workflow passes `-photonAppId ${{ secrets.PHOTON_APP_ID }}` (and optionally `-photonRegion`) in `customParameters`. The build script writes `Assets/Resources/pooket-net.json`, which [`NetConfig.cs`](../../../Assets/Scripts/Net/NetConfig.cs) loads at runtime.
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

1. Get the package, either from Photon or with `gh release download fusion-2.1.3 -R jhiggins-tech/pooket-slug-vendor`. Then either import it in the editor, or run `python3 -I tools/unpack_unitypackage.py <package> .`.
2. To play co-op from the editor, create `Assets/Resources/pooket-net.json` containing `{"appId": "<your Fusion AppId>", "region": ""}`. Git ignores it.
3. Without the SDK, the project compiles and plays solo. The Fusion code is behind `#if FUSION2`.
