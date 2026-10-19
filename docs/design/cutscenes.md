# Cutscenes

Every cutscene in Pooket Slug, written as a TV-style production script: what happens on screen, who says what, the
narrator, the cards, the sounds and how long each shot runs. It's the one place cutscenes are designed. Other
design docs link here and don't repeat them.

**Status:** the scenes keep the sign-offs they had in story.md, tanks.md and lobby.md (noted on each). Moved into
one script on 2026-10-19 (cutscenes-script).

**Read this when** you're writing or changing a cutscene, a line, a narrator recording, or anything a scene shows.

Up: [Design](README.md) · The story around the scenes: [story](story.md) · How the game plays a scene:
[writing the cutscenes](../scenes.md)

## How to read the script

**This script leads.** Each scene's words, shots and timings are decided here. The game plays a scene from its text
file in `scenes/` ([writing the cutscenes](../scenes.md)), which adds the staging an engine needs (positions, eases,
props). When a scene here changes, its scene file is changed to match, and that goes live without a build.

Each scene opens with a short note: when it plays, how long it runs, whether it can be skipped, whose
[character intro](#character-intros) it carries, and its scene file. Then comes the script, laid out like a
production script:

| In the script | What it is |
|---|---|
| `1. INT. THE HANGOUT - NIGHT` | A shot: its number, inside or outside, the place, the time of day. Its length is on the right. |
| Plain paragraphs | The action: what we see and hear. A character's name is in capitals the first time they're on screen in a scene. |
| `TONES2`, then the line | A speech bubble. Bubbles are in the game's capitals, a few words each. |
| `NARRATOR (V.O.)` | The narrator: subtitles along the bottom and read out loud. The recording's id is in square brackets. |
| `ANNOUNCER (V.O.)` | The arcade announcer, from the vendor repository's `voice/lines.json`. |
| `(GLITCHY)` | How a line is said or shown: a Trollogram's bubble flickers. |
| `INTRO:` | A [character intro](#character-intros) and the second it lands. |
| `SUPER:` | Text on screen: the caption card, a banner or a name card. |
| `SFX:` / `MUSIC:` | A sound cue, and what the music does. |
| `CUT TO:` / `INTO:` | How a shot ends: a cut to the next, or straight into play. |

The narrator says "tanks" where the subtitles say "tabks", and says kie as "Kye" and kcaj as "Kadge", so the
recordings' own text in `voice/narration.json` is spelled that way. All the lines are placeholders for the owner to
rewrite in the crew's own voice.

### Every scene at a glance

| Scene | Plays | About | Scene file | State |
|---|---|---|---|---|
| [The opening](#the-opening) | Mission one starts | 49 s | `opening.txt` | Built, eight shots (2026-10-19) |
| [The motor pool reveal](#the-motor-pool-reveal) | The first time tones2 reaches the motor pool | 24 s | `motor-pool.txt` | Built |
| [The boss intro](#the-boss-intro) | Every boss tabk's arrival | 3 s | none: in the game itself (`BossIntroShow`) | Built |
| [Freeing kie](#freeing-kie) | Trollogram kie beaten on foot | 28 s | `freeing-kie.txt` (shots 2 to 5) | Built |
| [The first debrief](#the-first-debrief) | After every solo win | 12 s | none: a screen (`DebriefView`) | Built |
| [The first visit](#the-first-visit) | The den's first opening, once ever | 4 s | none: in the den itself (`DenBeats`) | Built (2026-10-19) |
| [A crewmate moves in](#a-crewmate-moves-in) | The den, after each freeing | 3 s | none: in the den itself (`DenBeats`) | Built (2026-10-19) |
| [kie's tabk comes home](#kies-tabk-comes-home) | The tabk bay, the first visit after Mission two | 9 s | none yet | Drafted |
| [The Mission two briefing](#the-mission-two-briefing) | Mission two starts | 24 s | none yet | Designed, narrator voiced |
| [kcaj's tabk arrives](#kcajs-tabk-arrives) | Mission two's boss | 3 s, plus his intro | none: the boss intro | Designed |
| [Freeing kcaj](#freeing-kcaj) | Trollogram kcaj beaten on foot | 15 s | none yet | Designed, narrator voiced |
| [Two of the crew](#retired-two-of-the-crew) | Retired | 8 s | none | Retired, 2026-10-18 |
| [The co-op check](#the-co-op-check) | `?test=scene` and CI only | 36 s | `coop-test.txt` | A test, not the story |

## How every cutscene plays

Cutscenes are **in-engine scenes**: the game's own art, drawn in code, with speech bubbles. There's no video.

- **Each runs a few seconds a shot.** A tap moves to the next shot, and each moves on by itself too. Once you've seen
  a scene you can skip it, but a reveal always plays in full the first time.
- **The kinds:** a **briefing** before a mission (where you're going, and who's held there); a **boss intro**, the
  tabk's entrance; a **reveal**, the first time something new appears, once ever; a **freeing**, where the fake pilot
  glitches out and the real crewmate appears and joins; and the **debrief**, the mission's tally, XP and rank.
- **Shots hold long enough to read** (about three words a second). Bubbles are a few words each, in the game's
  capitals, so they read at a glance on a phone; the narrator's subtitles are in sentence case.
- **A narrator tells each scene,** in subtitles along the bottom of the screen and out loud (turned well down, to
  sit with the game's sounds), so what's happening is never left to guesswork. The owner asked for this after trying
  the first cut of the opening, 2026-10-15. The voice is a camp, softly spoken, Louis Theroux-style documentary
  narrator (voice F from the narrator voice spike, MiniMax, picked by the owner 2026-10-16), made by the vendor
  repository's Narration workflow from its `voice/narration.json`. A line made once is kept, so a new or changed line
  is the only one made again.
- **A narrator line always gets its whole voice,** plus a breath, before the next line or the end of the shot; if a
  re-recorded line runs longer, the shot waits for it (up to 3 seconds).
- **Bubbles never sit on top of each other:** comic-book style, a later one stacks above the ones already up, or
  below them when there's no room left above.
- **Sound:** each scene uses the music around it ([music](music.md)); the crew talk in bubbles, and the announcer
  calls the big moments in a generated voice. The music ducks under any recorded line.
- **In co-op,** scenes play for both players, on both phones, and a shot moves on early only once both have tapped
  (the timer still moves it on for both). A line says who's being waited for. A scene seen before skips only once both
  players have pressed SKIP, so a partner who hasn't seen it gets to watch it; a partner who leaves holds nobody up. A
  scene plays in co-op when the players start together from the den; someone joining a game already under way drops
  straight in. Mission one is single player only, so its scenes are always solo.
- **No strobing** ([the design's rule](README.md)): an alarm is a slow wash, once a second.

### Character intros

**Every character gets an intro the first time they're on screen** (canonical, the owner, 2026-10-16): the camera
crash-zooms onto them, their name slams up big ("TONES2"), time goes slow-mo, and a big musical hit plays. It's part
of that scene, so it plays whenever the scene does, and never anywhere else.

**How it plays:** it builds up, then it lands, like the run-up to a drop. The owner asked for the build-up after the
first cut came "out of nowhere, like a car crash", 2026-10-16.
- **First we see them.** The character is on screen for a few seconds before anything happens, so the intro has
  someone to land on.
- **The build-up,** about 2.5 seconds: a chip riser climbs (a snare roll that speeds up from quarter notes to a blur,
  over a noise sweep and a rising tone, all swelling), while the camera creeps in on them, the scene darkens a little,
  and it shakes as the riser peaks. A tap before it jumps to the build-up's start, and a tap during it does nothing, so
  it always plays whole.
- **The landing,** about 1.8 seconds, at the riser's peak: a white flash and a crash zoom onto the character, so they
  fill the left of the screen; their name slams in on a slanted band to their right, in the caption card's gold; the
  scene runs at slow-mo underneath (its bubbles, cues and snow all slow down); a chip musical hit rings out (a wide
  chord over a crash and a bass drop). Then it eases back out and the shot carries on at full speed. A tap can't cut it
  short.
- **Levels:** the riser and the hit sit at the level of the game's other sounds, not above them.
- A scene that can be skipped once seen skips its intro with it. The narrator waits for the intro to land before
  speaking, and their subtitles carry on through it.

**What counts:** named characters, the crew and their Trollogram copies. Each Trollogram copy is a character of its
own (it's that mission's boss), so a crewmate gets two intros: their Trollogram where it first appears, then the real
one when they're freed. Nameless enemies (the Trollogram goons) and vehicles don't get one; a tabk gets its name card
in [the boss intro](#the-boss-intro) instead.

| Character | Scene | Shot | Name on screen |
|---|---|---|---|
| tones2 | [The opening](#the-opening) | 1, as he strolls in with the snacks | TONES2 |
| Trollogram kie | [The motor pool reveal](#the-motor-pool-reveal) | 3, as he pops up in the hatch | TROLLO-KIE |
| kie | [Freeing kie](#freeing-kie) | 2, as the real kie drops to the floor | KIE |
| Trollogram kcaj | [kcaj's tabk arrives](#kcajs-tabk-arrives), the first time | As he looks out of the hatch | TROLLO-KCAJ |
| kcaj | [Freeing kcaj](#freeing-kcaj) | 2, as he steps out of the frost | KCAJ |
| Each later crewmate's Trollogram | Their mission's [boss intro](#the-boss-intro), the first time | As the pilot shows in the hatch | TROLLO-&lt;NAME&gt; |
| Each later crewmate | Their freeing scene | As the real one appears | &lt;NAME&gt; |

The debrief and the den's beats bring in nobody new, so they have no intro.

## Mission one

All of Mission one is single player, so every scene here plays for one player only.

### The opening

The first cutscene, and Mission one's briefing. It plays as Mission one starts, in full the first time; after that a
SKIP button shows from the start. About 49 seconds, eight shots of 4.5 to 8.5 seconds. Approved by the owner,
2026-10-15, then slowed down and given a narrator (the first cut ran 15 seconds and felt rushed). Reworked
2026-10-19 (opening-script): after the door, the empty den, then a run into the empty tabk bay, then back for the
Trollogram. Intro: tones2, shot 1. Scene file: `scenes/opening.txt`.

**Tabks are foreshadowed, never shown** (owner, 2026-10-19): tones2 cries out for them and finds their bay empty,
but no tabk is on screen, and nothing says what one is. The first tabk anyone sees is still kie's, in
[the motor pool reveal](#the-motor-pool-reveal). Nothing in it hints at vehicles you drive, or at co-op.

**What it teaches:** the Trollograms are the enemy and they glitch out when beaten (shot 6), tones2 sprays water,
and the keycard's close-up makes it plain that the base is where the crew went. **What it sets up:** the empty den
is the one the crew come back to as they're freed, and the empty bay is where their tabks will park again
([the lobby](lobby.md)).

**New in the rework** (for the cards that follow it):
- **Shots:** 3 and 4 are the trashed den (3 is the old "Inside, raided", now in the den), 5 is new, in the tabk bay,
  and 6 to 8 are the old 4 to 6, with tones2 coming back in through the roller door at the start of 6.
- **Backdrops:** the den on raid night, trashed (shots 3, 4 and 6 to 8; replaces the old hangout room;
  opening-trashed-den-art), and the tabk bay on raid night, the bay's plate with a mess layer (shot 5;
  opening-bay-mess-art). The street outside (shots 1 and 2) is as before.
- **Narrator lines:** two new, `opening-3b` (shot 4) and `opening-3c` (shot 5), for opening-narration. The rest keep
  their recordings.
- **Staging:** tones2 runs out through the den's roller door (on the right of the ground floor), and the bay's
  roller door is at its far left, so he comes in from the left and goes back the same way (opening-stage).

```text
FADE IN:

1. EXT. THE HANGOUT - NIGHT                             8.5 s

A side-on street at night, snow falling. The crew's
hangout: a garage with a hand-painted POOKET sign and
warm light in the window.

TONES2 strolls in from the left, arms full: a bag of
snacks and a big slushie.

SFX: quiet street ambience.

          INTRO: TONES2 (builds as he walks, lands at 4 s)

                    NARRATOR (V.O.)
                    [opening-1]
          Friday night. tones2's back from the snack
          run.

He carries on to the door.

                    TONES2
          SNACKS ARE HERE LADS

                                                   CUT TO:

2. EXT. THE HANGOUT, AT THE DOOR - NIGHT                4.5 s

The door swings in, hanging off one hinge. The warm light
flickers and turns cold cyan. He stops.

SFX: the door; a static crackle on the flicker.

                    NARRATOR (V.O.)
                    [opening-2]
          But the door's been kicked in.

                    TONES2
          ...LADS?

                                                   CUT TO:

3. INT. THE DEN - NIGHT                                 5 s

The den, trashed: the couch flipped, the TV and the
console sparking, controllers on the floor, the loft's
lights out, cyan scorch marks up the walls. Every station
dark. Nobody's there.

                    NARRATOR (V.O.)
                    [opening-3]
          The hangout's trashed, and the crew are
          gone.

A long look. The snack bag drops. Chips everywhere.

SFX: the bag drops. A beat of quiet, then a crackle of
static.

                                                   CUT TO:

4. INT. THE DEN - CONTINUOUS                            4.5 s

His eyes go to the roller door on the right. It's been
wrenched half up, and cold air blows under it.

                    NARRATOR (V.O.)
                    [opening-3b]
          Then he remembers the garage.

                    TONES2
          OH NO. MY POOKET-TABKS!

He runs for it, slushie still in hand, and ducks under
the door.

SFX: the roller door rattles as he ducks under.

                                                   CUT TO:

5. INT. THE TABK BAY - NIGHT                            6.5 s

He skids in through the roller door at the far left. The
long garage is a wreck: tools knocked off the wall, the
crane's hook swinging over an empty repair bay, and every
parking space on both decks empty. Tyre tracks and cyan
Trollogram scuffs run the length of the floor to the
street shutter, left half open on the snow.

(We never see a tabk: only the spaces where they were.)

SFX: the crane chain creaks; the street shutter rattles
in the wind.

                    NARRATOR (V.O.)
                    [opening-3c]
          Empty. Whatever was parked in here, they
          took that too.

                    TONES2
          ...ALL OF THEM?

                                                   CUT TO:

6. INT. THE DEN - NIGHT                                 8.5 s

tones2 walks back in through the roller door, slowly,
into the wrecked den.

A TROLLOGRAM GOON flickers up by the couch.

                    GOON (GLITCHY)
          INTRUDER. INTRUD-

                    NARRATOR (V.O.)
                    [opening-4a]
          One of the raiders is still here: a
          Trollogram.

tones2 sprays it with ten-1. It glitches out, the goons'
own death. A keycard clatters to the floor where it stood.

SFX: the jet; the glitch-out; the card drops.

                    NARRATOR (V.O.)
                    [opening-4b]
          It glitches out, and drops its keycard.

tones2 walks over and picks it up.

                                                   CUT TO:

7. INSERT - THE KEYCARD                                 5 s

The room dims and the card slides up, big enough to read,
flickering like the Trollograms do.

SUPER (ON THE CARD): MILITARY BASE · NORTH GATE ·
ACCESS: ALL AREAS

                    NARRATOR (V.O.)
                    [opening-5]
          A pass for the military base, up on the
          hill.

                    TONES2 (O.S.)
          THAT'S WHERE THEY TOOK THEM.

                                                   CUT TO:

8. INT. THE DEN - NIGHT                                 6 s

Out of the window, searchlights sweep the sky over a hill
in the distance.

                    NARRATOR (V.O.)
                    [opening-6]
          So tones2 goes in alone.

                    TONES2
          RIGHT.

He finishes the slushie in one go, crushes the cup, and
walks out of the front door into the snow.

SFX: the cup crushed.

SUPER (THE CAPTION CARD SLAMS IN): MISSION 1 ·
THE MILITARY BASE / FIND THE CREW

                    ANNOUNCER (V.O.)
                    [mission-start]
          Mission one... start!

MUSIC: the mission start sting, into Mission one's theme.

                                           INTO: MISSION ONE
```

**Built** (2026-10-19, opening-stage): `scenes/opening.txt` plays all eight shots on the raid-night plates (sets `den`
and `bay`). He comes in by the den's side door on the left and leaves by it in shot 8. In the bay the camera runs
along the empty decks to the street shutter and back to him. The den's painting has no window, so shot 8 has no
searchlights: the keycard's close-up is what points to the base.

**Built** (2026-10-19, opening-street-plate): shots 1 and 2 play on Design's street plate (B-2, a snowy night,
`concepts/opening-exterior/`). The plate paints the POOKET sign; its side door (right of the roller shutter) is cut out
so it swings in shot 2, with the light behind it, and the lit window on the right takes the cold cyan.

### The motor pool reveal

The first-ever reveal: tabks exist. It plays once, ever, the first time tones2 reaches the motor pool, and it's kie's
tabk's entrance: the [boss intro](#the-boss-intro) after it skips the roll-out and goes straight to the banner, the
name card ("KIE'S TABK") and the music. Every later fight, a replay included, gets only the boss intro. About 24
seconds, four shots; tap moves on, and it can't be skipped the first time. Drafted 2026-10-15, built 2026-10-16 with
the narrator. Intro: Trollogram kie, shot 3. Scene file: `scenes/motor-pool.txt`.

```text
1. EXT. THE BASE, THE MOTOR POOL'S SHUTTER - NIGHT      4.5 s

TONES2 steps in from the left in front of the base's
wall. The motor pool's blast shutter grinds up, juddering,
and cold light pours out across the apron from the bay
behind it.

SFX: the shutter's grind, then a hum.

                    NARRATOR (V.O.)
                    [motor-pool-1]
          Behind the shutter: the base's motor pool.

                                                   CUT TO:

2. INT. THE MOTOR POOL - NIGHT                          5.5 s

The camera eases right and in: the crew's tabks parked in
a row under work lights, each in its crewmate's paint
(tones2's orange and black among them), cyan Trollogram
junk flickering all over them, cables across the floor.
The space at the front is empty, its paint scuffs still
steaming.

                    NARRATOR (V.O.)
                    [motor-pool-2]
          And parked inside, all in a row: the crew's
          tabks.

                    TONES2
          ...ARE THOSE OURS?

                                                   CUT TO:

3. INT. THE MOTOR POOL - CONTINUOUS                     9.5 s

An engine roars. KIE'S TABK rolls in from the right into
the empty space, its turret pointing up and away. The
hatch pops, and up comes TROLLOGRAM KIE: kie, but cyan,
flickering, grinning.

SFX: the engine; the hatch; a crackle of static.

          INTRO: TROLLO-KIE (builds as he rises, lands at
          4.5 s)

                    NARRATOR (V.O.)
                    [motor-pool-3]
          That's kie. Or something wearing his face.

                    TROLLOGRAM KIE (GLITCHY)
          HEY TONES. WANNA RACE?

                                                   CUT TO:

4. INT. THE MOTOR POOL - CONTINUOUS                     4.5 s

Trollogram kie drops into the hatch and it slams. The
turret swings round on to tones2.

SFX: the slam; the turret.

                    NARRATOR (V.O.)
                    [motor-pool-4]
          kie has never once offered to race.

                    TONES2
          THAT'S NOT KIE.

                                INTO: THE BOSS INTRO, FROM
                                THE WARNING (control back)
```

**Built** (2026-10-16): it plays as the boss tabk wakes, the first time, solo; once it ends, the tabk is in its place
and the boss intro runs. Test starts (`?test=boss`, `pilot`, `team`) skip it; `?test=boss,reveal` plays it every
time.

### The boss intro

A boss tabk's arrival is the mission's big moment: a warning banner, the music changes, and the tabk makes its
entrance. It isn't a scene file: it plays in the game itself, and **you keep control throughout**, so it never feels
like the game took the stick away (about three seconds). Picked 2026-10-15. The same for every boss tabk; each
mission's version names its own (kie's below, [kcaj's](#kcajs-tabk-arrives) in Mission two).

- **The second time** you meet a tabk (a replay, or after a lost fight at the boss-door checkpoint), it's quick: the
  banner and siren only, with the tabk already in place, and the boss music starts at once.
- **The pilot's intro:** from Mission two on, the first time a mission's boss intro plays, its Trollogram pilot gets
  their [character intro](#character-intros) as the tabk stops and the hatch pops. It's the one moment the intro holds
  the action, and it never plays again. In Mission one, Trollogram kie gets his in the motor pool reveal instead.
- **In co-op,** the world's authority starts it and both players see it together. Nobody taps, so neither waits.
- **When it ends:** wrecking the tabk plays a short victory sting over the boss music, which runs on through
  [the pilot round](tanks.md#the-pilot-round) and stops when the pilot glitches out.
- **The music** (picked 2026-10-15): the mission's looping track through the run, and a faster, minor-key boss track
  for the fight, both made in code with the chip synth ([music](music.md); when it changes is `Core/Audio/Soundtrack`).

```text
1. EXT. THE BOSS ARENA (MISSION ONE: THE MOTOR POOL)    3 s

In play. tones2 steps into the arena. The screen locks.

MUSIC: the mission's theme cuts out.

SFX: a siren, twice.

The screen edges flash red. Hazard stripes sweep across
the top and bottom of the screen, Metal Slug style.

SUPER (FLASHING RED, SLAMS IN): WARNING!

                    ANNOUNCER (V.O.)
          Warning!

The motor pool's blast shutter grinds open, and KIE'S
TABK rolls out onto the arena floor. It can't be hurt and
doesn't fire until it stops.

(The first time, the motor pool reveal has already
brought it in: it's in place, and this starts at the
WARNING!)

SUPER (DROPS IN UNDER THE BANNER): KIE'S TABK

                    ANNOUNCER (V.O.)
          Here comes the tabk!

The tabk fires its first shot. Its health bar fills
across the top.

MUSIC: the boss theme kicks in on that first shot.

                                           INTO: THE FIGHT
```

**Built** (2026-10-16): `Core/Bosses/BossIntroShow` (the cue timeline) and `Game/BossIntroFlow`. The announcer's
lines are in the vendor repository's `voice/lines.json`.

### Freeing kie

The freeing scene, after Trollogram kie is beaten on foot ([the pilot round](tanks.md#the-pilot-round)). kie frees
tones2's own tabk, then bows out (owner, 2026-10-16): he's spent, and sits out the rest of Mission one, so tones2
finishes it alone, with his tabk. About 28 seconds: the glitch-out's 3 seconds in the game, then about 25 of scene.
It can be skipped once seen, on a replay. Intro: kie, as he drops. Scene file: `scenes/freeing-kie.txt` (shots 2 to
5; shot 1 is the game's).

**How the release is done** (owner, 2026-10-16): the cargo drone hauls the tabk out, and kie hands over its remote.
It's what makes every call-in drop from the sky ([calling in a tabk](tanks.md#calling-in-a-tabk)). Then straight into
[the coda](story.md#the-coda), every time.

```text
1. INT. THE MOTOR POOL, THE ARENA - NIGHT (IN PLAY)     3 s

TROLLOGRAM KIE staggers, freezes mid-taunt, and glitches
apart: the big version of the goons' death.

                    TROLLOGRAM KIE (GLITCHY)
          WAIT, I WAS WINN-

MUSIC: the boss music cuts dead.

                                                   CUT TO:

2. INT. THE MOTOR POOL - NIGHT                          7.5 s

After the fight: the shutter up, kie's tabk a smoking
wreck in its space, the row behind. TONES2 stands by,
holding the snack bag.

Where Trollogram kie stood, a cyan cage of light flickers
out, and the real KIE drops to the floor in a heap,
dazed, in his own colours. Stars circle his head.

SFX: the cage; kie drops.

          INTRO: KIE (lands at 3 s)

                    NARRATOR (V.O.)
                    [freeing-1]
          And out drops the real kie. Bit peaky.

                    KIE
          ...HOW LONG WAS I IN THERE?

                                                   CUT TO:

3. INT. THE MOTOR POOL - CONTINUOUS                     4.5 s

tones2 comes over and holds out the last of the snacks
from the opening.

                    TONES2
          BROUGHT SNACKS.

kie gets up off the floor, swaying, and takes them.

SFX: the snacks.

                    KIE
          LEGEND.

                                                   CUT TO:

4. INT. THE MOTOR POOL - CONTINUOUS                     6 s

kie staggers over to tones2's tabk in the row and rips
the cyan Trollogram clamp off it. A cargo drone drops
through the roof hatch, hooks the tabk and hauls it up
and out.

SFX: the rip; the drone; the lift.

                    NARRATOR (V.O.)
                    [freeing-2]
          Then he sets tones2's tabk free.

kie comes back and presses its call-in remote into
tones2's hand.

SFX: the remote.

                    KIE
          YOURS. CALL IT WHEN YOU NEED IT.

                                                   CUT TO:

5. INT. THE MOTOR POOL - CONTINUOUS                     7.5 s

Alarms start up somewhere in the base: a beacon turns
over the shutter, and a slow red wash, once a second.
kie sits down hard against the wall with the snacks in
his lap.

SFX: the alarm.

                    KIE
          I'M GONNA... SIT THIS ONE OUT.

He nods off. Zs.

                    NARRATOR (V.O.)
                    [freeing-3]
          kie's done for the night. tones2 isn't.

                                             INTO: THE CODA
```

**Built** (2026-10-16): shot 1 plays in the game itself (`Enemies/Pilot`; the numbers are `PilotFreeingSpec` in
`Core/Crew/KieTuning.cs`, the look `HoloLook.Big`), then the scene plays shots 2 to 5 on the motor pool's stage.
`?test=pilot,weak` gets there fastest: it starts at the tabk, and the pilot goes down on his own a few seconds after
he jumps out.

### The first debrief

Mission one's debrief, the first time: the tally, then XP and rank are revealed
([what the player sees, and when](levels.md#what-the-player-sees-and-when)). It's a screen more than a scene: the
crew stand to one side and the numbers count up on the other. Each part counts up fast, and a tap fills it at once;
another tap moves on. Draft, 2026-10-15.

On a replay, it's the same screen without the first-time lines, and with any new looks tokens shown. The sign-in step
stays: it shows on every debrief while the player is signed out.

```text
1. INT. THE DEBRIEF SCREEN                              3 s

The crew stand to one side.

SUPER: MISSION COMPLETE!

MUSIC: the mission complete sting, into the debrief's loop.

                    ANNOUNCER (V.O.)
          Mission complete!

(Only if the coda didn't already call it.)

The numbers count up: score, time, goons, accuracy, and
the lives and continues left. The best score so far shows
beside it.

SUPER (IF IT IS): NEW BEST!

                                                   CUT TO:

2. INT. THE DEBRIEF SCREEN - CONTINUOUS                 3 s

The first time ever, a new bar slides in, labelled XP. It
fills from the mission's parts: the mission cleared, kie
freed, and the kills.

                    KIE
          YOU GET XP FOR THAT.

                                                   CUT TO:

3. INT. THE DEBRIEF SCREEN - CONTINUOUS                 3 s

The rank badge appears. You go up from the lowest rank on
the ladder, where everyone starts, to the one above it.
Only that rank's name is shown; the ladder's hidden ranks
stay hidden.

SUPER (STAMPED): RANK UP!

MUSIC: the rank up sting.

                                                   CUT TO:

4. INT. THE DEBRIEF SCREEN - CONTINUOUS                 3 s

(Only for a player who isn't signed in.)

                    KIE
          SIGN IN SO THIS DOESN'T VANISH.

A line points to the sign-in button at the top left, as
on the title screen.

SUPER: SIGN IN AT THE TOP LEFT TO KEEP YOUR XP AND RANK

Signing in here saves the run's progress straight away.
Skipping is fine, and it asks again on the next debrief.

                                            INTO: THE DEN
```

**Where it goes next** (owner, 2026-10-19): straight to the den, which opens for the first time with
[the first visit](#the-first-visit). Every later debrief goes back to the den too ([what START plays](story.md#what-start-plays)).

**Built** (2026-10-16, `Core/Lobby/Debrief.cs`, `Ui/DebriefView.cs`): after every solo win, `?test=debrief` to try
it. XP is placeholder: 200 for the clear, 300 for kie freed, 1 a kill. Rank is the record's stages beaten: a first
clear climbs one rung (4 stages), a replay one stage, using the ladder ported from Pooket Tabks
(`Core/Lobby/Ranks.cs`). Looks tokens aren't in yet.

## The den

The den has no narrator: it's a menu with the crew in it, so it only has speech bubbles, and its music is the title
theme ([the lobby](lobby.md)).

### The first visit

The first time the den opens: straight after Mission one's first debrief (owner, 2026-10-19). About 4 seconds, once
ever. Kept with the player's progress, so it follows a sign-in. From [the lobby](lobby.md), approved 2026-10-16.

```text
1. INT. THE DEN (THE HANGOUT) - NIGHT                   4 s

The hangout, still trashed from the raid. KIE heaves the
couch back upright. TONES2 plugs the TV back in, and the
room warms up: lamp light, string lights, screens
glowing.

                    KIE
          HOME SWEET HOME.

                           INTO: THE DEN (two stations lit,
                           six dark)
```

**Built** (2026-10-19, den-beats-build): in the den itself, not a scene file: `Core/Lobby/DenBeats` (when it's due and
its timing, tested) and `Ui/Den/DenBeatView` (drawn over the den). The raid-night den painting lies over the den and
fades away as the room warms; a tap skips it. It's a reveal in the profile (`den-first-visit`), and a `?test=` jump
skips it.

### A crewmate moves in

The first visit to the den after a crewmate is freed. About 3 seconds; tap to skip. From [the lobby](lobby.md),
approved 2026-10-16. Their corner was wrecked and dark until now.

```text
1. INT. THE DEN - DAY                                   3 s

The roller door rattles up. The NEW CREWMATE walks in,
looks round their wrecked corner, and the station lights
up.

SFX: a chip clunk.

                    TONES2 OR KIE (FROM THE COUCH)
          (A hi.)

                                            INTO: THE DEN
```

**Built** (2026-10-19, den-beats-build): with the first visit (`DenBeats`, `DenBeatView`). The roller door rolls up,
the newcomer steps in under it and walks in, tones2 (kie, if it's tones2 moving in) says HEY. WELCOME HOME. from where
he is, and the newcomer's moved-in layer fades in with the clunk; then they head for their station. One per crewmate
freed, a reveal each (`moved-in-<name>`); tones2 and kie were home for the first visit. Never in a co-op room.

### kie's tabk comes home

kie's tabk was wrecked in Mission one's boss fight, and it comes back once Mission two is beaten
([tanks](tanks.md)), rebuilt as THE PROJECTIONIST (owner, 2026-10-19, kie-tabk-design): kie builds it round a
Trollogram projector he took from the creamery's cold store, so it comes home still frosted, wrapped in copper freezer
pipes. This beat plays the first time the player walks into [the tabk bay](lobby.md#the-tabk-bay) after Mission two is
beaten. About 9 seconds, three shots; tap to skip. Once ever, kept with the player's progress, so it follows a sign-in.
Drafted 2026-10-19 (kie-tabk-return-script).

**Who's in it:** kie and whoever the player is playing (`PLAYER` below), with tones2 as the default. kie's line works
for anyone. **The paint** (owner, 2026-10-19, kie-tabk-design): kie's blue hull with a green stripe and green reel
spokes, frost, copper pipes and rust spots, and the lens glowing the friendly gold, never the villains' cyan.

```text
1. INT. THE TABK BAY - NIGHT                            3 s

Sparks under the crane. kie's tabk sits on the repair
stands, half a tabk: a patched hull, its old olive
showing under fresh paint. KIE's legs stick out from
underneath. A WEASEL holds up a spanner; a hand takes
it, and hands back an empty ice cream cone.

SFX: a chip welding crackle, on and off.

                                                   CUT TO:

2. INT. THE TABK BAY - CONTINUOUS                       3 s

The crane lowers the projector onto the turret ring.
Frost on its body, steam curling off the copper pipes.
KIE slides out, pushes up his welding mask and pats it.

                    KIE
          NICKED IT FROM THE CREAMERY.

                    PLAYER
          IS IT MEANT TO STEAM?

                    KIE
          TROLLOGRAMS RUN HOT.

                                                   CUT TO:

3. INT. THE TABK BAY - CONTINUOUS                       3 s

KIE climbs in. The two reels spin up, the lens glows
gold, and a little Trollogram of the tabk flickers up on
the bay wall and waves. kie waves back. The weasel
hops in after him.

SFX: the reels whirr up, then a chip clunk.

SUPER (THE NAME CARD DROPS IN): KIE'S TABK ·
THE PROJECTIONIST

                    KIE
          GOOD AS NEW. BETTER, ACTUALLY.

                       INTO: THE TABK BAY (kie's tabk in its
                       space, the player's loaded tabk back
                       on the stands)
```

**Afterwards:** kie's tabk parks in its space downstairs and can be picked like any other
([picking another](lobby.md#the-tabk-bay)). During the beat the player's loaded tabk waits in its own space, so the
stands are kie's; the cargo drone swaps them back as the beat ends.

## Mission two

kcaj's mission, at the Brain Freeze creamery ([why the creamery](story.md#mission-two-the-creamery)). It's the
first mission co-op can play, so every scene here plays on both phones in co-op. Drafted 2026-10-16; the story was
approved by the owner on 17 Oct, and the narrator lines are voiced (`voice/narration.json`).

### The Mission two briefing

Played when Mission two starts from the den, solo or in co-op, so a partner sees it too (owner, 2026-10-19). Until
then kcaj's string board glows in his dark corner to point the way ([the lobby](lobby.md)). About 24 seconds, four shots; it can be skipped once seen. kcaj's string
board briefs it (owner, 2026-10-16): it stays up in his dark corner of the den before he's freed. **Who's in it:**
always tones2 and kie, whoever the players picked, since they're the only crew free at this point in the story; a
replay is the same. No intro.

```text
1. INT. THE MOTOR POOL - MORNING                        5 s

The base, the morning after. The blast shutter is up and
the bay is empty. Tyre tracks and cyan scuffs lead out of
the back gate. TONES2 and KIE stand in the middle of it.

                    NARRATOR (V.O.)
                    [m2-briefing-1]
          By morning, the motor pool was empty.

                    KIE
          THEY TOOK THE LOT.

                                                   CUT TO:

2. INT. THE DEN, THE UPPER FLOOR - DAY                  6 s

kcaj's corner is dark and wrecked, but his red string
board is still on the wall, sticky notes everywhere.
tones2 and kie climb the ladder to it.

                    NARRATOR (V.O.)
                    [m2-briefing-2]
          kcaj's string board. Everyone thought it was
          about the moon landing.

                                                   CUT TO:

3. INSERT - THE STRING BOARD                            6 s

One red string runs off the board to a photo of a
factory with a giant soft-serve sign. Scrawled under it:
THEY NEED IT COLD.

                    KIE (O.S.)
          HE WAS RIGHT?

                    TONES2 (O.S.)
          HE'S NEVER BEEN RIGHT.

                                                   CUT TO:

4. EXT. THE DEN, THE ROLLER DOOR - DAY                  7 s

Back out of the den's roller door into daylight.

                    NARRATOR (V.O.)
                    [m2-briefing-4]
          The Trollograms like it cold. So, as it
          turns out, does kcaj.

SUPER (THE CAPTION CARD SLAMS IN): MISSION 2 ·
THE CREAMERY / FIND KCAJ

                    ANNOUNCER (V.O.)
                    [mission-two-start]
          Mission two... start!

                                           INTO: MISSION TWO
```

### kcaj's tabk arrives

Mission two's [boss intro](#the-boss-intro), with the creamery's entrance. The first time, the hatch pops as the tabk
stops and Trollogram kcaj gets his intro: the one moment the intro holds the action. After that, it's the quick
version.

```text
1. INT. THE CREAMERY, BEFORE THE COLD STORE (IN PLAY)   3 s,
                                                plus the intro

The screen locks.

MUSIC: the mission's theme cuts out.

SFX: a siren, twice.

SUPER (FLASHING RED, SLAMS IN): WARNING!

                    ANNOUNCER (V.O.)
          Warning!

The cold store's doors fog open, and KCAJ'S TABK rolls
out of the mist, a cone on its antenna.

SUPER (DROPS IN UNDER THE BANNER): KCAJ'S TABK

                    ANNOUNCER (V.O.)
          Here comes the tabk!

(The first time only:) the hatch pops as it stops, and
TROLLOGRAM KCAJ looks out, holding two cones.

          INTRO: TROLLO-KCAJ

                    TROLLOGRAM KCAJ (GLITCHY)
          YOU'RE NOT IN MY NOTES.

MUSIC: the boss theme kicks in on the tabk's first shot.

                                           INTO: THE FIGHT
```

### Freeing kcaj

After Trollogram kcaj is beaten on foot ([the pilot round](tanks.md#the-pilot-round)), like
[freeing kie](#freeing-kie). About 15 seconds, five shots; it can be skipped once seen. Intro: kcaj, shot 2.
**Afterwards:** the next time the den loads, kcaj [moves in](#a-crewmate-moves-in) to his nook on the upper floor and his corner lights
up.

```text
1. INT. THE CREAMERY, THE COLD STORE (IN PLAY)          3 s

TROLLOGRAM KCAJ freezes mid-taunt and tears apart, the
big version of the goons' death.

                    TROLLOGRAM KCAJ (GLITCHY)
          IT'S ALL CONNECT-

MUSIC: the boss music cuts out.

                                                   CUT TO:

2. INT. THE CREAMERY, THE COLD STORE                    4.5 s

The cold store's door creaks open, frost rolls out, and
the real KCAJ stumbles out in his own colours, rimed with
frost, holding an empty tub.

          INTRO: KCAJ

                    NARRATOR (V.O.)
                    [m2-freeing-2]
          And out comes the real kcaj. Slightly
          frozen.

                    KCAJ
          I'VE BEEN IN THERE FOR DAYS.

                                                   CUT TO:

3. INT. THE COLD STORE - CONTINUOUS                     2.5 s

He looks at the tub, then at the crew.

                    KCAJ
          I ATE THE EVIDENCE.

                                                   CUT TO:

4. INT. THE COLD STORE - CONTINUOUS                     2.5 s

TONES2 holds out a slushie. kcaj walks past it, opens a
freezer, and eats two cones at once.

                    NARRATOR (V.O.)
                    [m2-freeing-4]
          He's had a difficult week.

                                                   CUT TO:

5. INT. THE COLD STORE - CONTINUOUS                     2.5 s

kcaj points at the factory around them, then at the
crew. Nobody answers.

                    KCAJ
          I TOLD YOU. I TOLD ALL OF YOU.

He sticks a sticky note on tones2's back.

                                          INTO: THE DEBRIEF
```

## Retired: two of the crew

**Retired** (owner, 2026-10-18, start-screen): the title is now a splash with only START and SETTINGS, so it never
grows, and this scene was taken out of the game. Its words are kept in case kie's line finds a new home, in the den
perhaps. It was the third reveal, co-op exists, played once on the title screen the first time it loaded after
Mission one was beaten. It was built 2026-10-16 (`Core/Story/CrewReveal.cs`, `Ui/CrewRevealView.cs`, since removed).

```text
1. EXT. THE TITLE SCREEN                                1 s

The title as on first launch: the logo, START, SETTINGS
and the sign-in button. TONES2 stands at the left, alone,
as he has been all along.

                                                   CUT TO:

2. EXT. THE TITLE SCREEN - CONTINUOUS                   2 s

KIE walks in from the right, eating the snacks, and stops
beside tones2.

                    KIE
          MISSED ME?

                                                   CUT TO:

3. EXT. THE TITLE SCREEN - CONTINUOUS                   3 s

kie elbows the START button. It cracks and splits into
three that drop in one at a time: MISSIONS, CO-OP and
CREW. SETTINGS and sign-in stay where they were.

SFX: three chip clunks.

                                                   CUT TO:

4. EXT. THE TITLE SCREEN - CONTINUOUS                   2 s

kie leans on the CO-OP button and points out at the
player. It pulses once.

                    KIE
          TWO OF US NOW. BRING A MATE.

                                          INTO: THE TITLE
```

## The co-op check

Not part of the story: a stand-in for later missions' co-op scenes, so the two-phone check (`?test=scene`, CI's
co-op test) has a scene to tap through, since Mission one's own scenes never play in a co-op room. Six plain shots of
6 seconds outside the hangout, each held long enough that a phone tapping alone is plainly waiting. CI counts the
moves between them, so it stays at six. Scene file: `scenes/coop-test.txt`.

```text
1-6. EXT. THE HANGOUT - NIGHT                     6 s each

TONES2 stands outside the hangout in the snow.

                    TONES2
          TEST SHOT 1 OF 6: TAP TOGETHER

(And so on, up to 6 of 6.)
```
