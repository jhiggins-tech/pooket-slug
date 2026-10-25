# The audio domain

## What this domain is
The sound: the chip synth, the music (its notation, tracks and renderer), the sound effects, when each music track
plays, and the vendor takes (announcer, narrator, orchestra, title and den recordings) the game pins. Every other
domain asks this one for a sound. The design it decides is `docs/design/music.md`.

## The files
The map (`.claude/hierarchy/OWNERSHIP.txt`) is the authority:
- `Assets/Scripts/Core/Audio/` (`Chip`, `Notation`, `MusicTracks` + `MusicWhen` (its other half: which track plays when), `MusicRender`, `MusicMidi`, `Wav`, `Soundtrack`)
- `Assets/Scripts/Audio/` (`Sfx`, `ChipSounds`, `Music`)
- `Assets/Tests/EditMode/Audio/` (`MusicTests`, `SoundtrackTests`, `WavTests`)
- `Assets/Scripts/Game/MusicFlow.cs`
- `docs/design/music.md`

Outside the map's coverage, but this domain's work: `vendor.lock` and `tools/vendor_lock.sh` (the pinned takes),
`tools/vendor_fetch.sh`, and the vendor repository's `music/`, `voice/` and `pin/`. The narrator's lines are written by
the story and cutscene side and recorded here. The cue ids and `CueSounds` (which clip a cutscene cue plays) belong to
the cutscene domain (`Core/Story/Cues.cs`, `Ui/Cutscene/CueSounds.cs`); the clip itself is this domain's.

## The rules this domain keeps
- **A new weapon, cue or goon names its sound in `Sfx` / `ChipSounds`.** The asking domain hands over; it does not edit
  these files (CLAUDE.md, the sound recipes). A sound cue is a constant and effect in `Cues` plus a clip in `Sfx` /
  `ChipSounds`; a boss-intro cue goes on `BossIntroShow`'s `Cue` timeline.
- **Sound effects are Pooket Tabks' 8-bit recipes on `Chip`**, each rendered once into a clip at startup
  (`ChipSounds.Make`). `Chip` matches Tabks' `src/audio/chip.ts` call for call, so a recipe ports over unchanged. Cut a
  recipe down if the game fires it far more often than Tabks would (a machine gun).
- **Music is code, four voices like a Game Boy**: two pulse channels, a triangle bass, a noise channel for drums. A
  track may use fewer voices, never more. Lead on 25% or 12.5% pulse, harmony on 50% (music.md, The style).
- **A loop is a whole number of bars in every part** and repeats with no seam; notes ringing past its end fold back onto
  its start (`MusicRender`). A sting plays once. (`MusicTests.EveryLoopIsWholeBarsInEveryPart`)
- **The music sits under the action**: about half the effects' volume (`Music.Volume`), paused quieter, ducked briefly
  under a big blast (`Sfx` calls `Music.Duck`) and under any recorded line. The MUSIC setting (`Options.Music`) scales
  it, and with music off nothing is rendered up front (but see Known unfinished: a track played while muted still renders).
- **Only the tracks for what's next are rendered.** Mission one's, the boss tracks and the two stings at launch
  (`MusicTracks.RenderedAhead`, through `MusicFlow.Prepare`); with the music muted then, `Music.Prepare` keeps the list
  and renders it the moment the music is unmuted (MUSIC or SOUND turned on, `Music.Muted`'s setter), once. The debrief's
  loop and the other stings render the first time they play.
- **Which track plays when is Core's** (`MusicTracks`, in `Core/Audio/MusicWhen.cs`, tested in `MusicTests`): the menu
  recordings, `MissionTheme(mission)`, `StartSting`, `BossTheme(playing)`, `BossBeaten`, `FinalSection` and its
  recording, `CodaTheme`, `StingFor(cue)`, `DebriefDelay`, `DebriefLoop`, `GameOverSting`, `RankUpSting`,
  `RenderedAhead`. `MusicFlow` only plays what they say; a new moment's choice goes there with a test (card
  dca-music-tests, 25 Oct). `MissionTheme` returns Steel Night for Mission two on purpose, pinned by
  `EveryMissionPlaysSteelNight_TillMissionTwoHasItsOwnTheme`: writing its theme changes that line.
- **When a mission's music changes mid-play is `Soundtrack`'s rule** (the countdown and back, the glitch-out's cut,
  the win), with tests. Keep a new mid-mission change there, not in a view. (CLAUDE.md: rules live in Core.)
