# Story, plot and cutscenes

The story of Pooket Slug, how it unfolds across the missions, and how cutscenes and reveals are shown.

**Status:** signed off by the owner, 2026-10-14.

**Read this when** you're writing a mission's story, a cutscene, or anything that reveals something new to the player.

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

1. **The opening:** a short scene. tones2 comes back from a snack run to find the crew's hangout raided and empty, and heads in alone. Nothing in it mentions tabks.
2. **Mission one, the military base:** tones2 fights through the base to the motor pool. There comes the reveal: the crew's stolen tabks are parked inside, and kie's tabk rolls out to fight, with a Trollogram kie at the controls. Beat the tabk, then Trollogram kie on foot, and the real kie is freed.
3. **The coda** (owner, 2026-10-16): with the last of his strength, kie frees tones2's own tabk and bows out, exhausted. tones2 calls it in for the first time and blasts his way out of the base in a short, explosive mini-level after the boss, still part of Mission one. It ends the mission and teaches the tabk.
4. **Two of the crew:** with kie back, co-op opens. kie's first line sets it up as the menu changes.
5. **Mission two frees kcaj.** Each mission after that frees one more crewmate from their own tabk.
6. **The coming-soon crew** (shotdownboyz, lankcity, odsey and doctorfox) arrive later, each with a mission of their own.
7. **There's no finale for now.** The story stays open-ended and grows one crewmate at a time, like Coming soon in Tabks.

## Cutscenes

