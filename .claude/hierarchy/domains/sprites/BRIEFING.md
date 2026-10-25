# The sprites domain

## What this domain is
The art, and the frames and clips that step through it: the hand-drawn ASCII pixel art, the sprite builder, the shared
crew figure's frames and clips, the effects' beats, the stage poses, the painted background plates and the title logo's
files. It makes sure every entity that draws a figure has a frame for every state it can be in, at the right size,
with the right outline. It does not write a goon's brain, a kit's firing or a slot's behaviour: that is the gameplay
domain. A new goon is two cards, the frames (here) and then the brain (gameplay).

## The files
The map (`.claude/hierarchy/OWNERSHIP.txt`) is the authority. The domain owns exactly:
- `Assets/Scripts/Art/` (`Art.cs`, `ArtData.cs`, `CrewArt.cs`)
- `Assets/Scripts/Core/Anim/Clip.cs`
- `Assets/Scripts/Core/Anim/Fx.cs`
- `Assets/Scripts/Core/Anim/CrewFigure.cs`
- `Assets/Scripts/Core/Anim/StagePose.cs`
- `Assets/Resources/Plates/` (a folder per mission id, `mission-1`, `mission-2`)
- `Assets/Resources/Title/` (`logo.bytes`, `parts*.bytes`, `layout.txt`)
- `Assets/Tests/EditMode/Anim/ClipTests.cs`, `CrewFigureTests.cs`, `StagePoseTests.cs`
- `Assets/Tests/EditMode/Title/` (`TitleMotionTests.cs`)
- `docs/design/art.md` (the models, the look and the sprite workflow)

What each is: `ArtData` is every frame as rows of characters (the main file); `Art` turns rows into sprites at startup,
holds the palette and the white-flash and Trollogram swaps (`Art.Flash`, `Art.Holo`); `CrewArt` joins each crewmate's
head, the shared body and legs into a `PlayerSkin`; `Clip` and `Clips` are frames at a rate; `Fx` and `Pulse` are the
blinks and flickers; `CrewFigure` is the shared figure's moves; `StagePose` is how a stage soldier stands.

## The rules this domain keeps
- Sprites are hand-made in `ArtData` and stay that way: image models cannot draw a usable sprite or sheet at the
  game's size (`docs/design/art.md`, "Sprites"). Do not generate a sprite on your own.
- Generated sprite animation is only for a sprite or move the owner has promoted on the Roadmap, because hand-made art
  is failing at it. The steps are in `docs/design/art.md`: a video model animates a hand-drawn sprite, the frames are
  cut back to game pixels, and a person tidies them before they go in `ArtData`. This domain runs that workflow; it
  never decides which sprite to generate.
- Image generation runs in the private vendor repository's CI, not in a cloud session, and it costs money. Ask for it
  only on a queued card the owner has approved, and say so on the card. Which model is current comes from
  `docs/design/art.md`: since 21 Oct it is Qwen Image 2.1 Turbo for every image, new plates and edits alike (it
  replaced Seedream). Do not copy figures or API details into this file; read them there.
  Room plates are made by the blocks recipe (`tools/room_ref.py --blocks`, the den's and loft's plates as style guides; the
  kitchen alone used the older two-reference recipe), and the image endpoint is `POST /api/v1/images`, all in `art.md`
  (fixed to match the vendor scripts on card dca-art-recipe).
- Plates are made from the style line and the plate's own prompt alone, then cut to game size and 16 colours, and
  checked in the game, not only on their own (`docs/design/art.md`, "Background plates"). To change one part of a
  picked painting, edit it with a red mark and paste the answer back through a mask, so the rest stays as picked.
- A frame that is missing is a bug in this domain. Every `CrewMove`, `CrewLegs` and `CrewBody` value has art, every
  body frame places the head, and a crewmate's head frame they have not drawn falls back to their forward one
  (`CrewLook.Head`). `CrewFigureTests.EveryMoveHasItsClipAndEveryBodyFramePlacesTheHead` holds the first two.
- A blink, flicker or walk cycle goes through `Fx` or a `Clip`, never an inline `(int)(t * hz) % n`
  (`CLAUDE.md`, `ClipTests`, `CrewFigureTests.TheLastInlineBlinksKeepTheirBeat`). A new rate is a named `Pulse` in `Fx`
  or a `Clip` in `Clips`, so the Trollogram flickers the same on the boss, in a cutscene and on the HUD.
- A figure on a stage is posed with `StageFigure.Pose`, from a `StagePose` (`StagePoseTests`). The poses' names are the
  cutscene scripts' words, so renaming one breaks scene files (`StagePoseTests.CutscenePosesKeepTheirNames`).
- A new crewmate gets every shared move by giving a `CrewLook` in `CrewArt`; their own body or legs frame is an
  override, not a fork (`CrewArt.cs` header, "Add a crew member" in `CLAUDE.md`).
- Anything that flashes a large part of the screen is at most 2.5 times a second; small sprites may blink faster
  (`docs/design/README.md`, "No strobing").
- The hidden ranks are never named or described in art text players read (`CLAUDE.md`).
- Words in anything players read, including art and plate captions: "tabk", never tank, and "Trollogram", never
  hologram. Code identifiers keep their names (`Art.Tank`, `Art.Holo`, `HoloLook`).

## The vocabulary
- The canvas: crew parts sit on a shared 24x32 canvas so torso and legs line up when stacked. The game draws at 16
  pixels a unit (`Tuning.Pixels.PerUnit`, `Art.PPU`). A 1 px dark outline (`Art.OutlineColor`) is added automatically
  around opaque pixels, and `.` is transparent in a row.
