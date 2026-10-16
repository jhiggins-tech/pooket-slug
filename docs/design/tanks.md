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
1. **Before the first tabk,** nothing mentions tabks: not the menus, the HUD, the loading screens, the controls or the web page around the game. No enemy tabks appear before then either.
2. **The first tabk** is kie's, at the end of Mission one, with a full boss intro.
3. **The first call-in and the first drive** come straight after kie is freed (owner, 2026-10-16): kie frees tones2's own tabk and bows out, and in Mission one's coda, a short mini-level after the boss, a prompt has you call it in, then drive it out of the base ([the coda](story.md#the-coda)).
4. **kie's tabk** is wrecked in the boss fight, so Mission one wins only tones2's. kie's comes back repaired once Mission two is beaten (owner, 2026-10-16).
5. **The tabk choice** on the mission screen appears once you have two tabks to pick from: after Mission two.

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

A boss tabk's arrival is the mission's big moment ([story](story.md#cutscenes): a warning banner, the music changes, and the tabk makes its entrance). Draft, 2026-10-15, waiting for the owner's read-through.

**How it plays, in order** (about three seconds; you keep control throughout, so it never feels like the game took the stick away):
1. **The screen locks** as you step into the arena, and the mission's music cuts out.
2. **The warning:** a siren sounds twice, the screen edges flash red, and a big flashing red **WARNING!** slams in between hazard stripes that sweep across the top and bottom of the screen, Metal Slug style (picked 2026-10-15).
3. **The entrance:** the motor pool's blast shutter grinds open and the tabk rolls out onto the arena floor. It can't be hurt and doesn't fire until it stops.
4. **The name card:** the tabk's name ("KIE'S TABK") drops in under the banner, and the announcer shouts "Here comes the tabk!" (it shouts "Warning!" with the banner in step 2).
5. **The fight:** the boss music kicks in on the tabk's first shot, and its health bar fills up across the top.

**The pilot's intro:** from Mission two on, the first time a mission's boss intro plays, its Trollogram pilot gets their [character intro](story.md#character-intros) as the tabk stops and the hatch pops: crash zoom, TROLLO-&lt;NAME&gt;, slow-mo and the musical hit. It's the one moment the intro holds the action, and it never plays again. In Mission one, Trollogram kie gets his in [the motor pool reveal](story.md#the-motor-pool-reveal) instead.

**The announcer** (picked 2026-10-15) is a generated voice: a gravelly arcade announcer made with text-to-speech by the vendor repository's CI from its `voice/lines.json`, so a line is changed or added by editing that file. It also calls "Mission one... start!" and "Mission complete!".

**The music** (picked 2026-10-15): a looping Mission one track plays through the run, and a faster, minor-key boss track takes over for the fight. Both are made in code with the chip synth the sound effects use. The full track list is in [music](music.md).

**The second time** you meet a tabk (a replay, or after a lost fight at the boss-door checkpoint), the intro is quick: the banner and siren only, with the tabk already in place, and the boss music starts at once.

**In co-op,** the world's authority starts the intro and both players see it together. Nobody needs to tap anything, so neither waits on the other.

**When it ends** (picked 2026-10-15): wrecking the tabk plays a short victory sting over the boss music, which runs on through the pilot round and stops when the pilot glitches out ([freeing kie](story.md#freeing-kie)).

**Settings:** the music has its own MUSIC on/off beside SOUND, so a player can keep the sound effects and lose the tune.

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

**When it's beaten,** the pilot glitches out like the Trollogram goons, and the freeing scene plays ([story](story.md#cutscenes)). The boss music keeps playing through the pilot round ([the boss intro](#the-boss-intro)).

**The plan to build it:**
1. **Sides:** a soldier gets a side, players or Trollograms. Shots, blasts and kit effects already know whether they're hostile; the pilot's are hostile to players.
2. **The brain** (in `Core`, so it's tested without Unity): it reads what the pilot can see (its position, yours, shots coming, its meters) and writes the pilot's `SoldierInput` for the frame. Its tests drive it against scripted players.
3. **The playbook** for each soldier, starting with kie's.
4. **The Trollogram look** the goons use, on the pilot, and its glitch-out death.
5. **Co-op:** the world's authority runs the pilot, and the partner's game shows it like a remote player, using the soldier pose that's already sent for players.
6. **In the mission:** the pilot round starts when the tabk is wrecked, after the boss tabk build and the boss intro.

**What it depends on** (picked 2026-10-15): Mission one's pilot is kie, so kie's own kit is built for players first, and the pilot is built and tuned on it.

**Built** (2026-10-15): kie's pilot is in Mission one. Wreck the tabk and Trollogram kie jumps out; beat him and he glitches out and the mission's won (`?test=pilot` starts at the boss and wrecks the tabk as it wakes). The brain and kie's playbook are `Core/PilotBrain.cs`, his numbers `KiePilotSpec` (`Core/KieTuning.cs`), and the fighter `Pilot.cs`. His health is 40, not 15: a soldier's 5 hearts take whole hits, while a pistol lands a point every 0.18 s, so his bar is counted in shots to make the 30 to 45 second duel. His kit is a little softer than a player's (two weasels a press, two Trollograms that refill), and his weasels are the tabk's. In co-op he goes for whoever is nearest (or your Trollogram, if it's up). Since Mission one became single player only (owner, 2026-10-16), kie's pilot only ever fights one player; the co-op code stays for later missions' pilots.

## Driving a tabk

- **Climb in** to a tabk you've won. It has its own health bar, separate from yours.
- **The controls are Metal Slug's:** the stick drives it and points the gun, the same eight ways a soldier aims on foot, and FIRE shoots at once. There's no turret to swing and no power to build: that's how boss tabks fight, not yours.
- **Rounds never refill:** a tabk has 5 main shots, 3 specials and 1 super.
- **Fuel runs out, as in Pooket Tabks** (owner, 2026-10-16). A tabk has a fuel bar that drains as it drives (and hops). When it's dry, the tabk can't move and stays where it is, but it can still aim and fire. Fuel belongs to each tabk: a tabk arrives full, nothing refuels it, and ejecting is the way out of a dry one (the empty tabk still rolls on and blows up, on its own momentum, Metal Slug style).
- **Eject before it blows.** The empty tabk rolls on and explodes into the goons, Metal Slug style.
- **A wrecked tabk is gone** until the next mission.
- **Any soldier can drive any tabk you've won,** so tones2 can take kcaj's tabk out.

**Built** (2026-10-16, on for everyone; the spike was 2026-10-15): your tabk is part of the game, solo and co-op. As with everything tabk, it shows only once a tabk's been won (progressive disclosure); to try it, `?test=tank` parks one beside each player at the boss checkpoint and gives one call-in, and `?unlock=tank` gives the call-in alone. Jump into it, or press JUMP touching it, to climb in. Inside, the stick drives it and points the gun the eight ways, keeping the last aim when let go; FIRE shoots a shell at once, SP lobs a mortar, SU fans a barrage (5, 3 and 1, never refilled); JUMP hops it, and down and JUMP ejects. It has its own health bar (TK, under the kit's meters) and a fuel bar (FU, under that: 18 seconds of driving, up from the spike's 12; a hop burns half a second; dry, it stays put but still aims and fires). Driving (or landing) on a goon on foot squashes it: soldiers, runners, drones and weasels, never barrels, tabks or the pilot; that's the payoff for the fuel. Every hit meant for the driver lands on the tabk. Ejected, it rolls on and blows up on the first goon or wall; wrecked, it throws you clear and blows up. In co-op each player's tabk runs on their own screen, like their soldier: its pose travels with the soldier's replicated state, a partner sees it drive, aim, fire and blow up, and enemy shots stop on it; its shells go as the player's shots (weapons 18 and 19) and squashed goons as their hits. Code: `PlayerTank.cs`, `TankLook.cs` (the look, shared with a partner's copy in `RemotePlayer.cs`), numbers `PlayerTankSpec` and `TankPose` (`Core/PlayerTankSpec.cs`).

## tones2's tabk

The first tabk you win, freed by kie at the end of Mission one and called in for the first time in [the coda](story.md#the-coda). Draft, 2026-10-16: the guns, ten-1, ten-2, ten-3's length, the paint and single player are the owner's picks; the name and the drone are Claude's defaults, to review.

- **Its own guns** (owner, 2026-10-16): each crewmate's tabk fires tabk-sized versions of their kit, as in Pooket Tabks, where the tank is the kit. Rounds stay 5, 3 and 1, never refilled, and the controls stay the same ([driving a tabk](#driving-a-tabk)).
  - **ten-1, the water cannon** (FIRE, 5 rounds; owner, 2026-10-16): one round is one spray cycle, as in Pooket Tabks. Press FIRE and the pressure builds in spurts from a wide, weak spray to a clean, hard arc, holds there for a moment, then eases off (about three seconds in all), and you aim it as it goes. The full-pressure arc does the real damage and shoves goons back a long way. Off a wall point-blank it splashes back, harmless, as on foot.
  - **ten-2, the mud jump** (SP, 3 rounds; owner, 2026-10-16: as true to the Pooket Tabks tank as it can be): the tabk shakes and rumbles as it builds up (about a second, cut down from Tabks' 10 s turn), then blasts off along the aim like a jetpack, flies and comes down somewhere new. The exhaust is a huge blast of toxic mud that carpets the ground around the launch and poisons goons walking through it. It aims the upper five ways (a flat aim launches long and low, a downward one straight up), burns no fuel, and landing on goons squashes them. In the coda it's how you get over the sandbag nest and leave the goons behind it in the mud.
  - **ten-3, the spew** (SU, 1 round; owner, 2026-10-16): tabk-sized, and twice as long as on foot: three seconds of spew along the aim (the soldier's is 1.5 s, `Tones2Tuning`), melting anything close and leaving a long trail of sludge. He can drive while it pours.
- **Paint** (owner, 2026-10-16): orange and black, with a pumpkin-head stencil on the hull.
- **Its name** on screen while you drive it: TONES2'S TABK, over its rounds (WATER 5, MUD 3, SPEW 1).
- **The rest is shared** with every tabk: health, fuel, speed, the hop, squashing goons, eject and the runaway.
- **One player:** Mission one is single player only (owner, 2026-10-16), so the coda never has two tabks. Open: what a co-op partner calls in during Mission two, before kie's tabk is back.
- **The cargo drone** that lowers it is the crew's own, in tones2's orange and black, and every call-in uses it ([freeing kie](story.md#freeing-kie)). Its look is still to draw.

## Calling in a tabk

- **Once per mission,** hold the stick down and hold SP, and a tabk you've won drops in beside you, with a full tabk of fuel. A tap of SP still fires your special, even while you're crouching.
- **The call charges up first.** Holding SP charges a short call-in before the tabk drops. Letting go before it's charged cancels the call.
- **Choose which tabk** on the mission screen, before you start.
- **In co-op, each player calls their own,** so both of you can have a tabk out at once.

**Built so far:** the call-in drops your real tabk ([driving a tabk](#driving-a-tabk)) for anyone who has one to call, solo and co-op; until tabks are won in play, only `?unlock=tank` or `?test=tank` give one. **Spike** (2026-10-16, `?spike=callin`, solo): plays as if tones2's tabk were won, the way the coda brings it in. You have one call-in, and a moment after the START banner a blinking prompt says HOLD DOWN + SP TO CALL YOUR TABK, until you start a call (in the spike every visit; for real only the first time, in the coda, and the `CallInHinted` event is where the game marks it seen). The call charges, a cargo drone (placeholder: the goons' drone, scaled up and painted gold) lowers the tabk on a cable while a marker blinks where it will land, lets go as it lands, and climbs away. Code: `Core/CallInCoach.cs`, `World.CallInCoach`, the prompt in `HudView`, the marker and drone in `PlayerTank`. Real players get it from Mission one's coda on (owner, 2026-10-16): the coda's HOLD DOWN + SP prompt is the hint's first showing, and every mission after starts with the call-in. Open: the tabk choice on the mission screen, which waits for a second tabk.

## Not planned

- **Won't:** flying vehicles, submarines and animal mounts.
- **Could:** two-seat tabks, and riding on top of a partner's tabk.
- **The only vehicles you can take** are crew tabks, won by beating them.

## Open questions

- **Each tabk's boss attacks** after kcaj's, worked out from each crewmate's kit.
- **The charge-up time:** how long you hold SP before the tabk drops, to be tuned in play.
- **Whether your tabk jumps,** as Metal Slug's does.
