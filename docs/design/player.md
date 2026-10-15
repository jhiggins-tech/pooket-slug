# Player mechanics

How a soldier plays: the crew, the controls, the kit, health and lives, crates, status effects and co-op, plus what the player keeps between missions.

**Status:** signed off by the owner, 2026-10-14, after one revision: the secret feats and status mods are gone, and the kit's tiers are spelled out. Revised 2026-10-15: tones2's kit as spiked, and the team super is a combo for each pair.

**Read this when** you're working on anything the player controls or earns.

Up: [Design](README.md)

## The crew

- **There are eight soldiers,** all from Pooket Tabks. You start with tones2 alone and free one more at the end of each mission: kie first, then kcaj.
- **Each one plays differently,** in how they move as well as how they fight:

| Soldier | Plays like |
|---|---|
| tones2 | A water jet that builds pressure (point-blank splashback slows him), a straight-up rocket jump that leaves toxic mud, and a spew he can walk with that leaves long-lasting sludge |
| kie | Weasels that hunt goons down, a decoy hologram, and a roulette that steals power-ups |
| kcaj | Twin cones, a beam that locks on and burns, and a screen full of pills |
| torikloud | Words fired as bullet bursts, sound waves through walls, and a twin who copies him. He has the most health |
| ciarra | The best jumper, with a needle gun that marks targets, stitches that pin them, and an unstoppable sprint |
| larinovsky | Lobbed pills, a homing heart, hits turned into ammo, and a nap back to full health |
| garyoldmancorp | The fastest on foot, with a wheel of chance that can freeze every enemy. In beta: his real moves are designed in Tabks first |
| kiwicore | A cap thrown like a boomerang, a drum solo that heals, and a stage dive across the goons |

- **Full kits** are in [the crew's kits](kits.md).
- **Four more soldiers** (shotdownboyz, lankcity, odsey and doctorfox) come later, each with a mission.

## Controls

- **Phones come first,** with an arcade pad:
  - **Left thumb:** a stick to run, aim up, and crouch (or aim down while in the air).
  - **Right thumb:** FIRE (hold to keep firing), JUMP, special (SP), super (SU), and an ability (AB) for the soldiers who have one.
  - **Once you've won a tank,** holding the stick down and holding SP calls it in ([tanks](tanks.md#calling-in-a-tank)).
- **A keyboard works too.** Gamepads are a Could.
- **Moving:** run, jump (hold for higher), crouch, and drop through thin floors. Getting hit knocks you back a little.
- **Aiming:** up, down while in the air, and diagonals.
- **Big hits land with weight:** a brief pause, a flash and sparks.

## The kit

Each soldier's kit comes from their Pooket Tabks tiers:
- **Tier 1 is the primary,** with unlimited ammo.
- **Tier 2 is the special,** with 3 charges that refill over time.
- **Tier 3 is the super,** charged by kills.
- **The bonus move is the ability,** on a cooldown. Only larinovsky, garyoldmancorp and kiwicore have one.

**Some soldiers have a melee attack.** For them, FIRE becomes a melee hit when a goon is right next to them. It's part of their kit, not every soldier's.

**The classic Metal Slug weapon styles live in the kits.** The crew's own weapons cover them (beams, cones, lobbed shots, homing shots and more), so there are no separate classic guns.

## Health and lives

- **Hearts:** 100 health, shown as five hearts, with damage numbers floating off you.
- **Lives and continues:** each player has their own.
- **1-ups:** in co-op, a 1-up goes first to a partner who's out, so a survivor can bring them back.

## Crates and power-ups

Pickups last for the mission, so every mission starts level.
- **Upgrade crates** raise your primary, special or super a level (I → II → III). At level II you choose one of two forks.
- **Gun crates** give you a crewmate's gun. It replaces your primary until it runs dry.
- **Medkits** heal hearts. **Bomb crates** add charges to the special, with no cap. How many depends on the soldier, to keep the strong specials in check: about +3 for a strong special, up to +10 for a weak one.
- **Drops:** goons and containers can drop crates.

## Status effects

Statuses come only from the soldiers' kits: each one belongs to a particular soldier's weapons, and no crate adds them. Tank bosses use their crewmate's statuses on you too:

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
- **The team super:** when both players' supers are charged, they can fire them together for a screen-clearing team super. Each pair of soldiers gets its own combo, designed first as a table of ideas, before any code.
- **Friendly fire is off.**
- **Your partner is always easy to find:** an arrow when they're off-screen, and a warning when their health is low or they've switched away from the game.

## What the player keeps

- **The crew they've freed,** and whether co-op is open.
- **Career XP,** from clearing missions, kills and freeing crewmates. Each level gives tokens to spend on looks, as in Tabks: hats, skins, tank paint, death animations, kill effects and victory poses. XP never unlocks anything else.
  - **Placeholder amounts, on Tabks' scale, to tune after playtesting:** +200 for each mission clear, +300 for each crewmate freed, and +1 for each kill. Every 600 XP earns a looks token.
- **Rank:** the ladder from Pooket Tabks. Each stretch of a mission you beat moves you a little way up it, replays included, and you never drop down. Eight ranks are known, and the other 25 stay hidden until you reach them.
- **Stats** for each soldier and weapon: shots, hits, accuracy, damage dealt and taken, and kills.
- **Best scores,** which also go on a friends leaderboard ([leaderboards](levels.md#leaderboards)).
- **Signing in with Google** keeps all of this across phones and computers, under a display name the player picks ([when it's offered](levels.md#the-order-things-open-up)).

## Discovering the kit

The game shows only what the player has already met:
- **Mission one starts with move, jump, fire and the special.** Each button is labelled, and an ⓘ screen covers the kit, as in Tabks. There's no tutorial.
- **The super button lights up the first time it's charged,** with a one-off prompt.
- **The ability button only appears for soldiers who have one.**
- **The crew screen appears once there's a second soldier to pick.**
- **Weapon forks appear at level II.**
- **XP, rank and looks appear at the first debrief.**

## Not planned

These are all Won't:
- more than two players, couch co-op, and bot partners
- versus modes
- wall jumps, slides and swimming

## Open questions

- **The same soldier twice:** can both players in co-op pick the same soldier?
- **Melee:** which soldiers get it.
- **The team super:** each pair's combo, from the table of ideas.
- **Career XP:** tuning the placeholder amounts after playtesting, and what looks cost in tokens.
- **Rank:** where each mission splits into stages, and how many stages make a rung.
- **Kits still to design:** the coming-soon crew's, and garyoldmancorp's real moves.
- **Timings** for the weapons that charge or build up, to be tuned in play.
