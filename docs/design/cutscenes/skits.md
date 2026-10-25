# Skits

Part of [the cutscenes](README.md), the one script for every scene: the rules, the format and every scene at a glance are there.

Short comic crew moments, written for the skit-scripts card (2026-10-25): **drafts, for the owner to pick from.** These
four are the pitch's short list. Staging, art, sound and the build each get their own card once a skit is picked.

**How a skit plays:**
- **Once ever,** like the den's beats ([the den](den.md)), each under its own once-only key, kept with the player's
  progress so it follows a sign-in. The keys below are proposals: a key goes in `OnceOnly.All` when its skit is built,
  and never changes after.
- **In the room itself,** not as a scene file: the den's skits play in the den, as its beats do (`DenBeats`,
  `DenBeatView`), and the bay's in the bay, as kie's homecoming does (`KieHomecoming`, `BayHomecomingView`). The player's
  walking waits while it plays, and a tap skips it. Afterwards everyone's back where they were.
- **Speech bubbles only.** The den and the bay have no narrator, and the music is the den's theme throughout.
- **Solo only.** A skit never plays in a co-op room: it waits for the next visit on your own.
- **Always tones2 and kie (and kcaj, once he's home), whoever's picked,** as in the freeing scenes: they're the crew in
  the story. If the soldier you're playing is one of them, that's who you see.
- **One skit a visit,** never on the same visit as a beat (the first visit, a move-in or a homecoming), except where
  a skit says it follows one.

| Skit | Where | Trigger | Once-only key (proposed) | About |
|---|---|---|---|---|
| [Five-second rule](#five-second-rule) | The den | The den's second opening, the visit after the first visit | `skit-five-second-rule` | 12.5 s |
| [Wanna race?](#wanna-race) | The tabk bay | The first visit to the bay after kie's tabk comes home | `skit-wanna-race` | 18.5 s |
| [Special delivery](#special-delivery) | The den, between missions | The first time the den opens after a Mission one replay | `skit-special-delivery` | 14 s |
| [Probably a Trollogram](#probably-a-trollogram) | The den | Straight after kcaj's move-in beat | `skit-probably-a-trollogram` | 19 s |

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
