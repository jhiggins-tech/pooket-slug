# The protocols: cards, handovers and briefings

How work moves between the layers: what a card is, how it changes state, how one domain manager hands to another,
and what a briefing says.

**Read this when** you're opening a card, moving a card, handing work over, or writing a briefing.

Up: [the platform architecture](../SKILL.md) · [the domain map](domains.md) · [the roles](roles.md)

## The card

A card is one item of work. Its record is a file: `.claude/hierarchy/cards/<id>.md`. The Roadmap is the human board
over the same thing — the owner works from the Roadmap, the agents work from the cards, and the two are kept in step
by the roadmap PM.

```
# <id> — <title>

**Domain:** den
**State:** in-progress
**Owner:** den-manager
**Branch:** claude/den-loft-layout
**Depends on:** —
**Blocks:** den-art-loft

**Done when:** the loft is walkable end to end, the ladder reaches the hatch, the house map is redrawn, and
`tools/check.sh` is green.

**Note:** the loft's width is 382 art pixels, and the den stays one screen wide.

## Log

- 2026-10-21 — claimed. Read the briefing; the loft's floor is `DenMap`'s, and `LoftH` is the thing to change.
- 2026-10-21 — workhorse on the ladder. Branch pushed, CI green.
```

The **Note** is the running log. Every domain manager appends to it as it goes, so the card is readable by someone
who was not there — which, since no agent is ever there twice, is everybody.

## The states

| State | Set by | Means |
|---|---|---|
| `open` | the roadmap PM | queued, no owner |
| `claimed` | a domain manager | taken; the briefing has been read |
| `in-progress` | a domain manager | coders are working |
| `in-review` | a domain manager | pushed; the diff is being read |
| `done` | the roadmap PM | merged into the main line |
| `blocked` | anyone | needs a decision or a handover |

**A card is never left `in-progress` across a session.** If a session ends with a card in progress, the card goes to
`blocked` with a note saying where it got to. An agent that dies mid-task leaves a card that says so; a card that
quietly stays `in-progress` is a lie the next agent will believe.

## The handover

A handover is the note one domain manager leaves for the next. It is the only thing that crosses a boundary, and it
is on disk, so it survives the agent that wrote it.

```
## Handover: gameplay → sprites

**Branch:** claude/mission-two-rifleman
**Commit:** abc1234
**What changed:** the rifleman's `EnemyDef` row (id 41) and its `SpawnKind`; the map character `r` in
`Level.Markers`; the `Spawner` row.

**What I assumed:** the rifleman reuses the runner's legs, so it needs no new leg frames — **check this**: if the
sprites domain decides it needs its own, the `gameplay` domain's `Enemies.cs` has to pick the right clip.

**What is left:** the frames themselves. The rifleman needs a standing, a crouch, a walk of 4 frames and a death.
The body is 24x32 like the runner's.

**What the next owner must check:** the new id is 41 and must never be reused; `ContentTests` pins the ids.
```

Four things, always: **what changed**, **what I assumed**, **what is left**, **what the next owner must check**. The
third is the work; the fourth is what stops the next owner from trusting the first three.

The receiving domain manager **checks the handover against the code** before continuing. A handover is a claim, not
a truth.

## The briefing

Each domain has one: `.claude/hierarchy/domains/<domain>/BRIEFING.md`. It is the domain's memory, and the domain
manager maintains it.

A briefing is not a design document and not a status report. It is what the next agent needs to do the next piece of
work in this domain without asking a human.

```
# The den domain

## What this domain is
The den: its design and art, the room layout, the unlock and progression, and moving around it.

## The files
(ask the architect; the map is the authority)

## The rules this domain keeps
- The den is one screen wide; the loft is a short walk up, not a new screen.
- Walking to a station is the menu. (lobby.md)
- A room behind a tarp stays tarped until the crewmate who brings them moves in.
- The house map (`docs/design/lobby/house.py`) is the canonical layout: when a room moves in the game, redraw it.

## The vocabulary
The places are `DenPlace` (couch, board, gym, trophies, bunks, showers, laundry, wintergarden, vending, side door,
roller door, rooftop). The floors are `DenFloor` (ground, upper, loft, bunks). Do not rename them.

## The invariants
- The den is drawn on the cutscene domain's stage. Do not change `Stage/`; ask.
- The ladder's x, the floor heights and the soldier's height are in `DenMap`. **A room's doors are meant to be 37 px
  tall** — 1.7 times the 22 px soldier — and the build checks them against it. The den's own painting does not meet
  that yet: its ground-floor doors are taller (`DenMap.Doors`), and the scale check is `[Test, Ignore]` for that
  reason. A new room follows the 37 px rule; the old ground floor is a known exception, not a pattern to copy.

## How it is checked
`tools/check.sh` (the type-check, the EditMode tests, the scene check, the docs link check). The den's own tests are
in `Assets/Tests/EditMode/Lobby/`.

## Known unfinished
- The loft is not in the game yet: the den is still the one-screen room below.
- The couch fist bump is not built.
- The host cannot change the mission from the den.
```