- **Menu music is one rule, `MusicTracks.MenuRecording`**: the title recording on the splash and the settings over it,
  the den's recording in the den (quieter, `Level`; fading in over 3 s, `FadeIn`; dipping to 60% under the kit or
  mission screen, `MenuDip`), none while a den beat plays, and one fades out before the other starts (music.md, the
  den's theme; the owner, 19 and 20 Oct).
- **Recordings are not in the notation.** The title and den themes are WAVs, played by `Music.Loop` from
  `Resources/Music/<id>.bytes`; the orchestra layer for the boss's final section is `Resources/Music/tank-alert-final`.
  A WAV is kept as raw bytes and made into a clip in code (`Wav.Read`), because some Chromium WebGL builds can't play
  compressed audio.
- **Co-op plays the same music on both phones, each following its own screen**, like the effects. Music is not networked.
- **A vendor take reaches the game only when a branch pins it.** List it in the vendor repository's `pin/pins.txt`, let
  its Pin workflow copy it to the `pinned` release, then run `tools/vendor_lock.sh <release> [file]` on the item's branch
  so `vendor.lock` holds its name and SHA-256. CI fetches exactly those (`.github/actions/unity-project/action.yml`),
  and the main line fails rather than ship without a pin.
- **Generation is not this domain's.** Takes (narration, announcer, music recordings) come from the Media generation
  role and the vendor repository's workflows, including the spend and ledger. Ask it. Even narration routed from story
  needs a roadmap card first (Jack, 17 Oct).
- **Words players read** (CLAUDE.md): "tabk", never tank. `TankAlert` is the code name, "Tabk Alert" the track's name.

## The vocabulary
- `MusicTrack` (name, bpm, `Loop`, four parts `Lead`, `Harmony`, `Bass`, `Drums`), `MusicTracks.All`, `MusicTracks.Bar`
  (16 steps; a step is a sixteenth note).
- Loops: `SteelNight` (Mission one, 150 bpm, 32 bars, 51.2 s), `TankAlert` (boss, 172 bpm, 16 bars, 22.3 s),
  `TankAlertFinal` (pilot round, 200 bpm, 16 bars, 19.2 s), `Debrief` (100 bpm, 8 bars, 19.2 s). The stings run 1.3 s
  (`BossVictory`) to 9 s (`ContinueCountdown`, exactly `Tuning.Lives.ContinueSeconds`). Measured from the compiled
  tracks, 25 Oct. Stings: `MissionStart`, `BossVictory`, `MissionComplete`, `RankUp`,
  `GameOver`, `ContinueCountdown`, `NewCrewmate`. Recordings: `TitleRecording` (`title`), `DenRecording` (`den`).
- Notation (`Notation.cs`): `c5:2` a note of 2 steps (lengths stick), `c4+e4+g4` a chord, `r` a rest, drums `k s h c`,
  slides; read by `Notation.Steps` and friends.
- `MusicCue` (`Countdown`, `Resume`, `Cut`, `Complete`), `MusicFlow` (`Prepare`, `Menus`, `MissionStart`, `BossIntro`,
  `BossFightStarted`, `TankWrecked`, `Coda`, `Tick`, `Debrief`, `GameOver`, `RankUp`), `Music` (`Play`, `Loop`, `Stop`,
  `Silence`, `Sting`, `Layer`, `Duck`, `Dip`, `Gain`, `Muted`).
- `Sfx.Say(id)` plays an announcer line from `Resources/Voice`, `Sfx.Narrate(id)` a narrator line from
  `Resources/Narration`. The vendor releases in `vendor.lock`: `announcer`, `narration`, `orchestra`, `start-music`
  (`title.wav`), `den-music` (`den.wav`).
- The vendor scripts (`voice/narration.json`, `voice/lines.json`) spell words the way the voice should say them: "Kye"
  for kie, "Kadge" for kcaj (Jack, through story), "tank" for tabk. The subtitles in `scenes/` keep "kie", "kcaj"
  and "tabk". Checked line by line on 25 Oct: every narrator line in `scenes/` has its script and its recording in the
  pinned zip, and the words agree once those spellings are allowed for. The pinned `announcer` zip holds
  `mission-two-start` and `team-super` too, so Mission two's voice is in. The announcer's `boss-tank` says "Here comes
  the tank!" (spoken only).

## The invariants
- Track names are unique (tracks have no ids: `MusicTests.EveryTrackHasADifferentName`), and a loop renders to exactly
  its length (`ALoopRendersToExactlyItsLength`).
- Gameplay randomness never touches sound. A cosmetic effect uses its own random source (CLAUDE.md invariants).
- A blink, flicker or cycle goes through `Fx` / `Clip`; a sound that follows one plays from the event, not a timer.
- Music rendering must not stall a frame. Today that's kept by rendering ahead: `MusicFlow.Prepare` renders the mission's
  tracks at launch, and the rest render in one go the first time they play (`Music.ClipOf`) and are kept for the session.
  Spreading a render across frames and letting clips go is in music.md's plan, not built.
- Core's rules (`Soundtrack`, and `MusicWhen.cs`: `MenuRecording`, `MissionTheme` and the other moments) are tested without a scene; keep new music rules there.
- Never commit a pinned WAV, an orchestra or a recording into the repository: they are fetched by hash in CI.
- A change to the pin file for a take touches `vendor.lock` only through `tools/vendor_lock.sh`.

## How it is checked
`tools/check.sh` (compile, the EditMode tests, the scene check, the docs link check); `tools/check.sh compile` for a
type change. The tests are in `Assets/Tests/EditMode/Audio/`: the notation, the tracks as designed, the loop lengths,
the countdown, the MIDI file, `Soundtrack`'s moments, and `Wav`. Nothing checks what a sound or track sounds like:
listening is by the preview build's Try it link (not a CI-rendered WAV: see Known unfinished). The scene check and `CueTests` hold the cue
side (each cue has an effect). `tools/check_scenes.sh` fetches the narration to check reading time.

## Known unfinished
- **Later missions' music.** `MusicTracks.MissionTheme` returns `SteelNight` for every mission, so Mission two (the
  creamery) reuses Mission one's theme, on purpose and tested. music.md calls for "each later mission's theme, written
  with its map". No card yet; when it's written, `MissionTheme` and its test change, and `RenderedAhead` should say
  whether it renders at launch.
