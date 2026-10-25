# The room plates

Painted plates for the house's outer rooms, each its own screen behind a door
([the design](../../lobby.md#the-outer-rooms), [the map](../house-layout.svg)).

**These are the paintings and the takes made on the way to them.** The owner
signed the plates off (2026-10-21), and they're in the game: it loads them from
`Assets/Resources/Den/room-*.bytes`, cut at each room's own size.

Each room is 420 × 180 in the game's art pixels (a screen and a bit, so the
player scrolls across it), except the wintergarden, which is one screen across
and two up, at 320 × 360. **A plate is cut at the room's own aspect**: the
paintings come back at 1260 × 540, a third of the room's size, and one cut to
another shape squeezes the room.

| Room | Reached from | Plate | At game size |
|---|---|---|---|
| The kitchen | the den's side door | `wip-kitchen.png` | `game-kitchen.png` |
| The back yard | the kitchen's back door | `wip-yard.png` | `game-yard.png` |
| B1, the basement | down the trapdoor where the board used to hang | `wip-basement.png` | `game-basement.png` |
| B2, the bunker | down through the hole in B1's floor | `wip-bunker.png` | `game-bunker.png` |
| The rooftop | the loft's far-left door | `wip-rooftop.png` | `game-rooftop.png` |
| The shower block | the loft's shower door | `wip-shower.png` | `game-shower.png` |
| The wintergarden | the loft's wintergarden door | `wip-winter.png` | `game-winter.png` |

[Open the page that shows them all](index.html).

`wip-*.png` is the painting as the model made it, at 1280 × 720 (the wintergarden
at 1024 × 1024). `game-*.png` is the same painting cut down to 64 colours at
the first cut's size: 320 × 180 (the wintergarden 320 × 360). That squeezed the
420-wide rooms: the game's own plates are cut at 420 × 180 (re-cut on
2026-10-23), so for those rooms `game-*.png` is older than what the game loads. The palette is cut with a
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
