# Tabks

How the player discovers tabks, fights them as bosses, wins them, drives them and calls them in.

**Status:** signed off by the owner, 2026-10-14. Revised 2026-10-15: boss tabks and the tabks you drive are separate things; kie's roulette steals your special; craters can be dropped if they're very hard.

**Read this when** you're designing a tabk boss, a tabk's controls, or anything tabks touch.

Up: [Design](README.md)

## The idea

Every crewmate owns a tabk, and the Trollograms have stolen them all. Each one is a boss first, then yours to ride. On foot the game is fast. A boss tabk is slow, heavy artillery, and its arrival is a big moment. Once it's yours, it drives like a Metal Slug tabk.

## Two kinds of tabk

A crewmate's tabk shows up in two forms, and they're separate things in the game, built separately:
- **The boss tabk** is the one a Trollogram drives at the end of a mission. It fights like a Pocket Tanks tabk: it swings its turret, charges up its shots and lobs them across the arena ([tabk bosses](#tabk-bosses)).
- **Your tabk** is the one you drive after winning it, or call in during a mission. It moves and aims Metal Slug style, quick and direct, like a soldier on foot ([driving a tabk](#driving-a-tabk)).

They share a crewmate's look and paint, and nothing else needs to carry over.

## Discovering tabks

Tabks stay hidden until they matter ([progressive disclosure](README.md#progressive-disclosure-the-thread-through-all-four)):
1. **Before the first tabk,** nothing mentions tabks: not the menus, the HUD, the loading screens, the controls or the web page around the game. No enemy tabks appear before then either. The one exception is [the opening](cutscenes.md#the-opening), as foreshadowing: tones2 cries out for the crew's tabks and finds their bay empty, but none is shown (owner, 2026-10-19).
2. **The first tabk** is kie's, at the end of Mission one, with a full [boss intro](cutscenes.md#the-boss-intro).
3. **The first call-in and the first drive** come straight after kie is freed (owner, 2026-10-16): kie frees tones2's own tabk and bows out, and in Mission one's coda, a short mini-level after the boss, a prompt has you call it in, then drive it out of the base ([the coda](story.md#the-coda)).
4. **kie's tabk** is wrecked in the boss fight, so Mission one wins only tones2's. kie's comes back repaired once Mission two is beaten (owner, 2026-10-16), rebuilt as [THE PROJECTIONIST](#kies-tabk) ([kie's tabk comes home](cutscenes.md#kies-tabk-comes-home)).
5. **The tabk choice** is made in the den's tabk bay once you have two tabks to pick from, after Mission two: the one on the repair stands is the one you call in ([the tabk bay](lobby.md#the-tabk-bay)).

## Tabk bosses

- **Each mission ends with a crewmate's tabk.** A Trollogram of that crewmate drives it, using the crewmate's own moves, and the tabk gets faster as it gets hurt.
- **Boss tabks move and aim like artillery.** The turret swings to aim, and the big shots charge up before they fire, so you can see them coming and get out of the way.
- **Then comes the [pilot round](#the-pilot-round).** Wreck the tabk and its Trollogram pilot jumps out to fight you on foot with the crewmate's kit. It's a mirror match, and a taste of who you're about to free. Beat the pilot and it glitches out, and the real crewmate is freed.
- **Replays keep the pilot round.** Only the freeing scene is skipped.
- **Bosses fight in arenas,** the only places where blasts dig craters in the ground. If craters prove very hard to build, arenas get solid ground like everywhere else.
- **kie's tabk comes first.** It cycles through three attacks, all from kie's kit:
  - waves of weasels that run at you and pop
  - Trollogram copies of itself, where only the real one can be hurt
  - a roulette that steals your special (or your super's charge) until you hit it hard enough to drop it
- **kcaj's tabk is next,** in Mission two. It sweeps a beam, then rains pills across the screen.
- **Later tabks** take their attacks from their own crewmates' kits in the same way.

## The boss intro

A boss tabk's arrival, with its warning banner, name card and music, is a cutscene played in the game itself: its
script and rules (the quick second time, the pilot's intro, co-op, the music) are in
[cutscenes](cutscenes.md#the-boss-intro).

## The pilot round

Wreck the tabk and its Trollogram pilot jumps out to fight you on foot with the crewmate's own kit ([tabk bosses](#tabk-bosses), [kits](kits.md)). Draft, 2026-10-15, waiting for the owner's read-through.

**Can one AI drive any kit? Yes.** Every kit is played through the same few buttons: move, aim, JUMP, FIRE, SP, SU and, for three soldiers, the ability. The game already works this way: a soldier does whatever one frame's button presses (`SoldierInput`) tell it, whether they come from a keyboard, a pad or the network. So the pilot is simply a soldier on the Trollograms' side, and one brain presses its buttons. Nothing about a kit has to be rewritten for the pilot to use it.

**The brain** is the same for every pilot:
- It keeps to a range, faces you and aims the eight ways a player can, with a short reaction delay and a little aim error, so it reads as a person and not an aimbot.
- It jumps your shots and grenades when it sees them coming, and jumps or drops between levels to follow you.
- It fires in bursts with pauses between them, and every big move gets a tell (a flash and a sound) half a second before it lands, so you can see it coming, as with the tabk's charged shots.
- It gets quicker and bolder as its health falls, like the tabk.

**A playbook per kit** tunes the brain with a handful of settings, not new code: the range it likes, whether FIRE is held or tapped, and when to use SP, SU and the ability. For example:
- **kie** (Mission one): lobs Weasel Pop from mid-range and over cover; drops a Trollogram when you close in, and swaps places with it when you shoot the decoy; Steal when charged, lifting your special.
- **tones2:** closes in for the jet; rocket jumps to dodge and to get above you; spews when you're close.
- **kcaj:** holds Hyperfixate on you from range; rains Unmedicated when you're in the open.
- A few moves need a line of code in the playbook, such as kiwicore's drum solo (tap FIRE on the beat) and kie's swap (press SP again). The brain itself stays the same.

**Fairness:** the pilot never sees off-screen, never reads your input ahead of time, and its kit follows the same rules as yours (charges and cooldowns), with one difference: its super charges from its hits on you rather than from kills, since a duel has nothing else to kill (picked 2026-10-15). Its shots hurt you and your partner, and never the goons or itself.

**The fight's size** (picked 2026-10-15): a boss duel. The pilot has about three times a soldier's health and its own boss bar, so the duel lasts around 30 to 45 seconds.

**In co-op** (owner, 2026-10-15): one pilot with exactly the same health and moves as in solo, since missions don't change with two players ([difficulty](levels.md#difficulty)); the fight is simply easier with two. It switches targets between you: whoever is nearest, or whoever hurt it last.

**When it's beaten,** the pilot glitches out like the Trollogram goons, and the freeing scene plays ([freeing kie](cutscenes.md#freeing-kie)). The boss music keeps playing through the pilot round ([the boss intro](cutscenes.md#the-boss-intro)).

**The plan to build it:**
1. **Sides:** a soldier gets a side, players or Trollograms. Shots, blasts and kit effects already know whether they're hostile; the pilot's are hostile to players.
2. **The brain** (in `Core`, so it's tested without Unity): it reads what the pilot can see (its position, yours, shots coming, its meters) and writes the pilot's `SoldierInput` for the frame. Its tests drive it against scripted players.
3. **The playbook** for each soldier, starting with kie's.
4. **The Trollogram look** the goons use, on the pilot, and its glitch-out death.
5. **Co-op:** the world's authority runs the pilot, and the partner's game shows it like a remote player, using the soldier pose that's already sent for players.
6. **In the mission:** the pilot round starts when the tabk is wrecked, after the boss tabk build and the boss intro.

**What it depends on** (picked 2026-10-15): Mission one's pilot is kie, so kie's own kit is built for players first, and the pilot is built and tuned on it.

**Built** (2026-10-15): kie's pilot is in Mission one. Wreck the tabk and Trollogram kie jumps out; beat him and he glitches out and the mission's won (`?test=pilot` starts at the boss and wrecks the tabk as it wakes). The brain and kie's playbook are `Core/Bosses/PilotBrain.cs`, his numbers `KiePilotSpec` (`Core/Crew/KieTuning.cs`), and the fighter `Pilot.cs`. His health is 40, not 15: a soldier's 5 hearts take whole hits, while a pistol lands a point every 0.18 s, so his bar is counted in shots to make the 30 to 45 second duel. His kit is a little softer than a player's (two weasels a press, two Trollograms that refill), and his weasels are the tabk's. In co-op he goes for whoever is nearest (or your Trollogram, if it's up). Since Mission one became single player only (owner, 2026-10-16), kie's pilot only ever fights one player; the co-op code stays for later missions' pilots.

## Driving a tabk

- **Climb in** to a tabk you've won. It has its own health bar, separate from yours.
- **The controls are Metal Slug's:** the stick drives it and points the gun, the same eight ways a soldier aims on foot, and FIRE shoots at once. There's no turret to swing and no power to build: that's how boss tabks fight, not yours.
- **Rounds never refill:** a tabk has 5 main shots, 3 specials and 1 super.
- **Fuel runs out, as in Pooket Tabks** (owner, 2026-10-16). A tabk has a fuel bar that drains as it drives (and hops). When it's dry, the tabk can't move and stays where it is, but it can still aim and fire. Fuel belongs to each tabk: a tabk arrives full, nothing refuels it, and ejecting is the way out of a dry one (the empty tabk still rolls on and blows up, on its own momentum, Metal Slug style).
- **Eject before it blows.** The empty tabk rolls on and explodes into the goons, Metal Slug style.
- **A wrecked tabk is gone** until the next mission.
- **Any soldier can drive any tabk you've won,** so tones2 can take kcaj's tabk out.

**Built** (2026-10-16, on for everyone; the spike was 2026-10-15): your tabk is part of the game, solo and co-op. As with everything tabk, it shows only once a tabk's been won (progressive disclosure); to try it, `?test=tank` parks one beside each player at the boss checkpoint and gives one call-in, and `?unlock=tank` gives the call-in alone. Jump into it, or press JUMP touching it, to climb in. Inside, the stick drives it and points the gun the eight ways, keeping the last aim when let go; FIRE, SP and SU fire its three guns (5, 3 and 1, never refilled; since 2026-10-16 tones2's, [below](#tones2s-tabk)); JUMP hops it, and down and JUMP ejects. It has its own health bar (TK, under the kit's meters; 24, doubled from 12 on 2026-10-20 because tabks died too quickly) and a fuel bar (FU, under that: 18 seconds of driving, up from the spike's 12; a hop burns half a second; dry, it stays put but still aims and fires). Driving (or landing) on a goon on foot squashes it: soldiers, runners, drones and weasels, never barrels, tabks or the pilot; that's the payoff for the fuel. Every hit meant for the driver lands on the tabk. Ejected, it rolls on and blows up on the first goon or wall; wrecked, it throws you clear and blows up. In co-op each player's tabk runs on their own screen, like their soldier: its pose travels with the soldier's replicated state, a partner sees it drive, aim, fire and blow up, and enemy shots stop on it; squashed goons go as their hits. Code: `PlayerTank.cs`, `TankLook.cs` (the look, shared with a partner's copy in `RemotePlayer.cs`), numbers `PlayerTankSpec` and `TankPose` (`Core/Tabks/PlayerTankSpec.cs`).

## tones2's tabk

The first tabk you win, freed by kie at the end of Mission one and called in for the first time in [the coda](story.md#the-coda). Approved by the owner, 2026-10-16.

- **Its own guns** (owner, 2026-10-16): each crewmate's tabk fires tabk-sized versions of their kit, as in Pooket Tabks, where the tank is the kit. Rounds stay 5, 3 and 1, never refilled, and the controls stay the same ([driving a tabk](#driving-a-tabk)).
  - **ten-1, the water cannon** (FIRE, 5 rounds; owner, 2026-10-16): one round is one spray cycle, as in Pooket Tabks. Press FIRE and the pressure builds in spurts from a wide, weak spray to a clean, hard arc, holds there for a moment, then eases off (about three seconds in all), and you aim it as it goes. The full-pressure arc does the real damage and shoves goons back a long way. Off a wall point-blank it splashes back, harmless, as on foot.
  - **ten-2, the mud jump** (SP, 3 rounds; owner, 2026-10-16: as true to the Pooket Tabks tank as it can be): the tabk shakes and rumbles as it builds up (about a second, cut down from Tabks' 10 s turn), then blasts off along the aim like a jetpack, flies and comes down somewhere new. The exhaust is a huge blast of toxic mud that carpets the ground around the launch and poisons goons walking through it. It aims the upper five ways (a flat aim launches long and low, a downward one straight up), burns no fuel, and landing on goons squashes them. In the coda it's how you get over the sandbag nest and leave the goons behind it in the mud.
  - **ten-3, the spew** (SU, 1 round; owner, 2026-10-16): tabk-sized, and twice as long as on foot: three seconds of spew along the aim (the soldier's is 1.5 s, `Tones2Tuning`), melting anything close and leaving a long trail of sludge. He can drive while it pours.
- **Paint** (owner, 2026-10-16): orange and black, with a pumpkin-head stencil on the hull.
- **Its name** on screen while you drive it: TONES2'S TABK, over its rounds (WATER 5, MUD 3, SPEW 1).
- **The rest is shared** with every tabk: health, fuel, speed, the hop, squashing goons, eject and the runaway.
- **One player:** Mission one is single player only (owner, 2026-10-16), so the coda never has two tabks. Open: what a co-op partner calls in during Mission two, before kie's tabk is back.
- **The cargo drone** that lowers it is the crew's own, in tones2's orange and black, and every call-in uses it ([freeing kie](cutscenes.md#freeing-kie)). For now it's the goons' drone repainted, scaled up.

**Built** (2026-10-16): your tabk is tones2's now, wherever one appears (`?test=tank`, `?unlock=tank`, and the coda); the shell, mortar and barrage are retired (weapons 18 and 19 and shots 9 and 10 stay reserved). The hull is the tabk's shape painted orange with black tracks and gun, a jack-o'-lantern face on its flank. FIRE runs one spray cycle per round (1 s building in four spurts, 0.8 s held, 1 s easing off), SP shakes for 1 s and launches at 21 along the aim with a mud carpet 6.4 wide, SU pours 3 s of spew. In co-op a partner sees the tabk and its mud, not its water or spew (Mission one is single player). Code: `PlayerTank.cs`, numbers `Tones2TabkSpec` (`Core/Crew/Tones2TabkSpec.cs`, as `Tones2Tuning.Tabk`), art `Tones2Art.TabkHull`, `TabkBarrel`, `TabkDrone`.

## kie's tabk

THE PROJECTIONIST: the second tabk you win, kie's own, back after Mission two (owner, 2026-10-19, kie-tabk-design; page https://claude.ai/artifact/Hp8MXScfoogFsPouTpdexn).

- **The theme** (owner picked the projector, 2026-10-19): the boss tabk was wrecked in Mission one, and kie rebuilds it around a Trollogram projector he took from the creamery's cold store (the Trollograms' server room). So it comes back still frosted, wrapped in copper freezer pipes, patched with welded plates and a few rust spots. The projector is the turret, its short fat lens the gun, and two film reels on top spin while a shot charges. Its return is a den beat ([kie's tabk comes home](cutscenes.md#kies-tabk-comes-home)).
- **Paint** (owner, 2026-10-19): kie's blue (#4ea8ff) with a green (#46d27a) stripe on the hull and green on the reels' spokes, from his Pooket Tabks colours. The lens glows warm gold, the friendly colour, never cyan (cyan is the villains'). The reference painting is blue take 2 (`/mnt/project-files/concepts/kie-tabk/proj-blue-2.png`).
- **Its own guns:** kie's kit, tabk-sized, like tones2's; rounds 5, 3 and 1, never refilled, and the same controls ([driving a tabk](#driving-a-tabk)).
  - **Weasel Pop** (FIRE, 5 rounds): weasels tumble out of the lens, lobbed, and scurry under goons to pop, as on foot.
  - **Trollogram** (SP, 3 rounds): the lens beams a Trollogram of the tabk, a decoy goons shoot at.
  - **Steal** (SU, 1 round): the roulette spins on the projector and lifts a power-up off the toughest enemy on screen, as on foot.
  - How each is tuned is for its build card; these follow kie's kit on foot ([kits](kits.md#kie)).
- **Its name** on screen while you drive it: KIE'S TABK, over its rounds (WEASEL 5, TROLLOGRAM 3, STEAL 1).
- **The sprite** is 52 × 28, six rows taller than the tabk's 52 × 22 for the reels: the tabk's own hull and tracks, the projector body over them with frost on top and a copper coil and pipe behind, the lens in front and the two reels above. Its source (the rows and a renderer) is in `/mnt/project-files/concepts/kie-tabk/sprite/`, for the build card to put in `ArtData`.
- **Mission one's boss stays the plain olive tabk** with Trollogram kie at the controls ([tabk bosses](#tabk-bosses)); this one is clearly kie's own.
- **In the bay** it parks downstairs once it's back, and the drone swaps it onto the repair stands when you pick it ([the tabk bay](lobby.md#the-tabk-bay)).
- **Built so far** (kcaj-tabk-build, 2026-10-20): drivable when it's loaded in the bay, solo and co-op. The sprite is Design's two frames in `Crew/Kcaj/KcajTabkArt.cs` (the second while a volley or the beam is going), the nozzle cut out of them to turn at its pivot. Guns: `Crew/Kcaj/KcajTabk.cs`, numbers and rules in `Core/Crew/KcajTabkSpec.cs` (tested). FIRE wobbles the swirl, then pipes three pairs of his cones along the aim, faster and further than his. SP is his beam from the nozzle, longer and ticking hotter; a beam that touches nothing keeps its round, and the tabk stands still while it holds. SU rains his pills, the same count as his, harmless to the crew; the rain is the world's, so it keeps falling if the tabk's ejected or blown. A partner's screen draws it in its own look and plays its beam and pills from it (`KcajCopy`, in its tabk mode). `?test=softserve` parks one at the start.
- **Built so far** (kie-tabk-build): drivable when it's loaded in the bay, solo and co-op (a partner's screen draws it in its own look, from the tabk's pose). The sprite is Design's rows in `Crew/Kie/KieTabkArt.cs`, the lens turning at the projector's front, the reels a second frame. Guns: `Crew/Kie/KieTabk.cs`, numbers and rules in `Core/Crew/KieTabkSpec.cs` (tested). FIRE spins the reels for a moment, then lobs five weasels (his are three), bigger pops; with no room for a volley the round isn't spent. SP spins the reels and beams a Trollogram of the whole tabk 3.6 ahead, on screen, that goons shoot instead and that blows up bigger than his own (it can't be swapped with). SU is his Steal; the loot is his, and while he drives FIRE shoots it from the lens until it's spent (the HUD shows it in the first gun's place). `?test=kietabk` parks one at the start.

## kcaj's tabk

SOFT SERVE: the third tabk you win, kcaj's own, from Mission two (owner, 2026-10-20, kcaj-tabk-design; page https://claude.ai/artifact/LHWwsaCRSVdzqgiFZwsdKm). Hand-drawn only, no generated art (owner, 2026-10-20).

- **The theme** (owner picked the soft-serve machine, 2026-10-20): an ice cream van gone to war. The turret is a soft-serve machine with a serving hatch under a striped awning, a menu sign and a van's loudspeaker, and a twist of vanilla with a strawberry ribbon on top; the gun is its chrome nozzle. It keeps the cone on its antenna from [its arrival](cutscenes.md#kcajs-tabk-arrives).
- **How it gets home:** the cargo drone hauls the wreck back from the creamery after Mission two, and kcaj rebuilds it round a soft-serve machine he brought out with him, as kie did with his projector. The creamery's own look turned on the Trollograms is the joke: it's his now, in his colours, not Brain Freeze's.
- **Paint:** kcaj's purple (#a060e0, shade #6a3aa0) with his gold (#ffc53d) in a stripe along the hull, on the awning and the loudspeaker, from his Pooket Tabks colours; the machine is pale grey (#e2e8f0); a cone decal on the flank. The bay card's stand-in magenta goes.
- **Its own guns:** kcaj's kit, tabk-sized, like tones2's and kie's; rounds 5, 3 and 1, never refilled, and the same controls ([driving a tabk](#driving-a-tabk)).
  - **Double Park** (FIRE, 5 rounds): the nozzle pipes out twin cones, lobbed either side of the aim, that splat on what they land on, as on foot.
  - **Hyperfixate** (SP, 3 rounds): the beam shines from the nozzle and sticks to the first thing it touches, ticking harder the longer it holds, as on foot.
  - **Unmedicated** (SU, 1 round): the serving hatch flies open and pills rain across the screen, as on foot.
  - How each is tuned is for its build card; these follow kcaj's kit on foot ([kits](kits.md#kcaj)).
- **Its name** on screen while you drive it: KCAJ'S TABK, over its rounds (CONES 5, FIXATE 3, PILLS 1).
- **The sprite** is 52 × 28 like kie's: the tabk's own hull and tracks, the machine over them, the swirl above, the nozzle in front and the antenna behind. A second frame, while a shot charges, wobbles the swirl and drips cream from the nozzle. Its source (the rows, `soft.py` and the frames in `out/soft-1.txt`, `soft-2.txt`) is in `/mnt/project-files/concepts/kcaj-tabk/sprite/`, for the build card to put in `ArtData`.
- **Mission two's boss stays the plain tabk** with Trollogram kcaj at the controls and a cone on its antenna ([tabk bosses](#tabk-bosses)); this one is clearly kcaj's own.
- **In the bay** it parks downstairs once it's back, and the drone swaps it onto the repair stands when you pick it ([the tabk bay](lobby.md#the-tabk-bay)).

## Calling in a tabk

- **Once per mission,** hold the stick down and hold SP, and a tabk you've won drops in beside you, with a full tabk of fuel. A tap of SP still fires your special, even while you're crouching.
- **The call charges up first.** Holding SP charges a short call-in before the tabk drops. Letting go before it's charged cancels the call.
- **Choose which tabk** in the den's tabk bay: the one on the repair stands is loaded ([the tabk bay](lobby.md#the-tabk-bay)). The mission screen shows it with the mission.
- **In co-op, each player calls their own,** so both of you can have a tabk out at once.

**Built so far:** the call-in drops your real tabk ([driving a tabk](#driving-a-tabk)) for anyone who has one to call, solo and co-op; until tabks are won in play, only `?unlock=tank` or `?test=tank` give one. The first time, in the coda, a blinking prompt says HOLD DOWN + SP TO CALL YOUR TABK until you start a call (the `CallInHinted` event is where the game marks it seen; the `?spike=callin` that tried it first was retired on 2026-10-16). The call charges, a cargo drone (the goons' drone scaled up, in tones2's orange and black) lowers the tabk on a cable while a marker blinks where it will land, lets go as it lands, and climbs away. Code: `Core/Tabks/CallInCoach.cs`, `World.CallInCoach`, the prompt in `HudView`, the marker and drone in `PlayerTank`. Real players get it from Mission one's coda on (owner, 2026-10-16): the coda's HOLD DOWN + SP prompt is the hint's first showing, and every mission after starts with the call-in (built 2026-10-20: `MissionStory.CallIns`, one per mission, each player their own, so co-op phones start alike; it comes back when the mission restarts, not on a continue). The tabk choice is in the bay (tabk-select-build: `Core/Tabks/Garage.cs`, `Game/BayFlow.cs`, `Ui/Den/BayView.cs`), kept with the settings; kie's tabk is built (kie-tabk-build), so a mission drops it when it's loaded; kcaj's is built too (kcaj-tabk-build), so every tabk in the bay drops as itself. A tabk won stays won and always drops as itself (owner, 2026-10-20: unlocked is unlocked; bay-card-note took out the built check and the bay's "MISSIONS DROP TONES2'S FOR NOW" note).

## Not planned

- **Won't:** flying vehicles, submarines and animal mounts.
- **Could:** two-seat tabks, and riding on top of a partner's tabk.
- **The only vehicles you can take** are crew tabks, won by beating them.

## Open questions

- **Each tabk's boss attacks** after kcaj's, worked out from each crewmate's kit.
- **The charge-up time:** how long you hold SP before the tabk drops, to be tuned in play.
- **Whether your tabk jumps,** as Metal Slug's does.
