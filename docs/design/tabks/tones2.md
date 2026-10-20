# tones2's tabk

The first tabk you win: its guns, paint and name.

**Read this when** you're changing tones2's tabk.

Up: [Tabks](../tanks.md)

The first tabk you win, freed by kie at the end of Mission one and called in for the first time in [the coda](../story.md#the-coda). Approved by the owner, 2026-10-16.

- **Its own guns** (owner, 2026-10-16): each crewmate's tabk fires tabk-sized versions of their kit, as in Pooket Tabks, where the tank is the kit. Rounds stay 5, 3 and 1, never refilled, and the controls stay the same ([driving a tabk](driving.md#driving-a-tabk)).
  - **ten-1, the water cannon** (FIRE, 5 rounds; owner, 2026-10-16): one round is one spray cycle, as in Pooket Tabks. Press FIRE and the pressure builds in spurts from a wide, weak spray to a clean, hard arc, holds there for a moment, then eases off (about three seconds in all), and you aim it as it goes. The full-pressure arc does the real damage and shoves goons back a long way. Off a wall point-blank it splashes back, harmless, as on foot.
  - **ten-2, the mud jump** (SP, 3 rounds; owner, 2026-10-16: as true to the Pooket Tabks tank as it can be): the tabk shakes and rumbles as it builds up (about a second, cut down from Tabks' 10 s turn), then blasts off along the aim like a jetpack, flies and comes down somewhere new. The exhaust is a huge blast of toxic mud that carpets the ground around the launch and poisons goons walking through it. It aims the upper five ways (a flat aim launches long and low, a downward one straight up), burns no fuel, and landing on goons squashes them. In the coda it's how you get over the sandbag nest and leave the goons behind it in the mud.
  - **ten-3, the spew** (SU, 1 round; owner, 2026-10-16): tabk-sized, and twice as long as on foot: three seconds of spew along the aim (the soldier's is 1.5 s, `Tones2Tuning`), melting anything close and leaving a long trail of sludge. He can drive while it pours.
- **Paint** (owner, 2026-10-16): orange and black, with a pumpkin-head stencil on the hull.
- **Its name** on screen while you drive it: TONES2'S TABK, over its rounds (WATER 5, MUD 3, SPEW 1).
- **The rest is shared** with every tabk: health, fuel, speed, the hop, squashing goons, eject and the runaway.
- **One player:** Mission one is single player only (owner, 2026-10-16), so the coda never has two tabks. In Mission two each player calls in the tabk loaded in their own bay, tones2's or kie's.
- **The cargo drone** that lowers it is the crew's own, in tones2's orange and black, and every call-in uses it ([freeing kie](../cutscenes/mission-one.md#freeing-kie)). For now it's the goons' drone repainted, scaled up.

**Built** (2026-10-16): your tabk is tones2's now, wherever one appears (`?test=tank`, `?unlock=tank`, and the coda); the shell, mortar and barrage are retired (weapons 18 and 19 and shots 9 and 10 stay reserved). The hull is the tabk's shape painted orange with black tracks and gun, a jack-o'-lantern face on its flank. FIRE runs one spray cycle per round (1 s building in four spurts, 0.8 s held, 1 s easing off), SP shakes for 1 s and launches at 21 along the aim with a mud carpet 6.4 wide, SU pours 3 s of spew. In co-op a partner sees the tabk and its mud, not its water or spew (Mission one is single player). Code: `PlayerTank.cs`, numbers `Tones2TabkSpec` (`Core/Crew/Tones2TabkSpec.cs`, as `Tones2Tuning.Tabk`), art `Tones2Art.TabkHull`, `TabkBarrel`, `TabkDrone`.
