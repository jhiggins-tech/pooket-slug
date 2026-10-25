# The cutscene domain

## What this domain is
All things cutscene: the loop from a line in the script to a scene on a phone. The script (`docs/design/cutscenes/`),
the scene files that stage it, the reader and the player in Core, the stage and the views that draw it, the cue
sounds, the checks, and the two ways the owner looks at a scene without a build. The boss intro is a show, so it is
here too. The domain owns how a scene plays and the words in it. It does not own what the story is (the story domain
does).

## The files
Exactly what `.claude/hierarchy/OWNERSHIP.txt` lists:
- `Assets/Scripts/Core/Story/` (`SceneScript`, `SceneLibrary`, `Staging`, `Cutscene.cs`, `Cues.cs`)
- `Assets/Scripts/Core/Bosses/BossIntroShow.cs`
- `Assets/Scripts/Ui/Cutscene/` (`CutsceneView` and its parts: backdrops, cast, effects, bubbles, captions, camera,
  `CueSounds`, `CutsceneSnacks`)
- `Assets/Scripts/Stage/` (`StageKit` with `StageFigure`, `StageScreen`)
- `Assets/Scripts/Game/SceneFlow.cs`, `Assets/Scripts/Game/BossIntroFlow.cs`
- `Assets/Tests/EditMode/Story/`, `Assets/Tests/EditMode/Bosses/BossIntroShowTests.cs`
- `scenes/`, `tools/scenecheck/`, `docs/scenes.md`, `docs/design/cutscenes/`

Next door, not here: `Core/Anim/StagePose.cs` (sprites), `MissionStory.cs` and its test `MissionStoryTests.cs`
(story), `Ui/Den/` and `DebriefView` (shell and den, who borrow the stage).

## The rules this domain keeps
- The script leads. A scene's shots, words and timings are decided in `docs/design/cutscenes/` (a page per part, the
  README holds the rules every scene follows), then the scene file is changed to match. A scene file changed without
  the script is an error to catch in review. The script is the only place a cutscene is designed; other docs link to it.
- A scene is a text file in `scenes/`, read by `SceneScript`, staged on `StageKit`. The format is `docs/scenes.md`.
  A change to a line, time or position goes live without a build (see below); a new thing or verb for the stage is a
  build (`Staging.cs` for the vocabulary, `Ui/Cutscene/` for its look).
- Each scene's `net` number is its own and never changes: a partner's phone knows the scene by it
  (`SceneScriptTests.TheScenesKeepTheirNumbersAndShapes`, `SceneCheck.Numbers`). Today: opening 1, motor-pool 2,
  freeing-kie 3, m2-briefing 4, freeing-kcaj 5, kcaj-tabk 6, coop-test 63. `UseSite` refuses a site copy whose number differs.
  Every one is pinned by number in that test, which also pins `SceneLibrary.Names`, so a new scene fails it until its
  number is pinned there too (and a getter added to `TestScenes`). `SceneCheck.Numbers` skips a scene with no `net` line (0).
