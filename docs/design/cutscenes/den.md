# The den

Part of [the cutscenes](README.md), the one script for every scene: the rules, the format and every scene at a glance are there.

The den has no narrator: it's a menu with the crew in it, so it only has speech bubbles, and its music is the den's
own theme ([the den's theme](../music.md#the-dens-theme)).

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
waiting (owner, 2026-10-20, docs-progression-fixes). About 12 seconds, three shots; tap to skip. Once ever, kept with
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

2. INT. THE TABK BAY - CONTINUOUS                         6 s

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
runs 6 seconds, not 3, so its three lines can be read at about three words a second (the owner picked the longer shot,
2026-10-25, dca-homecoming-pace), which makes it about 12 seconds in all; in the third, kie's line comes in with the
name card so it gets its reading time too. `BeatReadingTests` holds this beat's lines, and the den's, to the rule. The crane lowers
the projector (the sprite's top 14 rows) onto the bare hull. A tap skips it, and a `?test=` jump never plays it.

## kcaj's tabk comes home

kcaj's tabk was wrecked in Mission two's boss fight, and it comes back rebuilt as SOFT SERVE
([kcaj's tabk](../tabks/kcaj.md)): the cargo drone hauls the wreck home from the creamery, and kcaj rebuilds it round
the soft-serve machine he brought out with him, as kie did with his projector. The joke is the creamery's own look
turned on the Trollograms: it's his now, in his colours, not Brain Freeze's. Drafted 2026-10-25
(kcaj-tabk-homecoming), for the owner to read before it's staged.

**Plays** in [the tabk bay](../lobby.md#the-tabk-bay) the first time the player walks in on their own once kcaj's
tabk is won (Mission two cleared, `TabkCards.Won`) and kcaj has moved in (`moved-in-kcaj`). The bay is through the
den, so the move-in always comes first; the check only makes sure of it. Never in a co-op room: like the skits, it waits for the next visit on your own,
since [Does it do cones?](skits.md#does-it-do-cones) plays straight on from it. About 13 seconds, three shots; tap to
skip. Once ever, under the key `kcaj-tabk-home` (proposed, as `kie-tabk-home` is kie's), kept with the player's
progress so it follows a sign-in. A `?test=` jump never plays it.

**Who's in it:** kcaj and tones2, whoever's picked, as in the skits, because Does it do cones? carries on with the same
two and brings kie in. This differs from kie's homecoming, where the second voice is the player's own soldier: here the
stage plays the crew, the soldier you walked in as steps out of the shot for the beat and the skit, and is back where
they were after. **The paint** ([kcaj's tabk](../tabks/kcaj.md)): kcaj's purple with his gold stripe, the machine's
pale grey, and the plain olive under it until the paint goes on in shot 3.

```text
1. INT. THE TABK BAY - DAY                              3 s

The repair stands are empty. The CARGO DRONE comes down
the bay from the far end, slow and low, a wreck hanging
off its cable: the plain olive tabk from the creamery,
scorched, frosted, cyan scuffs down its side, the cone
still on its antenna. It sets it down on the stands.

KCAJ wanders in after it, eating two cones at once.

SFX: the drone's whine, straining; a heavy thump as the
wreck sets down.

                                                   CUT TO:

2. INT. THE TABK BAY - CONTINUOUS                       6 s

The crane lowers a soft-serve machine onto the turret
ring: pale grey, a striped awning, a menu sign, and a
BRAIN FREEZE sticker on its side. kcaj steps back and
looks at it. Then at TONES2.

                    KCAJ
          THE CREAMERY. THE TROLLOGRAMS. THE MOON.

                    TONES2
          THE MOON?

                    KCAJ
          IT'S ALL CONNECTED.

                                                   CUT TO:

3. INT. THE TABK BAY - CONTINUOUS                       4 s

kcaj peels a sticky note off his pad and slaps it over
the BRAIN FREEZE sticker. It says KCAJ, in his scrawl.

He pulls the machine's lever. It hums, the swirl on top
wobbles, and his purple and gold run down the hull from
the machine like a drip, all the way to the tracks,
until no olive is left.

SFX: the note; the machine's whirr; a chip clunk.

SUPER (THE NAME CARD DROPS IN): KCAJ'S TABK ·
SOFT SERVE

                    KCAJ
          MINE NOW.

He takes a rag out of his pocket, leans on the tabk and
starts polishing the chrome nozzle.

                       INTO: DOES IT DO CONES? (the skit,
                       straight on, with SOFT SERVE still on
                       the stands)
```

**Reading time:** the lines are 6, 2 and 3 words in shot 2, which leaves the crane about 1.3 seconds before the first
one (as kie's does) and each line its three words a second; MINE NOW. comes in with the name card, as kie's last line
does. `BeatReadingTests` would hold it to that, as it holds kie's.

**Afterwards:** Does it do cones? plays at once, on the stands. When it ends, the drone lifts SOFT SERVE up to its
space on the top deck (`Garage`: kcaj's home is the top deck's first space) and swaps the player's loaded tabk back onto
the stands, as it does after kie's beat. During the beat and the skit the loaded tabk waits in its own space. A skip
ends the beat at its last frame and the skit still plays: it has its own key and its own tap.

**Staging notes** (for the build card), from what kie's beat already has (`KieHomecoming`, `BayHomecomingView`):
- **Reused:** the bay, the repair stands, the crane and its cable lowering a part onto the turret ring, the name card,
  speech bubbles, the `note` stage prop, the `cones` prop for kcaj's two cones, the drone and its cable, and SOFT
  SERVE's two frames as `KcajTabkArt` draws them (the second, while a shot charges, is the swirl's wobble in shot 3).
  kcaj and tones2 are the stage's own figures.
- **New, small:** SOFT SERVE cut in two, as `KieTabkArt.Bare` and `Turret` are: the hull and tracks, and the machine
  above them. The wreck is that hull in the plain tabk's olive (a paint swap, the way each `TabkCard` paints the
  tabk's two body colours), with frost and scorch over it; the cone on the antenna is on it already.
- **New, small:** the drone carrying a tabk in from off the screen. `DroneShow` only moves a tabk from one spot to
  another today; this is one move whose start is past the screen's right edge.
- **New, small:** the BRAIN FREEZE sticker on the machine's side (a few pixels of Brain Freeze's colours from the
  creamery's signs), and the note over it, which then stays. Whether the note stays on SOFT SERVE for good, in the bay
  and in missions, is the owner's call; the script only needs it till the skit ends.
- **New:** the paint running down. A wipe from the olive hull to the purple one, top to bottom, with a ragged edge so it
  reads as a drip. If that's too much for its card, a fade (as kie's frost fades) will do.
- **New:** the soldier you walked in as stepping out for the beat and the skit (see Who's in it). kie's beat has the
  player's own soldier speak, and this one can't, because the skit needs tones2, kcaj and kie whoever's picked.
- **Nothing pipes a cone.** The beat never shows the nozzle working, so the skit's question is still a question: the
  first cones out of it are the two it splats on tones2.
