# The den domain

## What this domain is
The den: its design and art, the room layout, what opens and when, and moving around it. That is the den itself (the
ground and upper floors, the loft above them, the bunks in its roof), the tabk bay behind the roller door, and the
house's outer rooms (kitchen, back yard, basement, bunker, rooftop, shower block, wintergarden). It is the owner's
domain: the layout and the art are picked by the owner and written down in `docs/design/lobby.md` first.

## The files
Exactly the `den` lines of `.claude/hierarchy/OWNERSHIP.txt`:
- `Assets/Scripts/Core/Lobby/Den.cs`, `DenMap.cs`, `DenBeats.cs`, `BayMap.cs`, `RollerDoor.cs`, `RoomMap.cs`,
  `RopeClimb.cs`, `SwingDoor.cs` (the rules, plain C#)
- `Assets/Scripts/Ui/Den/` (every file: `DenView`, `DenRoom`, `DenCrew`, `DenConsole`, `DenInfo`, `DenBeatView`,
  `DenBitPlayer`, `DenRollerDoor`, `DenTrapdoor`, `BayView`, `BayHomecomingView`, `RoomView`, `RoomBackDoor`)
- `Assets/Scripts/Game/DenFlow.cs`, `BayFlow.cs`, `RoomFlow.cs`, `RoomsFlow.cs`
- `Assets/Resources/Den/` (the paintings, as `.bytes`)
- `Assets/Tests/EditMode/Lobby/`: `BayMapTests.cs`, `DenBeatsTests.cs`, `DenMapTests.cs`, `DenTests.cs`,
  `RollerDoorTests.cs`, `RoomMapTests.cs`, `RopeClimbTests.cs`, `SwingDoorTests.cs`
- `docs/design/lobby.md`, `docs/design/lobby/` (the house map `house-layout.svg` drawn by `house.py`, `rooms.md`,
  `rooms-todo.md`, `plates/`)

Not this domain's, though they sit in the same folder: `Profile`, `Options`, `Ranks`, `Debrief` (shell), `CoopRoom`
(net). `CoopRoomTests`, `DebriefTests`, `OptionsTests` and `ProfileTests` share `Tests/EditMode/Lobby/` and are theirs.
The tabk bay's rules about which tabk is whose (`Garage`, `CoopTabks`, `KieHomecoming`) are in `Core/Tabks/`, the
gameplay domain's: `BayFlow` and `BayView` call them.

## The rules this domain keeps
- Walking to a station is the menu: tap the floor to walk, tap a station or a door to walk there and use it.
  (`docs/design/lobby.md`, Den 2.0; `DenFlow.Use`)
- The soldier you play is the one you move, and the couch is the soldier pick: two seats, left for player one, right
  for player two. Picking, readying and starting a mission never needs a scroll. (`Den.cs`, `DenTests`)
- READY sits bottom right and BACK top left; no station's tap area reaches into the bottom corners. (`lobby.md`)
- The den is the crew's: a crewmate's corner stays wrecked and dark, with no name, until their move-in beat has played
  and they are freed. The exception is kcaj's string board, which stays up in his dark corner. (`lobby.md`,
  `DenRoom.Paint` draws a crewmate's moved-in layer only when freed)
- The mission board is kcaj's string board, on the upper floor at x 60 (`DenPlace.Board`); the ground floor keeps a nail.
  Solo only: a co-op room's host picking the mission from the den is not built. (`DenFlow.Use`)
- The beats are the first visit (once ever) and a crewmate moving in (once each); tones2 and kie are home from the
  start and have no move-in. Co-op plays none. Tapping skips; a skipped beat counts as seen. The den has no narrator,
  only speech bubbles. (`DenBeats.Due`, `DenBeatsTests`, `docs/design/cutscenes/den.md`)
- Co-op: each phone shows its own den built from its own progress. A soldier your partner is sitting as, or playing, is
  taken. In the bay, a tabk your partner has loaded is taken, and ours swaps out if we clash. One of each tabk
  (owner, 21 Oct). (`Den.Taken`, `BayFlow.Taken`, `DenFlow.Tabks`)
- The tabk bay opens with the den, after Mission one, with tones2's tabk on the repair stands; kie's is won with Mission one (and comes home with its own beat on the
  first visit after) and kcaj's by beating Mission two (`Garage.Won`). Your loaded tabk is
  the one on the stands. The drive-through wash is off. (`lobby.md`, The tabk bay; `BayMap.BayPlace.Wash`)
- A roller door, swing door or rope takes no walking while it runs, and goes one way at a time. (`RollerDoorTests`,
  `SwingDoorTests`, `RopeClimbTests`)
- The house map (`docs/design/lobby/house-layout.svg`, drawn by `house.py` from `DenMap`, `BayMap` and the stations)
  is the canonical layout: when a room moves in the game, redraw it.
- Cosmetic idle cycles and bits use the cosmetic randomness, never the game's `Rng`. Blinks and walk cycles go
  through `Fx` or a `Clip`. (`CLAUDE.md`, Invariants)
- Words players read: "tabk", never tank; "Trollogram", never hologram. Never name or describe the 25 hidden ranks in
  anything players read; the trophy shelf shows only the rank the player holds.
- BACK in an outer room uses that room's own `RoomWay.Back`, the way you came in: the yard goes to the kitchen (at
  its back door), the bunker climbs its rope to B1, the rest go to the house. (`docs/design/lobby/rooms.md`;
  `RoomFlow.Tick`, `RoomMapTests.TheWayBack_GoesWhereYouCameFrom`)
