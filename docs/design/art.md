# Generated art: the look

How generated art is made for Pooket Slug, so every new background matches the ones before it. The owner picked this from the art showcase on 2026-10-17 (the gen-art-look item: https://claude.ai/artifact/7KaagiXRYxbuhq5rodNx18).

**Read this when** you're making a background plate, or any art with an image model.

Up: [Design](README.md)

## Background plates
- **Model:** Seedream 5 Flash on OpenRouter (`bytedance-seed/seedream-5-0-flash`), about $0.02 an image. Design uses it for all background work from now on.
- **Prompt only.** A new plate is made from its prompt alone, with no reference picture. A hand-drawn reference is made only when a plate's prompt doesn't quite do it.
- **The style line**, put in front of every plate's own prompt:
  > Pixel art in the style of 1990s Neo Geo arcade run-and-gun games like Metal Slug: hand-placed pixels, bold dark outlines, rich three- or four-tone shading, chunky readable shapes. Crisp hard pixel edges, no anti-aliasing, no blur, no text.
- **The plate's prompt** says what the layer shows and nothing else: no ground, vehicles or characters unless they're the point. A layer that sits over others asks for everything around it to be flat magenta `#FF00FF` (a chroma key), with no gradient.
- **Pixels:** the image is cut down to the game's size (the plate's size at 16 pixels a unit, for example 320 × 88 for a far skyline) and to 16 colours, with no dithering, by the vendor repository's `art/plates.py`. Then it's checked in the game, not only on its own.
- Images are made by the vendor repository's CI (the Plates and Showcase workflows), and their spend goes in its usage ledger.

## Sprites
Parked (owner, 2026-10-17): none of the showcase's models made sprites that work at the game's size, so sprites stay hand-made in `ArtData` for now. A later card comes back to it.

## What was tried
The showcase drew a rifleman, a tabk and a skyline with nine models in three styles (16-bit arcade, 8-bit console, and "match the game" with the hand-made sprites attached). Its page keeps every image, each cut to game pixels beside the hand-made art.
