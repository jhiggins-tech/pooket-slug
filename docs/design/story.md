# Story, plot and cutscenes

The story of Pooket Slug, how it unfolds across the missions, and how cutscenes and reveals are shown.

**Status:** signed off by the owner, 2026-10-14.

**Read this when** you're writing a mission's story, a cutscene, or anything that reveals something new to the player.

Up: [Design](README.md)

## The premise

- **The crew are missing, and only tones2 is left.** kie's Trollogram holograms have gone rogue. They raid the crew's hangout while tones2 is out on a snack run, and he comes back to an empty room and glitchy hologram soldiers everywhere.
- **What's really going on comes out as you play.** The holograms have stolen the crew's tanks, and each tank is driven by a hologram copy of its owner. The game keeps this to itself until the first tank appears ([reveals](#reveals-are-story-beats)).
- **Every mission has the same shape.** You fight through to a crewmate's tank, beat it, then beat its fake pilot. The fake glitches out, and the real crewmate is freed to play from then on.

## Tone

- **Arcade action, played straight:** hordes of goons, big explosions, and a boss at the end of every mission.
- **The jokes come from the crew:** their weapons, their catchphrases and in-jokes from Pooket Tabks. kie's weasels pop to Pop Goes the Weasel, and the hologram goons flicker and glitch.
- **Short and punchy.** It's made for phones, so there's never much to read.

## The plot

1. **The opening:** a short scene. tones2 comes back from a snack run to find the crew's hangout raided and empty, and heads in alone. Nothing in it mentions tanks.
2. **Mission one, the military base:** tones2 fights through the base to the motor pool. There comes the reveal: the crew's stolen tanks are parked inside, and kie's tank rolls out to fight, with a hologram kie at the controls. Beat the tank, then hologram kie on foot, and the real kie is freed.
3. **The first drive:** kie's tank is yours now. A short drive out of the base ends the mission and teaches you the tank's controls.
4. **Two of the crew:** with kie back, co-op opens. kie's first line sets it up as the menu changes.
5. **Mission two frees kcaj.** Each mission after that frees one more crewmate from their own tank.
6. **The coming-soon crew** (shotdownboyz, lankcity, odsey and doctorfox) arrive later, each with a mission of their own.
7. **There's no finale for now.** The story stays open-ended and grows one crewmate at a time, like Coming soon in Tabks.

## Cutscenes

Cutscenes are **in-engine scenes**: the game's own art, drawn in code, with speech bubbles. There's no video.
- **Each lasts a few seconds.** Tap to move on. Once you've seen a scene you can skip it, but a reveal always plays the first time.
- **There are five kinds:**
  - **Briefing**, before a mission: where you're going, and who's held there.
  - **Boss intro:** a warning banner, the music changes, and the tank makes its entrance ([tanks](tanks.md#the-boss-intro)).
  - **Reveal:** the first time something new appears. It plays once, ever.
  - **Freeing:** the fake pilot glitches out, and the real crewmate appears and joins.
  - **Debrief:** the mission's tally, XP and rank, then anything newly unlocked.
- **Sound:** there's a music track for each mission, and one for bosses. The crew talk in speech bubbles, and an arcade announcer calls the big moments ("Mission complete!"), in a generated voice ([tanks](tanks.md#the-boss-intro)).
- **In co-op,** scenes play for both players, and a scene ends once both have tapped. A partner who hasn't seen it gets to watch it.

## The opening scene

The first cutscene, and Mission one's briefing ([the plot](#the-plot), step 1). Draft, 2026-10-15. It's the test case for the in-engine cutscenes, so it uses only what they need: the game's own sprites, a few props, speech bubbles and one caption card.

**Rules it follows:** about 15 seconds in all, five shots of 2 to 4 seconds. A tap moves to the next shot, and each moves on by itself too. It plays in full the first time; after that a SKIP button shows from the start. Nothing in it hints at tanks, vehicles or co-op. Bubbles are a few words each, in the game's capitals, so they read at a glance on a phone. The lines are placeholders for the owner to rewrite in the crew's own voice.

| # | Shot | What happens | Bubbles and text | Time |
|---|---|---|---|---|
| 1 | **Outside the hangout, night** | A side-on street at night, snow falling, with the crew's hangout: a garage with a hand-painted POOKET sign and warm light in the window. tones2 walks in from the left, arms full: a bag of snacks and a big slushie. | tones2: "SNACKS ARE HERE LADS" | 3 s |
| 2 | **The door** | He nudges the door with his shoulder and it swings in, hanging off one hinge. The warm light flickers and turns cold cyan. He stops. | tones2: "...LADS?" | 2 s |
| 3 | **Inside, raided** | The hangout is trashed: the couch flipped, the gaming setup sparking, controllers on the floor, cyan scorch marks up the walls. Nobody's there. The snack bag drops; chips everywhere. | (no bubble: a beat of quiet, a crackle of static) | 3 s |
| 4 | **A straggler** | A hologram goon flickers up from behind the couch, sees him and shouts in a glitchy bubble. tones2 sprays it with ten-1 and it glitches out, the goons' own death. Where it stood, a cyan keycard clatters to the floor. | Goon: "INTRUDER. INTRUD-" · tones2 (picking up the card): "MILITARY BASE. NORTH GATE." | 4 s |
| 5 | **Heading out** | Out of the window, searchlights sweep the sky over a hill in the distance. tones2 finishes the slushie in one go, crushes the cup, and walks out to the right. The caption card slams in, and the announcer calls it as the mission starts. | tones2: "RIGHT." · Card: "MISSION 1 · THE MILITARY BASE · FIND THE CREW" · Announcer: "Mission one... start!" | 3 s |

**What it teaches without a word:** the holograms are the enemy and they glitch out when beaten (shot 4, using the goon death), tones2 sprays water (the jet), and the base is where the crew went.

**What it sets up:** the empty hangout is the room the crew come back to as they're freed. A later scene can show it filling up again.

**Sound:** quiet street ambience in shots 1 to 3, a static crackle on the cyan flicker, the jet and the glitch-out in shot 4, and the mission music kicks in with the caption card ([the boss intro](tanks.md#the-boss-intro) sets out the music).

**In co-op:** Mission one is solo the first time, so this scene first plays solo. On a co-op replay it plays for both players, and ends once both have tapped, or by itself ([cutscenes](#cutscenes)).

**What the cutscene engine needs from it:** a backdrop (the street, then the room), placed props (door, couch, setup, keycard), the soldier and a goon moving along simple paths, a goon dying the goon's way, speech bubbles anchored above a character, a caption card, and a timer with tap-to-advance and SKIP.

## The motor pool reveal

The first-ever reveal: tanks exist ([reveals](#reveals-are-story-beats)). It plays once, ever, the first time anyone reaches the motor pool; every later fight, a replay included, gets only [the boss intro](tanks.md#the-boss-intro). Draft, 2026-10-15. About 10 seconds, four shots; tap moves on, and it can't be skipped the first time.

| # | Shot | What happens | Bubbles and text | Time |
|---|---|---|---|---|
| 1 | **The shutter** | tones2 steps into the arena and the screen locks. The motor pool's blast shutter grinds up, and cold light pours out from behind it. | (no bubble: the shutter's grind, a hum) | 2 s |
| 2 | **The tanks** | The camera eases right and in: the crew's tanks parked in a row under work lights, each in its crewmate's paint, with cables and cyan hologram junk all over them. One space at the front is empty, its paint scuffs still steaming. | tones2: "...ARE THOSE OURS?" | 3 s |
| 3 | **kie's tank** | An engine roars off-screen, and kie's tank rolls out of the shadows into the empty space. On top, in the hatch: kie, but cyan, flickering, grinning. | Hologram kie, glitchy bubble: "HEY TONES. WANNA RACE?" | 3 s |
| 4 | **Hatch shut** | Hologram kie drops into the hatch and it slams. The turret swings round to tones2. Straight into the boss intro's WARNING! banner, name card and boss music, with control back in your hands. | tones2: "THAT'S NOT KIE." | 2 s |

**How it fits the boss intro:** the first time, this scene is the tank's entrance, so the intro that follows skips the roll-out and goes straight to the banner, the name card ("KIE'S TANK") and the music.

## Freeing kie

The freeing scene, after hologram kie is beaten on foot ([the pilot round](tanks.md#the-pilot-round)). Draft, 2026-10-15. About 10 seconds, four shots; it can be skipped once seen, on a replay.

| # | Shot | What happens | Bubbles and text | Time |
|---|---|---|---|---|
| 1 | **The glitch-out** | Hologram kie staggers, freezes mid-taunt, and glitches apart: the big version of the goons' death (the pilot glitch spike). The boss music cuts out. | Hologram kie: "WAIT, I WAS WINN-" | 3 s |
| 2 | **The real kie** | Where it stood, a cyan cage of light flickers out, and the real kie drops to the floor, dazed, in his own colours. | kie: "...HOW LONG WAS I IN THERE?" | 3 s |
| 3 | **The bit** | tones2 holds out the last of the snacks from the opening. kie takes them. | tones2: "BROUGHT SNACKS." · kie: "LEGEND." | 2 s |
| 4 | **The point** | kie looks over at his tank, still running, the hatch open. | kie: "SHE'S YOURS. GET US OUT OF HERE." | 2 s |

Then straight into the first drive, the first time; on a replay, into the debrief. In co-op, the partner's soldier stands beside tones2 throughout.

## The first drive

The second reveal: you can drive a tank ([reveals](#reveals-are-story-beats)). It plays once, ever, straight after freeing kie. Draft, 2026-10-15. A short scene, then a short playable drive that ends the mission. The drive teaches the controls by doing, one prompt at a time, each shown only until you've done it once.

**The scene** (about 4 seconds): tones2 climbs onto kie's tank and drops into the hatch; kie hops up behind the turret and holds on. Bubble, kie: "STICK DRIVES. FIRE SHOOTS. GO!" The motor pool's far wall is a gate, and the searchlights are swinging back.

**The drive** (about 30 seconds, playable): out of the motor pool and through the base's back gate to the road outside, the only way out. Goons pour in to stop you, and the prompts come one at a time:
1. **"DRIVE"** with the stick (or arrows), shown until you've moved.
2. **"AIM"**: a few goons on a gantry above; the gun points the same eight ways you aim on foot.
3. **"FIRE"**: the gate's guard post is in the way; shoot it down. The tank's rounds counter appears for the first time (5 main shots, 3 specials, 1 super), with a note that rounds never refill ([driving a tank](tanks.md#driving-a-tank)).
4. **"SP"**: a wall of goons behind sandbags at the gate; the special clears it.
5. **The gate:** you smash through it and out onto the road, the snow and the searchlights behind you. The caption card slams in, and the announcer calls "Mission complete!".

Eject and the super are left for later missions to teach, so the drive stays short. The first drive always happens solo, since Mission one is first beaten solo. On a co-op replay there's no drive scene: the mission ends at the freeing.

## The first debrief

Mission one's debrief, the first time: the tally, then XP and rank are revealed ([what the player sees, and when](levels.md#what-the-player-sees-and-when)). Draft, 2026-10-15. It's a screen more than a scene: the crew stand to one side and the numbers count up on the other. Each part counts up fast, and a tap fills it at once; another tap moves on.

1. **The tally** (about 3 s): MISSION COMPLETE! at the top, and the announcer says it if the drive didn't. Then the numbers count up: score, time, goons, accuracy, and how many lives and continues were left. Your best score so far shows beside it, with NEW BEST! if it is.
2. **The first XP** (about 3 s): a new bar slides in for the first time, labelled XP. It fills from the mission's parts: the mission cleared, kie freed, and the kills ([career XP](player.md#what-the-player-keeps)). kie's bubble: "YOU GET XP FOR THAT."
3. **The first step up the ladder** (about 3 s): the rank badge appears, and you go up from the bottom rank to the first rank above it, with a RANK UP! stamp and a chip fanfare. Only that rank's name is shown; the ladder's hidden ranks stay hidden ([rank](player.md#what-the-player-keeps)).
4. **Sign in to keep it** (only for players who aren't signed in): kie's bubble: "SIGN IN SO THIS DOESN'T VANISH." The sign-in button shows over the game, as on the title screen, beside CONTINUE. Signing in here saves the run's progress straight away; skipping is fine and asks again only on the next debrief.
5. **Continue:** to the title screen, where the menu has grown (the co-op opens scene).

On a replay, the debrief is the same screen without the first-time lines and the sign-in step, and with any new looks tokens shown.

## Reveals are story beats

The game hides what's coming ([progressive disclosure](README.md#progressive-disclosure-the-thread-through-all-four)), so each reveal gets a scene of its own:

| Reveal | When | How |
|---|---|---|
| Tanks exist | The motor pool, at the end of Mission one | kie's tank rolls out, with its boss intro |
| You can drive one | Straight after kie is freed | You climb into his tank and drive it out of the base |
| Co-op exists | Once Mission one has been beaten solo | The menu changes, and kie's first line sets it up |
| Each new crewmate | The end of their mission | The freeing scene |
| XP and rank | The first debrief | The first XP, and the first step up the ladder |

The second time you meet something, it's quick: a tank you've already fought only gets its boss intro.

## Open questions

- **Mission order after kcaj**, which sets the order of the story.
- **Each mission's story:** its briefing, and how its crewmate's freeing plays out.
