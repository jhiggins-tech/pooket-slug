# The story domain

## What this domain is
The plot: what happens, in what order, and each reveal. A change to the story goes through here, whoever asked for it.
The plot is written in one place and expressed in several others, so this domain owns the plot of record and its code
twin, and keeps the other places agreeing with them. It owns what the story is; the cutscene domain owns how a scene
plays.

## The files
- `Assets/Scripts/Core/Missions/MissionStory.cs`
- `Assets/Tests/EditMode/Missions/MissionStoryTests.cs`
- `Assets/Scripts/Core/Missions/OnceOnly.cs` and `Assets/Tests/EditMode/Missions/OnceOnlyTests.cs` (dca-once-only-keys)
- `docs/design/story.md`

`MissionStory.cs` holds three types: `MissionStory` (a mission's scenes and flags), `StoryRun` (how a mission is being
played: co-op, sandbox, test start) and `StoryRecord` (the once-only beats the profile remembers).

**Where the plot is expressed** (owned by others, kept in agreement by this domain):
| The plot's fact | Where it lives | Owner |
|---|---|---|
| The order of missions, their ids and net ids, each mission's `Story` | `Core/Missions/Missions.cs` (`Missions.All`) | gameplay |
| The roster and who frees whom | `Core/Crew/Crew.cs` (`CrewMember.FreedBy`, `Crew.All`) | gameplay |
| The scene names | `Core/Story/SceneLibrary.cs` (`Names`), the files in `scenes/` | cutscene |
| The reveals | `Reveals` in `Core/Lobby/Profile.cs` (`coop`), and `Profile.MarkSeen` | shell |
| What START plays | `Core/Missions/StartFlow.cs`, reading `MissionStory.BeforeDen` | gameplay |
| Every scene's script and shots | `docs/design/cutscenes/` (a page per part) | cutscene |

## The rules this domain keeps
- The plot of record is `docs/design/story.md`; the plot in code is the `MissionStory` rows in `Missions.cs`. They must say
  the same thing. Nothing tests that yet (see Known unfinished).
- A change decided in `story.md` is this domain's. A change decided in `docs/design/cutscenes/` is the cutscene domain's.
  A story change that needs a new or altered scene is a handover: this domain decides, the cutscene domain writes the
  script and the scene file. A story change that needs a mission change is asked of gameplay.
- Cutscene content lives only in `docs/design/cutscenes/`; `story.md` links to it and does not copy it (the owner's rule,
  19 Oct). A new scene is scripted there first.
- Reader-facing text says "tabk" and "Trollogram". Hidden ranks are never named or described in text players read, and
  `story.md` stays inside that (it names XP and rank only as the first debrief's beat).
- Never scale enemies, bosses or pilots for co-op (`docs/design/levels.md`, Difficulty): a story change must not rest on
  two players changing a mission.
- Slug may deviate from Pooket Tabks; consistency is checked within Slug only.

**Sign-off:** the owner signed `story.md` off on 2026-10-14. The coda and Mission two were written after (2026-10-16) and
approved on their own Roadmap cards: the coda's design card `scene-first-drive` (done and promoted to its build, 16 Oct)
and `m2-story` (17 Oct, "Approved: canon in story.md"). The page's status line says so (fixed by domain-consistency-audit;
the stale-docs pass had called them drafts).

**The premise** (do not change without the owner):
- The crew are missing and only tones2 is left. kie's Trollograms have gone rogue and stolen the crew's tabks, each driven
  by a Trollogram copy of its owner. The game keeps that to itself until the first tabk appears.
- Every mission has one shape: fight through to a crewmate's tabk, beat it, beat its fake pilot on foot, and the real
  crewmate is freed to play from then on.
- The opening names tabks but never shows one (owner, 19 Oct). Tone: arcade action played straight, jokes from the crew,
  little to read.
- Mission one (the military base) is solo, frees kie, and ends in the coda: kie frees tones2's own tabk, you call it in
  and blast out of the base. The night Mission one ended the Trollograms moved every tabk left in the motor pool.
- Mission two (the Brain Freeze creamery) frees kcaj; its cold store is the Trollograms' server room. It is the first
  mission open to co-op.
- kie's tabk, The Projectionist, comes home after Mission one, rebuilt round the projector that made Trollogram kie, and
  opens the bay for players who go looking (owner, 20 Oct). "Two of the crew" is retired (`cutscenes/retired.md`).
