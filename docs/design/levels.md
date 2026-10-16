# Maps, missions and progression

What a mission is, how its map is built and what's in it, and the order in which the game opens up: missions, crew, co-op and menus.

**Status:** signed off by the owner, 2026-10-14. Revised 2026-10-15: craters can be dropped if they're very hard.

**Read this when** you're designing a mission or a map, or changing what unlocks when.

Up: [Design](README.md)

## Missions

- **Handmade levels, one for each crewmate to free.** There's one mission to start, and kcaj's comes next. The rest arrive one crewmate at a time.
- **Every mission has the same shape:** a run through the map, then the boss arena. The crewmate's tabk comes first, then its Trollogram pilot ([tabks](tanks.md#tabk-bosses)). Freeing the crewmate ends the fight. Mission one then has a short coda, a tabk run out of the base ([the coda](story.md#the-coda)), and the debrief follows.
- **A checkpoint at the boss door.** Lose the boss fight and you restart at the arena, not at the start of the run.
- **Replays:** you can play any mission you've cleared again, for score, XP, rank and stats. Your best score and your clears are kept for each mission.

## Mission one: the military base

- **Metal Slug played straight:** sandbags and watchtowers, ending at the motor pool, where the stolen tabks are parked.
- **The look: a steel base at night** (picked 2026-10-15). Snow on concrete and steel, hazard stripes, searchlights, live fences and steam vents, with the motor pool's blast shutter as the boss door. The cold palette makes the Trollogram goons and the fire stand out. The concept sheets are in the project's files, under concepts/m1-map.
- **About five minutes long.**
- **Single player only** (owner, 2026-10-16), as tones2, straight in with no tutorial. It's the game's prelude: beating it opens the lobby and co-op, so it is never played with two, replays included. It stays on the den's mission board, but picking it forces single player: player two's seat closes and nobody can watch (owner, 2026-10-16).
- **Goons:** riflemen (crouch under their shots), knife runners and bomb-dropping drones, all as flickering Trollograms.
- **The boss** is kie's tabk, then Trollogram kie ([tabks](tanks.md#tabk-bosses)).
- **Two reveals:** the first tabk, then your own. Freed, kie frees tones2's tabk and bows out, and in the coda, a short explosive mini-level after the boss, you call it in and drive it out of the base ([the coda](story.md#the-coda), owner 2026-10-16).
- **A checkpoint at the coda's start,** after the boss: lose there and you restart the coda, not the fight.

## Difficulty

- **Missions don't change with two players.** The same mission is simply easier with two.
- **The late missions get too hard for one player,** so beating them needs co-op.
- **Every cleared mission from Mission two on gets a hard setting,** tuned for two players, which also needs co-op. Mission one, solo only, has none (owner, 2026-10-16).

## Building a map

- **Side-scrolling, left to right.** The screen locks for the boss arena.
- **Readable on a phone:** clear silhouettes, an obvious path, and foreground scenery that adds depth but never hides a threat.
- **What maps contain:**
  - **Secrets:** hidden areas and items.
  - **Breakables:** crates that drop items, and barrels that explode.
  - **Hazards:** pits, spikes, fire and falling debris.
  - **Moving platforms and lifts.**
  - **Mounted guns** that a soldier can man.
  - **Supply crates** (a Should). In co-op, whoever grabs one first gets it.
- **The ground is solid,** except in boss arenas, where blasts dig craters. If craters prove very hard to build, arenas are solid too.
- **Goons come in hordes.** Later missions add shield soldiers, grenadiers, paratroopers, troop trucks and snipers (all Shoulds), a few new kinds at a time.
- **Helicopters can appear in any mission. Enemy tabks only come after Mission one,** so the crew's tabks stay a surprise.
- **In co-op,** the camera keeps both players on screen.

## The order things open up

1. **First launch:** the title screen offers Start, which begins Mission one, plus settings and a small sign-in button. Returning players on a new device can sign in straight away and keep their progress.
2. **Mission one,** single player only, the prelude. The first tabk, the first call-in and the first drive happen here.
3. **Once Mission one has been beaten:**
   - **Co-op appears** in the menu: host a room, join with a code, quick match, browse rooms, or watch a game.
   - **The mission screen** appears, with replays and kcaj's mission.
   - **The crew screen** appears, with tones2 and kie: it's the gaming den ([the lobby](lobby.md)).
   - **The debrief** shows XP and rank for the first time, and offers sign-in to players who haven't.
4. **Each later mission** frees another crewmate, adds their tabk to call in, and opens its map for replays and its hard setting.
5. **The Roguelite mode** is on ice. It's kept in [DESIGN.md](../../DESIGN.md#roguelite-on-ice) for when it returns.

## What the player sees, and when

The game shows only what the player has already met. Nothing on the title screen, in the menus or on the web page hints at tabks, co-op or online play before their moment. The menu grows with the player:

| | First launch | After Mission one | Later |
|---|---|---|---|
| Title menu | Start, plus settings and a small sign-in button | Missions, Co-op, Crew | |
| Online | Hidden | Host, join, quick match, browse, watch | |
| Crew | tones2, with no crew screen | The crew screen: tones2 and kie | One more per mission |
| Tabks | Never mentioned | tones2's tabk, and the call-in | One more per mission |
| Missions | Mission one | Replays, hard settings, the next mission | One more per crewmate |
| Progress | High score | XP, rank, looks, leaderboard | Hidden ranks appear as you reach them |

**Invite links respect the order.** Suppose a friend who hasn't beaten Mission one opens your invite link. The game tells them co-op opens after Mission one, and starts it for them, so nothing ahead of them is spoiled.

## Online, once it's open

- **Rooms:** join with a code or an invite link, use quick match, or browse open rooms.
- **A lobby,** where both players pick their soldiers and ready up: the crew's gaming den ([the lobby](lobby.md)).
- **Spectators** can watch a game, but only of missions they've already cleared, so nothing is spoiled.
- **Invites:** a button copies the room's link to share.

## Leaderboards

- **A friends board for each mission,** showing best scores to signed-in players.
- **It runs on trust.** Scores come from players' own phones, so the board is for friends, not the public.

## Open questions

- **Mission order after kcaj,** and each mission's setting.
- **How hard is hard:** how far the late missions and the hard setting go beyond what one player can do.
- **Solo players and co-op-only content:** how a solo player finds a partner when a late mission needs two. Quick match is one way.
- **The Roguelite's return:** when it comes back, plus its own open points:
  - shop prices
  - how two players agree at the shop
  - whether runs free crew
  - how often you can call in a tabk
