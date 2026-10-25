# Operations: the architect's checks, and the limits

What the platform architect does to keep the hierarchy working, and the hard limits the design has to live inside.

**Read this when** you're auditing the system, or deciding whether the hierarchy can take on more work.

Up: [the platform architecture](../SKILL.md) · [the domain map](domains.md)

## The architect's job

The architect maintains the hierarchy itself. It does not do the domains' work.

**It changes:**

- the agent definitions in `.claude/agents/`, and their models and efforts;
- the domain map, when a file moves or a boundary is wrong in practice;
- the shape of a briefing, and the card format;
- the rules of the hierarchy — this skill — when the way of working changes.

**It does not:**

- write game code;
- own a card;
- decide what the game should be.

When a domain manager and the owner disagree about a rule, the architect is not the arbiter — the owner is. The
architect's job is to make the disagreement expressible: to say what each side thinks, and what each side would cost.

## The health checks

The architect runs these, and they are the system's own tests.

### 1. Every domain has a briefing, and it is current

A domain without a briefing is a domain whose memory is one agent's context, which is lost. A briefing older than
the domain's last change in the main line is a briefing that is wrong.

```
find .claude/hierarchy/domains -name BRIEFING.md
git log -1 --format=%cd -- .claude/hierarchy/domains/<domain>/BRIEFING.md
git log -1 --format=%cd -- <the domain's files>
```

### 2. Every card is real

A card's branch exists, its CI is green, its state matches what is on disk, and none has been `in-progress` for
more than a working week. A card that lies about its state is worse than no card: the PM schedules from it.

### 3. Every file has exactly one owner

The domain map lists every file. A file in two domains' lists is a boundary that is wrong. A file in none is a hole.
**The check is `tools/check.sh ownership`**, which reads `.claude/hierarchy/OWNERSHIP.txt` and proves all three: no
file has two owners, none has none, and no claim points at a file that has moved. It runs in the local checks, so the
boundary cannot rot quietly.

### 4. Every definition parses, and matches the policy

Each agent definition's `name`, `model` and `effort` are as the policy says: opus/medium for the domain managers,
sonnet/low for the workhorse, haiku/low for the scout, opus/high for the deep coder, opus/high for the architect,
sonnet/medium for the PM.

### 5. The map still matches the code
A file that has grown past its subject, a folder that has become a second subject, a workflow that has moved: the map
is corrected. This check is the one that decays, so it is the one the architect does most often.

### 6. The plot still agrees with itself
The `story` domain's own check, and the architect's too, because it is the one thing in the game that is written
once and expressed in many places: the mission order, the roster, which scene plays when, what has been revealed.
`MissionStoryTests` already holds a mission's own story behaviour; what is missing is the *agreement* between the
plot in `docs/design/story.md` and the plot in the code. The story domain's first card is that test, and until it
exists the plot is checked by reading it, which is how plots rot.

### 7. The nesting is still within its depth

Three layers below the main conversation: architect → domain manager → coder. If a domain manager has needed a fourth
layer, either the domain is too big or the coder is too weak — and both are fixed by a change of the hierarchy, not
by raising the depth limit. The limit is raised only when the owner decides it.

## The limits, and why they are what they are

### Nesting depth: three layers

The `Agent` tool is withheld from a subagent at the depth limit, so a subagent at the limit does the work itself and
returns one summary. The hierarchy uses exactly three: the domain manager spawns coders, and a coder never spawns.

`CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH` sets the depth (default 3). `1` turns nesting off entirely.

**A coder-deep is the one exception:** it may pull in a scout for a question while it thinks, because it is still
inside the third layer — its scout is the fourth, and would be at the limit. In practice the deep coder reads the
files itself; the scout is for when the question is wide.

### Concurrency: twenty subagents

`CLAUDE_CODE_MAX_CONCURRENT_SUBAGENTS` (default 20) caps the subagents running at once.

A domain manager with three coders live is four running agents. So **at most four domains work at a time**, and the
architect sequences the rest. Twenty is the default and can be raised, but a wide fan-out costs more than it saves:
five domains in flight is five sets of rules being applied with nobody checking any of them, and the consistency the
hierarchy exists to provide is the first thing to go.

**The architect's queue is the answer to a bottleneck.** If four domains are busy and a fifth card is ready, the card
waits. It does not jump the queue because it is small.

**With eight domains, the queue matters more, not less.** Four of eight working means three are always waiting, so
the architect's job includes saying which four. The natural order, when the owner has not said otherwise, is by what
blocks the most: `gameplay` first (everything borrows from it), then `story` and `cutscene` (the plot and its
scenes), then `sprites`, then `den`, then `shell` and `net`.

## The costs, honestly

The hierarchy is not free, and the architect should say so when the owner asks.

- **A domain manager is a real cost.** Opus at medium effort, per card, is the single most expensive thing in the
  system. A card that one workhorse could have done end to end is now done by opus reviewing sonnet. That is the
  price of the consistency, and it is worth paying when the change is a rule, a shared contract, or a new entity. It
  is not worth paying for a typo.
- **The briefing is upkeep.** A stale briefing costs more than a missing one, because it is trusted. The rule that it
  is updated in the same commit is the whole defence.
- **The handover is friction.** Two domains meeting is slower than one. The handover protocol exists so the
  friction is paid once, in writing, and not again when the next agent picks it up.
- **The four-at-a-time limit is a real limit.** Work will queue at the architect.

**The escape hatch:** a card that is genuinely trivial — a typo, a comment, a one-line fix with a test — does not go
through the hierarchy at all. It is done by a coder, straight to a branch, and the architect sees it in the audit. The
hierarchy is for work that needs consistency, not for work that needs a process.

## The first run

The spec is untested, and the first real card will disagree with it somewhere. So:

1. Create the definitions, the card store and the briefing folders.
2. Write one briefing, for `gameplay`, since every other domain borrows from it.
3. Take one real card through the whole thing: the architect, the domain manager, a workhorse and a scout, the
   review, the merge.
4. **Fix the spec where the run disagreed with it**, and say what disagreed.

A spec that has run one card is worth more than a spec that reads well. The first card should be small and real.
