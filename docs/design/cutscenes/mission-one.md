# Mission one

Part of [the cutscenes](README.md), the one script for every scene: the rules, the format and every scene at a glance are there.

All of Mission one is single player, so every scene here plays for one player only.

## The opening

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
([the lobby](../lobby.md)).

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

## The motor pool reveal

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

## The boss intro

A boss tabk's arrival is the mission's big moment: a warning banner, the music changes, and the tabk makes its
entrance. It isn't a scene file: it plays in the game itself, and **you keep control throughout**, so it never feels
like the game took the stick away (about three seconds). The one exception is a pilot's part at its end, where a
mission has one (see [kcaj's tabk arrives](mission-two.md#kcajs-tabk-arrives)): solo, the game holds still from the hatch
to the band's end (`BossIntroShow.Holding`). Picked 2026-10-15. The same for every boss tabk; each
mission's version names its own (kie's below, [kcaj's](mission-two.md#kcajs-tabk-arrives) in Mission two).

- **The second time** you meet a tabk (a replay, or after a lost fight at the boss-door checkpoint), it's quick: the
  banner, one siren and the announcer's "Warning!" only, with the tabk already in place, and the boss music starts at once.
- **The pilot's intro:** the first time, each boss tabk's Trollogram pilot gets their
  [character intro](README.md#character-intros) in a reveal scene before the boss intro, the camera pushing in on the hatch:
  Trollogram kie in [the motor pool reveal](#the-motor-pool-reveal), Trollogram kcaj in
  [kcaj's tabk arrives](mission-two.md#kcajs-tabk-arrives) (owner, 2026-10-19). The boss intro after a reveal skips the roll-out.
- **In co-op,** the world's authority starts it and both players see it together. Nobody taps, so neither waits.
- **When it ends:** wrecking the tabk plays a short victory sting, then the boss music's faster final section (and the
  orchestra after it, in builds that have it) through [the pilot round](../tabks/bosses.md#the-pilot-round); it stops
  when the pilot glitches out (`MusicFlow.TankWrecked`).
- **The music** (picked 2026-10-15): the mission's looping track through the run, and a faster, minor-key boss track
  for the fight, both made in code with the chip synth ([music](../music.md); when it changes is `Core/Audio/Soundtrack`).

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

## Freeing kie

The freeing scene, after Trollogram kie is beaten on foot ([the pilot round](../tabks/bosses.md#the-pilot-round)). kie frees
tones2's own tabk, then bows out (owner, 2026-10-16): he's spent, and sits out the rest of Mission one, so tones2
finishes it alone, with his tabk. About 28.5 seconds: the glitch-out's 3 seconds in the game, then 25.5 of scene.
It can be skipped once seen, on a replay. Intro: kie, as he drops. Scene file: `scenes/freeing-kie.txt` (shots 2 to
5; shot 1 is the game's).

**How the release is done** (owner, 2026-10-16): the cargo drone hauls the tabk out, and kie hands over its remote.
It's what makes every call-in drop from the sky ([calling in a tabk](../tabks/driving.md#calling-in-a-tabk)). Then straight into
[the coda](../story.md#the-coda), every time.

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

## The first debrief

Mission one's debrief, the first time: the tally, then XP and rank are revealed
([what the player sees, and when](../levels.md#what-the-player-sees-and-when)). It's a screen more than a scene: the
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
[the first visit](den.md#the-first-visit). Every later debrief goes back to the den too ([what START plays](../story.md#what-start-plays)).

**Built** (2026-10-16, `Core/Lobby/Debrief.cs`, `Ui/DebriefView.cs`): after every win, `?test=debrief` to try it. In
co-op each phone shows its own player's (their kills, lives and XP; the score is the shared one), and CONTINUE goes
back to the room's den. XP is placeholder: 200 for the clear, 300 for kie freed, 1 a kill. Rank is the record's stages beaten: a first
clear climbs one rung (4 stages), a replay one stage, using the ladder ported from Pooket Tabks
(`Core/Lobby/Ranks.cs`). Looks tokens aren't in yet: the XP bar fills towards the next one, every 600 XP.
