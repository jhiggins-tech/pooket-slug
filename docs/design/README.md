# Pooket Slug design: the sign-off documents

The game's design: four documents written to be read through and signed off, plus the crew's kits. This folder is the one source for the design. It drew on three sources:
- the owner's brainstorm in [DESIGN.md](../../DESIGN.md), frozen on 2026-10-15 as a historical record (the Roguelite, and what was considered and not picked)
- the owner's MoSCoW pass over the [feature list](../research/roadmap/feature-candidates.csv)
- the owner's answers to the documents' questions, on 2026-10-14

Each document ends with what's still open.

**Read this when** you're signing off the design, or need the big picture before building part of the game.

Up: [Repository README](../../README.md)

| Document | What it covers | Status |
|---|---|---|
| [Story, plot and cutscenes](story.md) | The premise, the plot mission by mission, and how cutscenes and reveals play | Signed off, 2026-10-14 |
| [Tanks](tanks.md) | Tank bosses, winning a tank, driving one and calling it in | Signed off, 2026-10-14 |
| [Player mechanics](player.md) | The crew, controls, kits, health and lives, crates, co-op, and what the player keeps | Signed off, 2026-10-14 |
| [Maps, missions and progression](levels.md) | What a mission is, difficulty, how maps are built, and the order things open up in, menus included | Signed off, 2026-10-14 |
| [The crew's kits](kits.md) | Every soldier's weapons, forks and abilities | Moved from DESIGN.md, 2026-10-15 |
| [Team supers](team-supers.md) | The combo attack for each pair of soldiers in co-op | Draft, 2026-10-15 |
| [Music](music.md) | Every track and sting, the style, and how the chip synth makes them | Draft, 2026-10-15 |

## Progressive disclosure, the thread through all four

The game shows only what the player has already met. Nothing on screen, in the menus or on the web page hints at what's still to come, and each new thing gets a moment of its own:
- **Tanks** aren't mentioned until the first one rolls out at the end of Mission one ([tanks](tanks.md#discovering-tanks)).
- **Driving a tank** stays secret until the first chance to climb in ([tanks](tanks.md#discovering-tanks)).
- **Co-op, and online play of any kind,** appear only once Mission one has been beaten solo, when the menu changes ([progression](levels.md#what-the-player-sees-and-when)).
- **Smaller things** follow the same idea: buttons, the crew screen, ranks and XP ([player](player.md#discovering-the-kit)).

## After sign-off

All four were signed off on 2026-10-14, on a [sign-off page](https://claude.ai/artifact/SuFxdReB83W6eM4T3VvYwv) (private to the owner). They're now the reference for building the game. On 2026-10-15 the kits moved here from DESIGN.md, which is now frozen as history; where it differs, these documents win. Pooket Tabks is only a thematic link: Slug's design doesn't depend on Tabks' code or docs.
