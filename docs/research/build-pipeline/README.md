# Build pipeline

How a push becomes a playable page. This overview fits on one screen; the linked documents have the detail.

**Read this when** you're changing CI, the Unity build or the hosting, or a build has gone red.

## The flow

```text
git push to main or claude/**        (pushes that only touch *.md or docs/** are skipped)
   │
   ▼
GitHub Actions: .github/workflows/webgl-pages.yml   (two jobs on ubuntu-latest)
   1. Free disk space                ~1 min; the Unity editor image is ~7 GB
   2. Checkout
   3. Fetch Photon Fusion SDK        private release → SHA-256 check → unpack into Assets/Photon/
   4. Restore the Library cache
   5. game-ci/unity-test-runner      v4.3.2: EditMode tests; a failure stops here
   6. game-ci/unity-builder@v4       Docker image unityci/editor:ubuntu-6000.3.26f1-webgl-3
        └─ Unity -executeMethod PooketSlug.EditorTools.WebGLBuild.Build
             bake the Photon AppId, create the scene, set PlayerSettings, BuildPlayer(WebGL)
   7. Upload the build as an artifact
   8. Push build/WebGL/WebGL to the gh-pages branch (orphan commit)
   then, in a second job:
   9. Two-player co-op test          two headless browsers against the new build
   │
   ▼
GitHub Pages serves gh-pages at https://jhiggins-tech.github.io/pooket-slug/
```

A push to `claude/**` or `main` is live in about 6 to 8 minutes. See [numbers](numbers.md).

## Key decisions

| Decision | Why | Detail |
|---|---|---|
| GameCI `unity-builder@v4` with a custom `buildMethod` | It worked first time with a Personal license, and one editor script controls the whole build | [Workflow](workflow.md#6-the-build-step) |
| EditMode tests before the build | A failing test stops the job, so a broken build never deploys | [EditMode tests](../verification/edit-mode-tests.md) |
| A code-only Unity project | No hand-authored Unity YAML; scenes, art and sound are generated | [Unity project](unity-project.md#a-code-only-project) |
| WebGL compression disabled | Pages can't send `Content-Encoding` for Unity's pre-compressed files, and it gzips responses itself | [GitHub Pages](github-pages.md#compression) |
| Deploy through a `gh-pages` branch | Lets a feature branch deploy without changing environment protection rules | [GitHub Pages](github-pages.md#deploying) |
| Photon SDK fetched at build time | Photon's license forbids committing it to this public repo | [SDK in CI](../netcode/sdk-in-ci.md) |

## Documents

- [workflow.md](workflow.md): every workflow step and why it's there, plus secrets, caching, concurrency and reading CI logs.
- [unity-project.md](unity-project.md): the Unity version and image, the code-only project, packages and modules, input handling and `WebGLBuild.cs`.
- [github-pages.md](github-pages.md): deploying, compression, the WebGL template, and what browsers download.
- [numbers.md](numbers.md): build times and download sizes for each measured run, before and after Fusion.

Source files:
- [`.github/workflows/webgl-pages.yml`](../../../.github/workflows/webgl-pages.yml)
- [`Assets/Editor/WebGLBuild.cs`](../../../Assets/Editor/WebGLBuild.cs)
- [`Assets/WebGLTemplates/PooketSlug/index.html`](../../../Assets/WebGLTemplates/PooketSlug/index.html)
- [`Packages/manifest.json`](../../../Packages/manifest.json)
- [`tools/unpack_unitypackage.py`](../../../tools/unpack_unitypackage.py)
