# Pooket Slug design: the sign-off documents

The game's design: four documents written to be read through and signed off, plus the crew's kits. This folder is the one source for the design. It drew on three sources:
- the owner's brainstorm of 14 Oct, whose Roguelite and ideas not picked are kept in [history](history.md)
- the owner's MoSCoW pass over an early feature list
- the owner's answers to the documents' questions, on 2026-10-14

Each document ends with what's still open.

**Read this when** you're signing off the design, or need the big picture before building part of the game.

Up: [Repository README](../../README.md)

| Document | What it covers | Status |
|---|---|---|
| [Story, plot and cutscenes](story.md) | The premise, the plot mission by mission, and how cutscenes and reveals play | Signed off, 2026-10-14 |
| [Tabks](tanks.md) | Tabk bosses, winning a tabk, driving one and calling it in | Signed off, 2026-10-14 |
| [Player mechanics](player.md) | The crew, controls, kits, health and lives, crates, co-op, and what the player keeps | Signed off, 2026-10-14 |
| [Maps, missions and progression](levels.md) | What a mission is, difficulty, how maps are built, and the order things open up in, menus included | Signed off, 2026-10-14 |
| [The crew's kits](kits.md) | Every soldier's weapons, forks and abilities | Moved from the brainstorm, 2026-10-15 |
| [Team supers](team-supers.md) | The combo attack for each pair of soldiers in co-op | Draft, 2026-10-15 |
| [The lobby](lobby.md) | The gaming den: a station per freed crewmate, their idle bits, and picking and readying up, solo and co-op | Approved, 2026-10-16 |
| [Generated art](art.md) | The look of generated art: the model, prompt and pixels for background plates | Picked, 2026-10-17 |
| [Music](music.md) | Every track and sting, the style, and how the chip synth makes them | Approved, 2026-10-15 |
| [History](history.md) | The Roguelite (on ice), and ideas considered and not picked | Kept from the first brainstorm |

## Canonical terms

Use these words in everything players or readers see: in-game text, voice lines, and these documents (owner, 2026-10-16). Code identifiers keep their old names (`PlayerTank`, `HoloLook` and so on), and so does the file name of [tanks.md](tanks.md).
- **Tabk**, never tank. The typo is on purpose, after Pooket Tabks: a tabk, tabks, TABK WRECKED!. The one exception is Pocket Tanks, the real game.
- **Mission one is single player only** (owner, 2026-10-16): the prelude. Before it's beaten the title screen is just START and SETTINGS, and START goes straight into it; beating it opens the den and co-op. It's never played with two, replays included: picking it on the den's mission board closes player two's seat and spectating.
- **Trollogram**, never hologram. kie's projections are Trollograms, from the raiders to Trollogram kie; in a character intro the title is TROLLO-KIE.

## Safe flashing

- **No strobing, anywhere** (owner, 2026-10-16, after a cutscene's static strobed on for its whole second half). Anything that flashes a large part of the screen (a white or red screen flash, the static's cyan, the boss intro's WARNING!) does it at most 2.5 times a second, under the 3 a second that can set off photosensitive seizures, and a glitch over the whole screen is one soft pulse that fades. Small things (a sprite blinking, a marker) can flash faster.

## Progressive disclosure, the thread through all four

The game shows only what the player has already met. Nothing on screen, in the menus or on the web page hints at what's still to come, and each new thing gets a moment of its own:
- **Tabks** aren't mentioned until the first one rolls out at the end of Mission one ([tabks](tanks.md#discovering-tabks)).
- **Calling in and driving a tabk** stay secret until Mission one's coda, when kie frees tones2's own ([tabks](tanks.md#discovering-tabks)).
- **Co-op, and online play of any kind,** appear only once Mission one has been beaten, when the menu changes ([progression](levels.md#what-the-player-sees-and-when)).
- **Smaller things** follow the same idea: buttons, the crew screen, ranks and XP ([player](player.md#discovering-the-kit)).

## After sign-off

All four were signed off on 2026-10-14, on a [sign-off page](https://claude.ai/artifact/SuFxdReB83W6eM4T3VvYwv) (private to the owner). They're now the reference for building the game. On 2026-10-15 the kits moved here from the brainstorm, and on 2026-10-16 its last parts became [history](history.md). Pooket Tabks is only a thematic link: Slug's design doesn't depend on Tabks' code or docs.
