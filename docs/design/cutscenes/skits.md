# Skits

Part of [the cutscenes](README.md), the one script for every scene: the rules, the format and every scene at a glance are there.

Short comic crew moments, written for the skit-scripts card (2026-10-25). The owner's picks (25 Oct) are [Two of us now](#two-of-us-now), [Keep it down](#keep-it-down),
[Five-second rule](#five-second-rule), [Special delivery](#special-delivery) and [They're tracking us](#theyre-tracking-us), with
[A Trollogram of anything](#a-trollogram-of-anything) and [Does it do cones?](#does-it-do-cones) workshopped by him.
He dropped Wanna race? from the pitch. Staging, art, sound and the build each get their own card once a skit is picked.

**How a skit plays:**
- **Once ever,** like the den's beats ([the den](den.md)), each under its own once-only key, kept with the player's
  progress so it follows a sign-in. The keys below are proposals: a key goes in `OnceOnly.All` when its skit is built,
  and never changes after.
- **In the room itself,** not as a scene file (Keep it down plays in a mission, in play: see its note): the den's skits play in the den, as its beats do (`DenBeats`,
  `DenBeatView`), and the bay's in the bay, as kie's homecoming does (`KieHomecoming`, `BayHomecomingView`). The player's
  walking waits while it plays, and a tap skips it. Afterwards everyone's back where they were.
- **Speech bubbles only.** The den and the bay have no narrator, and the music is the den's theme throughout.
- **Solo only,** but for They're tracking us, at the debrief, which each phone has its own of. Otherwise a skit never
  plays in a co-op room: it waits for the next visit on your own.
- **Always tones2 and kie (and kcaj, once he's home), whoever's picked,** as in the freeing scenes: they're the crew in
  the story. If the soldier you're playing is one of them, that's who you see.
- **One skit a visit,** never on the same visit as a beat (the first visit, a move-in or a homecoming), except where
  a skit says it follows one (Two of us now, A Trollogram of anything).

| Skit | Where | Trigger | Once-only key (proposed) | About |
|---|---|---|---|---|
| [Two of us now](#two-of-us-now) | The den | Straight after the first visit, the den's first opening | `skit-two-of-us` | 14 s |
| [Keep it down](#keep-it-down) | Mission one's coda, in play | The coda getting under way, on a replay | `skit-keep-it-down` | 3 s |
| [Five-second rule](#five-second-rule) | The den | The den's second opening, the visit after the first visit | `skit-five-second-rule` | 11 s |
| [Special delivery](#special-delivery) | The den, between missions | The first time the den opens after a Mission one replay | `skit-special-delivery` | 14 s |
| [A Trollogram of anything](#a-trollogram-of-anything) | The den | Straight after kcaj's move-in beat | `skit-trollogram-of-anything` | 15 s |
| [Does it do cones?](#does-it-do-cones) | The tabk bay | The first visit to the bay after kcaj's tabk comes home | `skit-cones` | 18 s |
| [They're tracking us](#theyre-tracking-us) | The debrief | Mission two's first clear, solo or co-op | `skit-tracking-us` | 12 s |

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

**Built** (2026-10-25): under the key `skit-keep-it-down` (`KeepItDown`, `Core/Missions/MissionSkits.cs`); kie stirs and
says it 0.6 s after the coda gets under way, and his Zs stop until the popup's gone. The engine's sound plays when tones2
has climbed into his tabk (walking on without it, kie still says it). `?test=coda` shows it every time without spending it.

**Staging notes:** today kie's line shows as a score popup over him (`Scoring.Popup`, the way the pilots' last words
show), in kie's colours; a proper speech bubble in play would be new. `SleepingKie` is built and stays as he is. If the
owner wants it on every replay rather than once, it can drop the key.

## Five-second rule

**Picked by the owner** (2026-10-25).

**Plays** in the den, the second time it opens: the first visit after [the first visit](den.md#the-first-visit), after
Mission one. About 11 seconds, three shots; tap to skip. **Who's in it:** tones2 and kie.

**What it rests on:** the snacks tones2 brings kie in the motor pool ([freeing kie](mission-one.md#freeing-kie)) are a
fresh bag from Family Mart, not the bag he dropped in the den in [the opening](mission-one.md#the-opening) (the owner,
25 Oct: [story](../story.md#lore)). tones2 says otherwise, to wind kie up, and the bag gives him away.

```text
1. INT. THE DEN - NIGHT                                 4 s

The den, put back together: lamp light, string lights,
the TV on. KIE sprawls on the couch with the snack bag
from the motor pool in his lap, eating. TONES2 is at his
slushie machine.

kie stops, a chip halfway to his mouth.

                    KIE
          WHERE'D THESE COME FROM?

                                                   CUT TO:

2. INT. THE DEN - CONTINUOUS                            3.5 s

tones2 nods at the floor by the side door, where the bag
burst on raid night. Dead straight face.

                    TONES2
          FIVE-SECOND RULE.

                                                   CUT TO:

3. INT. THE DEN - CONTINUOUS                            3.5 s

kie turns the bag round. The label: FAMILY MART, crisp
and new. He looks at it, then at tones2, unconvinced.

tones2 slurps his slushie, loudly.

                    KIE
          ...LEGEND.

kie eats the chip.

                                            INTO: THE DEN
```

**Staging notes:** the den, kie sitting on the couch, the `bag` in his lap, tones2 at his station. New: the bag's
FAMILY MART label, big enough to read as kie turns it, which the stage's `bag` doesn't have today. Family Mart is a real
shop, so whether its name goes on screen as it is, or a look-alike label, is the owner's call on the art card.

## Special delivery

**Picked by the owner** (2026-10-25).

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

## A Trollogram of anything

**Reworked by the owner** (2026-10-25) around this exchange, in his words: "lets make it so that kcaj asks 'can you make
a trollogram of 'anything'? / kie: yeah why do you ask / ...Silence... / kie: absolutely not."

**Plays** in the den straight after kcaj's [move-in beat](den.md#a-crewmate-moves-in), the first visit after Mission
two: the move-in ends with kcaj heading for his nook, and this picks up as he gets there. About 15 seconds, four shots;
tap to skip. **Who's in it:** kcaj (up in his nook on the upper floor), kie (at his bench) and tones2 (at his slushie
machine). The den screen holds all three, so nothing scrolls.

```text
1. INT. THE DEN - DAY                                   4 s

KCAJ, just moved in, stands in his nook on the upper floor
by his string board, his corner lit. He leans out over
the rail and looks down at KIE, who is at his bench
below, tinkering with the projector.

                    KCAJ
          CAN YOU MAKE A TROLLOGRAM OF ANYTHING?

                                                   CUT TO:

2. INT. THE DEN - CONTINUOUS                            3 s

kie doesn't look up.

                    KIE
          YEAH. WHY DO YOU ASK?

                                                   CUT TO:

3. INT. THE DEN - CONTINUOUS                            5 s

Silence. Nobody speaks.

kcaj looks at the projector on kie's bench. Then, slowly,
he turns and looks at his string board: every note, every
red string. Then he looks back down at kie.

Below them, TONES2 is halfway through a slurp of
slushie. He stops, straw in his mouth, and looks from one
to the other.

(No bubbles, no sound but the den's music, ducked. The
silence is the joke: it holds the whole shot.)

                                                   CUT TO:

4. INT. THE DEN - CONTINUOUS                            3 s

kie puts the screwdriver down.

                    KIE
          ABSOLUTELY NOT.

kcaj peels a sticky note off his board, folds it in two
and tucks it in his pocket, for later.

SFX: tones2 finishes the slurp.

                                            INTO: THE DEN
```

**Staging notes:** existing pieces only: the den (by day, as the move-in beat is), the three figures at their
stations, kie in his repair pose at the bench, the projector on his bench (painted, and its little Trollogram from his
idle cycle stays off), the `note` from kcaj's board to his hand. Shot 3 is staged by looks alone: kcaj turning to face
each thing in order (the bench, the board, kie), and tones2 frozen mid-slurp. The music ducks for it, as it does under
a line.

**The sticky note on tones2's back** (from [freeing kcaj](mission-two.md#freeing-kcaj)) is no longer this skit's: what it
says is still open.

## They're tracking us

**Picked by the owner** (2026-10-25: "Absolutely").

**Plays** at [the debrief](mission-one.md#the-first-debrief) on Mission two's first clear, as its numbers count up.
Once ever. About 12 seconds, three shots. The debrief's first part waits for it, and a tap skips it as it fills the
numbers. **The one skit that plays in co-op:** each phone shows its own player's debrief, so it plays on each phone
whose first clear it is, on its own, with nothing to wait for between the two. **Who's in it:** the crew to one side of
the debrief, tones2, kie and, for the first time, kcaj.

```text
1. INT. THE DEBRIEF SCREEN                              4 s

MISSION COMPLETE! The crew stand to one side: TONES2,
KIE, and KCAJ, still a little frosty, on the debrief for
the first time. The numbers count up: score, time,
goons.

ACCURACY starts to count. kcaj leans in towards it,
slowly, and stares.

                    KCAJ
          ACCURACY? THEY'RE TRACKING US.

                                                   CUT TO:

2. INT. THE DEBRIEF SCREEN - CONTINUOUS                 3.5 s

kie doesn't turn his head.

                    KIE
          IT'S THE SCORE.

                                                   CUT TO:

3. INT. THE DEBRIEF SCREEN - CONTINUOUS                 4.5 s

                    KCAJ
          THAT'S WHAT THEY WANT YOU TO THINK.

He peels a sticky note off his pad and slaps it over the
accuracy number. tones2 looks at the note, and then at
the player.

SFX: the note.

The note stays there for the rest of the debrief. The
counting carries on underneath it.

                            INTO: THE DEBRIEF (the XP bar)
```

**Built** (2026-10-25): under the key `skit-tracking-us` (`TrackingUs`, `Core/Missions/MissionSkits.cs`, played by
`DebriefCrew`). kcaj now stands on every debrief once he's freed, frosty on this one. The accuracy counts slowly while he
stares and is still counting under the note. A tap skips to the skit's end, with the note on. `?test=debrief&mission=2`
shows it every time without spending it.

**What's new work:** kcaj on the debrief. `DebriefView` draws only tones2 and kie today, so a third figure (kcaj's,
which the stage already has) and room for him are new; which crew stand on the debrief at all is undecided, and this
needs at least the freed crew. The `note` is an existing stage prop, but putting it over one of the debrief's numbers
and keeping it there is new staging. The skit never touches the numbers themselves: the accuracy is still counted and
saved as before. No narrator, as the debrief has none.

## Does it do cones?

**Workshopped by the owner** (2026-10-25), in his words: "at the mention of 'cones' Kie comes running in - but it's not
that type ok cone....". What kie hoped for is a wink: nobody names it, and the joke is his face.

**Plays** in [the tabk bay](../lobby.md#the-tabk-bay), the first visit after kcaj's tabk, SOFT SERVE, comes home, after
Mission two. About 18 seconds, four shots; tap to skip. **It needs kcaj's tabk homecoming first:** that beat isn't
written yet ([kcaj's tabk](../tabks/kcaj.md) says how the tabk gets home, and nothing plays it), so this skit waits on
its card. **Who's in it:** tones2, kcaj and kie.

```text
1. INT. THE TABK BAY - DAY                              4 s

SOFT SERVE in its space downstairs: kcaj's purple and
gold, the soft-serve machine on top under its striped
awning, the cone on its antenna. KCAJ leans on it,
polishing the chrome nozzle.

TONES2 walks up and eyes the swirl on top.

                    TONES2
          DOES IT DO CONES?

                                                   CUT TO:

2. INT. THE TABK BAY - CONTINUOUS                       4.5 s

Running feet. KIE comes in under the roller door at a
sprint and pelts the whole length of the bay floor, the
camera running with him, grinning ear to ear. He skids
to a stop between them.

SFX: the roller door; running feet; a skid.

                    KIE
          DID SOMEONE SAY CONES?

                                                   CUT TO:

3. INT. THE TABK BAY - CONTINUOUS                       4 s

kcaj swings the nozzle round on to tones2 with a little
flourish. The swirl wobbles, and the nozzle pipes out
two big soft-serve cones, one either side of his aim.
They splat on tones2, one in each hand.

SFX: the machine's whirr; two splats.

                    KCAJ
          IT DOES TWO.

                                                   CUT TO:

4. INT. THE TABK BAY - CONTINUOUS                       5.5 s

kie looks at the cones. At tones2, dripping. At the
cones again. His grin falls, slowly, all the way down.

                    KIE
          ...OH. THAT KIND.

He turns round and walks back the way he came, slowly,
hands in his pockets.

tones2, still dripping, licks one of the cones.

                    TONES2
          ...OMFG.

                                       INTO: THE TABK BAY
```

**Staging notes:** the bay and SOFT SERVE as they are (its swirl wobble and nozzle pivot are the tabk's own, from
driving it); the `cones` prop in tones2's hands; kie's run is a walk along the bay with the camera following, as the
Mission two briefing's is. New: the cones splatting on a soldier (a small effect), and kie's face falling, which his
figure has no frames for today, so it's his whole pose that sells it (a slump, head down) unless a new face is drawn.
