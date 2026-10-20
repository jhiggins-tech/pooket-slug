# Driving and calling in a tabk

The tabk you've won: climbing in, its controls, rounds, fuel and health, and calling it in during a mission.

**Read this when** you're changing how any tabk you drive works, or the call-in.

Up: [Tabks](../tanks.md)

## Driving a tabk

- **Climb in** to a tabk you've won. It has its own health bar, separate from yours.
- **The controls are Metal Slug's:** the stick drives it and points the gun, the same eight ways a soldier aims on foot, and FIRE shoots at once. There's no turret to swing and no power to build: that's how boss tabks fight, not yours.
- **Rounds never refill:** a tabk has 5 main shots, 3 specials and 1 super.
- **Fuel runs out, as in Pooket Tabks** (owner, 2026-10-16). A tabk has a fuel bar that drains as it drives (and hops). When it's dry, the tabk can't move and stays where it is, but it can still aim and fire. Fuel belongs to each tabk: a tabk arrives full, nothing refuels it, and ejecting is the way out of a dry one (the empty tabk still rolls on and blows up, on its own momentum, Metal Slug style).
- **Eject before it blows.** The empty tabk rolls on and explodes into the goons, Metal Slug style.
- **A wrecked tabk is gone** until the next mission.
- **Any soldier can drive any tabk you've won,** so tones2 can take kcaj's tabk out.

**Built** (2026-10-16, on for everyone; the spike was 2026-10-15): your tabk is part of the game, solo and co-op. As with everything tabk, it shows only once a tabk's been won (progressive disclosure); to try it, `?test=tank` parks one beside each player at the boss checkpoint and gives one call-in, and `?unlock=tank` gives the call-in alone. Jump into it, or press JUMP touching it, to climb in. Inside, the stick drives it and points the gun the eight ways, keeping the last aim when let go; FIRE, SP and SU fire its three guns (5, 3 and 1, never refilled; since 2026-10-16 tones2's, [below](tones2.md)); JUMP hops it, and down and JUMP ejects. It has its own health bar (TK, under the kit's meters; 24, doubled from 12 on 2026-10-20 because tabks died too quickly) and a fuel bar (FU, under that: 18 seconds of driving, up from the spike's 12; a hop burns half a second; dry, it stays put but still aims and fires). Driving (or landing) on a goon on foot squashes it: soldiers, runners, drones and weasels, never barrels, tabks or the pilot; that's the payoff for the fuel. Every hit meant for the driver lands on the tabk. Ejected, it rolls on and blows up on the first goon or wall; wrecked, it throws you clear and blows up. In co-op each player's tabk runs on their own screen, like their soldier: its pose travels with the soldier's replicated state, a partner sees it drive, aim, fire and blow up, and enemy shots stop on it; squashed goons go as their hits. Code: `PlayerTank.cs`, `TankLook.cs` (the look, shared with a partner's copy in `RemotePlayer.cs`), numbers `PlayerTankSpec` and `TankPose` (`Core/Tabks/PlayerTankSpec.cs`).

## Calling in a tabk

- **Once per mission,** hold the stick down and hold SP, and a tabk you've won drops in beside you, with a full tabk of fuel. A tap of SP still fires your special, even while you're crouching.
- **The call charges up first.** Holding SP charges a short call-in before the tabk drops. Letting go before it's charged cancels the call.
- **Choose which tabk** in the den's tabk bay: the one on the repair stands is loaded ([the tabk bay](../lobby.md#the-tabk-bay)). The mission screen shows it with the mission.
- **In co-op, each player calls their own,** so both of you can have a tabk out at once.

**Built so far:** the call-in drops your real tabk ([driving a tabk](#driving-a-tabk)) for anyone who has one to call, solo and co-op; until tabks are won in play, only `?unlock=tank` or `?test=tank` give one. The first time, in the coda, a blinking prompt says HOLD DOWN + SP TO CALL YOUR TABK until you start a call (the `CallInHinted` event is where the game marks it seen; the `?spike=callin` that tried it first was retired on 2026-10-16). The call charges, a cargo drone (the goons' drone scaled up, in tones2's orange and black) lowers the tabk on a cable while a marker blinks where it will land, lets go as it lands, and climbs away. Code: `Core/Tabks/CallInCoach.cs`, `World.CallInCoach`, the prompt in `HudView`, the marker and drone in `PlayerTank`. Real players get it from Mission one's coda on (owner, 2026-10-16): the coda's HOLD DOWN + SP prompt is the hint's first showing, and every mission after starts with the call-in (built 2026-10-20: `MissionStory.CallIns`, one per mission, each player their own, so co-op phones start alike; it comes back when the mission restarts, not on a continue). The tabk choice is in the bay (tabk-select-build: `Core/Tabks/Garage.cs`, `Game/BayFlow.cs`, `Ui/Den/BayView.cs`), kept with the settings; kie's tabk is built (kie-tabk-build), so a mission drops it when it's loaded; kcaj's is built too (kcaj-tabk-build), so every tabk in the bay drops as itself. A tabk won stays won and always drops as itself (owner, 2026-10-20: unlocked is unlocked; bay-card-note took out the built check and the bay's "MISSIONS DROP TONES2'S FOR NOW" note).
