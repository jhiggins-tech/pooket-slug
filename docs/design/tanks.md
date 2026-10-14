# Tanks

How the player discovers tanks, fights them as bosses, wins them, drives them and calls them in.

**Status:** draft for sign-off, 2026-10-14.

**Read this when** you're designing a tank boss, a tank's controls, or anything tanks touch.

Up: [Design](README.md)

## The idea

Every crewmate owns a tank, and the holograms have stolen them all. Each one is a boss first, then yours to ride. On foot the game is fast. A tank is slow, heavy artillery, and its arrival is a big moment.

## Discovering tanks

Tanks stay hidden until they matter ([progressive disclosure](README.md#progressive-disclosure-the-thread-through-all-four)):
1. **Before the first tank,** nothing mentions tanks: not the menus, the HUD, the loading screens, the controls or the web page around the game. *(Proposed: no enemy tanks before then either.)*
2. **The first tank** is kie's, at the end of Mission one, with a full boss intro.
3. **The first drive** comes straight after kie is freed. His tank is yours, and a short drive out of the base ends the mission and teaches the controls.
4. **The first call-in** comes in the next mission, when you can first call a tank in. The game shows how, once, at a calm moment *(proposed)*.
5. **The tank choice** on the mission screen appears once you've won a tank.

## Tank bosses

- **Each mission ends with a crewmate's tank.** A hologram of that crewmate drives it, using the crewmate's own moves, and it gets faster as it gets hurt.
- **Then comes the pilot round.** Wreck the tank and its hologram pilot jumps out to fight you on foot with the crewmate's kit. It's a mirror match, and a taste of who you're about to free. Beat the pilot and it glitches out, and the real crewmate is freed.
- **Bosses fight in arenas,** the only places where blasts dig craters in the ground.
- **kie's tank comes first.** It cycles through three attacks, all from kie's kit:
  - waves of weasels that run at you and pop
  - hologram copies of itself, where only the real one can be hurt
  - a roulette that steals one of your upgrades until you hit it hard enough to drop it
- **Later tanks** take their attacks from their own crewmates' kits in the same way.

## Driving a tank

- **Climb in** to a tank you've won. It has its own health bar, separate from yours.
- **The controls:** the stick swings the turret. Hold FIRE to build power, then let go. It's made for big, slow hits.
- **Rounds are limited:** 5 of the tank's main shot, 3 specials and 1 super.
- **Eject before it blows:** when the tank's health runs out, jump clear.
- **Any soldier can drive any tank you've won,** so tones2 can take kcaj's tank out.

## Calling in a tank

- **Once per mission,** hold the stick down and press SP, and a tank you've won drops in beside you.
- **Choose which tank** on the mission screen, before you start.
- **In co-op,** each player calls their own *(proposed)*.

## Not planned

- **Won't:** flying vehicles, submarines and animal mounts.
- **Could:** two-seat tanks, and riding on top of a partner's tank.
- **The only vehicles you can take** are crew tanks, won by beating them.

## Open questions

- **Ramming and ejecting into enemies:** in or out? The feature list's row includes them, but the brainstorm only mentions ejecting.
- **Crouching specials:** stick down + SP calls a tank in. Once a tank is ready, can you still use your special while crouching?
- **Rounds and wrecks:** do a tank's rounds ever refill? Is a wrecked tank gone for the rest of the mission?
- **Co-op:** can both players have a tank out at once?
- **Each tank's boss attacks** after kie's, worked out from its crewmate's kit.
- **Replays:** does the pilot round play every time a mission is replayed?