- **The new crewmate sting** (`MusicTracks.NewCrewmate`) is written and tested but nothing plays it. The crew reveal it
  was for has gone; music.md says it waits for the den's. The den's crewmate-moving-in beat is the likely place.
- **The coda's music** is `TankAlert` again (music.md says so, stale-docs 25 Oct), an open pick in music.md (Open questions), along with one boss theme or one
  per boss, Mission one's feel, and who writes the melodies. The owner has not answered.
- **Rendering across frames.** music.md (How the tracks get made, 4) plans renders a few seconds at a time and clips let
  go when no longer needed; neither is built (see the invariants). No card.
- **The chip tracks' listen WAVs.** music.md says each track is rendered to a WAV in CI for listening; nothing in this
  repository does that (the vendor repository may). Check before promising a listen link. How the vendor's Orchestra
  workflow gets `MusicMidi`'s file is not recorded either: only `MusicTests` calls `MusicMidi.Write` here.
- **A track played while muted still renders.** `Music.Play` and `Music.Sting` render through `Music.ClipOf` whether or
  not the music is muted (muting is only the sources' `mute`), so with the music off a mission start still renders
  Steel Night (51 s) in that frame, silently. Turning the music on before then now renders the launch list at the tap
  instead (dca-music-tests), which moves that stall to the settings screen; it doesn't remove it. Skipping renders while
  muted (and rendering on unmute what's playing) is part of the rendering item above. No card.
- **Open findings from the consistency audit (25 Oct), each for the owner:**
  - Game over plays twice with the music on: `Player.Down` plays `Sfx.GameOver` (Tabks' sad trombone) as the soldier
    goes out, and `MusicFlow.GameOver` plays the game over sting as the run ends. The victory jingle is skipped when the
    music is on (`World.cs`, `DebriefView.cs`); game over isn't. Behaviour, gameplay's file.
  - The mission start does the same: `Game.cs` plays `Sfx.Start` and then `MusicFlow.MissionStart` plays the start
    sting. Design call (keep both, or gate `Sfx.Start` on `Music.Muted` like the victory jingle).
  - The style says four voices, with fast arpeggios standing in for chords, but seven tracks hold chords of two to four
    notes in the harmony part (`TankAlert`'s stabs, `Debrief`'s comps, the stings' last chords), and the kick is a
    triangle thump on top of the bass's triangle (`MusicRender.Drums`). Design call: loosen music.md, or rewrite.
  - The pilot round's music is described as the boss music running on in `docs/design/cutscenes/mission-one.md` (When it
    ends) and `docs/design/tabks/bosses.md` (When it's beaten); it is the final section (`TankWrecked`). Handed over.
- New narrator lines: ask the Media generation role for takes; the lines are the story side's to write.
