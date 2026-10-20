# Tabks

How the player discovers tabks, fights them as bosses, wins them, drives them and calls them in.

**Status:** signed off by the owner, 2026-10-14. Revised 2026-10-15: boss tabks and the tabks you drive are separate things; kie's roulette steals your special; craters can be dropped if they're very hard.

**Read this when** you need tabks' overall shape; for a boss, driving or one crewmate's tabk, read only its page under [Where it is](#where-it-is).

Up: [Design](README.md)

## The idea

Every crewmate owns a tabk, and the Trollograms have stolen them all. Each one is a boss first, then yours to ride. On foot the game is fast. A boss tabk is slow, heavy artillery, and its arrival is a big moment. Once it's yours, it drives like a Metal Slug tabk.

## Two kinds of tabk

A crewmate's tabk shows up in two forms, and they're separate things in the game, built separately:
- **The boss tabk** is the one a Trollogram drives at the end of a mission. It fights like a Pocket Tanks tabk: it swings its turret, charges up its shots and lobs them across the arena ([tabk bosses](tabks/bosses.md#tabk-bosses)).
- **Your tabk** is the one you drive after winning it, or call in during a mission. It moves and aims Metal Slug style, quick and direct, like a soldier on foot ([driving a tabk](tabks/driving.md#driving-a-tabk)).

They share a crewmate's look and paint, and nothing else needs to carry over.

## Discovering tabks

Tabks stay hidden until they matter ([progressive disclosure](README.md#progressive-disclosure-the-thread-through-all-four)):
1. **Before the first tabk,** nothing mentions tabks: not the menus, the HUD, the loading screens, the controls or the web page around the game. No enemy tabks appear before then either. The one exception is [the opening](cutscenes/mission-one.md#the-opening), as foreshadowing: tones2 cries out for the crew's tabks and finds their bay empty, but none is shown (owner, 2026-10-19).
2. **The first tabk** is kie's, at the end of Mission one, with a full [boss intro](cutscenes/mission-one.md#the-boss-intro).
3. **The first call-in and the first drive** come straight after kie is freed (owner, 2026-10-16): kie frees tones2's own tabk and bows out, and in Mission one's coda, a short mini-level after the boss, a prompt has you call it in, then drive it out of the base ([the coda](story.md#the-coda)).
4. **kie's tabk** is wrecked in the boss fight, so the coda is driven in tones2's. kie rebuilds his straight after Mission one (owner, 2026-10-20; it was after Mission two), as [THE PROJECTIONIST](tabks/kie.md), and it waits in the tabk bay for whoever goes looking ([kie's tabk comes home](cutscenes/den.md#kies-tabk-comes-home)).
5. **The tabk choice** is made in the den's tabk bay, which opens with the den after Mission one with two tabks to pick from, tones2's and kie's: the one on the repair stands is the one you call in ([the tabk bay](lobby.md#the-tabk-bay)). Nothing points you there: players who explore find it and the swap (owner, 2026-10-20). kcaj's joins them after Mission two.

## Where it is

| Page | What's in it |
|---|---|
| [Tabk bosses](tabks/bosses.md) | A crewmate's tabk as the boss at a mission's end: how it fights, its boss intro, and the pilot round after it. Sections: Tabk bosses, The boss intro, The pilot round. |
| [Driving and calling in a tabk](tabks/driving.md) | The tabk you've won: climbing in, its controls, rounds, fuel and health, and calling it in during a mission. Sections: Driving a tabk, Calling in a tabk. |
| [tones2's tabk](tabks/tones2.md) | The first tabk you win: its guns, paint and name. |
| [kie's tabk](tabks/kie.md) | THE PROJECTIONIST, kie's own tabk: its theme, paint, guns, name and sprite. |
| [kcaj's tabk](tabks/kcaj.md) | SOFT SERVE, kcaj's own tabk: its theme, paint, guns, name and sprite. |

## Not planned

- **Won't:** flying vehicles, submarines and animal mounts.
- **Could:** two-seat tabks, and riding on top of a partner's tabk.
- **The only vehicles you can take** are crew tabks, won by beating them.

## Open questions

- **Each tabk's boss attacks** after kcaj's, worked out from each crewmate's kit.
- **The charge-up time:** how long you hold SP before the tabk drops, to be tuned in play.
- **Whether your tabk jumps,** as Metal Slug's does.
