# Maps, missions and progression

What a mission is, how its map is built and what's in it, and the order in which the game opens up: missions, crew, co-op and menus.

**Status:** signed off by the owner, 2026-10-14. Revised 2026-10-15: craters can be dropped if they're very hard.

**Read this when** you're designing a mission or a map, or changing what unlocks when.

Up: [Design](README.md)

## Missions

- **Handmade levels, one for each crewmate to free.** There's one mission to start, and kcaj's comes next. The rest arrive one crewmate at a time.
- **Every mission has the same shape:** a run through the map, then the boss arena. The crewmate's tabk comes first, then its Trollogram pilot ([tabks](tabks/bosses.md#tabk-bosses)). Freeing the crewmate ends the fight. Mission one then has a short coda, a tabk run out of the base ([the coda](story.md#the-coda)), and the debrief follows.
- **A checkpoint at the boss door.** Lose the boss fight and you restart at the arena, not at the start of the run.
- **Replays:** you can play any mission you've cleared again, for score, XP, rank and stats. Your best score and your clears are kept for each mission.
- **The mission screen** opens from the den's mission board (the title has only START and settings): a card for each mission reached (its number, where it's set, best score and clears, and SOLO ONLY on Mission one), then the next mission's card, COMING SOON until it's built. A mission is reached once the one before it is cleared, so nothing further on shows. From the board, PICK puts the mission on it. A card's hard setting shows once it's open (a cleared mission from Mission two on, in co-op). **Built** 2026-10-16 (`Core/Missions/MissionBoard.cs`, `MenuView.RefreshMissions`); not yet: the host's pick in a co-op room, so hard can't be reached until then.

## Mission one: the military base

- **Metal Slug played straight:** sandbags and watchtowers, ending at the motor pool, where the stolen tabks are parked.
- **The look: a steel base at night** (picked 2026-10-15). Snow on concrete and steel, hazard stripes, searchlights, live fences and steam vents, with the motor pool's blast shutter as the boss door. The cold palette makes the Trollogram goons and the fire stand out. The concept sheets are in the project's files, under concepts/m1-map.
- **About five minutes long.**
- **Single player only** (owner, 2026-10-16), as tones2, straight in with no tutorial. It's the game's prelude: beating it opens the lobby and co-op, so it is never played with two, replays included. It stays on the den's mission board, but picking it forces single player: player two's seat closes and nobody can watch (owner, 2026-10-16).
- **Co-op plays Mission two,** the first mission open to it (`Missions.ForCoop`); Mission one is never played with two.
- **Goons:** riflemen (crouch under their shots), knife runners and bomb-dropping drones, all as flickering Trollograms.
- **The boss** is kie's tabk, then Trollogram kie ([tabks](tabks/bosses.md#tabk-bosses)).
- **Two reveals:** the first tabk, then your own. Freed, kie frees tones2's tabk and bows out, and in the coda, a short explosive mini-level after the boss, you call it in and drive it out of the base ([the coda](story.md#the-coda), owner 2026-10-16).
- **A checkpoint at the coda's start,** after the boss: lose there and you restart the coda, not the fight.

## Mission two: the Brain Freeze creamery

