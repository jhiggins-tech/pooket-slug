# Adding a room to the house

How a new room joins the house: its own screen behind a door, like the tabk bay.
This is the workflow for the **outer rooms** — the kitchen, the back yard, the
basement and the bunker, the rooftop, the shower block and the wintergarden.

**Read this when** you're adding a room to the house, or changing one that
exists.

Up: [The lobby](../lobby.md) · [The house map](house-layout.svg) · [The outer rooms](../lobby.md#the-outer-rooms)

## What a room is

- **Its own screen**, like the tabk bay. You go through a door and you're in
  another screen; the way back takes you to where you came from.
- **One plate**: a painting in `Assets/Resources/Den/room-*.bytes`, cut to the
  game's size — 320 × 180 in art pixels, or 320 × 360 for a tall room.
- **Its own ways in and out**: a door back the way you came, and maybe a way on
  to another room (the kitchen's back door to the yard; the basement's hole down
  to the bunker, once it's dug).
- **Drawn for the 22 px soldier**: a door is about 37 px tall (1.7 times him),
  and a stair's treads are a step apart, or he can't climb it.

## The steps

### 1. Design it, in the map

The map ([house-layout.svg](house-layout.svg), drawn by [house.py](house.py)) is
the canonical layout: **when a room moves in the game, redraw it**. Add the room
to `house.py`: its box, its contents, and the numbered badges on the two ends of
each way in (the same number on the door in the house and the door in the room).

Then say what it's for in [the lobby's design](../lobby.md#the-outer-rooms).

### 2. Paint it

With OpenRouter's Qwen Image 2.1 Turbo, and **two image references**:

1. **The room's vector reference**, from `tools/room_ref.py`: the room drawn to
   scale as flat vector art, with the game's soldier standing in it. This is
   what fixes the scale — without it the model draws a room for a person twice
   the soldier's height, with doors you could walk through sideways. The
   reference comes in two versions: the **annotated** one, with the labels, the
   screen outline and the scale note, for the owner to read; and the **clean**
   one (`--clean`), with the annotation taken out, which is what the painter
   works from. The soldier stays in the clean one, so the model still has the
   proportions.
2. **A swatch** of the game's own pixels, upscaled so the pixel technique is
   legible, with nothing recognisable in it. That is what makes the plate pixel
   art rather than a photograph. A whole plate as the second reference makes the
   model copy that room instead of drawing the one asked for.

The style line goes in front of every prompt, and the reference's caption between
it and the plate's own prompt. **The exact wording matters**: the provider's
moderation blocks some combinations, and it isn't deterministic. [The notes](../art.md#the-houses-room-plates)
have the recipe, and the plates are in [plates/](plates/).

The provider wants at least 512 × 512, and **the generated size should be the
room's own aspect**: a room 420 wide and 180 tall is generated at 1260 × 540. A
plate generated at the wrong aspect comes back with the wrong proportions, and
cutting it to the room's size squeezes it.

The result is cut down to the game's size and 64 colours afterwards. Cut the
palette with a **median cut**, not by keeping the most common colours: the
paintings have ramps of light, and the commonest-colours cut crushes a ramp into
a muddy band.

### 3. Put it in the game

- **The plate**: `Assets/Resources/Den/room-<name>.bytes`, with a `.meta` (run
  `tools/new_meta.py` on it).
- **The rules** in Core, tested: `RoomMap` (the room's size, its ways in and
  out, and where a tap is) and `RoomWalker` (walking, and any stair).
- **The view**: `Ui/Den/RoomView`, and the flow: `Game/RoomFlow`.
- **The door in the house**: a `DenPlace` in `Core/Lobby/DenMap.cs` (its tap
  area, where you stand to use it, and which way it opens), and the room's way
  back names that place, in `RoomMap.Leads`.

### 4. Check it

`tools/check.sh` before pushing: it compiles every assembly, runs the EditMode
tests, checks the scenes and the docs links. Then push and read the CI run.

## The traps

- **The order of the parts matters.** The painting is made when the room opens,
  but the soldier and the tap catcher are made when the screen is built: the
  painting lands *over* them unless you put it behind. And a picture has
  `raycastTarget` off, so a painting over the tap catcher doesn't just hide the
  player — it stops taps reaching the catcher under it, and the only way out of
  the room is ESC.
- **A room taller than the screen** (the wintergarden) has a tap rect taller
  than the world. Read the tap's position in the *tap rect's* own space, not the
  world's, or a tap up the room misses.
- **The world is 320 × 180** with its pivot in the middle; a room's plate is
  anchored at the world's bottom left, so a room's art pixels and the world's
  line up from the bottom.
- **The camera rises with the stair.** The wintergarden's camera follows you up
  the spiral stair, so the rose at the top is out of reach until you've climbed.
- **A room's x is the room's own**, from its bottom left. The map's badges and
  the code's `RoomMap.WayX` have to agree.
