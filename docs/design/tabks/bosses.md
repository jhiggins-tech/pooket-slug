# Tabk bosses

A crewmate's tabk as the boss at a mission's end: how it fights, its boss intro, and the pilot round after it.

**Read this when** you're designing or building a boss tabk, its intro or its Trollogram pilot.

Up: [Tabks](../tanks.md)

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

A boss tabk's arrival, with its warning banner, name card and music, is a cutscene played in the game itself: its
script and rules (the quick second time, the pilot's intro, co-op, the music) are in
[cutscenes](../cutscenes/mission-one.md#the-boss-intro).

## The pilot round

Wreck the tabk and its Trollogram pilot jumps out to fight you on foot with the crewmate's own kit ([tabk bosses](#tabk-bosses), [kits](../kits.md)). Draft, 2026-10-15, waiting for the owner's read-through.

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

**The fight's size** (picked 2026-10-15): a boss duel. The pilot is much stronger than a soldier, with its own boss bar, so the duel lasts around 30 to 45 seconds. Its health is tuned in play to that length; the current number is in the code.

**In co-op** (owner, 2026-10-15): one pilot with exactly the same health and moves as in solo, since missions don't change with two players ([difficulty](../levels.md#difficulty)); the fight is simply easier with two. It switches targets between you: whoever is nearest, or whoever hurt it last.

**When it's beaten,** the pilot glitches out like the Trollogram goons, and the freeing scene plays ([freeing kie](../cutscenes/mission-one.md#freeing-kie)). The boss music keeps playing through the pilot round ([the boss intro](../cutscenes/mission-one.md#the-boss-intro)).

**The plan to build it:**
1. **Sides:** a soldier gets a side, players or Trollograms. Shots, blasts and kit effects already know whether they're hostile; the pilot's are hostile to players.
2. **The brain** (in `Core`, so it's tested without Unity): it reads what the pilot can see (its position, yours, shots coming, its meters) and writes the pilot's `SoldierInput` for the frame. Its tests drive it against scripted players.
3. **The playbook** for each soldier, starting with kie's.
4. **The Trollogram look** the goons use, on the pilot, and its glitch-out death.
5. **Co-op:** the world's authority runs the pilot, and the partner's game shows it like a remote player, using the soldier pose that's already sent for players.
6. **In the mission:** the pilot round starts when the tabk is wrecked, after the boss tabk build and the boss intro.

**What it depends on** (picked 2026-10-15): Mission one's pilot is kie, so kie's own kit is built for players first, and the pilot is built and tuned on it.

**Built** (2026-10-15): kie's pilot is in Mission one. Wreck the tabk and Trollogram kie jumps out; beat him and he glitches out and the mission's won (`?test=pilot` starts at the boss and wrecks the tabk as it wakes). The brain and kie's playbook are `Core/Bosses/PilotBrain.cs`, his numbers `KiePilotSpec` (`Core/Crew/KieTuning.cs`), and the fighter `Pilot.cs`. His health is 40 for now (`KiePilotSpec.Hp`): a soldier's 5 hearts take whole hits, while a pistol lands a point every 0.18 s, so his bar is counted in shots to make the 30 to 45 second duel. His kit is a little softer than a player's (two weasels a press, two Trollograms that refill), and his weasels are the tabk's. In co-op he goes for whoever is nearest (or your Trollogram, if it's up). Since Mission one became single player only (owner, 2026-10-16), kie's pilot only ever fights one player; the co-op code stays for later missions' pilots.