kcaj's mission (picked by the owner, 2026-10-16, from three concepts; the sheets are in the project's files, under concepts/m2-setting).
- **An ice cream factory by day,** run left to right along the production line, ending at the cold store, where kcaj's tabk guards the door.
- **The look:** warm pastels after Mission one's cold night. Waffle-cone floors topped with pink icing and sprinkles, lilac factory walls, ice cream tubs for cover and a giant soft-serve sign as the landmark. The Trollogram goons' cyan stands out against it.
- **Hazards:** conveyor belts that carry you and the goons, nitrogen vents that freeze whoever walks through them for a moment, and scoop droppers overhead.
- **Breakables:** boxes of cones that drop items, and nitrogen tanks that explode.
- **The boss arena** is the loading yard at the end of the line (picked by the owner, 2026-10-18; m2-arena: https://claude.ai/artifact/RpDJ8cBynkza3HrdPsqJLA): outside by day under the run's pink sky, the factory's lilac wall with pink icing dripping down it and the loading bay on the left, the soft-serve sign on its pole in the middle, and the cold store on the right, a huge frosted block with icicles, its double doors at the screen's right edge and fog piling out under them. One locked screen, entered from the left out of the freezer corridor, with a flat waffle-cone floor and nothing standing on it. The plate is `concepts/m2-arena/arena-a1.png` in the project's files (320 × 180, 16 colours, the floor's top 32 px up, the doors 92 px tall), with `arena-a1-raw.png` at full size; the other takes are beside it.
- **The way in and the backdrop** (picked by the owner, 2026-10-18; m2-arena-concept, same page): the fogged freezer corridor (`concepts/m2-arena/corridor-2.png`: frosted lilac walls, rows of tubs, cold ceiling lights, fog on a waffle floor) ends at the loading bay's door, and the run steps out of it into the yard. Behind the whole run, the far layer is the creamery's skyline (`far-2.png`: chimneys puffing pink smoke, milk tanks and the soft-serve sign on a lilac roofline). The fight in the yard: the tabk parks 32 px from the right edge, its beam sweeps the floor from the barrel, its pills rain over the whole screen, and fog lies along the floor from the doors (`fight-a1.png`).
- **The boss door** is the cold store: it fogs open and the tabk rolls out of the mist. The boss is kcaj's tabk, then Trollogram kcaj ([tabks](tabks/bosses.md#tabk-bosses)).
- **Story beats on the map:** Trollogram projectors behind frosted glass (shoot one and the goons near it flicker out), a goon frozen solid riding a conveyor, and a fogged freezer corridor before the cold store. The tabk wears a cone on its antenna.
- **The story side** (why the Trollograms are here, the briefing and the freeing): [Mission two: the creamery](story.md#mission-two-the-creamery).
- **Built so far** (m2-map-build, 2026-10-18; `Missions.Two`, `Core/Missions/Creamery.cs`, `Missions/Two/`): the six sections as a text map (210 tiles), with Mission one's goons; belts ('>' '<') that carry soldiers and goons; scoop droppers that shake and drop a scoop (a heart); nitrogen vents that freeze a soldier for about a second and a goon for longer, with a grace after so you can walk out; boxes of cones that drop a medkit or bombs; nitrogen tanks that freeze everyone near them as they blow; and the cold store's doors, fog spilling out, where kcaj's tabk rolls out. Stand-ins until Design's art lands: Mission one's tiles and night backdrop, and the creamery's pieces drawn in code. Then (m2-goons-build): shield soldiers, who stop shots from the front and are slow to turn (blasts, mud and weasels get under the shield; tones2's jet still pushes them back); grenadiers, who lob from cover where you're standing after a wind-up; and the Big Scoop in the sign yard, who holds the yard until he's down, with a telegraphed slam (a shockwave each way along the floor), hurl and charge, and takes double while a nitrogen tank has him frozen. Then (m2-polish): fog along the freezer corridor's floor; two Trollogram projectors behind frosted glass, one between the belts and one under the hall's drones (shoot one and the goons within eight tiles of it flicker out, and no more come near it; the Big Scoop is too big for it); and a lift in the nitrogen hall up to a catwalk with a secret 1-up. Then (m2-finish, 19 Oct): Design's creamery plates (the factory outside behind the loading bay and the belts, the frozen corridor, and loading-yard arena A1 with its painted cold store doors over the code ones), stood on the floor's top; Trollogram kcaj's pilot round (his cones, his beam, which sticks to you and burns while he stands still until you hurt him enough to snap him out of it, and his pill rain), with his TROLLO-KCAJ intro at the end of the first boss intro; and freeing kcaj, on both phones in co-op. Still drawn in code: Mission one's tiles, and the creamery's pieces.

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
   - **Co-op appears** in the den: it joins the one co-op room, to play or to watch.
   - **The mission screen** appears, with replays and kcaj's mission.
   - **The crew screen** appears, with tones2 and kie: it's the gaming den ([the lobby](lobby.md)).
   - **The debrief** shows XP and rank for the first time, and offers sign-in to players who haven't.
4. **Each later mission** frees another crewmate, adds their tabk to call in, and opens its map for replays and its hard setting.
5. **The Roguelite mode** is on ice. It's kept in [history](history.md#the-roguelite-on-ice) for when it returns.

## What the player sees, and when

The game shows only what the player has already met. Nothing on the title screen, in the menus or on the web page hints at tabks, co-op or online play before their moment. The menu grows with the player:

| | First launch | After Mission one | Later |
|---|---|---|---|
| Title menu | Start, plus settings and a small sign-in button | The same: Start opens the den, which has the missions, the crew and co-op | |
| Online | Hidden | The one co-op room: play, or watch | |
| Crew | tones2, with no crew screen | The crew screen: tones2 and kie | One more per mission |
| Tabks | Never mentioned | tones2's tabk, and the call-in | One more per mission |
| Missions | Mission one | Replays, the next mission | One more per crewmate, and hard settings from Mission two on |
| Progress | High score | XP, rank, looks, leaderboard | Hidden ranks appear as you reach them |

**Invite links respect the order.** Suppose a friend who hasn't beaten Mission one opens your invite link. The game tells them co-op opens after Mission one, and starts it for them, so nothing ahead of them is spoiled.

## Online, once it's open

- **One room:** everyone's phone joins the same always-there co-op room (owner, 2026-10-18: we play with mates, so there's nothing to host, browse or type). CO-OP on the title, or the den's spare controller, joins it: a free seat plays, and a full room is watched.
- **A lobby,** where both players pick their soldiers and ready up: the crew's gaming den ([the lobby](lobby.md)).
- **Spectators** can watch a game, but only of missions they've already cleared, so nothing is spoiled.
- **Invites:** tapping the den's TV shares the game's link, which brings a mate into the room.

## Leaderboards

- **A friends board for each mission,** showing best scores to signed-in players.
- **It runs on trust.** Scores come from players' own phones, so the board is for friends, not the public.

## Open questions

- **Mission order after kcaj,** and each later mission's setting (Mission two's is the creamery).
- **How hard is hard:** how far the late missions and the hard setting go beyond what one player can do.
- **Solo players and co-op-only content:** how a solo player finds a partner when a late mission needs two. Quick match is one way.
- **The Roguelite's return:** when it comes back; its own open points are in [history](history.md).