- Every cue a scene names has an effect in `Cues.cs`, or is a `say:` cue (`CueTests`). A new cue is its id and
  `CueEffect` there, its clip in `CueSounds`, and its sound in `Sfx` / `ChipSounds` (audio's file; ask).
- Every spoken line is up long enough to read (3 words a second); every narrator line has its recording and time to
  play it plus 0.4 s, lengths taken from the clips (`tools/scenecheck`, `CutsceneTests`). A scene with a problem
  does not go live.
- Words players read: "tabk" and "Trollogram" in subtitles, bubbles and captions. The narrator recordings are
  spelled for the voice ("tanks", "Kye", "Kadge") in the vendor repository's `voice/narration.json`; that spelling
  stays out of the scene files. Bubbles are in capitals, a few words each; narrator subtitles in sentence case.
- Hidden ranks are never named in a scene or any text players read.
- A character's intro plays the first time they are on screen in a scene, never anywhere else; a Trollogram copy is a
  character of its own and gets its own (`docs/design/cutscenes/README.md`, Character intros). Intro timings are
  tested (`CutsceneTests`).
- A scene seen before can be skipped; a reveal cannot be the first time. In co-op a shot moves on early only when both
  have tapped, a skip needs both, and the timer still moves it on. Both phones play the same copy or both the
  built-in one (`SceneLibrary.StartCoop`, `SceneScriptTests`). A draft played with `?scene=` is never marked seen.
- No strobing: an alarm is a slow wash, once a second.
- The stage is the cutscene engine. The den and the tabk bay are drawn on it (`StageScreen`), the debrief takes only
  its figures and colours. They may not change it: a new figure or pose for them is a request to this domain, and the
  handover says what they must adapt to.
- Rules that live in a view move into Core with tests the next time the file changes, and a big screen file is split by
  part (CLAUDE.md, "Leave it better"). `CutsceneBackdrops`, `CutsceneEffects`, `CutsceneCast` and `SceneFlow` are the
  large ones.
- Never commit Photon code, secrets or private-repo code. The narrator's WAVs come from the vendor repository, never the
  public one.

## The vocabulary
- Scene names are `SceneLibrary.Names`: `opening`, `motor-pool`, `freeing-kie`, `coop-test`, `m2-briefing`,
  `freeing-kcaj`, `kcaj-tabk`. A scene's profile id is `scene-<name>`.
- Sets (`Staging.Sets`): `street`, `den`, `bay`, `motorpool`, `coldstore`. Things are `Staging.Things`: figures
  (`tones2`, `kie`, `kcaj`, `goon`), props and effects, with their verbs (`show hide at move turn size colour fade
  flicker blink shake on walk face pose look holds sway`). The full tables are in `docs/scenes.md`; the test
  `EveryThingAndVerbInTheVocabularyReads` keeps them honest.
- Speakers: `tones2`, `kie`, `kcaj`, `trollo-kie`, `trollo-kcaj`, `goon`, `narrator`, `caption`.
- Cue ids are in `Cues.cs`; a recording id (`opening-4a`, `m2-freeing-2`) is the vendor repository's, and `Sfx.Narrate`
  plays it. The announcer's lines (`mission-start`, `mission-two-start`) are `say:` cues.
- The stage is 320 by 180 art pixels, things placed by their feet from the bottom left. The bay is 993 wide.
- Boss intro: the full one (siren twice, WARNING!, name card, then a pilot's hatch and band) and the quick one (a
  replay of a cleared mission, or a retry after losing at the boss door: `StoryRecord.QuickBossIntro`),
  `BossIntroShow.Cue`. The pilot's part plays only when the mission's reveal hasn't just played: a test start, or a
  solo fight once the reveal has been seen, until the mission is cleared (`BossIntroFlow.Start`). Today that is
  Trollogram kcaj's TROLLO-KCAJ band, so he gets his intro outside his scene too; the README's Character intros says so.
- Sets: `motorpool` is Mission one's plates with the hangar drawn over them, `coldstore` is Mission two's
  loading-yard plate (doors on the right, x 261); each falls back to a code-drawn set in a build without its plates.
  `den` and `bay` are Design's raid-night paintings, `street` Design's plate B-2.
- Announcer lines in a scene are `say <line>` (`say mission-start`), not `cue`.

## The invariants
- Gameplay never depends on a scene. `Core/Story` knows nothing of Unity's scene or the network; the view and the flow
  draw and play sounds.
- Blinks, flickers and walk cycles on the stage go through `Fx` or a `Clip`, and a figure is posed with
  `StageFigure.Pose`; no inline `(int)(t * hz) % n`.
- The site's copy of a scene takes over only if it reads cleanly, has the built-in copy's `net` number and differs
  from it. Otherwise the built-in copy plays and the reason is logged. The built-in copies are `Resources/Scenes/<name>.txt`, copied from `scenes/` by the
  build (the editor reads `scenes/` itself); the tests read `scenes/` (`TestScenes`).
- `?scene=<name>` only ever fetches a file from the game's own site (`SetupText.IsName`, lower case).
- A scene's shots end by themselves; a tap moves on but not too soon (`CutsceneTests`).
- The two preview mechanisms are the owner's way to look at a scene. Keep both working:
  - `?scene=<name>` plays a draft of `scenes/<name>.txt` with SKIP, its name and any problems over it, then back to
    the title.
  - `.github/workflows/scene-files.yml` runs the scene check and puts `scenes/` up beside the game (the main line's on the
    live game, any other branch's in `previews/<item-id>/`). That workflow file is the architect's, not this domain's.
    A push to `scenes/` is live in a couple of minutes.
- Docs-only pushes do not build, and a push of only `scenes/` does not build either: it deploys scenes only.

## How it is checked
- `tools/check.sh` runs the EditMode tests, the scene check and the docs link check; `tools/check.sh docs` alone for
  the links. The scene check is `tools/scenecheck` (the game's own reader), `tools/check_scenes.sh` with the narrator's
  recordings in CI.
- Tests: `Assets/Tests/EditMode/Story/` (`SceneScriptTests`: reading, staging, the library and co-op copies;
  `CutsceneTests`: the player, intros, skip, co-op taps and the narrator's time; `CueTests`; `TestScenes`) and
  `Assets/Tests/EditMode/Bosses/BossIntroShowTests.cs`. `MissionStoryTests` is the story domain's.
- A scene's look is checked by playing it: push the branch and open `previews/<item-id>/?scene=<name>`, or play it
  from a start (`?test=scene`, `?test=reveal`, `?test=coda`). CI plays `coop-test` with two browsers.
- `docs/scenes.md`'s lists of sets, speakers, cues, things, verbs, poses, looks and colours are held to the code both ways by
  `ScenesDocTests` (`Assets/Tests/EditMode/Story/`). Adding a thing, verb, cue, speaker, set or colour in `Staging`,
  `SceneScript` or `Cues` means adding it to the page in the same change, or that test fails. It reads the page's
  backticked words, so keep each list in its current paragraph or table.

## Known unfinished
- The narrator's lines are placeholders for the owner to rewrite in the crew's voice (README, narrator note).
- kcaj's tabk has its own return beat in the den that is not designed or built (the den's script page has no scene for it).
- A narrator's recording made earlier can run longer after a re-record; the shot then waits up to 3 s, and the scene
  check fails the line if the clip no longer fits.
- The retired scene "two of the crew" stays in `docs/design/cutscenes/retired.md` for its words; no file plays it.
- Other scenes named in the story are undecided: Mission three's briefing and freeing wait on the story domain.
- `CutsceneBackdrops`, `CutsceneEffects`, `CutsceneCast` and `SceneFlow` do several jobs each; split them by part the
  next time they change.
- Script and scene file differ in small ways the script's Built notes record (the opening's shot 8 has no
  searchlights and leaves by the side door). The scene files' own `//` comments were left alone in stale-docs: a
  change to a scene file is a deploy and a new version for co-op.
- For the owner: the TROLLO-KCAJ band in later solo fights breaks "an intro never plays anywhere else". The docs now
  say what the code does; whether the code should change is a design call.
- Handed over in stale-docs (25 Oct), not this domain's: `MissionStory.Opening` says "before a solo start" but
  `m2-briefing` plays in co-op too (story); `docs/design/tanks.md` is an index now, yet comments still point to
  sections there (`MissionStory.QuickBossIntro`, `World.cs`, `MusicFlow.cs`, `TankBoss.cs`, the crew's tabk files).
