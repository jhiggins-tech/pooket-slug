# Skits

Part of [the cutscenes](README.md), the one script for every scene: the rules, the format and every scene at a glance are there.

Short comic crew moments, written for the skit-scripts card (2026-10-25). Four are the pitch's short list, drafts for
the owner to pick from; [Two of us now](#two-of-us-now) and [Keep it down](#keep-it-down) are the owner's picks (25 Oct). Staging, art, sound and the build each get their own card once a skit is picked.

**How a skit plays:**
- **Once ever,** like the den's beats ([the den](den.md)), each under its own once-only key, kept with the player's
  progress so it follows a sign-in. The keys below are proposals: a key goes in `OnceOnly.All` when its skit is built,
  and never changes after.
- **In the room itself,** not as a scene file (Keep it down plays in a mission, in play: see its note): the den's skits play in the den, as its beats do (`DenBeats`,
  `DenBeatView`), and the bay's in the bay, as kie's homecoming does (`KieHomecoming`, `BayHomecomingView`). The player's
  walking waits while it plays, and a tap skips it. Afterwards everyone's back where they were.
- **Speech bubbles only.** The den and the bay have no narrator, and the music is the den's theme throughout.
- **Solo only.** A skit never plays in a co-op room: it waits for the next visit on your own.
- **Always tones2 and kie (and kcaj, once he's home), whoever's picked,** as in the freeing scenes: they're the crew in
  the story. If the soldier you're playing is one of them, that's who you see.
- **One skit a visit,** never on the same visit as a beat (the first visit, a move-in or a homecoming), except where
  a skit says it follows one (Two of us now, Probably a Trollogram).

| Skit | Where | Trigger | Once-only key (proposed) | About |
|---|---|---|---|---|
| [Two of us now](#two-of-us-now) | The den | Straight after the first visit, the den's first opening | `skit-two-of-us` | 14 s |
| [Keep it down](#keep-it-down) | Mission one's coda, in play | The coda getting under way, on a replay | `skit-keep-it-down` | 3 s |
| [Five-second rule](#five-second-rule) | The den | The den's second opening, the visit after the first visit | `skit-five-second-rule` | 12.5 s |
| [Wanna race?](#wanna-race) | The tabk bay | The first visit to the bay after kie's tabk comes home | `skit-wanna-race` | 18.5 s |
| [Special delivery](#special-delivery) | The den, between missions | The first time the den opens after a Mission one replay | `skit-special-delivery` | 14 s |
| [Probably a Trollogram](#probably-a-trollogram) | The den | Straight after kcaj's move-in beat | `skit-probably-a-trollogram` | 19 s |

## Two of us now

**Picked by the owner** (2026-10-25), with the den's co-op button renamed: "And after this - the 'MATE' button appears
(lets change it to MULTI-PLAYER, with two rows multi and player to save space on the button)". It gives kie's parked
line from [the retired title scene](retired.md#retired-two-of-the-crew) its home.

**Plays** in the den straight after [the first visit](den.md#the-first-visit), on the same visit: the den's first
opening, after Mission one's first debrief. It's the moment the den's co-op button first appears. About 14 seconds,
three shots; tap to skip, and a skip still puts the button up. **Who's in it:** tones2 and kie.

**How it fits the co-op reveal** (as built today, and what changes):
- **Today** the den's co-op button (`+ MATE` on the couch's empty seat, `DenConsole`) shows whenever co-op is open:
  the profile's `coop`, set when Mission one is won solo (`Profile`, `Story.OpensCoop`). So it's there from the very
  first visit, with no moment of its own. The win that opened co-op says so on its victory screen
  (`ScreenWords.CoopOpened`: CO-OP IS OPEN: PRESS + MATE IN THE DEN TO PLAY WITH A FRIEND). The `coop` reveal
  (`Reveals.Coop`) is marked seen the first time the button is pressed (`Game.JoinCoop`), not when it shows.
- **With this skit** the button stays hidden on the first visit until the skit puts it up at its end; from then on
  it shows whenever co-op is open, as now. The skit's own key (`skit-two-of-us`) remembers it, and `coop` keeps its
  meaning: the button first pressed.
- **A phone that's past it:** where co-op is open and the skit hasn't played (a profile from before it was built, or
  from the cloud), it plays on the next solo visit to the den, unless `coop` is already seen: a player who has pressed
  the button has found co-op, so the button just shows. `?unlock=coop` and the `?test=` starts show the button at once,
  with no skit.
- **The rename** is the owner's (25 Oct) and its own build card: the button reads MULTI-PLAYER, on two lines (MULTI,
  then PLAYER) to fit the button. The words that name it change with it: the victory screen's line and the den's hint
  (`ScreenWords.MateButton`, `CoopOpened`, `CoopWaiting`). This script names it MULTI-PLAYER.

```text
1. INT. THE DEN - NIGHT                                 4 s

The den, just warmed up from the first visit: lamp
light, string lights, the TV glowing. TONES2 flops into
the left seat of the couch, a controller in his hands.

KIE picks the second controller up off the empty right
seat and looks at it.

                    KIE
          TWO OF US NOW.

                                                   CUT TO:

2. INT. THE DEN - CONTINUOUS                            5 s

kie turns to the camera and holds the controller out to
us, at arm's length.

                    KIE
          BRING A MATE.

tones2 puts his hand up.

                    TONES2
          I'M RIGHT HERE.

                                                   CUT TO:

3. INT. THE DEN - CONTINUOUS                            5 s

kie doesn't look round.

                    KIE
          ANOTHER MATE, MATE.

He tosses the controller at the camera. It leaves his
hand, and where it lay on the empty seat the co-op
button pops up, in the co-op blue, and pulses once.

SUPER (THE BUTTON, ON TWO LINES): MULTI- / PLAYER

SFX: a chip clunk.

kie drops onto the couch arm, pleased with himself.

                         INTO: THE DEN (the button stays up)
```

**Staging notes:** the den and both figures as they are. The controllers are painted into the couch today, so a
controller in kie's hand (and one in tones2's) is a small new prop, and kie's toss is that prop leaving the screen.
The button appearing is the den's own button, shown at that moment. The pulse is one soft pulse, never a flash.

## Keep it down

**Picked by the owner** (2026-10-25).

**Plays** in Mission one's [coda](../story.md#the-coda), in play, on replays only: once the coda has been cleared
before, the same rule that drops its prompts, so the first play stays clean. It plays as the coda gets under way, which
is when tones2 climbs into his tabk or walks on without it (`World.CodaUnderway`), right beside kie asleep against the
wall (`SleepingKie`). Once ever. About 3 seconds; you keep control throughout, and nothing stops or waits. Mission one
is always solo, so it's never in co-op. **Who's in it:** tones2 and kie, asleep.

```text
1. EXT. THE MOTOR POOL'S BACK GATE (IN PLAY)            3 s

The coda's start: alarms going, the back gate up, goons
pouring in. KIE sleeps against the wall with the snacks
in his lap. TONES2's tabk engine turns over right beside
his head.

SFX: the tabk's engine.

kie stirs, without opening his eyes.

                    KIE (ASLEEP)
          ...KEEP IT DOWN.

He settles back. Zs. The coda carries on.
```

**Staging notes:** today kie's line shows as a score popup over him (`Scoring.Popup`, the way the pilots' last words
show), in kie's colours; a proper speech bubble in play would be new. `SleepingKie` is built and stays as he is. If the
owner wants it on every replay rather than once, it can drop the key.

## Five-second rule

**Plays** in the den, the second time it opens: the first visit after [the first visit](den.md#the-first-visit), after
Mission one. About 12.5 seconds, three shots; tap to skip. **Who's in it:** tones2 and kie.

**What it settles:** where the snacks tones2 brings kie in the motor pool came from ([freeing kie](mission-one.md#freeing-kie)),
since he dropped the bag in the den in [the opening](mission-one.md#the-opening). He picked them up off the floor on
his way out.

```text
1. INT. THE DEN - NIGHT                                 4.5 s

The den, put back together: lamp light, string lights,
the TV on. KIE sprawls on the couch with the snack bag
from the motor pool in his lap, eating. TONES2 is at his
slushie machine.

kie stops, a chip halfway to his mouth, and looks at it.

                    KIE
          WHERE'D THESE COME FROM?

                                                   CUT TO:

2. INT. THE DEN - CONTINUOUS                            5 s

tones2 looks down at the floor by the side door, where
the bag burst on raid night. A few chips are still
there.

SFX: a slurp of slushie.

                    TONES2
          FIVE-SECOND RULE.

kie looks at the chips on the floor. Then at the bag.

                    KIE
          IT WAS A WHOLE MISSION.

                                                   CUT TO:

3. INT. THE DEN - CONTINUOUS                            3 s

kie shrugs, and eats the chip.

SFX: a crunch.

                    KIE
          ...LEGEND.

                                            INTO: THE DEN
```

**Staging notes:** existing pieces only: the den, kie sitting on the couch (his seat pose), the `bag` in his lap,
`chips spill` by the side door (where the opening spilled them), tones2 at his station. The chips stay on the floor
after it, as part of the room, or go: a choice for the staging card.

## Wanna race?

**Plays** in [the tabk bay](../lobby.md#the-tabk-bay), the first visit after [kie's tabk comes home](den.md#kies-tabk-comes-home),
after Mission one. About 18.5 seconds, four shots; tap to skip. **Who's in it:** tones2, kie and the little gold
Trollogram of THE PROJECTIONIST from the homecoming.

```text
1. INT. THE TABK BAY - NIGHT                            4 s

Under the roller door and along the floor: kie's tabk,
THE PROJECTIONIST, in its space downstairs, frost on its
copper pipes. KIE kneels by its tracks with a rag,
polishing.

TONES2 walks up from the roller door, hands in pockets.

                                                   CUT TO:

2. INT. THE TABK BAY - CONTINUOUS                       5 s

tones2 nods at the tabk.

                    TONES2
          WANNA RACE?

kie stops polishing. He turns round, slowly.

                    KIE
          I HAVE NEVER ONCE OFFERED TO RACE.

                                                   CUT TO:

3. INT. THE TABK BAY - CONTINUOUS                       5 s

The lens glows gold. Up on the bay wall above the tabk,
its little Trollogram flickers up, the same one that
waved at the homecoming, and bobs keenly.

SFX: the projector's whirr; a soft crackle.

                    LITTLE TROLLOGRAM (GLITCHY)
          I WOULD.

kie and tones2 look up at it.

                                                   CUT TO:

4. INT. THE TABK BAY - CONTINUOUS                       4.5 s

Without looking, kie reaches round the back of the
projector and pulls a cable. The little Trollogram
fizzles out.

SFX: a plug pulled; a fizzle.

kie goes back to polishing.

                    KIE
          WE DON'T RACE.

                                       INTO: THE TABK BAY
```

**Staging notes:** the bay and kie's tabk as they are; kie polishing is his repair pose. The little Trollogram is the
homecoming's (`BayHomecomingView`), in the friendly gold, never the villains' cyan; a bubble for it is new, flickering
as a Trollogram's does. The bay scrolls, so the camera runs from the roller door to kie's tabk in shot 1, as the bay's
other beats do.

## Special delivery

**Plays** between missions: the first time the den opens after a replay of Mission one, from its debrief. About 14
seconds, three shots; tap to skip. **Who's in it:** tones2, kie and the cargo drone.

**What it settles:** how kie gets home. Mission one leaves him asleep against the motor pool wall
([freeing kie](mission-one.md#freeing-kie)), and nothing showed him getting back. The drone brings him, every time.

```text
1. INT. THE DEN - NIGHT                                 4 s

The den, quiet. TONES2 sits on the couch alone, the
tabk's remote in his hand, watching the roller door.

                    TONES2
          ANY SECOND NOW.

The roller door rattles up.

SFX: the roller door.

                                                   CUT TO:

2. INT. THE DEN - CONTINUOUS                            6 s

The CARGO DRONE flies in under the roller door. Hanging
off its cable by the belt, face down and fast asleep,
is KIE, the snack bag still in his hand. Zs.

SFX: the drone's whine.

It carries him across the room and lowers him onto the
couch beside tones2. The hook lets go, and the drone
climbs away and out under the door.

SFX: a soft flump on the cushions; the drone going.

                    TONES2
          SAME AS LAST TIME.

                                                   CUT TO:

3. INT. THE DEN - CONTINUOUS                            4 s

kie, still asleep, rolls over and hugs the snack bag.

                    KIE (ASLEEP)
          ...LEGEND.

tones2 puts the remote down and turns the TV back on.

                                            INTO: THE DEN
```

**Staging notes:** existing pieces only: the den, the `drone` and its `cable`, the `remote`, the `bag`, kie's crouch
pose for hanging and lying, and `zz` on kie. The roller door rolls up and down as the den's own does
(`DenRollerDoor`). If the owner prefers, its shot 2 could instead be a short prelude to the first visit; then it plays
once, after Mission one's first debrief, and the replay trigger goes.

## Probably a Trollogram

**Plays** in the den straight after kcaj's [move-in beat](den.md#a-crewmate-moves-in), the first visit after Mission
two: the one case where a skit follows a beat on the same visit. About 19 seconds, four shots; tap to skip. **Who's in
it:** tones2, kie and kcaj (up in his nook on the upper floor).

**What it settles:** what the sticky note says that kcaj sticks on tones2's back at the end of
[freeing kcaj](mission-two.md#freeing-kcaj). It's been on there ever since.

```text
1. INT. THE DEN - DAY                                   4 s

kcaj has just gone up to his nook; his corner glows. TONES2
turns back to his slushie machine, and we see his back:
the yellow sticky note from the loading yard, still stuck
there.

KIE, at his bench, sees it.

                    KIE
          THERE'S A NOTE ON YOUR BACK.

                                                   CUT TO:

2. INT. THE DEN - CONTINUOUS                            5 s

tones2 turns round to look at it. And round. And round,
like a dog after its tail. The note stays on his back.

                    TONES2
          WHAT'S IT SAY?

                                                   CUT TO:

3. INT. THE DEN - CONTINUOUS                            5 s

kie walks over and peels it off. He reads it out.

                    KIE
          "PROBABLY A TROLLOGRAM."

tones2 looks down at his own hands, and turns them over.

                    TONES2
          ...AM I?

                                                   CUT TO:

4. INT. THE DEN - CONTINUOUS                            5 s

Upstairs, KCAJ leans out of his nook over the rail, a
pen behind his ear, squinting down at tones2.

                    KCAJ
          STILL CHECKING.

                    TONES2
          OMFG.

SFX: a sticky note slapped onto the string board.

                                            INTO: THE DEN
```

**Staging notes:** existing pieces only: the den (by day, as the move-in beat is), the `note` on tones2's back
(`note on tones2 -6 16`, where the freeing left it), and the three figures at their places. kie holding the note is
the note riding on him. kcaj's nook is on the den screen, so nothing scrolls.
