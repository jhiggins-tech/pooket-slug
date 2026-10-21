# The room plates (work in progress)

Painted plates for the house's outer rooms, each its own screen behind a door
([the design](../../lobby.md#the-outer-rooms), [the map](../house-layout.svg)).

**These are work in progress, for looking at and choosing between.** They're not
in the game yet: the game loads its paintings from `Assets/Resources/Den/` as
`.bytes`, and nothing here has been put there.

Each room is one screen, 320 × 180 in the game's art pixels, except the
wintergarden, which is a tall room at 320 × 360: one screen across, two up.

| Room | Reached from | Plate | At game size |
|---|---|---|---|
| The kitchen | the den's side door | `wip-kitchen.png` | `game-kitchen.png` |
| The back yard | the kitchen's back door | `wip-yard.png` | `game-yard.png` |
| B1, the basement | down the trapdoor in front of the couch | `wip-basement.png` | `game-basement.png` |
| B2, the bunker | down through the hole in B1's floor | `wip-bunker.png` | `game-bunker.png` |
| The rooftop | the loft's far-left door | `wip-rooftop.png` | `game-rooftop.png` |
| The shower block | the loft's shower door | `wip-shower.png` | `game-shower.png` |
| The wintergarden | the loft's wintergarden door | `wip-winter.png` | `game-winter.png` |

[Open the page that shows them all](index.html).

`wip-*.png` is the painting as the model made it, at 1280 × 720 (the wintergarden
at 1024 × 1024). `game-*.png` is the same painting cut down to the game's size
and 64 colours: that's the one the game would load. The palette is cut with a
**median cut**, not by keeping the most common colours: the model's paintings
have ramps of light, and keeping the commonest colours crushes a ramp into one
muddy band.

`wip-kitchen.png`, `wip-kitchen-artmd.png` and `wip-kitchen-arcade.png` are three
takes of the kitchen, kept so the difference the references make can be seen:
the first two were made from the prompt alone and came out photoreal, and only
the one with the references came out as pixel art.

## How they're made

With OpenRouter's Qwen Image 2.1 Turbo (`qwen/qwen-image-2.1-turbo`, the
Alibaba endpoint). Two image references:

1. **The room's layout**, cut out of [the house map](../house-layout.svg) and
   rendered as a picture. The model then puts the things where the map says.
2. **A swatch** of the game's own pixels, upscaled so the pixel technique is
   legible, with nothing recognisable in it. That is what makes the plate pixel
   art rather than a photograph.

The style line is in front of every prompt, from [the art design](../../art.md):

> Pixel art in the style of 1990s Neo Geo arcade run-and-gun games like Metal Slug: hand-placed pixels, bold dark outlines, rich three- or four-tone shading, chunky readable shapes. Crisp hard pixel edges, no anti-aliasing, no blur, no text.

Then the layout hint, then the plate's own prompt, saying what the room shows
and nothing else. The provider's moderation blocks some ways of putting these
together, so the exact wording matters: [the notes](../../art.md#the-houses-room-plates).
