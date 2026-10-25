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
- `Assets/Tests/Gameplay/ArtTests.cs` (the frame contract for what is drawn by id or by frame: a PlayMode test, CI only)
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
  (`CrewLook.Head`). `CrewFigureTests.EveryMoveHasItsClipAndEveryBodyFramePlacesTheHead` holds the first two;
  `ArtTests.EveryBodyAndLegsFrameHasArtForEveryone` holds every skin (the trooper's and each crewmate's) to a sprite
  for every `CrewBody` and `CrewLegs` value.
- Every projectile and pickup in `Content` has a sprite of its own in `Art`'s tables (`ArtTests`, through
  `Art.PaintedProjectile` and `Art.PaintedPickup`). A def with none still draws: a projectile borrows `Art.Shell`, a
  pickup `Art.CrateHeavy`, so a shot never flies invisible, but the test fails until it is given one. The tabk's own
  shell and mortar shots (ids 9 and 10, fired by the unused weapons 18 and 19) draw the shell (card
  dca-tabk-shot-sprites).
- A blink, flicker or walk cycle goes through `Fx` or a `Clip`, never an inline `(int)(t * hz) % n`
  (`CLAUDE.md`, `ClipTests`, `CrewFigureTests.TheLastInlineBlinksKeepTheirBeat`). A rate that more than one site
  shares is a named `Pulse` in `Fx` or a `Clip` in `Clips`, so the Trollogram flickers the same on the boss, in a
  cutscene and on the HUD; a site with a rate of its own calls `Fx.Blink`, `Fx.Step` or `Fx.Shiver` with its number
  (about 30 sites do, checked 25 Oct).
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
  Sit), `CrewBody` (Forward, Up, Down, UpDiag, DownDiag, Unarmed, Pad, Lounge, Repair1 to Repair4, Carry) and `CrewHead`
  (Front, Down). `HeadPlace` says where a body frame wants the head. Do not rename or reorder them: `PlayerSkin` indexes
  its arrays by the enum value.