- There is no finale for now. The coming-soon crew (shotdownboyz, lankcity, odsey, doctorfox) arrive later, one mission each.

## The vocabulary
- The reveals are the ids in `Reveals`: `coop` is a real reveal; `tank` is only the key of `?unlock=tank` (a test start),
  and play grants the call-in in the coda. Keep the id strings as they are, they are in saves.
- `Reveals` is not the whole list. Every once-only key lives in `Profile.seen`, set from five places: `Reveals.Coop`
  (`Game`), a scene's `scene-<name>` (`SceneScript.IdOf`, marked by `SceneFlow`), `StoryRecord`'s three, `DenBeats`
  (`den-first-visit`, `moved-in-<name>`) and `KieHomecoming.Reveal` (`kie-tabk-home`, marked by `BayFlow`). The plot's
  reveals table in `story.md` names each of these beats (the den rows added by domain-consistency-audit).
- **`OnceOnly.All`** (`Core/Missions/OnceOnly.cs`, dca-once-only-keys) is those five gathered in one list: a key and
  what it is per beat, built from the sources' own constants (scene keys from `SceneLibrary.Names`, move-ins from
  `Crew.All` less `DenBeats.Starter`), so no source changed. This is the list the story check reads. `OnceOnlyTests`
  pins the 14 keys as saved today, holds them unique, and scans `Assets/Scripts` for every `MarkSeen(...)` and
  `Mark(...)` call: each call site is in its `CallSites` table with the keys it can save, so a new place that marks a
  key fails until it is listed. `Reveals.Tank` is not in the list (nothing saves it). A new scene or crewmate adds a
  key, and the pin test asks for it to be pinned.
- The once-only beats are `StoryRecord`'s keys: `coda`, `debrief`, `super`. They are in players' saves and never change.
  A scene's own once-only mark is `SceneScript.IdOf(scene)`, tested by `StoryRecord.Due`.
- A mission's story fields: `Opening` (played as the mission starts, solo or, from the den, co-op), `BossReveal` (the boss's entrance, once ever, solo) with
  `BossRevealCoop` (every co-op run), `PilotIntro` and `PilotIntroLine` (in a full boss intro the reveal didn't play before: solo once the reveal's been seen until cleared, or a test start), `PilotFreed` with `PilotFreedCoop`, `Coda`,
  `SoloOnly`, `OpensCoop`, `BeforeDen`. `CallIns` is derived: none where there is a coda, else one.
- "Coda", "the motor pool reveal", "the opening" and "freeing kie/kcaj" are the plot's names for beats; they are headings in
  `docs/design/cutscenes/`.

## The invariants
- No crewmate is freed twice, and each is freed by the mission the plot says (`CrewMember.FreedBy`).
- A once-only beat stays once-only; the second time you meet something it is quick. Two rules carry that: a tabk whose
  reveal has been seen gets only its boss intro (`PlaysBossReveal`, with `StoryRecord.Due`), and the boss intro itself is
  the quick one on a replay of a cleared mission or a retry at the boss door (`StoryRecord.QuickBossIntro`).
- Mission one is solo only and beating it solo opens co-op (`OpensCoop`, `Profile` sets `coop`). Until it is beaten, START
  plays the opening and Mission one, and its debrief goes on into the den's first visit (`BeforeDen`, `StartFlow`). After
  it, START and every debrief go to the den. Mission two's briefing plays as it starts from the den, solo or co-op.
- A boss reveal never plays in a sandbox, and a test start skips it unless it is `?test=reveal` (`PlaysBossReveal`,
  covered by `MissionStoryTests`). Without `PilotFreedCoop` a co-op pilot goes the goons' way and the mission is won at once.
- The game asks the mission's script and never knows Mission one by name; a new mission is a new `MissionStory`.
- A scene named by a mission must exist in `SceneLibrary.Names` and as a file in `scenes/`. The scene check
  (`tools/check_scenes.sh`) covers the files; `coop-test` is in the library but is a test scene, not part of the plot.
- The super shows once it has charged, or Mission one is beaten, or co-op is open (`StoryRecord.ShowsSuper`).

