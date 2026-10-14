# Player mechanics

How a soldier plays: the crew, the controls, the kit, health and lives, crates, status effects and co-op, plus what the player keeps between missions.

**Status:** draft for sign-off, 2026-10-14.

**Read this when** you're working on anything the player controls or earns.

Up: [Design](README.md)

## The crew

- **There are eight soldiers,** all from Pooket Tabks. You start with tones2 alone and free one more at the end of each mission, kie first.
- **Each one plays differently,** in how they move as well as how they fight:

| Soldier | Plays like |
|---|---|
| tones2 | A water jet that builds pressure, a rocket jump that leaves toxic mud, and a spew that melts anything close |
| kie | Weasels that hunt goons down, a decoy hologram, and a roulette that steals power-ups |
| kcaj | Twin cones, a beam that locks on and burns, and a screen full of pills |
| torikloud | Words fired as bullet bursts, sound waves through walls, and a twin who copies him. He has the most health |
| ciarra | The best jumper, with a needle gun that marks targets, stitches that pin them, and an unstoppable sprint |
| larinovsky | Lobbed pills, a homing heart, hits turned into ammo, and a nap back to full health |
| garyoldmancorp | The fastest on foot, with a wheel of chance that can freeze every enemy. In beta: his real moves are designed in Tabks first |
| kiwicore | A cap thrown like a boomerang, a drum solo that heals, and a stage dive across the goons |

- **Full kits** are in [DESIGN.md](../../DESIGN.md#the-crew).
- **Four more soldiers** (shotdownboyz, lankcity, odsey and doctorfox) come later, each with a mission.

## Controls

- **Phones come first,** with an arcade pad:
  - **Left thumb:** a stick to run, aim up, and crouch (or aim down while in the air).
  - **Right thumb:** FIRE (hold to keep firing), JUMP, special (SP), super (SU), and an ability (AB) for the soldiers who have one.
- **A keyboard works too.** Gamepads are a Could.
- **Moving:** run, jump (hold for higher), crouch, and drop through thin floors. Getting hit knocks you back a little.
- **Aiming:** up, down while in the air, and diagonals.
- **Big hits land with weight:** a brief pause, a flash and sparks.

## The kit

Each soldier has:
- a **primary**, with unlimited ammo
- a **special**, with 3 charges that refill over time
- a **super**, charged by kills
- sometimes an **ability**, on a cooldown (larinovsky, garyoldmancorp and kiwicore)

**The classic Metal Slug weapon styles live in the kits.** The crew's own weapons cover them (beams, cones, lobbed shots, homing shots and more), so there are no separate classic guns.

## Health and lives

- **Hearts:** 100 health, shown as five hearts, with damage numbers floating off you.
- **Lives and continues:** each player has their own.
- **1-ups:** in co-op, a 1-up goes first to a partner who's out, so a survivor can bring them back.

## Crates and power-ups

Pickups last for the mission, so every mission starts level.
- **Upgrade crates** raise your primary, special or super a level (I → II → III). At level II you choose one of two forks.
- **Status mods** make your shots put a status effect on what they hit.
- **Gun crates** give you a crewmate's gun. It replaces your primary until it runs dry.
- **Medkits** heal hearts. **Bomb crates** refill specials.
- **Drops:** goons and containers can drop crates.

## Status effects

Statuses come from the kits, and tank bosses use them on you too:

| Status | Effect |
|---|---|
| Burn | Damage over time |
| Cooked | Deals half damage |
| Tattooed | Takes more damage from everything |
| Pinned | Can't move; a pinned flyer falls |
| Toxic | Ground that poisons whatever walks through it |

## Co-op

- **Two phones, online.** Co-op opens once Mission one has been beaten solo ([progression](levels.md#the-order-things-open-up)), and a partner can drop in mid-mission.
- **Lives and scores:** each player has their own, and their stats are kept separately.
- **Some kits help a partner:** kiwicore's drum solo heals them, ciarra's marks make their shots hit harder, and kcaj's pills never hurt them.
- **Friendly fire is off.**
- **Your partner is always easy to find:** an arrow when they're off-screen, and a warning when their health is low or they've switched away from the game.
- **Two players can pick the same soldier,** in different colours *(proposed)*.

## What the player keeps

- **The crew they've freed,** and whether co-op is open.
- **Career XP,** from clearing missions, kills and freeing crewmates. It only ever unlocks looks: hats, skins, tank paint, death animations, kill effects and victory poses.
- **Rank:** the ladder from Pooket Tabks. You climb it slowly by beating stages, and never drop down it. Eight ranks are known and the rest are hidden, and some hidden rungs need a secret feat.
- **Stats** for each soldier and weapon: shots, hits, accuracy, damage dealt and taken, and kills.
- **Best scores,** which also go on a friends leaderboard ([leaderboards](levels.md#leaderboards)).
- **Signing in with Google** keeps all of this across phones and computers, under a display name the player picks.

## Discovering the kit

The game shows only what the player has already met *(proposed details)*:
- **Mission one starts with move, jump, fire and the special.** Each button is labelled, and an ⓘ screen covers the kit, as in Tabks. There's no tutorial.
- **The super button lights up the first time it's charged,** with a one-off prompt.
- **The ability button only appears for soldiers who have one.**
- **The crew screen appears once there's a second soldier to pick.**
- **Weapon forks appear at level II.** Status mods appear when you first find one.
- **XP, rank and looks appear at the first debrief.**

## Not planned

These are all Won't:
- more than two players, couch co-op, and bot partners
- versus modes
- wall jumps, slides and swimming

## Open questions

- **Team moves** (a Must): combined attacks, or a shared super bomb? The brainstorm only has kits that help a partner.
- **A point-blank knife** (a Should): does every soldier get one, or only some?
- **Career XP:** how much does each thing earn? And how does it pay out: tokens to spend, a fixed track, a track per soldier, or mystery crates?
- **Rank:** what counts as a stage, how many stages make a rung, and do replays count?
- **The secret feats:** all 25 still need designing.
- **Kits still to design:** the coming-soon crew's, and garyoldmancorp's real moves.
- **Timings** for the weapons that charge or build up, to be tuned in play.
