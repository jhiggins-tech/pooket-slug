# Build pipeline

How a push becomes a playable page. This overview fits on one screen; the linked documents have the detail.

**Read this when** you're changing CI, the Unity build or the hosting, or a build has gone red.

## The flow

```text
git push to main or claude/**        (pushes that only touch *.md or docs/** are skipped)
   │
   ▼
GitHub Actions: .github/workflows/webgl-pages.yml   (jobs on ubuntu-24.04)
   plan ─┬─ scenes                   the scene check
         ├─ tests                    EditMode, then PlayMode (game-ci/unity-test-runner v4.3.2)
         └─ build                    game-ci/unity-builder@v4.8.2, image unityci/editor:ubuntu-6000.3.26f1-webgl-3
              │                        └─ Unity -executeMethod PooketSlug.EditorTools.WebGLBuild.Build
              ├─ touch               touch play on an emulated phone
              └─ demos               the kit screen's clips (only when the crew changes)
   build-and-deploy                  once scenes, tests, build and touch pass: the build + demos, sandbox and scenes
                                     as PooketSlug-WebGL; a preview deploys to previews/<branch>/ now
   coop-test (7 scenarios, 3 at once) then, on the main line, deploy-live to the site root
   (tests and build each get the project ready the same way: Docker on the second disk, the Photon SDK, the vendor
   sounds, the scenes, the Library cache; .github/actions/unity-project)
   │
   ▼
GitHub Pages serves gh-pages at https://jhiggins-tech.github.io/pooket-slug/
```

A preview is up about 12 minutes after a push; a main-line push is live in about 20, once the co-op tests pass (ci-faster, 20 Oct: 15 and 23 before).

## Key decisions

| Decision | Why | Detail |
|---|---|---|
| GameCI `unity-builder@v4` with a custom `buildMethod` | It worked first time with a Personal license, and one editor script controls the whole build | [Workflow](workflow.md#6-the-build-step) |
| Unity tests beside the build | They run at once, and a failing test stops the deploy, so a broken build never goes up | [EditMode tests](../verification/edit-mode-tests.md) |
| A code-only Unity project | No hand-authored Unity YAML; scenes, art and sound are generated | [Unity project](unity-project.md#a-code-only-project) |
| WebGL compression disabled | Pages can't send `Content-Encoding` for Unity's pre-compressed files, and it gzips responses itself | [GitHub Pages](github-pages.md#compression) |
| Deploy through a `gh-pages` branch | Lets a feature branch deploy without changing environment protection rules | [GitHub Pages](github-pages.md#deploying) |
| Photon SDK fetched at build time | Photon's license forbids committing it to this public repo | [SDK in CI](../netcode/sdk-in-ci.md) |

## Documents

- [workflow.md](workflow.md): every workflow step and why it's there, plus secrets, caching, concurrency and reading CI logs.
- [unity-project.md](unity-project.md): the Unity version and image, the code-only project, packages and modules, input handling and `WebGLBuild.cs`.
- [github-pages.md](github-pages.md): deploying, compression, the WebGL template, and what browsers download.

Source files:
- [`.github/workflows/webgl-pages.yml`](../../../.github/workflows/webgl-pages.yml)
- [`Assets/Editor/WebGLBuild.cs`](../../../Assets/Editor/WebGLBuild.cs)
- [`Assets/WebGLTemplates/PooketSlug/index.html`](../../../Assets/WebGLTemplates/PooketSlug/index.html)
- [`Packages/manifest.json`](../../../Packages/manifest.json)
- [`tools/unpack_unitypackage.py`](../../../tools/unpack_unitypackage.py)
