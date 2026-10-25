---
name: platform-arch
description: The spec for Pooket Slug's agent hierarchy — the platform architect, the roadmap/project manager, the domain managers and their three coder agents. Use this whenever you are working on the hierarchy itself: adding or changing an agent definition, cutting a domain boundary, handing a task between domains, writing or moving a task card, or checking that the agent system is in good working order. Also use it before spawning a subagent for game work, so you know which role owns the work and how the layers are meant to nest.
when_to_use: "Setting up the agent hierarchy, changing an agent definition or a domain boundary, handing work between domains, tracking task cards, or auditing the agent system."
---

# Platform architecture: the agent hierarchy

**Status:** a spec, written 2026-10-21. Nothing here is built yet; the agent definitions, the briefings and the card
store are the work this describes.

**Read this when** you're changing the hierarchy, cutting a domain, moving a card, or auditing the agent system. If you
only want to know who owns a piece of the game, read [the domain map](references/domains.md) and stop there.

Up: [the repository's working rules](../../../CLAUDE.md) · [the design](../../../docs/design/README.md)

## The shape

Three layers, plus the owner above them.

```
the owner ── the Roadmap ──▶ platform architect (L0) ─┬─▶ roadmap PM (L0)   cards, the queue
                                                        │
                                                        └─▶ domain manager (L1) ─┬─▶ coder workhorse (L2, sonnet)
                                                             (one per domain)   ├─▶ coder scout     (L2, haiku)
                                                                                 └─▶ coder deep      (L2, opus)
```

| Layer | Who | How many | Model | Effort | Lives for |
|---|---|---|---|---|---|
| L0 | platform architect | 1 | opus | high | the whole system: the definitions, the domain map, the health checks |
| L0 | roadmap PM | 1 | sonnet | medium | the cards: what is queued, what is claimed, what is done |
| L1 | domain manager | one per domain | opus | medium | one card, or a few at a time, inside its domain |
| L2 | coder workhorse | 3 per domain manager | sonnet | low | one scoped change |
| L2 | coder scout | 3 per domain manager | haiku | low | one scoped question |
| L2 | coder deep | 3 per domain manager | opus | high | one hard piece of thinking |

The exact definitions, with the frontmatter to create them, are in [the roles](references/roles.md).

## The central idea: the briefing is the memory

**Every agent is ephemeral.** A domain manager is spawned for a card and dies when the card is done. It holds no
context for the next card, and no agent's context survives the session. So the durable state of a domain is a file,
and the file is the brief.

Each domain has a briefing: `.claude/hierarchy/domains/<domain>/BRIEFING.md`. It is the domain's shared starting
instructions, and **the domain manager maintains it**. It carries:

- the domain's subject and the exact files it owns;
- the rules it keeps, and where each one lives;
- its vocabulary, its ids, and the invariants a change must not break;
- how it is checked, and which tests cover it;
- what is known to be unfinished or inconsistent.

A coder is spawned with a prompt that says: *read the briefing first; it is the source of truth for this domain*. A
domain manager's first act on a card is to read the briefing and the card; its last act is to bring the briefing up to
date. **If the briefing and the code disagree, the briefing is the bug** — the domain manager fixes one of them before
it spawns any coder.

This is the whole trick of the hierarchy: nothing is remembered by an agent, so everything worth remembering is written
down in the place its owner will read it again.

## The model policy

The owner's policy, and the reasons for it:

- **Domain managers are opus with medium effort.** They hold the consistency of a domain, and consistency is judgement
  work, not code-writing. Medium effort is deliberate: a domain manager that thinks for a long time on every step
  spends the budget that belongs to the coders.
- **Coders are sonnet for workhorse, haiku for scout, opus for deep.** The workhorse is the default: a scoped,
  well-understood change. The scout answers a narrow question about the code and nothing else, so the cheap model is
  right. The deep coder is for the genuinely hard thing — a tangled refactor, a netcode subtlety, a bug that has
  already defeated two cheaper attempts.
- **Domain managers set the model as required.** The Agent call's `model` and `effort` parameters override the
  definition, so a domain manager can send a task to the workhorse at opus when a job that looked routine turns out
  not to be, without editing any file. A domain manager that finds itself escalating twice for the same kind of work
  proposes a change to the definition instead ([the roles](references/roles.md)).

The three coder definitions are shared by every domain manager. They are roles, not per-domain copies: a change to
"how a workhorse is briefed" is one edit, not six.

## The domains

**A domain is a workflow, not a folder.** The owner set this: the unit of ownership is the job that gets done, and the
job usually cuts across the code's folders. The map, with the exact file lists, is in
[the domain map](references/domains.md). Seven domains to start, plus the architect's own:

| Domain | Subject | The owner's words |
|---|---|---|
| `den` | the den: its design and art, the room layout, the unlock and the progression, and moving around it | "design and art selection for den, room layout, room unlock and progression, movement around den" |
| `cutscene` | the cutscenes: the workflow, the accuracy to the scripts, and the preview links | "all things cutscene, cutscene workflow, maintaining accuracy to scripts, creating preview links for the cutscenes" |
| `sprites` | the art: hand-drawn or generated, the sprite workflow, the frames every entity needs, and their animation | "creating the art for sprites, hand drawn or generated, sprite workflow, NOT game object programming or slot programming — just making sure all required frames are filled for entities. Animation of frames/sprites" |
| `story` | the plot: what happens, in what order, and each reveal | "maintaining consistency in plot and changes to the story go through this domain" |
| `gameplay` | the rules of play: the crew's kits, the missions and their maps, the goons, the bosses, the world | |
| `shell` | the shell around the game: the state machine and its flows, the screens, the title, the HUD, the input | |
| `net` | the co-op: the netcode, the contracts, the room, the handover, the saves and the account | |

Three rules decide every boundary question:

1. **One file, one owner.** Every file in the repository belongs to exactly one domain, and the map says which. A
   folder may be split by file — `Core/Platform/` is `gameplay`'s except for the eight netcode files, which are
   `net`'s. A file with two owners has no owner. **The map is machine-checked**: `.claude/hierarchy/OWNERSHIP.txt` is
   the authority, and `tools/check.sh ownership` proves that no file has two owners, none has none, and no claim is
   stale.
2. **A file goes to the domain whose *workflow* would change it.** This is the rule that replaces "which folder is it
   in". A file is asked: *when it changes, who does the thinking?* The answer is its owner. `ArtData.cs` is the
   `sprites` domain's, because filling in a missing frame is the sprites' work, even though the file is code and the
   sprites' job is art. `Core/Story/` is the `cutscene` domain's, because a change to how a scene is read is the
   cutscene's work.
3. **The shared contracts belong to whoever is most hurt by them being wrong.** `Content.cs` and `Definitions.cs` are
   `gameplay`'s, because a wrong id breaks the game. `StageKit` and `StageScreen` are the `cutscene` domain's, because
   the stage is the cutscene engine and the den borrows it — a change there is a cutscene change the den must adapt
   to, and the handover says so.

A domain manager owns its boundary and defends it. It does not apply a change that belongs to another domain; it
hands it over ([the protocols](references/protocols.md)).

### The three awkward seams

Three boundaries in the map are not clean, and pretending otherwise would be the mistake:

- **The sprites domain and the code.** The sprites' work is mostly *not code*: a hand-drawn frame is ASCII art in
  `ArtData`, a generated plate is a PNG in `Resources/`, and a cut-down frame is made by a script in the vendor
  repository. The sprites domain owns the art and the *art data*, but it does not own the gameplay code that draws a
  sprite. Where a new entity needs both an art frame and a brain, the sprites' part and the gameplay's part are two
  halves of one card, done in sequence with a handover.
- **The den and the cutscene share the stage.** The den is drawn on the same stage the cutscenes use, with the same
  figures and the same `StagePose`. A change to the stage is a `cutscene` change; the den adapts. This is the one
  place where the den's own "design and art" needs a standing agreement, and it goes in the den's briefing as a
  standing rule, not as a card.
- **The story domain owns the plot, not the files that express it.** The plot is written down once and spread across
  many: the mission order, the roster, which scene plays when, what has been revealed. The `story` domain owns
  `MissionStory.cs` and `story.md`, and it owns *the agreement* of the rest — but the mission order stays
  `gameplay`'s and the scene names stay the `cutscene` domain's. It is the one domain whose authority is a rule
  rather than a file, which is why its consistency check is the thing it should build first.

## The card lifecycle

The Roadmap is the queue; a card is one item on it. The card's record lives in the repository, so it survives every
session, and the Roadmap is the human-facing board of the same thing.

| State | Who moves it there | What it means |
|---|---|---|
| `open` | the roadmap PM | queued, no owner yet |
| `claimed` | a domain manager | the domain manager has taken it and read its briefing |
| `in-progress` | a domain manager | coders are working |
| `in-review` | a domain manager | the work is pushed; the domain manager is reading the diff |
| `done` | the roadmap PM | merged into the main line, the card closed |
| `blocked` | anyone, resolved by the architect | needs a decision, a handover, or a hand from another domain |

A card carries: its id and title, its domain, its owning domain manager, its state, its branch, its dependencies, the
definition of done, and a running note. The card format is in [the protocols](references/protocols.md).

**Done means:** `tools/check.sh` passes, the branch is pushed, the CI run is green, the domain manager has read the
whole diff for consistency, and the branch is merged into the main line. The main line moves only when the owner
approves the item on the Roadmap — that is the project's own rule, and the hierarchy does not change it.

## Handover between domain managers

A task that crosses a boundary is done in pieces, each piece by the domain that owns it, with an explicit handover in
the middle.

1. The domain manager that can start takes the card, and claims it.
2. When it finishes its part, it writes a **handover block** on the card: what it changed (the branch and the commit),
   what it assumed, what is left, and what the next owner must check. The block is on disk, so it survives the agent
   that wrote it.
3. It moves the card to `blocked` with the next owner named, and messages the other domain manager if that one is
   alive. The message is the fast path; the card is the record.
4. The receiving domain manager reads the card and the briefing of its own domain, checks the handover against the
   code, and continues.

A handover is not a hand-off of responsibility for the whole card. Each domain manager is responsible for the
consistency of its own part, and the card's state says whose part it is.

## The consistency contract

A domain manager is strict about consistency inside its boundary, and the project's own rules say what that means:

- **A rule lives in Core, once.** A rule that has drifted into a view, a flow or an entity moves back into Core with
  tests the next time it changes.
- **Ids never change and are never reused.** They ride the network. This is the strongest constraint in the codebase
  and no domain may break it, ever, for any reason.
- **A big screen file is split by part** the next time it changes: a view or a flow doing several jobs is split when
  it is next touched.
- **One vocabulary per domain.** The words players read are fixed by the project: "tabk", never tank; "Trollogram",
  never hologram; kie is said "k-eye". Code identifiers keep their old names.
- **The design is the source.** `docs/design/` is where a rule is decided, and the change goes there in the same
  commit as the code, not after.

Consistency across a boundary is the domain managers' job together, and it is why they can hand over to each other:
the handover says what changed on one side that the other side must know.

## The architect's health checks

The platform architect keeps the hierarchy in working order. The check list, and what each check looks at, is in
[the operations](references/operations.md). In summary:

1. Every domain has a briefing, and it is newer than the domain's last change in the main line.
2. Every card is real: its branch exists, its CI is green, its state matches reality, and no card has been
   `in-progress` for more than a working week.
3. No two domains claim the same file, and every file in the repository has exactly one owner.
4. Every agent definition parses, and its model and effort match the policy above.
5. The domain map still matches the code map: a file that has grown into a second subject, or moved, is re-cut.

The architect does not do the domains' work. It changes the definitions, the map and the briefings, and it settles
the disputes that a handover cannot.

## Operating constraints

Two limits are real and shape the design:

- **Nesting depth is three layers** below the main conversation, and the `Agent` tool is withheld from a subagent at
  the limit. The hierarchy uses exactly that: main → architect → domain manager → coder. The coder is the last layer
  and never spawns anything. If a fourth layer is ever needed, `CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH` is raised first.
- **Twenty subagents may run at once.** A domain manager with three coders live is four running agents. So **at most
  four domains work at a time**; the architect sequences the rest. Twenty is the default and can be raised, but a wide
  fan-out costs more than it saves: five domains in flight means five sets of eyes on five sets of rules, and nobody
  is checking any of them.

## Today's role threads

The project runs today on standing role threads, not this hierarchy: the routing table and each role's notes are in
the project's memory (`roster.md`, `role-<name>.md`). If the hierarchy goes live, a domain's briefing takes over the
job of its role note, and the two are not kept side by side. As of 25 Oct the roles map like this:

| Role thread today | Becomes |
|---|---|
| Roadmap manager | the roadmap PM |
| Platform architect | the platform architect |
| Engineer: gameplay | `gameplay` |
| Engineer: story and screens | `cutscene` and `den` |
| Engineer: title screen | `shell` |
| Engineer: co-op and online | `net` |
| Story and lore | `story` (the scripts in `docs/design/cutscenes/` stay the `cutscene` domain's) |
| Design | `sprites` for the art; the den's and the missions' design go with `den` and `gameplay` |
| Media generation | `sprites`: the generated half of the art, and the spend and ledger in the vendor repository |
| Audio and music | no domain yet: its files are `gameplay`'s in the map |
| QA and release | no domain: it merges what the owner approves, which sits above the domain managers |
| Design consistency check | no domain: closest to the architect's health checks and the `story` check |

The Roadmap's statuses today are `idea`, `queued`, `building`, `review`, `feedback` and `done`. Only the owner's
approval sets `done`, and only the owner moves an `idea` to `queued`. The card states above map onto them: `open` is
`queued`, `claimed` and `in-progress` are `building`, `in-review` is `review`, and `blocked` has no status of its own
yet.

## What is not settled

These are the open questions, and they are the owner's to answer:

1. **Where the cards live.** The Roadmap page is the human board, but the durable record should be in the repository,
   where it survives and diffs. The proposal is a card file per item, with the Roadmap as the board over it.
2. **Whether the architect is a subagent or the main session.** The main session is simpler and keeps the owner in
   the conversation; a subagent architect frees the session to do the owner's own work. The spec assumes the main
   session is the architect, with the definitions and checks written down here.
3. **Whether the domain managers are one generic definition or one per domain.** The spec assumes one generic
   definition, with the domain named in the prompt and the briefing carrying the rest. One per domain would let each
   carry its own `initialPrompt`, at the cost of six files to keep in step.
4. **Whether the sprites domain has a tool of its own.** Its work is partly made outside the repository, in the
   vendor repository's image and video pipelines, and partly in the owner's own art files. The domain manager needs to
   say plainly what it owns and what the owner keeps.
5. **Where audio, release and the consistency check go.** Three of today's roles have no domain (the table above):
   audio could be its own domain or stay `gameplay`'s, and QA and release and the consistency check could be L0
   roles beside the roadmap PM.

## What to do next

1. Create the agent definitions from [the roles](references/roles.md), and the card store and briefing folders.
2. Cut the first domain's boundary and write its briefing, for `gameplay`, since everything else borrows from it.
3. Run one real card end to end through the hierarchy, and fix the protocol where the real thing disagrees with this
   spec. The spec is wrong in places; a card that runs is worth more than a spec that reads well.
