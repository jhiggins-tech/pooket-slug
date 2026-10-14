# Hosting on GitHub Pages

How the WebGL build is published and served: the `gh-pages` deploy, why compression is off, the page template, and what a visitor downloads.

**Read this when** you're changing how or where the build is published, or the page won't load.

Up: [Build pipeline](README.md)

## Deploying

- **The workflow pushes the build to the `gh-pages` branch** with `peaceiris/actions-gh-pages@v4`, using `publish_dir: build/WebGL/WebGL` and `force_orphan: true`. Each deploy is a single fresh commit with the message `Deploy <sha> from <branch>`.
- **One-time setting:** Settings → Pages → Build and deployment → Source: **Deploy from a branch** → `gh-pages` / `(root)`.
- **Why a branch and not `actions/deploy-pages`:** the official route deploys through the `github-pages` environment, which by default only accepts the default branch. The branch route let a `claude/**` feature branch deploy with no environment changes.
- **Timing:**
  - The first publish after switching the Pages source took a couple of minutes, and the site returned 404 until then.
  - After that, GitHub's own "pages build and deployment" job takes about 20 s per deploy.
- **Every successful build overwrites `gh-pages`,** whichever branch it came from. That's right for a spike. A team would want `main` at the root and branch previews in subfolders (`destination_dir` + `keep_files`). See [next steps](../next-steps.md).

## Compression

- **Pages can't set the `Content-Encoding` header** that Unity's pre-compressed `.gz`/`.br` files need.
- So the build script sets `PlayerSettings.WebGL.compressionFormat = Disabled` and `decompressionFallback = false`. Pages then gzips responses on the fly (`content-encoding: gzip`), and nothing has to be configured on the server.
- Brotli plus `decompressionFallback` would probably save only another 15–20%, at the cost of decompressing in JavaScript. It isn't worth it for now.
- Pages serves `.wasm` as `application/wasm`.
- Measured sizes are in [numbers](numbers.md#download-size).

## The WebGL template

[`Assets/WebGLTemplates/PooketSlug/index.html`](../../../Assets/WebGLTemplates/PooketSlug/index.html) wraps the canvas:
- a 16:9 stage, max 960 px wide. In a window too short for it, it narrows rather than being squashed: its width comes from the window's height, less 160 px for the header, toolbar and footer
- a loading bar and an error message
- a key legend and a fullscreen button
- `devicePixelRatio` capped at 2
- focus moving to the canvas on click

Template notes:
- **The template syntax is unchanged in Unity 6.3:** `{{{ VAR }}}` and `#if` / `#endif`.
- **CSS lines starting with `#`,** like `#stage { … }`, are not mistaken for directives. Unity's own templates do the same.
- **`codeUrl` is set unconditionally** instead of depending on `#if USE_WASM`.
- **The fixed 16:9 stage matters for co-op.** Both players see the same 20-unit-wide view, which the shared camera relies on ([architecture](../netcode/architecture.md#camera)).

## Files and caching

- **`nameFilesAsHashes` is off,** so the URLs are stable: `Build/WebGL.wasm`, `.data`, `.framework.js`, `.loader.js`.
- **`dataCaching` is on.** Unity stores `WebGL.data` in the browser's IndexedDB; its log says `[UnityCache] '…/WebGL.data' successfully downloaded and stored in the browser cache`.
- **The in-game version label shows which commit is live.** It's also in the page header, for example `0.2.0+791a155`.

## Gotchas

- **The deploy step needs `permissions: contents: write`.**
- **Browsers pause hidden tabs.** Unity WebGL runs on `requestAnimationFrame`, so a backgrounded tab stops the game, and in co-op it stops servicing the connection. See [sessions](../netcode/sessions.md#hidden-tabs).
