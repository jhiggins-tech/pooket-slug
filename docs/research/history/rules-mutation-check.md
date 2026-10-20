# Database rules: the mutation check

This is a record of a check run on 2026-10-14, not instructions. The live rules and tests are in [database rules](../accounts/database-rules.md).

Up: [Research index](../README.md)

All 30 of Cardmaster's rules tests pass, its own 17 included.

**Mutation check:** the new rules were weakened 32 ways, one at a time. Every time, the run failed and a named test reported it:
- **Saves:** deleting the record; no save number check; a save number that skips ahead; a first save at any number.
- **Progress:** a lower XP, rank, version, mission best, clear count or kill count; co-op closing again.
- **Shape:** a worn item that isn't owned; unknown fields; XP not required; fractional XP; no upper bound on XP; unbounded tokens; unknown settings; a tally entry without its counters; a list or a mission as a plain value; list entries other than `true`.
- **Times and access:** a client-chosen save time; a `first` that changes; others reading or writing the record.
- **The board:** others writing; a lower best; signed-out readers; a client-chosen time; extra fields; overlong mission ids.
