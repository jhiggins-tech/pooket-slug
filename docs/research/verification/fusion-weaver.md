# Running Fusion's IL weaver outside Unity

Fusion's networked properties and RPCs only work once its IL weaver has rewritten the compiled code, and the weaver has rules the C# compiler doesn't check. We run the real weaver, from the SDK, on our compiled assembly without Unity. Its errors then show up in seconds instead of in a CI run.

**Read this when** `check.sh` fails in the weaver step, you've added or changed a `NetworkBehaviour`, an `[Networked]` property, an `INetworkStruct` or an RPC, or upgraded Fusion.

Up: [Verification](README.md)

## How Unity runs the weaver

- **Fusion ships the weaver as source:** `Assets/Photon/Fusion/CodeGen`, an asmdef named `Unity.Fusion.CodeGen`, compiled only when `FUSION_WEAVER` is defined. It contains an `ILPostProcessor`, Unity's hook for rewriting compiled assemblies.
- **For each compiled assembly, Unity's pipeline:**
  1. builds an `ICompiledAssembly` (name, PE and PDB bytes, reference paths, defines)
  2. calls `WillProcess`, then `Process`
  3. uses the returned assembly and diagnostics.
- **Which assemblies get woven:**
  - Fusion's `ILWeaverBindings` reads `AssembliesToWeave` from the project's `*.fusion` config, which is why our [`NetworkProjectConfig.fusion`](../../../Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion) is committed.
  - It looks for the config at its default path, then `Temp/FusionILWeaverConfigPath.txt`, then by searching `Assets/**/*.fusion`.
  - It weaves only listed assemblies that reference Fusion.

## The harness

[`WeaveHarness.cs`](tools/WeaveHarness.cs) is about 70 lines. It:
1. Loads `Unity.Fusion.CodeGen.dll` (built by [`compile_unity.py`](tools/compile_unity.py)'s `sdk` stage).
2. Finds the `ILPostProcessor` subclass and creates it by reflection.
3. Hands it a minimal `ICompiledAssembly` built from the assembly to weave (`.dll` + `.pdb`), its reference list and its defines. That's the netcode's own assembly, `PooketSlug.Net`, by default; `ASSEMBLY=Assembly-CSharp weave.sh …` picks another.
4. Prints every diagnostic, writes the woven assembly, and exits non-zero on errors.

It runs on the **.NET 6 runtime in the Unity kit** (the editor's own `NetCoreRuntime`). An `AssemblyLoadContext` resolver finds the Fusion, Cecil and Unity DLLs.

[`weave.sh`](tools/weave.sh) builds and runs it. [`tools/check.sh`](../../../tools/check.sh) calls it after the compile. To see what the weaver generated, run it by hand with `DUMP="PlayerNet WorldNet"` (see its header comment).

Example output with `DUMP` set:

```text
Fusion.CodeGen.ILWeaverBindings.WillProcess(PooketSlug.Net) = True
woven assembly -> …/PooketSlug.Net.woven.dll (32768 bytes)
== PooketSlug.PlayerNet : NetworkBehaviour
   [NetworkBehaviourWeavedAttribute(8)]
   method RPC_Hurt@Invoker1732266735 (27 IL) [NetworkRpcWeavedInvokerAttribute] [PreserveAttribute] [WeaverGeneratedAttribute]
   NetPos getter IL: ldarg.0 | ldfld | ldc.i4.0 | add | ldobj | ret
== PooketSlug.WorldNet : NetworkBehaviour
   [NetworkBehaviourWeavedAttribute(151)]
   …
```

## Reading the result

- **No diagnostics** means the weaver accepted every networked type.
- **`[NetworkBehaviourWeavedAttribute(n)]`** is the behaviour's state size in 4-byte words. That's 8 for `PlayerNet`, and 151 for `WorldNet`: 24 enemies × 6 words + 7. `[NetworkStructWeavedAttribute(n)]` is the same for structs, 6 for `NetEnemy`. Each field takes at least a word, so a `byte` costs as much as an `int`.
- **Every RPC gets an `…@Invoker…` method** marked `[Preserve]`, so IL2CPP stripping keeps it.
- **Woven property getters read from Fusion's state buffer** (`ldfld` of the buffer pointer + offset) instead of a backing field.

## Pitfalls

- **`Unity.CompilationPipeline.Common.dll` must sit next to the harness.** The runtime resolves it while compiling `Main`, before the harness can install its own resolver. Without it you get `FileNotFoundException … Unity.CompilationPipeline.Common, Version=2022.2.0.1`.
- **Run from the project root.** The weaver locates the `.fusion` config with paths relative to the working directory.
- **Pass a PDB.** Compile with `-debug:portable`; the weaver reads and writes symbols.
- **The weaver also runs in CI,** in Unity's own pipeline. Confirm it with the build log's `IL weaver compiled: True` ([why that's not automatic](../netcode/sdk-in-ci.md#fusions-defines-and-the-il-weaver)).