Cutscenes are **in-engine scenes**: the game's own art, drawn in code, with speech bubbles. There's no video.
- **Each lasts a few seconds.** Tap to move on. Once you've seen a scene you can skip it, but a reveal always plays the first time.
- **There are five kinds:**
  - **Briefing**, before a mission: where you're going, and who's held there.
  - **Boss intro:** a warning banner, the music changes, and the tabk makes its entrance ([tabks](tanks.md#the-boss-intro)).
  - **Reveal:** the first time something new appears. It plays once, ever.
  - **Freeing:** the fake pilot glitches out, and the real crewmate appears and joins.
  - **Debrief:** the mission's tally, XP and rank, then anything newly unlocked.
- **A narrator tells each scene,** in subtitles along the bottom of the screen and out loud (turned well down, to sit with the game's sounds), so what's happening is never left to guesswork. The owner asked for this after trying the first cut of the opening, 2026-10-15. The voice is a camp, softly spoken, Louis Theroux-style documentary narrator (voice F from the narrator voice spike, MiniMax, picked by the owner 2026-10-16), generated like the announcer's by the vendor repository's Narration workflow from its `voice/narration.json`. A line made once is kept, so a new or changed line is the only one made again.
- **Sound:** there's a music track for each mission, and one for bosses. The crew talk in speech bubbles, and an arcade announcer calls the big moments ("Mission complete!"), in a generated voice ([tabks](tanks.md#the-boss-intro)).
- **In co-op,** scenes play for both players, on both phones, and a shot moves on early only once both have tapped (the timer still moves it on for both). A line says who's being waited for. A scene seen before skips only once both players have pressed SKIP, so a partner who hasn't seen it gets to watch it; a partner who leaves holds nobody up. A scene plays in co-op when the players start together from the den; someone joining a game already under way drops straight in.
- **Every character gets an intro the first time they're on screen** (canonical, the owner, 2026-10-16). The first time a character appears in the story's timeline, the camera crash-zooms onto them, their name slams up big ("TONES2"), time goes slow-mo, and a big musical hit plays. It's part of that scene, so it plays whenever the scene does, and never anywhere else. [Who gets theirs where](#character-intros) is set below, and each scene's design names its intro.

### Character intros

**How it plays:** it builds up, then it lands, like the run-up to a drop. The owner asked for the build-up after the first cut came "out of nowhere, like a car crash", 2026-10-16.
- **First we see them.** The character is on screen for a few seconds before anything happens (tones2 walks on toward the hangout), so the intro has someone to land on.
- **The build-up,** about 2.5 seconds: a chip riser climbs (a snare roll that speeds up from quarter notes to a blur, over a noise sweep and a rising tone, all swelling), while the camera creeps in on them, the scene darkens a little, and it shakes as the riser peaks. A tap before it jumps to the build-up's start, and a tap during it does nothing, so it always plays whole.
- **The landing,** about 1.8 seconds, at the riser's peak: a white flash and a crash zoom onto the character, so they fill the left of the screen; their name slams in on a slanted band to their right, in the caption card's gold; the scene runs at slow-mo underneath (its bubbles, cues and snow all slow down); a chip musical hit rings out (a wide chord over a crash and a bass drop). Then it eases back out and the shot carries on at full speed. A tap can't cut it short.
- **Levels:** the riser and the hit sit at the level of the game's other sounds, not above them.

A scene that can be skipped once seen skips its intro with it. The narrator waits for the intro to land before speaking, and their subtitles carry on through it.

**What counts:** named characters, the crew and their Trollogram copies. Each Trollogram copy is a character of its own (it's that mission's boss), so a crewmate gets two intros: their Trollogram where it first appears, then the real one when they're freed. Nameless enemies (the Trollogram goons) and vehicles don't get one; a tabk gets its name card in [the boss intro](tanks.md#the-boss-intro) instead.

| Character | Scene | Shot | Name on screen |
|---|---|---|---|
| tones2 | [The opening](#the-opening-scene) | 1, as he strolls in with the snacks | TONES2 |
| Trollogram kie | [The motor pool reveal](#the-motor-pool-reveal) | 3, as he pops up in the hatch | TROLLO-KIE |
| kie | [Freeing kie](#freeing-kie) | 2, as the real kie drops to the floor | KIE |
| Each later crewmate's Trollogram | Their mission's [boss intro](tanks.md#the-boss-intro), the first time | As the pilot shows in the hatch | TROLLO-&lt;NAME&gt; |
| Each later crewmate | Their freeing scene | As the real one appears | &lt;NAME&gt; |

[The coda](#the-coda), [the first debrief](#the-first-debrief) and [two of the crew](#two-of-the-crew) bring in nobody new, so they have no intro.

## The opening scene

The first cutscene, and Mission one's briefing ([the plot](#the-plot), step 1). Approved by the owner, 2026-10-15, then slowed down and given a narrator after the cutscenes spike. It's the test case for the in-engine cutscenes, so it uses only what they need: the game's own sprites, a few props, speech bubbles, the narrator's subtitles and one caption card.

**Rules it follows:** about 37 seconds in all, six shots of 4 to 8 seconds. The first cut ran 15 seconds and felt rushed, so every shot now holds long enough to read its lines (about three words a second), and the narrator says what's happening. A tap moves to the next shot, and each moves on by itself too. It plays in full the first time; after that a SKIP button shows from the start. Nothing in it hints at tabks, vehicles or co-op. Bubbles are a few words each, in the game's capitals, so they read at a glance on a phone; the narrator's subtitles are in sentence case. All the lines are placeholders for the owner to rewrite in the crew's own voice.

| # | Shot | What happens | Narrator, bubbles and text | Time |
|---|---|---|---|---|
| 1 | **Outside the hangout, night** | A side-on street at night, snow falling, with the crew's hangout: a garage with a hand-painted POOKET sign and warm light in the window. tones2 strolls in from the left toward it, arms full: a bag of snacks and a big slushie. His intro builds up as he walks and lands at 4 seconds; he carries on to the door. | TONES2 (his intro) · Narrator: "Friday night. tones2's back from the snack run." · tones2: "SNACKS ARE HERE LADS" | 8 s |
| 2 | **The door** | The door swings in, hanging off one hinge. The warm light flickers and turns cold cyan. He stops. | Narrator: "But the door's been kicked in." · tones2: "...LADS?" | 4.5 s |
| 3 | **Inside, raided** | The hangout is trashed: the couch flipped, the gaming setup sparking, controllers on the floor, cyan scorch marks up the walls. Nobody's there. A long look, then the snack bag drops; chips everywhere. | Narrator: "The hangout's trashed, and the crew are gone." (then a beat of quiet, a crackle of static) | 5 s |
| 4 | **A straggler** | A Trollogram goon flickers up by the couch and shouts in a glitchy bubble. tones2 sprays it with ten-1 and it glitches out, the goons' own death. A keycard clatters to the floor where it stood, and tones2 walks over and picks it up. | Goon: "INTRUDER. INTRUD-" · Narrator: "One of the raiders is still here: a Trollogram." then "It glitches out, and drops its keycard." | 8 s |
| 5 | **The keycard** | A close-up: the room dims and the card slides up big enough to read, flickering like the Trollograms do. It says MILITARY BASE, NORTH GATE, ACCESS: ALL AREAS. | Narrator: "A pass for the military base, up on the hill." · tones2: "THAT'S WHERE THEY TOOK THEM." | 5 s |
| 6 | **Heading out** | Out of the window, searchlights sweep the sky over a hill in the distance. tones2 finishes the slushie in one go, crushes the cup, and walks out to the right. The caption card slams in, and the announcer calls it as the mission starts. | Narrator: "So tones2 goes in alone." · tones2: "RIGHT." · Card: "MISSION 1 · THE MILITARY BASE · FIND THE CREW" · Announcer: "Mission one... start!" | 6 s |

**Character intro:** tones2, in shot 1 ([character intros](#character-intros)). He walks on for a second and a half, the build-up runs as he heads for the hangout, and it lands at 4 seconds, with him mid-street. The narrator's first line follows it. The shot holds about 1.5 seconds longer for the slow-mo.

**What it teaches:** the Trollograms are the enemy and they glitch out when beaten (shot 4, using the goon death), tones2 sprays water (the jet), and the keycard's close-up and the narrator make it plain that the base is where the crew went.

**What it sets up:** the empty hangout is the room the crew come back to as they're freed. A later scene can show it filling up again.

**Sound:** tones2's intro build-up and hit in shot 1, quiet street ambience in shots 1 to 3, a static crackle on the cyan flicker, the jet and the glitch-out in shot 4, and the mission music kicks in with the caption card ([the boss intro](tanks.md#the-boss-intro) sets out the music).

**Always solo:** Mission one is single player only ([Mission one](levels.md#mission-one-the-military-base)), so this scene only ever plays for one player.

**What the cutscene engine needs from it:** a character intro (crash zoom, name, slow-mo and musical hit), a backdrop (the street, then the room), placed props (door, couch, setup, keycard), the soldier and a goon moving along simple paths, a goon dying the goon's way, speech bubbles anchored above a character, a close-up insert, the narrator's subtitles, a caption card, and a timer with tap-to-advance and SKIP.

## The motor pool reveal

The first-ever reveal: tabks exist ([reveals](#reveals-are-story-beats)). It plays once, ever, the first time tones2 reaches the motor pool; every later fight, a replay included, gets only [the boss intro](tanks.md#the-boss-intro). Drafted 2026-10-15; built 2026-10-16 with the narrator and Trollogram kie's intro, which took it from about 10 seconds to about 22 (four shots of 4.5 to 8 seconds, as the opening's were slowed down). Tap moves on, and it can't be skipped the first time. Always solo, like the rest of Mission one.

| # | Shot | What happens | Narrator, bubbles and text | Time |
|---|---|---|---|---|
| 1 | **The shutter** | tones2 steps in from the left in front of the base's wall, and the motor pool's blast shutter grinds up, juddering, and cold light pours out across the apron from the bay behind it. | Narrator: "Behind the shutter: the base's motor pool." (the shutter's grind, then a hum) | 4.5 s |
| 2 | **The tabks** | The camera eases right and in: the crew's tabks parked in a row under work lights, each in its crewmate's paint (tones2's pink among them), with cyan Trollogram junk flickering all over them and cables across the floor. The space at the front is empty, its paint scuffs still steaming. | Narrator: "And parked inside, all in a row: the crew's tabks." · tones2: "...ARE THOSE OURS?" | 5.5 s |
| 3 | **kie's tabk** | An engine roars, and kie's tabk rolls in from the right into the empty space, its turret pointing up and away. The hatch pops, and up comes kie, but cyan, flickering, grinning. His intro builds up as he rises and lands at 4.5 seconds. | TROLLO-KIE (his intro) · Narrator: "That's kie. Or something wearing his face." · Trollogram kie, glitchy bubble: "HEY TONES. WANNA RACE?" | 8 s |
| 4 | **Hatch shut** | Trollogram kie drops into the hatch and it slams. The turret swings round on to tones2. Straight into the boss intro's WARNING! banner, name card and boss music, with control back in your hands. | Narrator: "kie has never once offered to race." · tones2: "THAT'S NOT KIE." | 4.5 s |

**Character intro:** Trollogram kie, in shot 3, as he pops up in the hatch: TROLLO-KIE ([character intros](#character-intros)). His glitchy bubble comes after it.

**How it fits the boss intro:** the first time, this scene is the tabk's entrance, so the intro that follows skips the roll-out and goes straight to the banner, the name card ("KIE'S TABK") and the music.

**Built** (2026-10-16): it plays as the boss tabk wakes, the first time, solo; once it ends, the tabk is in its place and the full boss intro runs (`Cutscenes.MotorPool` in `Core/Cutscene.cs`, its staging in `Ui/CutsceneView.cs`). Test starts (`?test=boss`, `pilot`, `team`) skip it; `?test=boss,reveal` plays it every time, so it can be tried again once seen. The narrator says "tanks" where the subtitles say "tabks".

## Freeing kie

The freeing scene, after Trollogram kie is beaten on foot ([the pilot round](tanks.md#the-pilot-round)). Draft, 2026-10-15, reworked 2026-10-16 for the owner's new ending: kie frees tones2's tabk, then bows out. About 12 seconds, five shots; it can be skipped once seen, on a replay.

| # | Shot | What happens | Bubbles and text | Time |
|---|---|---|---|---|
| 1 | **The glitch-out** | Trollogram kie staggers, freezes mid-taunt, and glitches apart: the big version of the goons' death (the pilot glitch spike). The boss music cuts out. | Trollogram kie: "WAIT, I WAS WINN-" | 3 s |
| 2 | **The real kie** | Where it stood, a cyan cage of light flickers out, and the real kie drops to the floor, dazed, in his own colours. | kie: "...HOW LONG WAS I IN THERE?" | 3 s |
| 3 | **The bit** | tones2 holds out the last of the snacks from the opening. kie takes them. | tones2: "BROUGHT SNACKS." · kie: "LEGEND." | 2 s |
| 4 | **The release** | kie staggers over to tones2's tabk in the row and rips the cyan Trollogram clamp off it. A cargo drone drops through the roof hatch, hooks the tabk and hauls it up and out. kie presses its call-in remote into tones2's hand. | kie: "YOURS. CALL IT WHEN YOU NEED IT." | 2 s |
| 5 | **Lights out** | Alarms start up somewhere in the base. kie sits down hard against the wall with the snacks in his lap, and nods off. | kie: "I'M GONNA... SIT THIS ONE OUT." | 2 s |

**Character intro:** the real kie, in shot 2, as he drops to the floor: KIE ([character intros](#character-intros)). His line comes after it.

**kie bows out** (owner, 2026-10-16): he's spent, and sits out the rest of Mission one; tones2 finishes it alone, with his tabk. kie is back, rested, for [two of the crew](#two-of-the-crew).

Then straight into [the coda](#the-coda), every time: the coda is part of Mission one, which is always solo.

**How the release is done** (owner, 2026-10-16): the cargo drone hauls the tabk out, and kie hands over its remote. It's what makes every call-in drop from the sky.

**Spike** (2026-10-16, `?spike=pilot-glitch`, solo): shots 1 and 2 in the game, without the scene around them. Beaten, Trollogram kie freezes mid-taunt ("WAIT, I WAS WINN-") and the boss music cuts dead; he glitches harder and harder for about a second, then tears apart like the goons, bigger and slower. Where he stood, a cyan cage of light flickers out, the real kie drops to the floor in his own colours, sits there dazed with stars round his head and says his line. The spike then ends the mission, standing in for the release shot and the coda, which aren't built yet. The numbers are `PilotFreeingSpec` (`Core/KieTuning.cs`), the look `HoloLook.Big` and `FreedKie` (`Pilot.cs`). `?test=pilot,weak` starts at the tabk, and the pilot goes down on his own a few seconds after he jumps out.

## The coda

The second reveal: your tabk, called in and driven ([reveals](#reveals-are-story-beats)). Mission one's last stretch, straight after [freeing kie](#freeing-kie): a short mini-level after the boss, still part of Mission one, played every time the mission is (owner, 2026-10-16). Draft. Short, action-packed and loud: lots of goons, lots of explosions, and a tabk to do it with.

**The setup** (a beat, no scene): the base's alarms are going, the motor pool's back gate grinds open, and goons pour in. kie stays behind, asleep against the wall. The first time, a prompt blinks: **HOLD DOWN + SP TO CALL YOUR TABK** ([calling in a tabk](tanks.md#calling-in-a-tabk)), shown until you call it. The call charges, and the cargo drone drops tones2's tabk beside him, full of fuel. Climb in.

**The run** (about a minute, playable): out of the back of the motor pool, along the base's service road and through the last gate onto the road outside. It's a gauntlet built for the tabk: goons in waves, drones overhead, sandbag nests, fuel drums and barrels that go up in chains, a watchtower that topples when you shoot its legs, and squashable goons all the way (the payoff for spending fuel). The first time, prompts teach the tabk one at a time, each shown only until you've done it once:
1. **"DRIVE"** with the stick (or arrows), shown until you've moved.
2. **"AIM"**: a few goons on a gantry above; the gun points the same eight ways you aim on foot.
3. **"FIRE"**: a guard post in the way; shoot it down. The tabk's rounds counter appears for the first time (5 main shots, 3 specials, 1 super), with a note that rounds never refill ([driving a tabk](tanks.md#driving-a-tabk)).
4. **"SP"**: a wall of goons behind a sandbag nest blocks the road; the mud jump hops the tabk over it, leaving the goons behind it in the mud ([tones2's tabk](tanks.md#tones2s-tabk)).
5. **The gate:** you smash through it and out onto the road, the snow, the searchlights and a few last explosions behind you. The caption card slams in, and the announcer calls "Mission complete!".

**Tuned to the tabk:** a full tank of fuel gets you through, and the run is beatable on foot if the tabk is wrecked, just harder. Lose your last life in the coda and the continue restarts you at its start, not at the boss.

**Replays:** there are no prompts. Like all of Mission one, the coda is single player only, so it only ever has tones2's one tabk in it. Eject and the super are left for later missions to teach.

## The first debrief

Mission one's debrief, the first time: the tally, then XP and rank are revealed ([what the player sees, and when](levels.md#what-the-player-sees-and-when)). Draft, 2026-10-15. It's a screen more than a scene: the crew stand to one side and the numbers count up on the other. Each part counts up fast, and a tap fills it at once; another tap moves on.

1. **The tally** (about 3 s): MISSION COMPLETE! at the top, and the announcer says it if the coda didn't. Then the numbers count up: score, time, goons, accuracy, and how many lives and continues were left. Your best score so far shows beside it, with NEW BEST! if it is.
2. **The first XP** (about 3 s): a new bar slides in for the first time, labelled XP. It fills from the mission's parts: the mission cleared, kie freed, and the kills ([career XP](player.md#what-the-player-keeps)). kie's bubble: "YOU GET XP FOR THAT."
3. **The first step up the ladder** (about 3 s): the rank badge appears, and you go up from the lowest rank on the ladder, where everyone starts ([rank](player.md#what-the-player-keeps)), to the one above it, with a RANK UP! stamp and a chip fanfare. Only that rank's name is shown; the ladder's hidden ranks stay hidden ([rank](player.md#what-the-player-keeps)).
4. **Sign in to keep it** (only for players who aren't signed in): kie's bubble: "SIGN IN SO THIS DOESN'T VANISH." The sign-in button shows over the game, as on the title screen, beside CONTINUE. Signing in here saves the run's progress straight away; skipping is fine and asks again only on the next debrief.
5. **Continue:** to the title screen, where the menu grows ([two of the crew](#two-of-the-crew)).

On a replay, the debrief is the same screen without the first-time lines and the sign-in step, and with any new looks tokens shown.

## Two of the crew

The third reveal: co-op exists ([reveals](#reveals-are-story-beats)). It plays once, ever, the first time the title screen loads after Mission one is beaten, which is usually straight after [the first debrief](#the-first-debrief). If the game is closed during the debrief, it plays the next time the title screen loads. Draft, 2026-10-15. About 8 seconds, four shots, played on the title screen itself: the title is the stage, so the menu grows in front of the player. A tap moves on; it can't be skipped, but it's short.

| # | Shot | What happens | Bubbles and text | Time |
|---|---|---|---|---|
| 1 | **The old title** | The title screen as on first launch: the logo, START, SETTINGS and the sign-in button. tones2 stands at the left, alone, as he has been all along. | (no bubble: the title's usual quiet) | 1 s |
| 2 | **kie walks in** | kie walks in from the right, eating the snacks from [freeing kie](#freeing-kie), and stops beside tones2. | kie: "MISSED ME?" | 2 s |
| 3 | **The menu grows** | kie elbows the START button. It cracks and splits into three that drop in one at a time, each with a chip clunk: **MISSIONS**, **CO-OP** and **CREW**. SETTINGS and sign-in stay where they were. | (no bubble: three clunks) | 3 s |
| 4 | **kie's line** | kie leans on the CO-OP button and points out at the player. It pulses once, and control comes back. | kie: "TWO OF US NOW. BRING A MATE." | 2 s |

**What each new button opens** ([what the player sees](levels.md#what-the-player-sees-and-when)):
- **MISSIONS:** the mission screen, with Mission one's replay and kcaj's mission.
- **CO-OP:** online play on two phones: host a room, join with a code, quick match, browse rooms, or watch a game ([online](levels.md#online-once-its-open)).
- **CREW:** the crew screen, with tones2 and kie.

**What it teaches without a word:** kie is a second soldier now, and the second soldier is a second player. The scene says "co-op" only through the button and kie's line, so nothing is explained in text.

**Where it never plays:**
- **In co-op.** Mission one is single player only, so this scene is always solo.
- **For a friend who opens an invite link before beating Mission one.** They're started on Mission one ([invite links](levels.md#what-the-player-sees-and-when)), and see this scene once they've beaten it themselves.

**Signed in or not:** whether it's been seen is kept with the rest of the player's progress, so a player who signs in on a new phone doesn't see it again.

**What the engine needs from it:** the title screen as a cutscene stage, with the crew's sprites standing on it; menu buttons that can animate in; and the unlock state that the [menu-grows spike](levels.md#what-the-player-sees-and-when) adds, saying Mission one has been beaten. The lines are placeholders for the owner to rewrite in the crew's own voice.

## Reveals are story beats

The game hides what's coming ([progressive disclosure](README.md#progressive-disclosure-the-thread-through-all-four)), so each reveal gets a scene of its own:

| Reveal | When | How |
|---|---|---|
| Tabks exist | The motor pool, at the end of Mission one | kie's tabk rolls out, with its boss intro |
| Your tabk: calling it in and driving it | The coda, straight after kie is freed | kie frees tones2's own tabk and bows out; you call it in and blast your way out of the base |
| Co-op exists | Once Mission one has been beaten | [Two of the crew](#two-of-the-crew): the menu grows, and kie's first line sets it up |
| Each new crewmate | The end of their mission | The freeing scene |
| XP and rank | The first debrief | The first XP, and the first step up the ladder |

The second time you meet something, it's quick: a tabk you've already fought only gets its boss intro.

## Open questions

- **Mission order after kcaj**, which sets the order of the story.
- **Each mission's story:** its briefing, and how its crewmate's freeing plays out.
