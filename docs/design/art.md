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
Sprites and their animations are hand-made in `ArtData`, and stay that way. Image models can't draw a usable sprite or sprite sheet at the game's size (gen-sprites, 2026-10-18). A video model can animate a sprite we drew, though: the workflow below was locked in on 2026-10-18 (sprite-model-research: https://claude.ai/artifact/LrvdRFdJF6R4qnVvWewhyw) and audited on the sprite-anim-pipeline card.

**When to use it:** only for a sprite or an animation the hand-made art is failing at, such as a movement in a cutscene that's hard to animate by hand. The owner picks each one and promotes it to this workflow on the Roadmap; nothing moves over on its own (owner, 2026-10-18).

### The workflow
1. **Start frame:** the hand-made frame, scaled up by a whole number on flat magenta `#FF00FF`, 16:9, nothing else in the picture.
   - *One sprite in the clip* (about 20×) for detailed things and big moves: a hero, a boss, a tabk.
   - *Several sprites in one clip* (about 12×, side by side, evenly spaced) for less detailed things with subtler moves, such as goons and props. One clip animates them all, each in its own spot.
2. **The clip:** Wan 3.0 (`alibaba/wan-3.0`) through OpenRouter's `POST /api/v1/videos`, the start frame as `frame_images` (`first_frame`), 480p, 3 to 5 seconds, about $0.05 a second. The prompt says what moves and holds the rest still: "Side-on 2D pixel-art game sprite. The character [does the move]. Exact same pixel-art style, colours and outline, crisp square pixels. Fixed camera, flat magenta background."
   - **A loop** (a run, an idle): give the start frame as the `last_frame` too.
   - **A tween** (a pose change, a death, a throw, an explosion): give a different hand-made `last_frame` and the model fills in the middle.
   - **Effects:** also try Veo 3.1 Lite (`google/veo-3.1-lite`, 720p only, 4 s at least, `generate_audio: false`), whose explosion had more sparks.
3. **Frames:** `ffmpeg -i clip.mp4 -vf fps=12 frames/f%03d.png`.
4. **Back to game pixels:** `recut.py frames START.png COLUMN OUT [COLUMNS]` cuts one sprite out of the clip (COLUMN of COLUMNS, for a clip of several). It keys the magenta out hard and scales so the first frame is the sprite's own height, with its feet on the same row. It takes one sample at the centre of each game pixel, and never averages, which would leave a magenta halo. Then it maps every pixel to the start frame's palette. It writes a strip of frames and a 4× GIF.
5. **A person finishes it:** pick a clean run of 4 to 8 frames, tidy stray pixels, and show it on the card's page beside the hand-made one. Only what the owner picks goes into `ArtData`.

### Where the tools are
The vendor repository's `art/sprite-anim/`: `video.py` (sends the takes in a JSON file, polls, and downloads the clips; a take already made isn't paid for again) and `recut.py`, with the workflow `sprite-anim.yml`, publishing to the `sprite-anim` release. The workflow runs only when started by hand, with the parameter file's path, so a push never starts a paid run. Clips are made by the vendor's CI and logged in its usage ledger, like every generation.

### Watch out for
- **The cut assumes the first frame is the whole sprite.** A tween that starts small, such as a grenade that becomes an explosion, needs the scale given rather than measured. `recut.py` also re-centres every frame on its own width, which takes out a move's travel (a lunge, a jump); keep the feet's x if the travel matters.
- **The frame rate is 12 a second,** with some noisy frames: plan to throw most of them away.
- **Other models:** HeyGen Video 1 flashes red and yellow frames; Grok Imagine 1.5 Lite is soft and noisy; Hailuo 3 Max takes only one keyframe, so it can't loop or tween.
- **A failed or retried clip is a new paid job:** change the prompt, don't resend it blindly.

## What was tried
The showcase drew a rifleman, a tabk and a skyline with nine models in three styles (16-bit arcade, 8-bit console, and "match the game" with the hand-made sprites attached). Its page keeps every image, each cut to game pixels beside the hand-made art.