- Each tabk has its own height in the bay (`BayMap.TabkHOf`: tones2's 33, kie's and kcaj's 42, their art's 22 and 28
  rows at 1.5 times). A spot's tap box (`BayMap.SpotAt`'s `heightAt`) and the TAKEN tag go by the tabk standing
  there. `BayMap.TabkH` stays as tones2's and the empty spot's. (`BayMapTests`)
- A crewmate's station idle alternates: a loop at random, then a breath, never two loops back to back (`DenIdle` in
  `Den.cs`, `DenTests.IdleLoops_AlwaysHaveABreathBetweenThem`; `docs/design/lobby.md`, Who lives where).
- kcaj's two loops are `KcajIdle` (Core, `Den.cs`): two cones at once (three bites, both up together), and a sticky note
  stuck on the string board while the screen's edges dim (`DenView.Edges`, only on the den screen, under the buttons).
  His idles never move him off his spot at x 60, so his tap box (`DenCrew.At`, 18 px wide) stays inside the board's
  area; `DenView.Tapped` checks the crew before places, so a tap on him swaps to him and the board is the rest of its
  area. (`DenTests.KcajsLoops_FitInALoop_AndTheDimComesAndGoes`)
- Two idle crewmates with no bit of their own just wave (`DenWave`, `DenBits.Choose`, `DenBits.HaveABit`): both stay at
  their stations, the first waves and the second waves back, no lines; either can go first, and a wave is one of the
  choices like a bit, never the same pair twice in a row while another fits. No waves to the couch. The wave's hand is
  the lounge torso's raised arm, up and down (`DenBitPlayer.HandUp`). (`DenTests`, the three wave tests)
- A rule found in a view or flow (`DenView`, `DenFlow`, `BayView`) moves into Core with tests in the same commit as
  the change that touches it. A big view file is split by part when next changed. (`CLAUDE.md`, Audit 2)

## The vocabulary
- Floors are `DenFloor` (Ground, Upper, Loft, Bunks). Places are `DenPlace` (Couch, Board, Gym, Trophies, Bunks,
  Showers, Laundry, Wintergarden, Vending, SideDoor, RollerDoor, Rooftop, Trapdoor). Modes are `DenMode` (Solo, Crew,
  Coop). Do not rename them.
- The bay: `BayDeck` (Floor, Top), `BayPlace` (Door, Lift, Wash, Street), `BayWalker`, `BayErrand`.
- The outer rooms: `RoomId`, `RoomWay` (the doors, in the house and in each room), `RoomMap.Leads`, `RoomWalker`.
- A painting is `Resources/Den/<name>.bytes`, loaded by `DenRoom.Painting(name)`, in art pixels (the den screen is
  320 x 180; the loft is 382 x 164 over it from x 19; the bay is 993 x 180). A crewmate's moved-in layer is
  `DenRoom.Layers`; `raid-den` and `raid-bay` are the opening's plates, also used by the cutscenes.
- `DenMode.Crew` (the old CREW button's: no mission) is still in `Den`, `DenConsole` and `DenTests`, but nothing
  opens the den in it now; START and co-op open it as Solo and Coop.
- Flags a soldier carries in the den: `Den.InDen`, `Seated`, `ReadyFlag`. They ride the network, so do not renumber.
- The beat ids `den-first-visit` and `moved-in-<name>` are saved in the profile: do not rename them.

## The invariants
- The den is drawn on the cutscene domain's stage (`StageKit`, `StageScreen`, `StageFigure.Pose`). Do not change
  `Assets/Scripts/Stage/`; ask, and the handover says what the den must adapt to.
- Everything is drawn for the 22 px soldier (`DenMap.SoldierHeight`). A new room's doors are about 37 px (1.5 to 1.9
  times him), a ladder's rungs 3 to 8 px apart, and headroom is at least the soldier's height. The loft and bunks are
  checked against this; the ground and upper floors are not (see Known unfinished).