- The crew's figure: `CrewMove` (Idle, Run, Jump, Crouch, Down, Repair), `CrewLegs` (Idle, Run1 to Run4, Jump, Crouch,
  Sit), `CrewBody` (Forward, Up, Down, UpDiag, DownDiag, Unarmed, Pad, Lounge, Repair1 to Repair4) and `CrewHead`
  (Front, Down). `HeadPlace` says where a body frame wants the head. Do not rename or reorder them: `PlayerSkin` indexes
  its arrays by the enum value.
- The stage's: `StageLegs` (Idle, Walk, Crouch, Sit, Jump, Hidden) and `StageTorso` (Forward, Unarmed, Lounge, Pad,
  Repair).
- Plates: a layer is `sky`, `far` or `mid`, back to front, and a plate is `<layer>-<n>.bytes`, a PNG renamed
  `.bytes`, numbered along the map (`Backdrop.PlatePath`). The strip repeats end to end; the sky is 180 px tall. The
  mission's `PlatesStand` lifts the far and mid plates. A layer with no plates is drawn in code.
- Named rates: `Clips.SoldierRun`, `RiflemanRun`, `RunnerRun`, `CreameryRun`, `StageWalk`; the `Fx` pulses
  (`TrolloFlicker`, `HurtBlink`, `DeathFlicker`, `Rotor` and the rest) each keep the rate their site always had.
- A sprite is keyed by its definition's stable id: pickups by `PickupDef.Id`, projectiles by `ProjectileDef.Id`
  (`Art.PickupSprite`, `Art.ProjectileSprite`). Those ids ride the network and never change.

## The invariants
- The cutscenes' motor pool set is painted from Mission one's `sky-0`, `far-2` and `mid-6`
  (`CutsceneBackdrops.PaintedMotorPool`, the cutscene domain's): renumbering or recutting those plates changes the
  set, so it is a handover to the cutscene domain. Without them the set falls back to a drawn one.
- `Art.BuildBackground` (the steel night base) is no longer Mission one's look: it is the fallback for any layer a
  mission has no plates for.
- Sprites are built in code at startup (`Art.Init`); there are no imported art assets, no scenes and no prefabs.
  Plates and the title logo are the only picture files, loaded as `.bytes` from `Resources`.
- `Art` is a `partial class`: per-crewmate, per-mission and per-tabk art files add to it from other domains' folders
  (`KcajArt`, `Tones2Art`, `KieTabkArt`, `KcajTabkArt`, `CreameryArt`, `CodaArt`, `ExitGateArt`). Those files belong to
  gameplay by the map. A change that adds frames there is a handover: this domain draws, gameplay wires.
- The title logo's parts file and the pictures must agree: `TitleMotionTests.TheBuildsParts_AreTenLettersAndTheTabk_InsideTheLogoAndTheSheet`
  reads `Resources/Title/layout.txt` and fails if they drift. `tools/title_logo.py` writes both.
- The den's paintings (`Assets/Resources/Den/`) are the den domain's. This domain makes the art the owner picks; the
  den decides which and where. A painting is the handover point.
- Gameplay randomness never touches art; cosmetic effects use their own `Rng`.
- A new `.cs` needs a `.meta` (and a new folder too): `tools/new_meta.py`. A plate or logo file needs its `.meta`.
- Ask the gameplay domain what a figure does with its frames; the cutscene domain for `Stage/` and `StageFigure`; the den
  domain for `Resources/Den/`; the shell domain for the title's code; the architect for the map.

## How it is checked
`tools/check.sh` (type-check as for WebGL, EditMode tests, scene check, docs link check), or `tools/check.sh compile`
for a type change. The domain's tests are `Assets/Tests/EditMode/Anim/` and `Assets/Tests/EditMode/Title/`. A change to
a sprite is judged by looking at it: push the branch and use its preview build, or the card's page for art that does
not have to run in game. Nothing tests the art itself (see below).

## Known unfinished
- The frame contract has no test. Nothing in the repository checks which entities have which frames, or that nothing
  draws a missing one; `CrewFigureTests` covers the crew's figure only. This is the domain's first card.
- Plates: Mission one has one sky, five far and ten mid plates. `far-4` is a second coda plate beside `far-3` (the same size, not the same pixels),
  added when the repeats were re-spaced (commit 9559233, 19 Oct). Mission two has one sky, one far and five mid plates,
  stood 2 units up (`PlatesStand`). The perimeter-to-yard seam idea (options and a pick of a gatepost) was put to the
  owner and its decision is not recorded anywhere in the repository.
- Generated sprite animation: the workflow exists on paper and in the vendor repository, and no promoted sprite has
  gone through it yet that the notes record. The first one needs an owner promotion on the Roadmap.
- Per-crewmate heads exist for tones2, kie and kcaj; the repair frames and bowed head were approved on 20 Oct. A
  co-op second-player marking was ruled out; crewmates are unique and tabks one each.
- `ArtData.CrouchDrop` and `CrewFigure.CrouchDrop` are the same 6 px, kept twice; a code card could make one
  read the other (not a doc fix, so left alone on stale-docs).
- The title logo and its animation are built. Its parts file, `Core/Title/TitleLogo.cs` and `TitleMotion.cs`, and the
  views belong to the shell domain; only the pictures and their tests are here.
