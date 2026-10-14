# Maps, missions and progression

What a mission is, how its map is built and what's in it, and the order in which the game opens up: missions, crew, co-op and menus.

**Status:** draft for sign-off, 2026-10-14.

**Read this when** you're designing a mission or a map, or changing what unlocks when.

Up: [Design](README.md)

## Missions

- **Handmade levels, one for each crewmate to free.** There's one mission to start. The rest arrive one crewmate at a time.
- **Every mission has the same shape:** a run through the map, then the boss arena. The crewmate's tank comes first, then its hologram pilot ([tanks](tanks.md#tank-bosses)). Freeing the crewmate ends the mission, and the debrief follows.
- **Replays:** you can play any mission you've cleared again, for score, XP, rank and stats. Your best score and your clears are kept for each mission.

## Mission one: the military base

- **Metal Slug played straight:** sandbags and watchtowers, ending at the motor pool, where the stolen tanks are parked.
- **About five minutes long.**
- **Solo, as tones2,** straight in with no tutorial.
- **Goons:** riflemen (crouch under their shots), knife runners and bomb-dropping drones, all as flickering holograms.
- **The boss** is kie's tank, then hologram kie ([tanks](tanks.md#tank-bosses)).
- **Two reveals:** the first tank, then the first drive, out of the base in kie's tank ([discovering tanks](tanks.md#discovering-tanks)).

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
- **The ground is solid,** except in boss arenas, where blasts dig craters.
- **Goons come in hordes.** Later missions add shield soldiers, grenadiers, paratroopers, troop trucks, helicopters and snipers (all Shoulds), a few new kinds at a time.
- **In co-op,** the camera keeps both players on screen.

## The order things open up

1. **First launch:** the title screen offers Start, which begins Mission one, plus settings.
2. **Mission one,** played solo. The first tank and the first drive happen here.
3. **Once Mission one has been beaten solo:**
   - **Co-op appears** in the menu: host a room, join with a code, quick match, browse rooms, or watch a game.
   - **The mission screen** appears, with replays and the next mission.
   - **The crew screen** appears, with tones2 and kie.
   - **The debrief** shows XP and rank for the first time.
4. **Each later mission** frees another crewmate, adds their tank to call in, and opens its map for replays.
5. **In phase 2, the Roguelite returns:** seeded runs of five stages that loop at a rising alert level, with coins and a shop between stages.

## What the player sees, and when

The game shows only what the player has already met. Nothing on the title screen, in the menus or on the web page hints at tanks, co-op or online play before their moment. The menu grows with the player:

| | First launch | After Mission one | Later |
|---|---|---|---|
| Title menu | Start | Missions, Co-op, Crew | Roguelite (phase 2) |
| Online | Hidden | Host, join, quick match, browse, watch | |
| Crew | tones2, with no crew screen | The crew screen: tones2 and kie | One more per mission |
| Tanks | Never mentioned | kie's tank, and the call-in | One more per mission |
| Progress | High score | XP, rank, looks, leaderboard | Feats open hidden rungs |

**Invite links respect the order.** Suppose a friend who hasn't beaten Mission one opens your invite link. The game tells them co-op opens after Mission one, and starts it for them, so nothing ahead of them is spoiled.

## Online, once it's open

- **Rooms:** join with a code or an invite link, use quick match, or browse open rooms.
- **A lobby,** where both players pick their soldiers and ready up.
- **Spectators** can watch a game.
- **Invites:** a button copies the room's link to share.

## Leaderboards

- **A friends board for each mission,** showing best scores to signed-in players.
- **It runs on trust.** Scores come from players' own phones, so the board is for friends, not the public.

## Open questions

- **When sign-in is offered.** Either a small button from the first launch, so returning players on a new device keep their progress, or only from the first debrief, when there's progress to save.
- **Mission order** after kie, and each mission's setting.
- **What counts as a stage** for rank: a whole mission, or a stretch of one.
- **Checkpoints** (a Could): are they needed once missions run longer than five minutes?
- **Difficulty with two players:** more goons, tougher goons, or the same?
- **Enemy tanks and helicopters** (a Should): from which mission? Enemy tanks can't appear before Mission one's boss.
- **Spectating and spoilers.** Watching a friend's later mission shows tanks and crew the spectator hasn't met. Allow it, or only let players watch missions they've cleared?
- **The Roguelite's return:** when it comes back, plus its own open points:
  - shop prices
  - how two players agree at the shop
  - whether runs free crew
  - how often you can call in a tank
