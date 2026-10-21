# The room plates (work in progress)

Painted plates for the house's outer rooms, each its own screen behind a door
([the design](../../lobby.md#the-outer-rooms), [the map](house-layout.svg)).

**These are work in progress, for looking at and choosing between.** They're not
in the game yet: the game loads its paintings from `Assets/Resources/Den/` as
`.bytes`, and nothing here has been cut to game pixels or put there.

Each room is one screen, 320 × 180 in the game's art pixels, except the
wintergarden, which is a tall room at 320 × 320.

| Room | Reached from | Plate |
|---|---|---|
| The kitchen | the den's side door | `wip-kitchen.png` |
| The back yard | the kitchen's back door | |
| B1, the basement | down the trapdoor in front of the couch | |
| B2, the bunker | down through the hole in B1's floor | |
| The rooftop | the loft's far-left door | |
| The shower block | the loft's shower door | |
| The wintergarden | the loft's wintergarden door | |

## How they're made

With OpenRouter's Qwen Image 2.1 Turbo (`qwen/qwen-image-2.1-turbo`, the
Alibaba endpoint), with the game's own paintings as image references so the
style stays in: `den.bytes` and `loft.bytes` are the references, and the
`street.bytes` one for the rooms that look outdoors.

The style line is in front of every prompt, from [the art design](../../art.md):

> Pixel art in the style of 1990s Neo Geo arcade run-and-gun games like Metal Slug: hand-placed pixels, bold dark outlines, rich three- or four-tone shading, chunky readable shapes. Crisp hard pixel edges, no anti-aliasing, no blur, no text.

Then the plate's own prompt, saying what the layer shows and nothing else.
