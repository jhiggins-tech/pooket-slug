# The outer rooms: what's left

The rooms are built and their art is picked. This is what's left to do, in the
order it should be done.

**Read this when** you're picking up the outer rooms again.

Up: [The lobby](../lobby.md) · [The workflow](rooms.md) · [The house map](house-layout.svg)

## To do

### 1. The board is in two places (an annotated edit)

The map says **the mission board moved up to kcaj** — the board *is* the string
board, so it hangs in his nook on the upper floor, and the ground floor keeps
only a nail where it used to be.

The **code** has moved: `DenPlace.Board` is on the upper floor at x 60, and its
tap area is kcaj's nook. The upper floor already has the board, in kcaj's
moved-in layer (`kcaj.bytes`, 96 × 52 at x 12, top 14).

But the **den's painting** (`den.bytes`) still has the old board on the ground
floor, with its papers on it. So the game shows a board the map says is gone,
and kcaj's nook has two.

**This needs an annotated edit of the den's painting**: take the ground-floor
board down, leave the nail. It can't be done in code — the board is painted into
the plate. (The trapdoor, which goes in the same spot, is drawn in code by
`Ui/Den/DenTrapdoor`, so it needs to be matched to the nail's look.)

### 2. The rooms have no content yet

They are spaces to walk round in. The design says what each thing is *for*, and
none of it happens yet:

| Room | What it should do |
|---|---|
| The kitchen | the table is to eat at |
| The back yard | the drone's pile (the tabks it moves) |
| B1, the basement | the PCs (a LAN), the arcade (a link to Pooket Tabks) |
| B2, the bunker | the supplies in crates, and the door that stays shut |
| The rooftop | the telescope **replays the missions** |
| The shower block | the sinks and the cubicles |
| The wintergarden | the rose, on its landing, for a cutscene with two crew |

The rooftop's telescope is the clearest one: it's the mission replay, and the
mission screen already exists (`BoardFlow`).

### 3. The unlock rules

Every room is **open** now, so the whole house can be looked at. The real game
needs the rules: the design says most of the outer places come after Mission
two ([den2-story](https://claude.ai/code/artifact/f6915f40-b462-4063-8250-a8c245d35353)
says what each is for), and the tabk bay opens with the den, after Mission one.
So the rules are known, they just aren't written down or built.

### 4. The trapdoor is drawn in code

`Ui/Den/DenTrapdoor` draws the hatch over the den's floor, because the painting
has no hatch in it. It's a flat grey rectangle in a painted room, and it should
match the boards. Folded into the annotated edit above.

### 5. The shower block's saloon doors

At game size they read more like privacy stalls than **swinging saloon doors**,
which is what the design calls for. The doors are painted flat, so making them
swing means the art, or drawing them in code over the plate.

### 6. The basement's ladder isn't climbed

The basement's way back up to the den is its **ladder** (up its left wall,
through the trapdoor in the den's floor), but the game doesn't climb it: the
way cuts straight back to the den, with no climb and no ladder drawn over the
plate. The **rope** is climbed both ways (down into the bunker, up and out of
it), so the ladder is the last of the ways that isn't.

### 7. The wintergarden paints no door

The room's way back to the loft is a door in its left wall, but the plate has
**glass** there: no door is painted, so the game draws its own door over the
glass (`RoomMap.WinterDoor*`), and the door swings like any other. The plate
still wants the same annotated edit as the den's board: a door painted in the
glass, where the loft's wintergarden door is.

### 8. Coming back into the house, you stand where you were

Going out of a room and back through a door of the house works, but **you
don't stand in the door you came through**: the den's own figure takes over
where it was, and nobody walks it in. The den's roller door does this already
(`DenRollerDoor`, and the crew that goes through it), so the rooms' doors want
the same: walk your soldier in through the door you came out of.

**Measured off the plates** (2026-10-24): the openings were measured off their
plates and pinned by the tests — the hole in B1's right wall, the house door in
the yard's wall, the shower block's door, the rope under the bunker's roof hole
and the stair hut's door on the rooftop. The kitchen's own doors were left as
they were: their plates agree with the boxes.


## Looked at, and fine

- The **wintergarden's stair** climbs to a landing wide enough for two crew,
  with the rose under its glass dome at the top, and the camera rises with you.
- The **rooftop** ends at a wall, and the **shower block** at a wall past the
  lockers, so neither looks cut off.
- The plates' **colour depth**: the palette is cut with a median cut, so the
  ramps of light survive.
