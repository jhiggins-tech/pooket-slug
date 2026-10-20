# Pooket Slug

A Metal Slug-style run-and-gun starring the crew from Pooket Tabks, for phone browsers (landscape, touch; keyboard and gamepad work too). Unity 6.3 WebGL, built by GameCI and hosted on GitHub Pages.

**Play:** https://jhiggins-tech.github.io/pooket-slug/

Mission one is solo and teaches the game. Beating it opens the crew and online two-player co-op, which uses one global room, MATES (no room codes, hosting or quick match). Sign in with Google is optional and keeps your save in the cloud.

## For people and agents working on it

- [CLAUDE.md](CLAUDE.md): the working rules, the code map and the recipes. Start here.
- [docs/design](docs/design/README.md): the game design, the one source for what the game is. Controls are in [player.md](docs/design/player.md) and `Assets/Scripts/Core/Crew/Controls.cs`.
- [docs/research](docs/research/README.md): how the build pipeline, netcode, sign-in and checks work.
- [docs/scenes.md](docs/scenes.md): writing cutscenes.
- [docs/sandbox.md](docs/sandbox.md): the sandbox's setups.

The Photon Fusion SDK is licensed, so it is never committed: CI fetches it from a private repository.
