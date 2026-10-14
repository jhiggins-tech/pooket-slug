# Pooket Slug: design

The game design for Pooket Slug, the second Pooket game: a Metal Slug-style run-and-gun starring the crew from [Pooket Tabks](https://github.com/jhiggins-tech/pooket-tabks). It records what the owner decided in a brainstorm on 14 Oct 2026 about bringing Tabks' crew, weapons, progression, ranks and stats across, and what's still open. Everything here is decided except what's marked open (mostly under [Open questions](#open-questions)).

**Read this when** you're deciding what Pooket Slug is, or building any part of it.

**For sign-off**, the design is also written up as four readable documents in [docs/design](docs/design/README.md): story and cutscenes, tanks, player mechanics, and maps and progression. They add progressive disclosure: the game reveals tanks, driving and co-op only as the player reaches them. They also hold the owner's later answers, and where they differ from this file, they win.

The tech spike (Unity 6.3 WebGL built by GameCI, with Photon Fusion co-op) is on the [`claude/gallant-keller-3qzp97`](https://github.com/jhiggins-tech/pooket-slug/tree/claude/gallant-keller-3qzp97) branch, with research notes in `docs/research/` (start at `AGENTS.md`). [Building on the spike](#building-on-the-spike) says how this design lands on it.

## The pitch

The crew climb out of their tanks and fight on foot. Their tanks have been stolen, so each one is a boss first, and then yours to ride. It plays like arcade Metal Slug: hordes of goons, chunky explosions, lives and continues. Missions come first (the Roguelite is on ice), live online co-op opens once you've beaten Mission one solo, and it's made for phones.

## Story

kie's Trollogram holograms went rogue and stole the crew's tanks. The goons are glitchy hologram soldiers. Each mission ends with a crew tank piloted by a hologram of its crewmate: beat it, the fake pilot glitches out, and the real one is freed.

## Modes

### Missions
- Handmade levels, the story, and live online co-op.
- **You start with tones2 alone.** Each mission ends with another crewmate's tank, and beating it frees its pilot to play from then on.
- **Mission one frees kie**: his holograms got loose, so freeing him starts the story. **You have to beat it solo before co-op opens.**
- **There's one mission to start**: make it great, then add a crewmate at a time. Every crewmate after tones2 arrives with their own mission, the coming-soon crew (shotdownboyz, lankcity, odsey and doctorfox) included, the way Coming soon works in Tabks.

### Mission one
- **A military base**, Metal Slug played straight: sandbags and watchtowers, ending at the motor pool where the stolen tanks are parked.
- **About 5 minutes**: a run through, then kie's tank.
- **You play tones2**, the only soldier you have at the start, and solo ([Missions](#missions)).
- **Straight in**: no tutorial. The button labels and an ⓘ screen, as in Tabks, cover the kit.
- **Goons**: the spike's three, as flickering holograms: riflemen (crouch under their shots), knife runners and bomb-dropping drones.
- **The boss, kie's tank**, cycles through its Tabks kit, faster as it gets hurt:
  - **Weasel Pop**: waves of weasels that scurry at you and pop. Jump them, or hose them down with ten-1.
  - **Trollogram**: it splits into hologram copies that shuffle round, and you have to hit the real one. A shot fake blows up, as in Tabks.
  - **Steal**: its roulette spins over your kit and takes one thing (a crate's upgrade, or your special) until you hit it hard enough to drop it.
- **The pilot round**: a hologram kie on foot, with Weasel Pop, Trollogram decoys and Steal. Beat him and the real kie is freed.

### Roguelite (on ice)
**On ice for now** (owner, 14 Oct): Missions come first. What was decided is kept here for when it's back.
- A separate mode of seeded runs. **A run is five stages**, with a crew tank at the end of each, **then it loops** round at a higher alert, as in Risk of Rain. Depth keeps counting through the loops.
- **Coins**: goons drop them, and **between stages there's a shop** selling [upgrade crates and gun crates](#crates), hearts and 1-ups. Whatever a run picks up, coins included, is gone when it ends.
- Any crew tank can be a boss, tones2's included, and every tank you beat is yours to ride for the rest of the run.
- **The alert level climbs as you go**, as in Risk of Rain: it goes up at each boss and every few minutes, so playing fast keeps it low. Each level brings more goons, faster bullets (fired more often, flying faster) and tougher tanks (more health, and an extra attack).
- **Co-op too, with one purse**: both players' coins go into it, and you agree at the shop.
- **Still open, for when it's back**: the shop's prices, how two players agree at it, whether beating a crew tank in a run frees its pilot, and how often a run lets you call in a tank.

## On foot

### Health and lives
- **Hearts are Tabks health**: 100 health, drawn as 5 hearts of 20, with Tabks' damage numbers floating off you. Every Tabks number carries over: Take a Nap heals to full, Band Aid up to 40%, Shower Beers 50%, and torikloud starts on 150 (7½ hearts).
- **Lives and continues**, each player their own. In co-op, a 1-up goes first to a partner who's out, so a survivor can bring them back.

### Controls: the arcade pad

```text
+--------------------------------------+
| x3   12,400            SUPER [###--] |
|                          (AB)        |
|    ^                     (SP)  (SU)  |
|  < o >                (JUMP) (FIRE)  |
|    v                                 |
+--------------------------------------+
```

- **Left thumb**: a stick to run; up aims up, down crouches (or aims down in the air).
- **Right thumb**: FIRE (hold to keep firing), JUMP, special (SP) and super (SU), plus the ability (AB) above the special for the soldiers who have one.
- **Calling in your tank**: hold the stick down and press SP.
- **In a tank**, the stick swings the turret and FIRE is held to build power, as in Tabks.

### The kit
Each soldier's Tabks tiers become a hero kit:
- **Tier 1 → primary**: unlimited.
- **Tier 2 → special**: 3 charges that refill over time.
- **Tier 3 → super**: charged by kills.
- **Bonus move → ability**, on a cooldown. larinovsky, garyoldmancorp and kiwicore have one.

### Crates
Pickups last the mission (or the run), so everyone starts level next time.
- **Upgrade crates**:
  - **Weapon levels**: a primary, special or super goes up a level (I → II → III). At II it forks one of two ways; each soldier's forks are under [The crew](#the-crew).
  - **Status mods**: whatever you fire picks up a Tabks status and puts it on what it hits. Stack a few and a primary becomes a debuff hose.
- **Gun crates**: a crewmate's gun, which replaces your primary until it's empty.

### Statuses
Tabks' statuses, with turns turned into seconds:
- **Burn**: damage over time (Hyperfixate).
- **Cooked**: deals half damage (the Rizzler).
- **Tattooed**: takes 25% more damage from everything (Tattoo Gun).
- **Pinned**: can't move, and a pinned flyer drops out of the sky (Sew).
- **Toxic**: mud and sludge that poison whatever walks through them (ten-2, ten-3).

## Tanks
- **Beat a crew tank and its pilot, then climb in.** Inside, Tabks rules apply: the tank's health bar, its Tabks kit with 5 / 3 / 1 rounds, the turret swinging round on the stick, and FIRE held to build power. It's slow, heavy artillery in a fast game. Eject before it blows.
- **Calling one in**: in Missions, once a mission, hold the stick down and press SP, and a tank you've won drops in beside you. You choose which tank on the mission screen (in the Roguelite, at the start of each stage).
- **Soldiers and tanks mix**: tones2 can ride kcaj's tank, Double Park, Unmedicated and all.

## Bosses
- **Each mission ends with a crew tank, fighting with its own Tabks kit**: kcaj's tank sweeps a Hyperfixate beam, then ends in Unmedicated. kie's is first ([Mission one](#mission-one)). The spike's tank boss (lobbed shells, and low bursts you jump) is where the code starts.
- **The pilot round**: wreck the tank and its hologram pilot jumps out to fight you on foot with that crewmate's own kit. It's a mirror match, and a taste of who you're about to free. Then it glitches out (Tabks has the hologram blast) and the real crewmate is freed.
- **Boss arenas are the only places with Tabks' pixel terrain**: blasts dig craters there, as they do in Tabks. Everywhere else, the ground is solid.

## Co-op
- **Live online**: two phones, both on screen. The spike does this with Photon Fusion 2 in Shared Mode, and a partner can drop in. There's no hotseat.
- **It opens once you've beaten Mission one solo.**
- **Each player has their own lives** ([Health and lives](#health-and-lives)).
- **Some kits help a partner**: Band Aid's pulses heal them, Tattoo Gun's marks boost their shots too, and Unmedicated never hurts them.

## The crew
In Tabks' setup order. Each Tabks weapon's own rules are documented in its kit file (`src/characters/kits/` in pooket-tabks).

### tones2 (id `tones`)
- **ten-1** (primary): hold FIRE for a jet that builds from spray to a clean arc in about a second. The droplets trickle damage and push goons back, and letting go resets the pressure. Splashback stays in: point-blank on something big, it knocks your aim off.
- **Fork at II**: Hydrant (more range and pressure) or Sprinkler (a wide fan).
- **ten-2** (special): half a second of shaking, then a rocket jump along your aim. The launch carpets the floor in toxic mud that poisons goons walking through it.
- **ten-3** (super): a second and a half of spew that melts anything close and leaves sludge that burns goons who walk through it.

### kie
- **Weasel Pop** (primary): each press tumbles out three weasels. Where they land, they scurry under the nearest goon and pop, to Pop Goes the Weasel. It's lobbed, so it reaches behind cover, but it's weak against flyers.
- **Fork at II**: Ferret Frenzy (more, smaller weasels) or Badger (one big one that won't stop chasing).
- **Trollogram** (special): drop a hologram of yourself that goons shoot instead of you, and press again to swap places with it. A shot hologram blows up on everything nearby. The running joke: these are the ones that went rogue.
- **Steal** (super): the roulette spins and kie lifts a random power-up off the toughest enemy on screen. The tougher it is, the bigger the prize: a status mod off a goon, say, or a weapon level or a whole crew gun off a boss tank.

### kcaj
- **Double Park** (primary): twin cones, either side of your aim.
- **Fork at II**: Triple Scoop (a third cone) or Brain Freeze (cones slow what they hit).
- **Hyperfixate** (special): the beam sticks to the first thing it touches and follows it while you hold the special, burning harder the longer it stays on. Meanwhile the screen edges dim, and anything else is free to shoot you.
- **Unmedicated** (super): 120 pills rain across the whole screen, bounce twice and pop. They never hurt kcaj or a co-op partner.

### torikloud (150 health)
- **Debate** (primary): hold FIRE and a random legal word comes out, one letter per bullet. Longer words are longer bursts: SUBPOENA is 8 rounds, CERTIORARI 10.
- **Fork at II**: Filibuster (only long words) or Objection! (every word ends in a "!" that explodes).
- **Sonic Boom** (special): waves of sound through walls, strongest up close. They also wipe out enemy bullets in their path, and the kookaburra laughs from the clouds.
- **Twins** (super): a twin appears, and his health is split between them (75 / 75 from full). Like the Ice Climbers, it follows a step behind and copies everything he does a beat later: running, jumping and firing (it argues in social-work words). If it falls behind, it scrambles to catch up. While it stands, the super button is Yolk Sucker, which evens out their health. As in Tabks, he's only down when both are.

### ciarra
- **Frog leaps**: the best jumper in the game; hold JUMP to leap further.
- **Tattoo Gun** (primary): a needle SMG. Anything it hits is tattooed and takes 25% more damage from everything for a few seconds, a partner's shots included.
- **Fork at II**: Full Sleeve (tattoos last twice as long) or Flash Sheet (three needle streams in a fan).
- **Sew** (special): a needle and thread stitch along her aim, through walls. Anything stitched takes a hit and is pinned for two seconds.
- **Marathon** (super): she pins on a race bib and becomes the runner, sprinting a leg along the ground over any hill, invulnerable and flattening everything in her path. A leg is about a screen wide, and she still finishes with Tabks' 50-damage blast.

### larinovsky
- **Pill Pusher** (primary): lobs a mixed handful of pills whose blasts walk across the ground, so it reaches over cover.
- **Fork at II**: Extra Strength (bigger blasts) or Bulk Buy (twice the pills, but smaller).
- **the Rizzler** (special): a spinning heart in shades that homes in on anything close. Whatever's in its blast is cooked and deals half damage for a few seconds.
- **Women in Scam** (ability): for three seconds, any attack that hits you is scammed. You still take the damage, but you get rounds of that weapon to fire back: scam a boss tank and you're holding its own Tabks weapon.
- **Take a Nap** (super): two seconds asleep, waking at full health with the special recharged. The two cats stand guard and swat away whatever comes, but each dies comically on the first hit it takes: a yowl, a puff of fur, a little halo floating off. They're back for the next nap (nine lives).

### garyoldmancorp (beta)
- **In beta here too**: stand-in shots until his real moves are designed in Tabks, where the teasers were kamp karl, the crinkler and stop the violence.
- **Scooter**: the fastest soldier on foot. Hitting a wall at speed is a CRASH! for 5.
- **Diced Coffee** (ability): spin the wheel. Lactose free: every enemy freezes for 3 seconds, Tabks' skipped turn in real time. Full cream: you're launched straight up, and it's gone for the mission. The chance of full cream starts at 10% and goes up 10% with every win.

### kiwicore
- **béretta M2** (primary): he throws the flat cap off his head through walls, out along his aim and back, and it hits on both passes. He can't throw again until he catches it, so up close he throws faster.
- **Fork at II**: Spare Cap (a second cap to throw while the first is out) or Wide Brim (longer, wider loops).
- **Band Aid** (special): the drum solo. He's stuck at the kit for four bars of the thrasher, tapping FIRE on the beat, and every hit sends a healing pulse to everyone nearby, a co-op partner included. Perfect hits heal double, up to 40% in all, and the drumstick swats goons who get too close.
- **Shower Beers** (ability, as queued for Tabks): a shower curtain goes up round him, steam rises, a can of XXXX Gold appears in the sky, and he gets half his health back.
- **Stage Dive** (super): he leaps into the goons and crowd-surfs across them to the thrasher, hurting everything he rides over. It could fill his Tabks tier 3 too, which is a stand-in there.
- His Slug kit is complete, so he isn't in beta here.

### Coming soon
shotdownboyz, lankcity, odsey and doctorfox each arrive as a new mission, with their tank as its boss. Their Tabks teasers suit a platformer: lankcity's lizard walk could climb walls, and shotdownboyz's summon digger could tunnel (in boss arenas, the one place the ground breaks), with his "tank build" passive making him the one soldier who's still basically a tank.

## Progression
- **Characters**: free the crew ([Missions](#missions)). Unlike Tabks, XP never unlocks characters.
- **Career XP** comes from clearing missions (more the first time), kills (a little for a goon, more for a tank) and freeing a crewmate (a big bonus, the first time). Its levels only ever give cosmetics: hats (Tabks already has kiwicore's flat cap), skins, tank paint, death animations, kill effects and victory poses.
- **Within a mission or run**: [crates](#crates).

## Ranks
- **The ladder is Tabks'**: 33 ranks, with their insignia and jingles. 8 are known (Bronze, Silver, Gold, Platinum, Diamond, Master, Grandmaster, Champion) and 25 are hidden. As in Tabks, don't name the hidden ones (or describe their looks) in docs, changelogs, tests or commit messages.
- **For now, you climb slowly with stages beaten**: each stage you beat moves you a little way up the ladder. It's not a rating, so a rank never goes down.
- **When the Roguelite is back**, the plan was milestones for how far you get and the alert level you reach.
- **Feats unlock rungs**: each hidden rank's rung also needs its secret feat (clearing a mission without firing, say, or beating a tank with its own stolen weapon), so some steps can't be climbed until someone works out how.
- **By your name in Slug**: your Slug rank. Your Tabks rank stays in Tabks.

## Stats
Tabks' tally, per soldier and weapon: shots, hits, accuracy, damage dealt and taken, and kills.

## Building on the spike
The spike is Unity 6.3 WebGL, built by GameCI and published to https://jhiggins-tech.github.io/pooket-slug/, with online co-op on Photon Fusion 2.

- **What it already has**: running, jumping, crouching and aiming up (or down in the air), which is the arcade pad's stick; health; heavy machine gun, bomb and medkit crates; a tank boss; online co-op with drop-in; and art and sound made in code.
- **Mission one reworks what the spike has**: its first mission (LVL-01), its three goons (ENM-01) and its tank boss (BOS-01).
- **What changes**:
  - Health becomes hearts.
  - The respawn after a couple of seconds becomes lives.
  - The heavy machine gun crate becomes gun crates.
  - The medkit heals hearts, and the bomb crate refills specials.
- **What carries over from Tabks**: its code is TypeScript, so that stays behind, but its rules and numbers carry over. Each weapon is documented in its kit file, the rank ladder is in `src/stats/ranks.ts` and the tally is in `src/game/tally.ts`.
- **The roadmap**: the feature list (`docs/research/roadmap/feature-candidates.csv`) is waiting for its MoSCoW pass. This design needs these rows:

  | The design | Feature candidates |
  |---|---|
  | The arcade pad on phones | ACC-02 touch controls, ACC-08 mobile browser performance |
  | The crew's kits | CHR-02 roster, CHR-03 character select, CHR-05 special moves, TEC-02 data-driven content |
  | Lives and 1-ups | SCR-03 lives and continues |
  | Crates | WPN-11 weapon stock and swapping, WPN-12 weapon level-ups |
  | Coins and the shop (Roguelite, on ice) | WPN-13 random item drops (there's no shop row yet) |
  | Statuses | TRN-01 status effects framework |
  | Riding tanks | VEH-01 tank vehicle, VEH-02 enter, exit, eject and ram |
  | Calling in a tank | POW-05 supply drops (the closest row) |
  | Crew tank bosses | BOS-02 multi-phase bosses (the tank, then the pilot round) |
  | Missions | LVL-02 multiple missions, as the crew grows (one to start) |
  | The Roguelite (on ice) | LVL-14 endless or procedural mode |
  | Boss-arena terrain | LVL-10 destructible scenery, in arenas only |
  | Freeing the crew, opening co-op, and feats | SCR-09 unlockables, SCR-08 achievements |
  | Stats | SCR-07 per-player stats |
  | Career XP and cosmetics | CHR-04 cosmetics, PRE-03 death animations per weapon |
  | Band Aid and Stage Dive | AUD-02 music (a beat to drum to) |
  | Keeping it all | SOC-01 sign in with Google, SOC-02 cloud saves, TEC-07 save system |

  Not picked in the brainstorm: versus and ghost races (VRS-01, VRS-03), team lives (COP-03), couch co-op (COP-05, COP-06) and online leaderboards (SCR-10).
- **Accounts**: the accounts plan's player record holds best score, runs and wins, and its rules allow nothing else. This design adds freed crew, whether co-op is open (Mission one beaten solo), career XP, cosmetics (owned and worn), rank progress (stages beaten) and feats, and the tally. Each is a rules change in Cardmaster, so they're worth planning into the first rules pull request. Like the scores, they're written by the player's own phone, which the rules can't make honest: fine for personal progress, and this design has no public leaderboard.

## Considered, not picked
- **One-hit deaths**, as in Metal Slug: the owner pivoted to hearts.
- **PvP**: run-by-run duels on a shared seed, live versus, and rating players against the bosses. Ranks come from progress instead.
- **Other ways to play together**: passing the phone, relay turns online, and two players on one phone. Co-op is live online only.
- **Lasting growth**: a career shared with Tabks, and per-soldier mastery levels. Growth is crates within a mission (or run), and cosmetics from career XP.
- **Chosen alert levels**: DEFCON 5 → 1 picked before a run, or Hades-style twists. The alert climbs as you go instead.
- **More pixel terrain**: everywhere, or soft ground in levels. It's only in boss arenas.
- **Other run lengths**: three stages, or endless. It's five, then loop.
- **Between stages**: picking 1 of 3, a rest stop, or choosing the next boss. It's a shop.
- **Tanks in Missions**: parked in the level, or chosen at the start. You call yours in.
- **The ability button**: holding the special, tapping your portrait, or swiping up. It has its own button.
- **Roguelite co-op**: own coins each, or solo only. It's co-op with one purse.
- **A dearer shop** as the alert rises.
- **Tabks' rank in Slug**: on its own, beside Slug's, or whichever is higher. It's Slug's.
- **Tabks' three starters** (tones2, kie and kcaj). It's tones2 alone.
- **More missions to start**: five, four then garyoldmancorp's, or five and a finale. It's one.
- **Other boss tanks**: the spike's boss restyled for each, or fighting in Tabks turns. Each fights with its own Tabks kit.
- **Which tank comes**: always the newest, or a wheel. You choose before.
- **Mission one freeing** kcaj, ciarra or kiwicore. It frees kie.
- **Mission one elsewhere**: the servo car park, the hologram lab, or the suburbs. It's a military base.
- **A longer Mission one**: about 10 minutes, or 15+ with checkpoints. It's about 5.
- **Other goons**: ones that all blow up, hologram copies of the crew, or hologram mini-tanks. It's the spike's three, glitched.
- **kie's tank with one move**: only the shell game, the weasel waves or the pickpocket. It cycles through all three.
- **Ranks for now**: mission feats, mission difficulty levels, waiting for the Roguelite, or showing your Tabks rank meanwhile. You climb slowly with stages beaten.
- **The call-in elsewhere**: a button that pops up when it's ready, or a tank icon in the HUD. It's stick down + SP.
- **XP for feats**. It comes from clears, kills and freeing the crew.
- **Teaching tones2's kit** as you go, or in a training yard first. Mission one goes straight in.

## Open questions
- **Ranks**: what counts as a stage in Missions (a whole mission, or a stretch of one), how many stages a rung takes, and whether replays count. When the Roguelite is back: its milestone table, where depth keeps counting through the loops and the alert only climbs, so the two rise together and stalling to raise the alert needs thought.
- **Missions**: the order after kie.
- **Crouching specials**: stick down + SP calls your tank in, so can you still fire your special while crouching once a tank is ready?
- **Bosses**: each tank's attacks after kie's, worked out from its Tabks kit.
- **Career XP**: how much each source earns, and how it pays out (not decided yet): tokens you spend as in Tabks, a fixed track, a track per soldier, or mystery crates.
- **The 25 feats** for the hidden rungs.
- **Kits still to design**: the coming-soon crew's, and garyoldmancorp's real moves (designed in Tabks first).
- **Timings**: ten-1's build-up, ten-3's spew, Sew's pin, the scam window.
