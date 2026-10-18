# Generated art: the look

How generated art is made for Pooket Slug, so every new background matches the ones before it. The owner picked this from the art showcase on 2026-10-17 (the gen-art-look item: https://claude.ai/artifact/7KaagiXRYxbuhq5rodNx18).

**Read this when** you're making a background plate, animating a sprite, or making any art with an image or video model.

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
Each sprite's key frames stay hand-made in `ArtData`. Image models can't draw a usable sprite or sprite sheet at the game's size (gen-sprites, 2026-10-18), but a video model can animate one we drew. The owner locked in this workflow on 2026-10-18 (the sprite-model-research item: https://claude.ai/artifact/LrvdRFdJF6R4qnVvWewhyw).
- **Model:** Wan 3.0 image-to-video on OpenRouter (`POST /api/v1/videos` with `frame_images`), at 480p, about $0.05 a second. Veo 3.1 Lite is the second try for effects (its explosion had more sparks).
- **Start frame:** the hand-made sprite scaled up whole-pixel on flat magenta `#FF00FF`, with a short prompt for the move ("run in place, facing right").
- **Two ways in, by how much detail the move needs:**
  - *One sprite in the clip* (about 20×) for detailed objects and big moves: the heroes, bosses, tabks.
  - *Several sprites in one clip* (about 12×, side by side) for less detailed things with subtler moves: goons, props. One clip animates them all, each in its own spot.
- **Tweens:** give both a first and a last frame and the model fills in the middle: a pose change, a death, a throw, an explosion (a grenade to an empty frame). Only Wan and Veo take a last frame.
- **Cutting to game pixels:** frames at 12 a second; key the magenta out hard; scale so the first frame is the sprite's own height, feet on the same row; take one sample at the centre of each game pixel (never average, which leaves a magenta halo); map each pixel to the sprite's own palette. The script is `recut.py` in the shared folder's `concepts/sprite-anim/recut/`.
- **A person finishes it:** pick a clean loop of 4 to 8 frames and tidy stray pixels before it goes in `ArtData`.

## What was tried
The showcase drew a rifleman, a tabk and a skyline with nine models in three styles (16-bit arcade, 8-bit console, and "match the game" with the hand-made sprites attached). Its page keeps every image, each cut to game pixels beside the hand-made art.
