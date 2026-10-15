# Tanks

How the player discovers tanks, fights them as bosses, wins them, drives them and calls them in.

**Status:** signed off by the owner, 2026-10-14. Revised 2026-10-15: boss tanks and the tanks you drive are separate things; kie's roulette steals your special; craters can be dropped if they're very hard.

**Read this when** you're designing a tank boss, a tank's controls, or anything tanks touch.

Up: [Design](README.md)

## The idea

Every crewmate owns a tank, and the holograms have stolen them all. Each one is a boss first, then yours to ride. On foot the game is fast. A boss tank is slow, heavy artillery, and its arrival is a big moment. Once it's yours, it drives like a Metal Slug tank.

## Two kinds of tank

A crewmate's tank shows up in two forms, and they're separate things in the game, built separately:
- **The boss tank** is the one a hologram drives at the end of a mission. It fights like a Pocket Tanks tank: it swings its turret, charges up its shots and lobs them across the arena ([tank bosses](#tank-bosses)).
- **Your tank** is the one you drive after winning it, or call in during a mission. It moves and aims Metal Slug style, quick and direct, like a soldier on foot ([driving a tank](#driving-a-tank)).

They share a crewmate's look and paint, and nothing else needs to carry over.

## Discovering tanks

Tanks stay hidden until they matter ([progressive disclosure](README.md#progressive-disclosure-the-thread-through-all-four)):
1. **Before the first tank,** nothing mentions tanks: not the menus, the HUD, the loading screens, the controls or the web page around the game. No enemy tanks appear before then either.
2. **The first tank** is kie's, at the end of Mission one, with a full boss intro.
3. **The first drive** comes straight after kie is freed. His tank is yours, and a short drive out of the base ends the mission and teaches the controls.
4. **The first call-in** comes in the next mission, when you can first call a tank in. The game shows how once, at a calm moment.
5. **The tank choice** on the mission screen appears once you've won a tank.

## Tank bosses

- **Each mission ends with a crewmate's tank.** A hologram of that crewmate drives it, using the crewmate's own moves, and the tank gets faster as it gets hurt.
- **Boss tanks move and aim like artillery.** The turret swings to aim, and the big shots charge up before they fire, so you can see them coming and get out of the way.
- **Then comes the [pilot round](#the-pilot-round).** Wreck the tank and its hologram pilot jumps out to fight you on foot with the crewmate's kit. It's a mirror match, and a taste of who you're about to free. Beat the pilot and it glitches out, and the real crewmate is freed.
- **Replays keep the pilot round.** Only the freeing scene is skipped.
- **Bosses fight in arenas,** the only places where blasts dig craters in the ground. If craters prove very hard to build, arenas get solid ground like everywhere else.
- **kie's tank comes first.** It cycles through three attacks, all from kie's kit:
  - waves of weasels that run at you and pop
  - hologram copies of itself, where only the real one can be hurt
  - a roulette that steals your special (or your super's charge) until you hit it hard enough to drop it
- **kcaj's tank is next,** in Mission two. It sweeps a beam, then rains pills across the screen.
- **Later tanks** take their attacks from their own crewmates' kits in the same way.

## The boss intro

A boss tank's arrival is the mission's big moment ([story](story.md#cutscenes): a warning banner, the music changes, and the tank makes its entrance). Draft, 2026-10-15, waiting for the owner's read-through.

**How it plays, in order** (about three seconds; you keep control throughout, so it never feels like the game took the stick away):
1. **The screen locks** as you step into the arena, and the mission's music cuts out.
2. **The warning:** a siren sounds twice, the screen edges flash red, and a big flashing red **WARNING!** slams in between hazard stripes that sweep across the top and bottom of the screen, Metal Slug style (picked 2026-10-15).
3. **The entrance:** the motor pool's blast shutter grinds open and the tank rolls out onto the arena floor. It can't be hurt and doesn't fire until it stops.
4. **The name card:** the tank's name ("KIE'S TANK") drops in under the banner, and the announcer shouts "Here comes the tank!" (it shouts "Warning!" with the banner in step 2).
5. **The fight:** the boss music kicks in on the tank's first shot, and its health bar fills up across the top.

**The announcer** (picked 2026-10-15) is a generated voice: a gravelly arcade announcer made with text-to-speech by the vendor repository's CI from its `voice/lines.json`, so a line is changed or added by editing that file. It also calls "Mission one... start!" and "Mission complete!".

**The music** (picked 2026-10-15): a looping Mission one track plays through the run, and a faster, minor-key boss track takes over for the fight. Both are made in code with the chip synth the sound effects use.

**The second time** you meet a tank (a replay, or after a lost fight at the boss-door checkpoint), the intro is quick: the banner and siren only, with the tank already in place, and the boss music starts at once.

**In co-op,** the world's authority starts the intro and both players see it together. Nobody needs to tap anything, so neither waits on the other.

**When it ends** (picked 2026-10-15): wrecking the tank plays a short victory sting over the boss music, which runs on through the pilot round and stops when the pilot glitches out ([freeing kie](story.md#freeing-kie)).

**Settings:** the music has its own MUSIC on/off beside SOUND, so a player can keep the sound effects and lose the tune.

## The pilot round

Wreck the tank and its hologram pilot jumps out to fight you on foot with the crewmate's own kit ([tank bosses](#tank-bosses), [kits](kits.md)). Draft, 2026-10-15, waiting for the owner's read-through.

**Can one AI drive any kit? Yes.** Every kit is played through the same few buttons: move, aim, JUMP, FIRE, SP, SU and, for three soldiers, the ability. The game already works this way: a soldier does whatever one frame's button presses (`SoldierInput`) tell it, whether they come from a keyboard, a pad or the network. So the pilot is simply a soldier on the holograms' side, and one brain presses its buttons. Nothing about a kit has to be rewritten for the pilot to use it.

**The brain** is the same for every pilot:
- It keeps to a range, faces you and aims the eight ways a player can, with a short reaction delay and a little aim error, so it reads as a person and not an aimbot.
- It jumps your shots and grenades when it sees them coming, and jumps or drops between levels to follow you.
- It fires in bursts with pauses between them, and every big move gets a tell (a flash and a sound) half a second before it lands, so you can see it coming, as with the tank's charged shots.
- It gets quicker and bolder as its health falls, like the tank.

**A playbook per kit** tunes the brain with a handful of settings, not new code: the range it likes, whether FIRE is held or tapped, and when to use SP, SU and the ability. For example:
- **kie** (Mission one): lobs Weasel Pop from mid-range and over cover; drops a Trollogram when you close in, and swaps places with it when you shoot the decoy; Steal when charged, lifting your special.
- **tones2:** closes in for the jet; rocket jumps to dodge and to get above you; spews when you're close.
- **kcaj:** holds Hyperfixate on you from range; rains Unmedicated when you're in the open.
- A few moves need a line of code in the playbook, such as kiwicore's drum solo (tap FIRE on the beat) and kie's swap (press SP again). The brain itself stays the same.

**Fairness:** the pilot never sees off-screen, never reads your input ahead of time, and its kit follows the same rules as yours (charges and cooldowns), with one difference: its super charges from its hits on you rather than from kills, since a duel has nothing else to kill (picked 2026-10-15). Its shots hurt you and your partner, and never the goons or itself.

**The fight's size** (picked 2026-10-15): a boss duel. The pilot has about three times a soldier's health and its own boss bar, so the duel lasts around 30 to 45 seconds.

**In co-op** (owner, 2026-10-15): one pilot with exactly the same health and moves as in solo, since missions don't change with two players ([difficulty](levels.md#difficulty)); the fight is simply easier with two. It switches targets between you: whoever is nearest, or whoever hurt it last.

**When it's beaten,** the pilot glitches out like the hologram goons, and the freeing scene plays ([story](story.md#cutscenes)). The boss music keeps playing through the pilot round ([the boss intro](#the-boss-intro)).

**The plan to build it:**
1. **Sides:** a soldier gets a side, players or holograms. Shots, blasts and kit effects already know whether they're hostile; the pilot's are hostile to players.
2. **The brain** (in `Core`, so it's tested without Unity): it reads what the pilot can see (its position, yours, shots coming, its meters) and writes the pilot's `SoldierInput` for the frame. Its tests drive it against scripted players.
3. **The playbook** for each soldier, starting with kie's.
4. **The hologram look** the goons use, on the pilot, and its glitch-out death.
5. **Co-op:** the world's authority runs the pilot, and the partner's game shows it like a remote player, using the soldier pose that's already sent for players.
6. **In the mission:** the pilot round starts when the tank is wrecked, after the boss tank build and the boss intro.

**What it depends on** (picked 2026-10-15): Mission one's pilot is kie, so kie's own kit is built for players first, and the pilot is built and tuned on it.

**Built** (2026-10-15): kie's pilot is in Mission one. Wreck the tank and hologram kie jumps out; beat him and he glitches out and the mission's won (`?test=pilot` starts at the boss and wrecks the tank as it wakes). The brain and kie's playbook are `Core/PilotBrain.cs`, his numbers `KiePilotSpec` (`Core/KieTuning.cs`), and the fighter `Pilot.cs`. His health is 40, not 15: a soldier's 5 hearts take whole hits, while a pistol lands a point every 0.18 s, so his bar is counted in shots to make the 30 to 45 second duel. His kit is a little softer than a player's (two weasels a press, two Trollograms that refill), and his weasels are the tank's. In co-op he goes for whoever is nearest (or your Trollogram, if it's up).

## Driving a tank

- **Climb in** to a tank you've won. It has its own health bar, separate from yours.
- **The controls are Metal Slug's:** the stick drives it and points the gun, the same eight ways a soldier aims on foot, and FIRE shoots at once. There's no turret to swing and no power to build: that's how boss tanks fight, not yours.
- **Rounds never refill:** a tank has 5 main shots, 3 specials and 1 super.
- **Eject before it blows.** The empty tank rolls on and explodes into the goons, Metal Slug style.
- **A wrecked tank is gone** until the next mission.
- **Any soldier can drive any tank you've won,** so tones2 can take kcaj's tank out.

## Calling in a tank

- **Once per mission,** hold the stick down and hold SP, and a tank you've won drops in beside you. A tap of SP still fires your special, even while you're crouching.
- **The call charges up first.** Holding SP charges a short call-in before the tank drops. Letting go before it's charged cancels the call.
- **Choose which tank** on the mission screen, before you start.
- **In co-op, each player calls their own,** so both of you can have a tank out at once.

## Not planned

- **Won't:** flying vehicles, submarines and animal mounts.
- **Could:** two-seat tanks, and riding on top of a partner's tank.
- **The only vehicles you can take** are crew tanks, won by beating them.

## Open questions

- **Each tank's boss attacks** after kcaj's, worked out from each crewmate's kit.
- **The charge-up time:** how long you hold SP before the tank drops, to be tuned in play.
- **Whether your tank jumps,** as Metal Slug's does.
