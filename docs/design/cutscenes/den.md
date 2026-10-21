# The den

Part of [the cutscenes](README.md), the one script for every scene: the rules, the format and every scene at a glance are there.

The den has no narrator: it's a menu with the crew in it, so it only has speech bubbles, and its music is the title
theme ([the lobby](../lobby.md)).

## The first visit

The first time the den opens: straight after Mission one's first debrief (owner, 2026-10-19). About 4 seconds, once
ever. Kept with the player's progress, so it follows a sign-in. From [the lobby](../lobby.md), approved 2026-10-16.

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

## A crewmate moves in

The first visit to the den after a crewmate is freed. About 3 seconds; tap to skip. From [the lobby](../lobby.md),
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

## kie's tabk comes home

kie's tabk was wrecked in Mission one's boss fight, and it comes back straight after Mission one
([tanks](../tanks.md)), rebuilt as THE PROJECTIONIST (owner, 2026-10-19, kie-tabk-design): kie builds it round the
Trollogram projector that made Trollogram kie, pulled from the wreck, and since Trollograms run hot he wraps it in
copper freezer pipes, which frost it over. This beat plays the first time the player walks into
[the tabk bay](../lobby.md#the-tabk-bay) after Mission one is beaten, so the players who go looking find a second tabk
waiting (owner, 2026-10-20, docs-progression-fixes). About 10.5 seconds, three shots; tap to skip. Once ever, kept with
the player's progress, so it follows a sign-in. Drafted 2026-10-19 (kie-tabk-return-script); moved to after Mission
one 2026-10-20.

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

2. INT. THE TABK BAY - CONTINUOUS                       4.5 s

The crane lowers the projector onto the turret ring.
Frost on its body, steam curling off the copper pipes.
KIE slides out, pushes up his welding mask and pats it.

                    KIE
          IT USED TO PROJECT ME.

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
([picking another](../lobby.md#the-tabk-bay)). During the beat the player's loaded tabk waits in its own space, so the
stands are kie's; the cargo drone swaps them back as the beat ends.

**Built** (2026-10-20, kie-tabk-home-m1): kie's tabk is won once Mission one is cleared (`TabkCards.Won`), and this
beat plays the first time the bay opens after that, in the bay itself rather than as a scene file:
`Core/Tabks/KieHomecoming` (when it's due and its timing, tested) and `Ui/Den/BayHomecomingView`. The second shot
runs 4.5 seconds, not 3, so its three lines can be read, which makes it about 10.5 seconds in all. The crane lowers
the projector (the sprite's top 14 rows) onto the bare hull. A tap skips it, and a `?test=` jump never plays it.
