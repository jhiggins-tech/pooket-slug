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
- **In the game** (`Backdrop`, m1-plates-build): a mission's plates are PNGs in `Assets/Resources/Plates/<mission id>/` (`mission-1`), renamed `.bytes`, one strip per layer: `sky-0`, `sky-1`, … then `far-…` and `mid-…`, numbered along the map. A strip's plates are laid end to end and repeat, and any width works; the height is the layer's. The layers, back to front: **sky** (moves 0.95 of the camera, from the screen's bottom, a full screen tall: 180 pixels), **far** (0.85, its bottom 1.2 units up) and **mid** (0.6, 1 unit up), with the sky gradient behind them all. A layer with no plates is drawn in code, as before.

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

## The title logo
Picked by the owner on 2026-10-18 (the start-art item: https://claude.ai/artifact/Xygy3vrT2fFQdav1WFPa14) for the splash screen: gold pixel lettering, POOKET SLUG on one line, with a small gold tabk sitting above the gap between the words, in the manner of an SNES title screen.
- **The file:** `/mnt/project-files/concepts/start-art/logo-c1.png` (2136 × 496, its black keyed out to transparency), cut from take `c-1.png` (2560 × 1440 on black). It was made with Seedream 5 Flash, prompt only.
- **On screen:** it sits in the upper two thirds over the screen's own background (a gentle colour ramp and a vignette, drawn by the game), with START and SETTINGS below it. The page tried it over a royal-blue ramp, `#060b3a` at the top to `#2b3aa8` at the bottom.
- **Keying:** the dark outline around the letters went with the black, so give the logo a one-pixel dark outline or a soft drop shadow if it looks thin on the ramp.
- **Built** (start-screen, 2026-10-18): `Ui/TitleView.cs`. The logo is `Assets/Resources/Title/logo.bytes`, a PNG made by `tools/title_logo.py`: scaled to 1280 wide with a 3-pixel dark outline and a soft shadow baked in, shown 440 of the canvas's 360-high units wide. The ramp is that royal blue, with a vignette darkening the corners by up to 60%; the splash fades up from black once a visit.
- **Its animation** (title-logo-animation, the owner's picks A2, H2 and G3, 2026-10-19; the options: https://claude.ai/artifact/CSmHaZPzP9B14dmK76LCXK): the logo is cut into its ten letters and the tabk (`tools/title_logo.py` writes them to `parts.bytes` with their shadows and gold masks, `layout.txt` says where each goes). **In:** the tabk drives in from the left along the top of the word to an engine turning over and revving, each letter springing up as its nose passes; it stops and recoils with a cannon shot and a short shake, the title music starts, and S-L-U-G pop up. START and SETTINGS fade in once it's landed; a tap skips to the end (the music starts at once). **Hover:** the logo bobs gently while its shadow stays put, darkening as the logo dips. **Glint:** a band of light sweeps over the gold as it lands and every six seconds, ending in a sparkle off the barrel's tip. The hover and glint are silent. The timings are `Core/Title/TitleMotion.cs`; the drawing `Ui/TitleLogoView.cs`.

## What was tried
The showcase drew a rifleman, a tabk and a skyline with nine models in three styles (16-bit arcade, 8-bit console, and "match the game" with the hand-made sprites attached). Its page keeps every image, each cut to game pixels beside the hand-made art.
