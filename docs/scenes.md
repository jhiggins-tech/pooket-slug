# Writing the cutscenes

What each scene says and shows (its shots, lines, narrator and timings) is designed in one script,
[docs/design/cutscenes.md](design/cutscenes.md). The script leads: change a scene there first, then its file here to
match. This page is how a file stages it.

Every cutscene is a text file in `scenes/`, one per scene `SceneLibrary.Names` lists (`coop-test.txt` is the two-phone
check's stand-in). The game reads them with its own reader and plays them on the shared stage
(`StageKit`). Change a line, a time or a position, push, and it's live **without a build**:

- `scene-files.yml` runs the **scene check** (`tools/scenecheck`), then puts the scenes up beside the game: the live
  game's for the main line, the branch's own preview for any other (`previews/<item-id>/`). It takes a couple of
  minutes. If a scene has a problem, nothing goes live, and the run's summary lists what's wrong and on which line.
- The game fetches the site's copies when it starts. A copy with a problem, or a site that can't be reached, means the
  game plays the copy built into it instead (the one from its last build).
- **Try one first**: `?scene=<name>` plays `scenes/<name>.txt` straight away, with SKIP, its name and any problems
  shown over it, and back to the title after. It never marks the scene seen. It only plays files on the game's own
  site. A new file needn't be one the game plays yet (`?scene=my-draft`).

Two phones in co-op play the same copy: each says which copies it has, and if they differ, both play the built-in ones.

The narrator's recordings go live with the scenes too: the deploy carries the latest `narration.zip` from the vendor
repository and lists each clip's hash, and a phone fetches only the ones that differ from its build's.

## The check

A scene goes live only if:

- every line reads (the problems name the line number and what's wrong);
- each scene's `net` number is its own;
- every spoken line is up long enough to read (3 words a second, until the shot ends; a narrator line until their
  next line);
- every narrator line has its recording, and time to play it plus 0.4 s. The lengths come from the clips themselves.

## How a file reads

One thing per line; `//` starts a note. Times are seconds from the start of the shot: `2.2` is a moment, `2.7-3.9` a
window (things that take time, like a walk, run over a window). A line with no time is how the shot starts.

```
// The opening.
net 1                      the scene's number for a partner's phone (1-255, each scene its own; never change it)

shot: Outside              each shot starts with its name
length 8.5                 seconds
set street                 the backdrop: street, den, bay, motorpool or coldstore (the creamery's loading yard)
show sign window door snow what's on stage from the start
intro 4 tones2: TONES2     a character intro at 4 s, with the name it slams up

0-5.6 tones2 walk -16 196  staging: [time] thing verb numbers
tones2 holds bag cup
4.3 narrator opening-1: Friday night...    spoken: time, who (and the recording), then the words
6.4 tones2: SNACKS | AND GAMES             | breaks the line
2.2 cue door               a sound cue
```

**Who speaks**: `tones2`, `kie`, `kcaj`, `trollo-kie` (Trollogram kie), `trollo-kcaj` (Trollogram kcaj: his bubble glitches), `goon`, `narrator` (subtitles, read out by its
recording: `narrator <recording>:`), `caption` (the big card).

**Cues**: `door`, `static`, `drop`, `goon`, `jet`, `glitch`, `card-drop`, `card-up`, `crush`, `card`, `shutter`,
`hum`, `engine`, `hatch`, `slam`, `turret`, `cage`, `kie-drop`, `snacks`, `rip`, `drone`, `lift`, `remote`, `alarm`,
`cold-door`, `frost`, `freezer`, `munch`, `note`,
`intro-build`, `intro`, `mission-start`; `say <line>` has the announcer read one of theirs.

**The camera**: `0-2.2 camera 1.15 20 -12 smooth`: how far in, and where it looks (from the stage's middle). The bay
(`set bay`) is 993 wide, three stages long, with the stage showing its left end: `0-3 camera 1 673 0 smooth` pans to
its far end.

## Things on the stage

Places are in art pixels: across from the left (the stage is 320 wide), up from the floor.

| Kind | Things |
|---|---|
| Soldiers | `tones2`, `kie`, `kcaj`, `goon` |
| Props | `bag`, `cup`, `card`, `remote`, `sign`, `window`, `door`, `doorway`, `spark`, `beacon`, `clamp`, `drone`, `cable`, `tones2-tabk`, `kie-tabk`, `kcaj-tabk` (with a cone on top), `turret` (whichever tabk's in the shot), `kcaj-corner` (his corner of the den, with the string board), the cold store's `store-opening` (the dark of its doorway: size it open) and `freezer`, `tub`, `slushie`, `cones`, `note` (a sticky note) |
| Effects | `snow`, `searchlights`, `chips`, `jet`, `closeup` (the keycard), `board` (kcaj's string board, close up), `shutter`, `lamps`, `row`, `junk`, `steam`, `cage`, `stars`, `zz`, `alarm`, `frost` (rolling out along the floor both ways from where it's put), `icicles` (on a cap: `icicles on kcaj 1 21`) |

A thing is on stage once a line stages it (or `show` names it), until `hide`.

**What anything can do**:

| Verb | Example |
|---|---|
| `show`, `hide` | `3.9 tones2-tabk hide`; over a window, `1.6-3.9 drone show` shows it just then |
| `at` | `kie at 100 10` |
| `move` | `1.5-1.8 kie move 100 10 100 0 in` (from, to, and an ease: `in`, `out`, `smooth`) |
| `turn` | `turret turn 14`, or `1-1.7 turret turn -150 6 smooth` |
| `size` | `size 1 0.5`, or from and to over a window |
| `colour` | `kie-tabk colour wreck`, or `1.2-1.8 colour warm cyan`: a name (`white black warm cyan gold paper ink water smoke wreck red`) or `#rrggbb` |
| `fade` | `fade 0.5`, or from and to over a window |
| `flicker` | `flicker warm cyan 14`: blinks the first colour now and then, how fast |
| `blink` | `blink 9 3`: on one beat in three, nine beats a second |
| `shake` | `0.2-2 kie-tabk shake 0.5` |
| `on` | `stars on kie 0 22`: rides on a soldier |

**Soldiers also**: `walk` (`0-1 kie walk 100 166`: they run), `face left` / `right`, `pose idle run crouch spray
hatch`, `look plain holo trollo`, `holds bag cup` (or `holds bag -7 9` for where), `sway`. The goon also `appear`s
(over a window) and `glitch-out`s.

**Effects' own verbs**: `chips burst 66` / `spill 66`; `jet spray 66 168` (over a window); `shutter open` (over a
window) / `up`; `lamps stutter`; `goon appear`; `clamp rip` (over a window); `cage flicker-out` (over a window).

## A new scene

Add its file, give it a `net` number no other scene has, and try it with `?scene=<name>`. For the game to play it
somewhere, it needs its name in `SceneLibrary.Names` and a place in a mission's story (`MissionStory`, on the
mission in `Missions.cs`: its opening, its boss reveal, or the scene once the boss's pilot is freed): that's a build. A new
thing or verb for the stage is a build too (`Staging.cs` for the vocabulary, `Ui/Cutscene/` for its look: the cast, the effects).