**The story domain's briefing is shaped differently**, because its authority is a rule rather than a file. It says
where the plot is expressed and who owns each place, not only what this domain owns:

```
# The story domain

## What this domain is
The plot: what happens, in what order, and each reveal. Changes to the story go through here.

## The files
`Assets/Scripts/Core/Missions/MissionStory.cs` and `docs/design/story.md`.

## The rules this domain keeps
- The plot of record is `docs/design/story.md`. The plot in code is `MissionStory.cs`. They must say the same thing.
- A change decided in `story.md` is this domain's; a change decided in `docs/design/cutscenes/` is the cutscene
  domain's. A story change that needs a scene is a handover to the cutscene domain.
- The mission order, the roster, the reveals and the scene names are not this domain's, but this domain keeps them
  agreeing with the plot: `Missions.All` (gameplay), `Crew.All` (gameplay), `Profile.Reveals` (shell),
  `SceneLibrary.Names` (cutscene).
- The premise: the crew are missing and only tones2 is left; kie's Trollograms have gone rogue; each mission frees
  one crewmate from their own tabk. Do not change it without the owner.

## The vocabulary
The reveals are the ids in `Profile.Reveals` (`coop`, `tank`). The story beats are `StoryRecord`'s (`coda`, `debrief`,
`super`). A scene is named by its file (`SceneLibrary`).

## The invariants
- No crewmate is freed twice, and each is freed by the mission the plot says.
- A once-only beat stays once-only. The second time you meet something, it is quick.
- Mission one is solo only, and it opens co-op when it is beaten.

## How it is checked
`tools/check.sh`, and `Assets/Tests/EditMode/Missions/MissionStoryTests.cs` for a mission's own story. **The plot's
agreement is not tested yet** — that is this domain's first card.

## Known unfinished
- Mission order after kcaj is an open question in `story.md`.
- Each mission's story after Mission two is undecided: its briefing, and how its crewmate's freeing plays out.
```

### The rule about updating it

**The briefing is updated in the same commit as the change that made it wrong.** Not afterwards, not in a separate
commit. A briefing that lags behind the code is worse than no briefing, because the next agent trusts it.

A change that alters what the next agent needs to know — a new rule, a new file, a changed invariant, a new piece of
vocabulary — updates the briefing in the same commit. A change that does not, leaves it alone.

### The rule about its size

A briefing that has grown past what an agent will read is two briefings. When it does, the domain is two domains, or
the briefing is losing the point. The architect's health check watches for this.

## A domain manager's routine

1. **Read** the card, then the briefing. If they disagree, the briefing is wrong or the card is; work out which
   before doing anything.
2. **Claim** the card, and say in the note what was read.
3. **Plan** the work. If it crosses a boundary, plan the handover as part of the work, not as an interruption to it.
4. **Brief the coders.** Each coder prompt carries the briefing's path, the task, and the rules that apply. A coder
   that has to have seen something before is a briefing that is missing it.
5. **Review** the whole diff when the work comes back. Not the summary — the diff. A consistency failure is
   invisible in a summary.
6. **Check**: `tools/check.sh`, and the CI run.
7. **Update the briefing** in the same commit, and the card's note.
8. **Hand over** or **close**. Either way, the card says where it got to.

## A coder's routine

1. **Read** the briefing, and the part of the card that is about the task.
2. **Do the one thing** asked. Not the thing next to it.
3. **Check** what applies: `tools/check.sh compile` for a type change, `tools/check.sh` for a real one.
4. **Report**: what changed, the files, what you checked, and what you did not do.

A coder that finds itself wanting to change something outside its task does not do it. It reports it, and the domain
manager decides. That is what keeps a scoped change scoped, and it is why the workhorse's brief says so.
