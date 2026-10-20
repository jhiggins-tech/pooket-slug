# Photon SDK in CI: decisions and the first build

This is a record, not instructions. The live version is [SDK in CI](../netcode/sdk-in-ci.md).

Up: [Research index](../README.md)

## Options considered

| Option | Verdict |
|---|---|
| Make the repo private and commit the SDK (Photon's normal setup) | Pages from a private repo needs a paid plan, or a separate public "site" repo |
| A private git submodule at `Assets/Photon/` | Works, but Fusion's per-project config would live in the private repo |
| A private Unity package (GameCI supports `gitPrivateToken`) | Fusion expects to live in `Assets/Photon` and write its config there, and packages are read-only |
| CI downloads from Photon directly | **Blocked.** The download URL returned 403 from Photon's bot protection, and downloads need a login |
| Ask Photon for an exception | Slow and uncertain |
| **A private release asset, fetched with a token** | **Chosen.** The SDK isn't in any branch's history, there's one fetch step, and a checksum pins the exact file |


## The first build log

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