- The stage's: `StageLegs` (Idle, Walk, Crouch, Sit, Jump, Hidden) and `StageTorso` (Forward, Unarmed, Lounge, Pad,
  Repair, Carry), and `StagePose.Tilt`, degrees toppled onto the back. The scene poses are `idle run crouch spray hatch repair
  down` (`StagePose.Cutscene`; the list the reader checks is `Staging.Poses`, the cutscene domain's). `down`
  (`StagePose.Down`, cutscene-polish, 25 Oct) is the crew's down frame (`CrewFigure.Down`: Jump legs, the forward body)
  toppled flat, `Tilt` 90; no art of its own. `CutsceneSoldiers` turns the figure by `Tilt` times its facing about its
  feet, as the game turns a downed soldier, so a scene stands it 7 up to lie on the floor (freeing kie's drop).
  (`StagePoseTests.Down_IsTheCrewsDownFrame_ToppledFlat`)
- No gun in a cutscene (stage-no-gun, 25 Oct): a scene's `arms gun down out` picks the top whatever the pose
  (`StagePose.Arms`, and `StagePose.Cutscene`'s `arms`): `gun` is the forward body (the default, each shot starts with
  it), `down` the den's `Unarmed` body, `out` the new `CrewBody.Carry` (`ArtData.TorsoCarry`: the forward body with the
  gun taken out, the front hand held out as on the pad frame and the back hand by their side, head `Front`). Shared, so
  it serves every crewmate. A `spray` keeps the gun and a `repair` its wrench. A held prop sits in the `out` hand at
  about `8 10` (`-8 10` facing left). (`StagePoseTests.Arms_TakeTheGunOutOfTheirHands_WhateverThePose`; the render is
  the card's `frame.png`.) The soldier in play never uses `Carry`.
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
- The cutscenes' own props drawn here sit under "The cutscenes' props" in `ArtData` and `Art`: today the tub stack,
  `Art.TubStack` (two frames, 23 by 25 with the outline: at rest, and its top tub hopped with the lid jumped off; painted
  by `Art.TubPaint`, the creamery's pastel lids). The cutscene domain places it and picks the frame (`ShotStage.Rattle`;
  card tub-stack, 25 Oct). A sprite there that a scene draws is a handover to that domain for its thing and verb.
- The per-crewmate, per-mission and per-tabk art files are their own static classes in other domains' folders
  (`KcajArt`, `Tones2Art`, `KieTabkArt`, `KcajTabkArt`, `CreameryArt`, `CodaArt`, `ExitGateArt`), which `Art` reads
  (`Art.Init` takes `KcajArt.Cone`, `CreameryArt.Shockwave` and `.Scoop` for projectiles). `Art` is a `partial class`,
  but only `CrewArt.cs` adds to it (checked 25 Oct). Those files belong to gameplay by the map. A change that adds
  frames there is a handover: this domain draws, gameplay wires.
- The title logo's parts file and the pictures must agree: `TitleMotionTests.TheBuildsParts_AreTenLettersAndTheTabk_InsideTheLogoAndTheSheet`
  reads `Resources/Title/layout.txt` and fails if they drift. `tools/title_logo.py` writes both.
- The den's paintings (`Assets/Resources/Den/`) are the den domain's. This domain makes the art the owner picks; the
  den decides which and where. A painting is the handover point.
- Gameplay randomness never touches art: `Art` scatters its texture with `System.Random` on fixed seeds, so every
  build draws the same pixels.
- A new `.cs` needs a `.meta` (and a new folder too): `tools/new_meta.py`. A plate or logo file needs its `.meta`.
- Ask the gameplay domain what a figure does with its frames; the cutscene domain for `Stage/` and `StageFigure`; the den
  domain for `Resources/Den/`; the shell domain for the title's code; the architect for the map.

## How it is checked
`tools/check.sh` (type-check as for WebGL, EditMode tests, scene check, docs link check), or `tools/check.sh compile`
for a type change. It skips the PlayMode assembly (`PooketSlug.Game.Tests`: `UNITY_INCLUDE_TESTS` is not defined
there), so `ArtTests` compiles and runs only in CI; to type-check it locally, call csc on `Assets/Tests/Gameplay/*.cs`
with `PooketSlug.Game.refs.txt`, `PooketSlug.Game.dll` and NUnit from check.sh's output folder. The domain's tests are `Assets/Tests/EditMode/Anim/` and `Assets/Tests/EditMode/Title/`. A change to
a sprite is judged by looking at it: push the branch and use its preview build, or the card's page for art that does
not have to run in game. Tests check that frames exist, not how they look (see below).

## Known unfinished
- The frame contract is only partly tested. `ArtTests` (PlayMode) covers projectiles, pickups and the crew's skins, and
  `CrewFigureTests` the crew's moves; nothing yet checks a goon's, a boss's or a set piece's frames for every state it
  can be in.
- Plates: Mission one has one sky, five far and ten mid plates. `far-4` is a second coda plate beside `far-3` (the same size, not the same pixels),
  added when the repeats were re-spaced (commit 9559233, 19 Oct). Mission two has one sky, one far and five mid plates,
  stood 2 units up (`PlatesStand`); its `mid-0` (836 px) and `mid-4` (800 px) are wholly transparent, gaps in the
  strip, not missing art. Plates are 16 colours, except Mission one's sky (24, set in the vendor's
  `art/m1-plates.json` for its gradient). The perimeter-to-yard seam idea (options and a pick of a gatepost) was put to the
  owner and its decision is not recorded anywhere in the repository.
- Generated sprite animation: the workflow exists on paper and in the vendor repository, and no promoted sprite has
  gone through it yet that the notes record. The first one needs an owner promotion on the Roadmap.
- Per-crewmate heads exist for tones2, kie and kcaj; the repair frames and bowed head were approved on 20 Oct. A
  co-op second-player marking was ruled out; crewmates are unique and tabks one each.
- The title logo and its animation are built. Its parts file, `Core/Title/TitleLogo.cs` and `TitleMotion.cs`, and the
  views belong to the shell domain; only the pictures and their tests are here.
- Untested art tables (domain-consistency-audit, 25 Oct): `CrewArt`'s `s_Bodies`, `s_BodyTops` and `s_Legs` must
  have a row per `CrewBody` and `CrewLegs` value, and `Art.Init`'s `s_Projectiles` and `s_Pickups` a sprite per
  content id; nothing checks either. A Gameplay (PlayMode) test after `Art.Init` could hold both: the frame contract's
  first piece.
- Two tabk projectiles have no sprite (domain-consistency-audit, 25 Oct): `Content.PlayerTankShellShot` (id 9) and
  `PlayerTankMortarShot` (id 10) get no entry in `Art.Init`, so `Art.ProjectileSprite` returns null for them and a
  `Projectile` launched with either would draw nothing. Its comment says ids past the table borrow the shell's;
  ids inside it with no entry do not. Nothing fires them today (their weapons, `PlayerTankCannon` and
  `PlayerTankMortar`, ids 18 and 19, are only in `Content.Weapons` and a test). An Idea card for the owner.
- `docs/design/art.md`, "The house's room plates", says the image endpoint is `POST /api/v1/images/generations` and
  finds Turbo with `GET /api/v1/models/qwen/qwen-image-2.1-turbo/endpoints`; its "Editing a picked painting" and the
  vendor's code and `docs/openrouter-media.md` say `POST /api/v1/images` and
  `GET /api/v1/images/models/{author}/{slug}/endpoints`. OpenRouter cannot be reached from a cloud session to settle
  it, so the line is left as the kitchen card wrote it (domain-consistency-audit, 25 Oct).
- Two inline blinks are left outside `Fx`, in other domains' files (handed over 25 Oct): `Core/Story/Staging.cs`
  (`Lit`, a scene's `blink`: `Fx.Step(now, n0, n1) == 0`) and `Core/Crew/KieTuning.cs` (`Lit`: `Fx.Blink(seconds,
  Flashes)`). The same numbers, so no beat changes.
