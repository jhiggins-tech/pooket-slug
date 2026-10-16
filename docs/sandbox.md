# The sandbox

A link that sets up a map, goons, a boss and tuning, so a tuning tweak or a move video needs no build. It's solo
only, and a visit that opens a sandbox plays nothing else: nothing in it goes on the profile (no scores, XP, ranks
or "seen" marks), and MENU starts it again. Agreed in the workflow review, session 2 (16 Oct); code in
`Assets/Scripts/Core/Platform/Sandbox.cs` (the setup), `SetupText.cs` (the link reader, which cutscenes as data will reuse),
`Knobs.cs` (the tuning by name) and `Game.cs` (`?sandbox`).

## Two ways to open one

- **A file**: `?sandbox=<name>` plays `sandbox/<name>.txt`, published beside the game. Push a file in `sandbox/` and
  `sandbox-files.yml` puts it up in a couple of minutes with no build: on the main line beside the live game, on any
  other branch beside that branch's preview (`previews/<item-id>/?sandbox=<name>`). Names are lower case letters,
  digits, `-` and `_`.
- **The link itself**: `?sandbox=soldier kie;ground ..P....s....T;at boss`. Lines are split by `;`. In a link a `+`
  reads as a space and `#` starts the fragment, so use `ground` (not `map`) for the floor, and write a medkit's `+` as
  `%2B`.

`?sandbox` alone is tones2 on flat ground with a few goons. `?sandbox=knobs` logs every knob there is (the console,
or "Copy logs"), then plays that.

## Setup lines

One per line, `keyword what-follows` (the keyword ends at a space, `:` or `=`). Lines starting `//` are notes.

| Line | What it does |
|---|---|
| `soldier kie` | Who plays: `tones2` (the default) or `kie`. |
| `ground ..P...s...T` | One row of markers on flat ground, sky above (like the kit demos). Markers are `Level.cs`'s: `P` start, `s` rifleman, `r` knife runner, `d` drone, `T` the boss tabk, `H` heavy machine gun crate, `B` bomb crate, `+` medkit, `U` 1-up, `x` barrel, `=` sandbags, `-` bridge, `Z` live fence, `V` steam vent, `L` lift, `K` checkpoint. |
| `air ......d.....` | With `ground`: a row higher up, for drones. |
| `map <row>` | A whole map instead, one row per line, top first, `#` for ground (files only). |
| `mission 1` | Mission one's own map. |
| `at boss` | Start a screen short of the boss tabk. Also `at coda` (Mission one's coda, its gate open) or `at 120` (a column). |
| `boss kcaj` | The boss tabk is kcaj's. `boss pilot` skips to the pilot round; `boss weak-pilot` too, with a pilot who goes down quickly. |
| `cheat invincible super specials tank` | Any of: nothing hurts; the super stays charged; the special stays full; a tabk to call in, as often as you like. |
| `tune kit.superkills 6` | Turns a knob (below). |
| `keys 0.4 3.4 fire` | A scripted take: keys held from and to these seconds. Join keys with `+` or `,` (`fire+right`): `left right up down jump fire special super ability` (`sp su ab`). |
| `seconds 6` | How long a take runs before it starts again (a second after the last key, if not given). |

A line it can't follow is a problem: the start banner says "SANDBOX · 1 PROBLEM: LINE 3: ...", the log lists them
all, and the rest of the setup still plays.

## Knobs

A knob is a path of field names, any case:

- `physics`, `soldier`, `crates`, `lives`, `kit`, `impact`: `Tuning.cs`'s sections (`physics.gravity`,
  `lives.spare`, `kit.superkills`). The camera, co-op and HUD numbers are built in.
- `tones2`, `kie`, `kcaj`: a crewmate's kit numbers (`Tones2Tuning`, `KieTuning`, `KcajTuning`), e.g.
  `tones2.jet.ramptime`, `kie.trollogram.swapcooldown`.
- Anything in `Content.cs` by its name: soldiers (`tones2.runspeed`, `kie.specialrefill`), enemies (`rifleman.hp`),
  weapons and shots (`pistol.cooldown`, `grenade.damage`), and the tabks' specs (`bosstank.*`, `playertank.*`).

Numbers and on/off settings (`on`/`off`) can be turned. A def's numbers are read when the thing is made, so a knob
that feeds another (`kit.specialrefill` is copied into kie's soldier at start-up) is turned on the soldier instead
(`kie.specialrefill`). The tests check every example in `sandbox/` reads cleanly and every knob it turns exists.

## Examples

`sandbox/goons.txt` (tones2 against a crowd, invincible), `boss.txt` (Mission one at the boss), `moon.txt` (knobs:
low gravity, a quick super), `jet-take.txt` (a scripted take of the water jet).