## How it is checked
`tools/check.sh` (type-check, EditMode tests, scene check, docs link check; `tools/check.sh docs` for the links alone). Tests:
`MissionStoryTests` (Mission one's scenes and coda, every mission's scenes against `SceneLibrary.Names` (`PilotIntro` is a title card, not a scene), all three save keys, a mission with no story, the once-only beats, the quick boss intro, the
super, the boss reveal and the pilot freed rules). `OnceOnlyTests` (the once-only keys: listed, saved, unique, pinned). Nearby: `StartFlowTests` and the other Missions tests; `CodaTests` covers the coda
(its summary cites `story.md`, but nothing reads the document). Ownership of these files is held by `tools/check_ownership.py`.

**Doing a story change:**
1. Read `story.md` and the cutscene page for the part. Decide the change in `story.md` first.
2. List the places in the table above it touches. Ask the owning domain for each; do not edit their files.
3. Hand a scene change to the cutscene domain with the plot decision stated; narrator lines then go to audio.
4. Keep `docs/design/` links relative and run `tools/check.sh docs`. Update this briefing in the same commit.

## Known unfinished
- Skits (skit-scripts, 25 Oct): eight in `docs/design/cutscenes/skits.md`. The owner picked Two of us now (the den's
  co-op button first appears at its end, renamed MULTI-PLAYER: a build card), Keep it down (Mission one's coda,
  replays), A Trollogram of anything (his rework of Probably a Trollogram, after kcaj moves in), They're tracking us
  (Mission two's first debrief; kcaj on the debrief is new work) and Does it do cones? (the bay; it waits on kcaj's tabk
  homecoming, which isn't written). Three are drafts to pick from (Five-second rule, Wanna race?, Special delivery), and
  the pitch's other two are on the card's page. Two of us now changes when the co-op
  button first shows (after the skit, not from the first visit's start); the `coop` reveal keeps its meaning (the button
  first pressed). They are not plot beats, but once built each is a once-only key (proposed `skit-<name>`), so
  `OnceOnly.All` and the reveals table's rule for keys that are not plot beats will need them. The snacks are settled:
  canon from the owner (25 Oct), in `story.md`'s Lore, that the motor pool bag came from Family Mart (noted in
  `cutscenes/mission-one.md`; Five-second rule jokes about it). One draft settles another gap until the owner picks: the
  cargo drone brings kie home after Mission one (Special delivery).
- What kcaj's sticky note on tones2's back says (the end of freeing kcaj) is still open: the skit that answered it was
  reworked by the owner (25 Oct) and no longer does.
- From domain-consistency-audit (25 Oct), left for the owner:
  - Mission two's run in `story.md` names two beats that aren't built: a goon frozen on a conveyor, and the scoop dropper's
    one-time gag (the droppers are a repeating hazard, `Creamery.ScoopCycle`). The page says so under Built so far. Build
    them (gameplay) or cut them from the plot (owner).
  - `MissionStoryTests` checks Mission one's scenes are in `SceneLibrary.Names` but no other mission's, and pins the
    `coda` and `debrief` keys but not `super`. Both belong in the consistency check card, or a small test card.
- The consistency check does not exist. It should read the plot out of the code and check it against `story.md`: the
  mission order and the id and net id rule; every scene a story names is in `SceneLibrary.Names` and reachable; each crewmate
  freed once, by the mission the plot says; every reveal in the plot exists and the reverse (the game's side is
  `OnceOnly.All`; the reveals table does not yet name `super`, the coda's prompts or `coop-test`, a test scene, so the
  check needs a rule for keys that are not plot beats); once-only beats marked in both.
  Nothing checks the mission order, roster or reveals against the document. This is the domain's first card.
- Mission order after kcaj is open (`story.md`, Open questions; with Design).
- Each mission's story after Mission two (its briefing, how its crewmate's freeing plays out) is undecided.
- kcaj's tabk coming home needs its own den beat and card; kie's is built (`cutscenes/den.md`, `Core/Lobby/DenBeats`).
- `wintergarden-chats` is a Roadmap idea (owned by Design); this domain would write its conversations if it is queued.
- Narrator audio is spelled phonetically in the vendor repository (kie, kcaj and tabks are respelled so the voice says them
  right); that is the audio domain's, but a story change that adds a name must tell it.
- Roadmap item pages start with a link back to the Roadmap (https://claude.ai/artifact/1AWnQDGtUmiKMnjPgcbru9).
