# Story and plot

The story of Pooket Slug, how it unfolds across the missions, and how reveals are shown. The cutscenes themselves, every
line and shot, are in one script: [cutscenes](cutscenes.md).

**Status:** signed off by the owner, 2026-10-14.

**Read this when** you're writing a mission's story, or anything that reveals something new to the player. For a
cutscene, read [cutscenes](cutscenes.md).

Up: [Design](README.md)

## The premise

- **The crew are missing, and only tones2 is left.** kie's Trollograms have gone rogue. They raid the crew's hangout while tones2 is out on a snack run, and he comes back to an empty room and glitchy Trollogram soldiers everywhere.
- **What's really going on comes out as you play.** The Trollograms have stolen the crew's tabks, and each tabk is driven by a Trollogram copy of its owner. The game keeps this to itself until the first tabk appears ([reveals](#reveals-are-story-beats)).
- **Every mission has the same shape.** You fight through to a crewmate's tabk, beat it, then beat its fake pilot. The fake glitches out, and the real crewmate is freed to play from then on.

## Tone

- **Arcade action, played straight:** hordes of goons, big explosions, and a boss at the end of every mission.
- **The jokes come from the crew:** their weapons, their catchphrases and in-jokes from Pooket Tabks. kie's weasels pop to Pop Goes the Weasel, and the Trollogram goons flicker and glitch.
- **Short and punchy.** It's made for phones, so there's never much to read.

## The plot

1. **The opening:** a short scene ([the opening](cutscenes.md#the-opening)). tones2 comes back from a snack run to find the crew's hangout raided and empty, their tabks' bay empty too, and heads in alone. It names tabks but never shows one (owner, 2026-10-19).
2. **Mission one, the military base:** tones2 fights through the base to the motor pool. There comes the reveal ([the motor pool reveal](cutscenes.md#the-motor-pool-reveal)): the crew's stolen tabks are parked inside, and kie's tabk rolls out to fight, with a Trollogram kie at the controls. Beat the tabk, then Trollogram kie on foot, and the real kie is freed ([freeing kie](cutscenes.md#freeing-kie)).
3. **The coda** (owner, 2026-10-16): with the last of his strength, kie frees tones2's own tabk and bows out, exhausted. tones2 calls it in for the first time and blasts his way out of the base in a short, explosive mini-level after the boss, still part of Mission one. It ends the mission and teaches the tabk.
4. **With kie back, the den opens, and co-op with it,** straight after Mission one's debrief ([what START plays](#what-start-plays)). ([Two of the crew](cutscenes.md#retired-two-of-the-crew), the title scene that announced it, is retired.)
5. **Mission two frees kcaj,** at the Brain Freeze creamery ([Mission two](#mission-two-the-creamery)). The night Mission one ended, the Trollograms moved every tabk left in the motor pool, so each is held somewhere new (owner, 2026-10-16). Each mission after that frees one more crewmate from their own tabk.
6. **The coming-soon crew** (shotdownboyz, lankcity, odsey and doctorfox) arrive later, each with a mission of their own.
7. **There's no finale for now.** The story stays open-ended and grows one crewmate at a time, like Coming soon in Tabks.

## What START plays

START on the title follows the player's progress (owner, 2026-10-19). Sign in sits at the title's top left; signed in,
the title says who you're signed in as, with SIGN OUT. Progress is kept either way, and signing in keeps it in the
cloud.

| Where you are | START plays | Then |
|---|---|---|
| Mission one not cleared | [The opening](cutscenes.md#the-opening), then Mission one | Mission one's debrief goes to the den ([the first visit](cutscenes.md#the-first-visit)) |
| Mission one cleared | The den | The player has control: missions, the crew and co-op. Mission two's [briefing](cutscenes.md#the-mission-two-briefing) plays as it starts, solo or in co-op |

## Cutscenes

Every cutscene (the briefings, reveals, boss intros, freeings and the debrief), its rules and the character intros
are in one TV-style script: [cutscenes](cutscenes.md) (owner, 2026-10-19). This page only names them where the plot
needs them.

## The coda

The second reveal: your tabk, called in and driven ([reveals](#reveals-are-story-beats)). Mission one's last stretch, straight after [freeing kie](cutscenes.md#freeing-kie): a short mini-level after the boss, still part of Mission one, played every time the mission is (owner, 2026-10-16). Draft. Short, action-packed and loud: lots of goons, lots of explosions, and a tabk to do it with.

**The setup** (a beat, no scene): the base's alarms are going, the motor pool's back gate grinds open, and goons pour in. kie stays behind, asleep against the wall. The first time, a prompt blinks: **HOLD DOWN + SP TO CALL YOUR TABK** ([calling in a tabk](tanks.md#calling-in-a-tabk)), shown until you call it. The call charges, and the cargo drone drops tones2's tabk beside him, full of fuel. Climb in.

**The run** (about a minute, playable): out of the back of the motor pool, along the base's service road and through the last gate onto the road outside. It's a gauntlet built for the tabk: goons in waves, drones overhead, sandbag nests, fuel drums and barrels that go up in chains, a watchtower that topples when you shoot its legs, and squashable goons all the way (the payoff for spending fuel). The first time, prompts teach the tabk one at a time, each shown only until you've done it once:
1. **"DRIVE"** with the stick (or arrows), shown until you've moved.
2. **"AIM"**: a few goons on a gantry above; the gun points the same eight ways you aim on foot.
3. **"FIRE"**: the watchtower standing in the road; shoot it down. The tabk's rounds counter appears for the first time (5 main shots, 3 specials, 1 super), with a note that rounds never refill ([driving a tabk](tanks.md#driving-a-tabk)).
4. **"SP"**: a wall of goons behind a sandbag nest blocks the road; the mud jump hops the tabk over it, leaving the goons behind it in the mud ([tones2's tabk](tanks.md#tones2s-tabk)).
5. **The gate:** you smash through it and out onto the road, the snow, the searchlights and a few last explosions behind you. The gate wins the mission: the announcer calls "Mission complete!", and the debrief's MISSION COMPLETE! banner follows. There's no caption card here (owner, 2026-10-16).

**Tuned to the tabk:** a full tank of fuel gets you through, and the run is beatable on foot if the tabk is wrecked, just harder. Lose your last life in the coda and the continue restarts you at its start, not at the boss.

**Built** (2026-10-16): the map runs on 82 tiles past the boss (`Missions.One`, from the 'G' back gate). Until kie's freed the back gate is a wall and nothing past it scrolls in (`Level.ScrollLimit`); after the freeing scene it grinds up (`BackGate`), kie's left asleep by the wall, a tabk call is given, and the first time the call-in prompt shows. tones2 starts it on full health (kie's snacks), and the goons wait until tones2 is in the tabk (or has walked 8 tiles on without it). Then the road: a checkpoint, goons pouring in, a gantry of goons overhead, a watchtower standing in the road that topples onto the goons ahead once it's shot down (`ToppleTower`), drones and chains of barrels, a two-high sandbag wall with goons behind it (the mud jump clears it; a hop doesn't), and the last gate (`ExitGate`), which goes down to shots or to the tabk driven into it, and wins the mission. The prompts (`CodaCoach`) show only while you're in the tabk, each from where its part of the road comes into view, and one you drive well past without doing is dropped. They show until the coda's been cleared once. A continue after losing in the coda starts at its checkpoint, without the boss. `?test=coda` starts there, prompts and all.

**Replays:** there are no prompts. Like all of Mission one, the coda is single player only, so it only ever has tones2's one tabk in it. Eject and the super are left for later missions to teach.

## Mission two: the creamery

kcaj's mission, at the Brain Freeze creamery ([the map](levels.md#mission-two-the-brain-freeze-creamery), picked by the owner 2026-10-16). Draft, 2026-10-16, from the story options on the item's page. Its scenes (the briefing, kcaj's tabk arriving, and freeing kcaj) are in [cutscenes](cutscenes.md#mission-two).

**Why the creamery:** Trollograms run hot. The creamery's industrial freezers keep the projectors that make them from melting down, so its cold store is the Trollograms' server room. They've parked kcaj's tabk in front of it as its guard, and shut kcaj in it, which, honestly, he'd have chosen anyway.

**Why not the base:** the night Mission one ended, the Trollograms moved every tabk left in the motor pool (owner, 2026-10-16). [The briefing](cutscenes.md#the-mission-two-briefing) opens on it.

### The run

No scenes, just the place telling it: goons on the production line, Trollogram projectors humming behind frosted glass (shoot one and a few goons nearby flicker out), a goon frozen solid on a conveyor rolling past still mid-shout, and the freezer corridor before the cold store, where breath fogs. A scoop dropper plops a scoop on a soldier's head once, the first time, for the gag.

### The end

kcaj's tabk comes out of the cold store ([kcaj's tabk arrives](cutscenes.md#kcajs-tabk-arrives)). Once it's wrecked and its pilot is beaten on foot, the real kcaj stumbles out of the frost ([freeing kcaj](cutscenes.md#freeing-kcaj)). The next time the den loads, he moves into his nook on the den's upper floor ([the lobby](lobby.md)) and his corner lights up.

## Reveals are story beats

The game hides what's coming ([progressive disclosure](README.md#progressive-disclosure-the-thread-through-all-four)), so each reveal gets a scene of its own:

| Reveal | When | How |
|---|---|---|
| Tabks exist | The motor pool, at the end of Mission one | kie's tabk rolls out ([the motor pool reveal](cutscenes.md#the-motor-pool-reveal)), with its [boss intro](cutscenes.md#the-boss-intro) |
| Your tabk: calling it in and driving it | The coda, straight after kie is freed | kie frees tones2's own tabk and bows out; you call it in and blast your way out of the base |
| Co-op exists | Once Mission one has been beaten | In the den, which opens after Mission one's debrief: its TV joins the co-op room |
| Each new crewmate | The end of their mission | The freeing scene ([freeing kie](cutscenes.md#freeing-kie), [freeing kcaj](cutscenes.md#freeing-kcaj)) |
| XP and rank | The first debrief | [The first debrief](cutscenes.md#the-first-debrief): the first XP, and the first step up the ladder |

The second time you meet something, it's quick: a tabk you've already fought only gets its boss intro.

## Open questions

- **Mission order after kcaj**, which sets the order of the story.
- **Each mission's story after Mission two:** its briefing, and how its crewmate's freeing plays out.
