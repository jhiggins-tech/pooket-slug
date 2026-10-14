# Roadmap

Where the game could go next. There's a list of candidate features to sort with MoSCoW, and the refactors that would prepare the code for them.

**Read this when** you're choosing what to build next, or planning a refactor.

Up: [Research index](../AGENTS.md)

## The process

1. **Candidate list (done, 2026-10-14):** [`feature-candidates.csv`](feature-candidates.csv) lists 176 side-scroller, Metal Slug and multiplayer mechanics, including co-op combinations, plus the engineering work they need. Existing features are marked `Done` or `Partial`, so the list doubles as a map of what the game has.
2. **MoSCoW (done by the owner, 2026-10-14):** the `moscow` column holds **M**, **S**, **C** or **W**. Some rows point to the game design (`See design doc`) or ask a question; the [refactor plan](refactor-plan.md) reads and answers those.
3. **Refactor pass:** prepare the code, build and netcode for the Must and Should features. The [refactor plan](refactor-plan.md) (proposed, waiting for the owner's go-ahead) orders the [refactor candidates](refactor-candidates.md), which map each refactor to the feature IDs it unlocks.

## Columns

| Column | Meaning |
|---|---|
| `id` | Stable ID. The prefix is the area (`MOV`, `AIM`, `WPN`, `VEH`, `ENM`, `BOS`, `LVL`, `POW`, `TRN`, `SCR`, `CHR`, `COP`, `NET`, `SOC`, `VRS`, `PRE`, `AUD`, `UIX`, `ACC`, `TEC`, `OPS`) |
| `area`, `feature`, `description` | What it is |
| `inspiration` | Metal Slug; Metal Slug (later entries); run-and-gun or platformer genre; co-op design; online games; engineering |
| `status` | `Done`, `Partial` or `Not started`, as of commit `fe39db8` |
| `multiplayer_angle` | What it means in online co-op, or the constraint it hits |
| `netcode_impact` | **None** = local only · **Low** = fits today's model (a new kind, pose bit or RPC) · **Medium** = new replicated state or arbitration on existing objects · **High** = new networked object types, ownership transfer, or a change to the authority model |
| `pipeline_impact` | **None** = code only · **Low** = a small package or a few assets · **Medium** = a new asset pipeline, CI job, or config/secrets · **High** = a new SDK, backend service or build target |
| `effort` | **S** ≤ 1 day · **M** 2–5 days · **L** 1–2 weeks · **XL** > 2 weeks, for one developer who knows the code |
| `depends_on` | IDs, separated by `;`, that should come first. These are often an engineering item (`TEC-…`) |
| `moscow` | The owner's call: M / S / C / W, or a note such as `See design doc` |
| `notes` | For you |

The impact and effort figures are first estimates, relative to the code as of 2026-10-14. The refactor pass will revisit them for the rows you mark Must or Should.

## Documents

- [feature-candidates.csv](feature-candidates.csv): the list.
- [refactor-candidates.md](refactor-candidates.md): the maintainability improvements we'd make with hindsight, each mapped to the features it unlocks.
- [refactor-plan.md](refactor-plan.md): which of those to do, in what order, for the chosen features. It also covers the open decisions and the player record for sign-in.
