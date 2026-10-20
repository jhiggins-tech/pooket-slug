# Music

The game's music: every track and short sting, the style they share, and how they're made in code with the chip synth the sound effects already use.

**Status:** approved by the owner, 2026-10-15. The draft's picks stand until the owner answers the open questions. The track names and tempos are first ideas; the notes get written when each track is built.

**Read this when** you're building the music player or writing a track, or a new mission or boss needs music.

Up: [Design](README.md) · Builds on: [the boss intro](cutscenes.md#the-boss-intro) (a mission track, then a faster minor-key boss track), [cutscenes](cutscenes.md#how-every-cutscene-plays) (scenes use the music around them)

## The track list

There are two kinds of music: **loops**, which play under a whole stretch of the game and repeat without a seam, and **stings**, short one-shots for a big moment that play over or between loops.

### Loops

| Track | Plays | Feel | Tempo | Length of one pass |
|---|---|---|---|---|
| **Title theme** | The splash screen (the new start: key art, Start and Settings) | A Final Fantasy-style title: harp arpeggios, then strings and a solo flute on a gentle, majestic melody. A recording, not chip: Lyria 3 take D, picked by the owner 2026-10-18 ([start-music](#the-title-theme)) | About 84 bpm | About 28 s (a crossfaded loop) |
| **Mission one theme** ("Steel Night") | The run through the military base, from the caption card to the boss door | A snare-led military march, minor key and driving, with searchlight-sweep arpeggios. Metal Slug's base levels, on a Game Boy | 150 bpm | About 50 s: an A part and a B part, 32 bars |
| **Boss theme** ("Tabk Alert") | Every tabk boss fight and its pilot round | Faster and in a minor key, as picked for the boss intro: a pounding triangle bass, alarm-like pulse stabs, double-time hi-hats | 172 bpm | About 25 s (16 bars) |
| **Boss theme, final section** ("Tabk Alert (final)") | The boss fight's second stage: the pilot round, once the tabk is wrecked (asked for 2026-10-15) | The boss theme even faster: its climbing half first, a hi-hat on every sixteenth, sweeps on top | 200 bpm | About 19 s (16 bars) |
| **Debrief** ("Debrief") | The debrief screen, under the count-ups | Laid back and pleased with itself: a swung, quiet groove that leaves room for the tally's blips and the rank-up fanfare | 100 bpm | About 20 s (8 bars) |
| **Each later mission's theme** | That mission's run | Its own melody and feel, written with its map (Mission two is kcaj's) | Its own | About 50 s |

### Stings

| Sting | Plays | Length |
|---|---|---|
| **Mission start** | Under the caption card, as the announcer calls the start, leading straight into the mission theme | About 2 s |
| **Boss victory** | When the tabk is wrecked, leading into the boss theme's final section for the pilot round | About 2 s |
| **Mission complete** | As MISSION COMPLETE! lands (the gate at the end of the coda), after the boss theme stops | About 3 s |
| **Rank up** | The RANK UP! stamp in the debrief: the chip fanfare the [first debrief](cutscenes.md#the-first-debrief) calls for | About 2 s |
| **Game over** | When the last continue runs out: a sad descending run that ends on a buzz | About 3 s |
| **Continue countdown** | Under the CONTINUE? count: a ticking bass note that rises a semitone every second | As long as the offer (9 s, `Tuning.Lives.ContinueSeconds`) |
| **New crewmate** | When a freed crewmate first joins the crew screen | About 2 s |

### What has no music of its own

- **Cutscenes** use what's playing around them: the opening is street ambience until the caption card, where the mission start sting and the mission theme kick in ([the opening](cutscenes.md#the-opening)). The motor pool reveal cuts the mission theme and leads into the boss theme ([the motor pool reveal](cutscenes.md#the-motor-pool-reveal)). The boss theme cuts out when the pilot glitches out ([freeing kie](cutscenes.md#freeing-kie)).
- **The boss intro's siren** is a sound effect, not music: the screen locks, the music cuts, the siren and WARNING! banner play in silence, and the boss theme kicks in on the tabk's first shot.
- **The coda** depends on a pick (below).
- **Pause** keeps the track playing, quieter. **Co-op** plays the same music on both phones, each following its own screen, like the sound effects.

## The style

- **Four voices, like a Game Boy:** two pulse channels (lead and harmony or arpeggio), a triangle bass and a noise channel for drums. The same voices as the sound effects, so music and effects sound like one machine. A track can use fewer, never more.
- **Lead on 25% or 12.5% pulse, harmony on 50%,** so the two pulse lines stay apart. Fast arpeggios stand in for chords, as on the real hardware.
- **Noise drums:** a low, crunchy noise for the kick, a mid burst for the snare and a short high tick for the hi-hat, as in the Tabks tunes.
- **Short loops that don't tire you out:** each loop has a B part, so it doesn't repeat every few bars, and ends on a turnaround that leads back to its start.
- **The music sits under the action.** It plays at about half the volume of the sound effects, and a big blast or an announcer line ducks it briefly, so the game's own noise always cuts through.
- **The jokes are the crew's.** Music stays straight-faced; a boss track can quote its crewmate's in-joke (a pick, below), but missions don't parody anything.
- **Original melodies.** Any tune quoted from outside the game is in the public domain (Pop Goes the Weasel is fine), so nothing has to be licensed.

## How the tracks get made

Everything is code: no audio files, nothing to download, and a track is a few lines of text. The parts already exist: `Chip` (Core/Audio/Chip.cs) is the synth, `ChipSounds` renders the effects into clips at startup, and `Sfx` plays them.

1. **A score notation** in `Core/Audio/Notation.cs`, the same as Pooket Tabks' `score.ts`: a part is a line of notes such as `c5:2 e5 g5 r:4`, with rests, chords, drum letters (`k s h c`) and slides. It's easy to write and read, and a tune can be checked in a unit test without Unity.
2. **One file per track** in `Core/Music/`: its tempo, its four parts as score lines, and where its loop starts and ends. Stings are the same, without a loop.
3. **A renderer** in `Music.cs` plays each part into a `Chip` buffer: the lead and harmony as pulse tones, the bass on the triangle, the drums as noise, each note with a short envelope. A note that runs past the end of a loop wraps round to its start, so the loop has no seam. Then it becomes an `AudioClip`.
4. **Rendered when needed, not all at startup.** A minute of music is about 10 MB of samples, so only the tracks for what's next are rendered: the title theme at launch, then a mission's theme and the boss theme when the mission loads, a few seconds at a time across frames so nothing stutters. Clips no longer needed are let go.
5. **A music player** (`Music.Play(track)`, `Music.Stop()`, `Music.Sting(sting)`): one looping source for the loops and one for the stings, with a quick fade when a track changes and the ducking above. It's driven by the game's existing moments: the caption card, the boss intro's hooks (`World.BossIntro`, `World.BossFightStarted`), the boss's death, the pilot's glitch-out, the debrief and game over.
6. **The MUSIC switch** beside SOUND in settings (picked 2026-10-15), so a player can keep the sound effects and lose the tune, saved on the phone. With music off, nothing is rendered at all.
7. **Tests:** the score notation reads every track; each loop's length is a whole number of bars; and every game moment that should start a track does.

**Writing the notes:** Claude composes each track in the notation, then renders it to a WAV in CI so it can be heard in a browser before it goes in the game. Each track goes on the Roadmap with a listen link, like a spike.

**First cut** (spike, 2026-10-15, `?spike=music`, solo only): the notation (`Core/Audio/Notation.cs`), the renderer (`Core/Audio/MusicRender.cs`), the player (`Music.cs`), Mission one's theme, the boss theme and its final section, and the mission start and boss victory stings (`Core/Audio/MusicTracks.cs`), hooked into the boss intro. Every track is rendered at launch for now.

**Chip to orchestra** (spike extension, 2026-10-16): in the boss's final section, after one pass in chip, the music crossfades over four seconds to an orchestra playing the same notes, in step. None of OpenRouter's music models takes an audio reference or a melody: its two music models, Google's Lyria 3 Clip and Lyria 3 Pro, take text or an image, and its other audio models (GPT Audio) are for speech. So the orchestra plays the game's own notes instead: `Core/Audio/MusicMidi.cs` writes a track as a General MIDI file scored for strings, horns, pizzicato, contrabass, tuba and concert percussion, and the vendor repository's Orchestra workflow renders it with FluidSynth and a General MIDI soundfont, looped to the chip version's length, into `orchestra.zip`; the game's CI puts it in `Resources/Music`. A better soundfont, or a real orchestral sample library, would make it sound richer with no change to the notes. Lyria could still make a loose orchestral take from a text prompt spelling out the melody, but it wouldn't be in step with the chip version.

**Built** (music-build, 2026-10-18): the music is on for everyone, solo and co-op, with no spike flag. New in `MusicTracks`: the debrief's loop (F major with sevenths, dotted swing, 8 bars, 19 s), and the mission complete, rank up, game over, continue countdown and new crewmate stings. `Core/Audio/Soundtrack.cs` is the rule for when a mission's music changes as it plays (the countdown and back, the glitch-out's cut, the win), with tests. The settings have a **MUSIC** button (HIGH, MID, LOW, OFF, like EFFECTS and VOICES; kept with the other options), and the music ducks under a big blast and under any recorded line. The title recording also looped in the den, until the den got its own theme (below). Mission one's music renders at launch (only with the music on); the debrief's loop and the other stings render the first time they play. With the music on, the mission complete and rank up stings take the place of the old victory jingle. Still to come: each later mission's theme (with its map), and the new crewmate sting's place (the crew reveal it was for has gone; it waits for the den's).

**Build order:** the notation and player first, with the boss theme (the intro already waits on it), then Mission one's theme, the stings, and the debrief. The title theme is a recording (below).

## The title theme

Picked by the owner on 2026-10-18 (Roadmap item start-music): **Lyria 3 take D**, a 30-second clip Google's Lyria 3 made from a written brief (harp arpeggios, then strings and a solo flute; gentle, nostalgic, majestic; about 84 bpm, C major; no vocals or drums). It beat two versions of an original chip theme (chip, and the same notes for orchestra) and another Lyria clip. Unlike the rest of the music it is a recording, so it has no chip version and isn't in the score notation.

- **The loop:** the clip's trailing silence is trimmed and its last 2.5 s are crossfaded (equal power) into its first, giving a 27.7 s seamless loop, `title.wav` (44.1 kHz stereo).
- **Where it lives:** the vendor repository's `start-music` release (made by the Media generation role). The game's CI fetches it like the orchestra (keeping only `title.bytes` from the zip), and `Music.Loop` plays it while the splash screen or the settings over it is up, fading as they're left (built: start-screen, 2026-10-18).

## The den's theme

Picked by the owner on 2026-10-19 (den-theme-music): **B, the console-shop bossa, take 2**, full length, cut to a 170 s seamless loop (`den.wav` in the vendor repository's `den-music` release). It's background music: it plays at about half the title theme's level, and dips a little more (to 60%, fading) while the kit screen or the mission screen is open over the den. The title theme stays on the splash; going between them, one fades out before the other starts. The debrief keeps its own loop, and cutscenes have none of the den's (built: den-theme-switch, 2026-10-19; `MusicTracks.MenuRecording`, `MusicFlow.Menus`). While one of the den's beats plays as you come back (the first visit, a crewmate moving in), the den is quiet, the debrief's loop fading out, and its theme starts once the beat's over or skipped, fading in over 3 s (the owner, 2026-10-20; den-music-after-cutscene).

## Open questions

- **One boss theme, or one per boss?** The draft has one shared theme.
- **The coda's music.**
- **Mission one's feel.** The draft is a military march.
- **Who writes the melodies.** The draft has Claude composing them.