- The ladder's x, the bunks' ladder, the floor heights and the walking limits are `DenMap`'s constants. A change to a
  floor's height moves the loft's painting offsets too (`LoftY`, `LoftH`).
- A place's tap area is in the painting's art pixels (`DenMap` areas, `RoomMap`); the painting leads, so a piece of
  furniture is placed where the art puts it, not the other way round.
- Core knows nothing of Unity's scene. `Core/Lobby` stays plain C#, and `DenWalker`, `BayWalker` and `RoomWalker` are
  tested without a view.
- Crewmates in the den are cosmetic figures: nothing in the den changes a mission's rules. What a mission opens is the
  shell's (`Profile` reveals) and gameplay's (`Crew`, `Garage`).

## How it is checked
`tools/check.sh` (the type-check, the EditMode tests, the scene check, the docs link check), and
`python3 tools/check_ownership.py` when you add a file. The den's tests are in `Assets/Tests/EditMode/Lobby/` (the
eight above). CI runs the touch play (`tools/touch.mjs`) and, when the netcode changes, `tools/coop.mjs`, which goes
title, Enter, den, H; a change to the den's flow must keep that route. The views have no tests: smoke them on the
branch's preview, with `?unlock=crew` to fill the den. A new `.cs` needs a `.meta` (`tools/new_meta.py`).

## Known unfinished
- The ground floor and upper floor's painting predates the scale rule: its doors are about 2.8 times the soldier, and
  `DenMapTests.TheDen_IsDrawnToTheSoldiersScale` is `[Test, Ignore]` for that, its reason "Design's call whether it's
  repainted". The owner kept the painting at its size (den2-den-rescale); `lobby.md` says so.
- The den's painting (`den.bytes`) still shows the old board on the ground floor, so kcaj's nook has two. It needs an
  annotated edit that takes the ground board down and leaves the nail; `DenTrapdoor` (drawn in code) should then match
  the nail, and the TV's mission name, written on that old board (`DenRoom.Board`, `DenConsole`), has to move.
  (`docs/design/lobby/rooms-todo.md`, item 1)
- The outer rooms are spaces to walk in with no content: the table to eat at, the drone's pile, the PCs and arcade, the
  crates and the shut door, the rooftop telescope (it should replay missions through `BoardFlow`), the sinks and
  cubicles, and the rose. (`rooms-todo.md`, item 2)
- Every room is open from the start. The unlock rules are known (most outer places after Mission two) but not written
  down or built. (`rooms-todo.md`, item 3)
- The shower block's doors read as stalls, not swinging saloon doors. (`rooms-todo.md`, item 5)
- The bunks, laundry and vending machine only say a line (`DenMap.Note`): "the crew roster, not yet", "not yet",
  "out of order".
- Design says the host picks the mission from the den in co-op, a MAP button shows the house as a cut-away, and a
  laptop on kie's bench is a group chat for swapping crew. None is in the code. The couch fist bump is not built, nor
  the board glowing until Mission two's played, nor a co-op partner walking round your den (they only take their
  seat), nor BACK walking you out of the roller door (it goes straight to the title).
- `lobby.md`'s "The room" and "The backdrop" are the first, one-screen design and its first painting (gen-lobby-bg),
  kept and marked as such; Den 2.0 wins where they differ.
- The house map's outer rooms are drawn on a 320 grid stretched to 420 (`house.py`'s `XS`), as their paintings
  were cut; only the hole and the rope are placed from `RoomMap`. The rooms' contents are the layout the plates were
  painted from, not measured off the plates.
- The painting of each unfreed crewmate's corner and the second room for the coming-soon crew are not built.
- The cargo drone's hook (`Garage`'s `DroneShow`, gameplay's file) still uses `BayMap.TabkH` for every tabk, so it
  dips 9 px into kie's and kcaj's tops; it should use `BayMap.TabkHOf`. Handed to gameplay (dca-den-fixes).
- The bits and lines are placeholders awaiting the owner's rewrite in the crew's voice (`DenBits`, `lobby.md`). kcaj's two bits (with
  torikloud, who isn't in the game, and the cone for tones2) wait on that rewrite (dca-kcaj-den, owner: "Idles now"), so
  every pair with kcaj in it only waves for now; when a bit is added for a pair, `HaveABit` stops their wave.
