# Tanks

How the player discovers tanks, fights them as bosses, wins them, drives them and calls them in.

**Status:** signed off by the owner, 2026-10-14. Revised 2026-10-15: boss tanks and the tanks you drive are separate things.

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
- **Then comes the pilot round.** Wreck the tank and its hologram pilot jumps out to fight you on foot with the crewmate's kit. It's a mirror match, and a taste of who you're about to free. Beat the pilot and it glitches out, and the real crewmate is freed.
- **Replays keep the pilot round.** Only the freeing scene is skipped.
- **Bosses fight in arenas,** the only places where blasts dig craters in the ground.
- **kie's tank comes first.** It cycles through three attacks, all from kie's kit:
  - waves of weasels that run at you and pop
  - hologram copies of itself, where only the real one can be hurt
  - a roulette that steals one of your upgrades until you hit it hard enough to drop it
- **kcaj's tank is next,** in Mission two. It sweeps a beam, then rains pills across the screen.
- **Later tanks** take their attacks from their own crewmates' kits in the same way.

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
