# The CI agent

Claude Code runs headless in GitHub Actions, on OpenRouter, and does the work you ask for on a branch of
its own: it commits, pushes and opens a pull request, so its work is reviewed like any other.

**Read this when** you want the agent to do a piece of work, or you want to know what it did.

Up: [Research](README.md)

## What it does

[`.github/workflows/claude-code.yml`](../../.github/workflows/claude-code.yml) is manual only: it spends money and
writes to the repository, so it runs when you ask for it, never on a push. It makes a branch off the base
(`claude/ci-<the task's words>`), installs Claude Code and runs it with the task on stdin, so the task's text
never passes through the shell. Whatever the agent leaves behind is committed and pushed, and a pull request
opens against the base branch even if the run failed.

## Running it

Actions → **Claude Code → PR** → **Run workflow**. The **task** input is what you want done, and it must not
be empty. The rest are optional: `model` (any OpenRouter model id; the default is
`minimax/minimax-m3.1-flash:free[1m]`, the `[1m]` asking for the 1M-token window), `base` (the branch it
starts from, default the main line `claude/gallant-keller-3qzp97`), `branch`, `title` and `max-turns` (40).

## Watching it

The log is the agent's window: [`tools/claude_log.py`](../../tools/claude_log.py) prints Claude Code's stream as
it arrives — 💭 its thinking, 💬 its words, 🔧 the tools it calls, ↳ what came back, and at the end ✓ the turns
and the cost. Open the run in the Actions tab, and read the "Run Claude Code" step.

## The @claude action

[`.github/workflows/claude.yml`](../../.github/workflows/claude.yml) is the official `claude-code-action`, answering
`@claude` on issues and pull requests. Before the action runs it builds the game (the pull request's head, for a
comment on one), serves it at `http://127.0.0.1:8001/index.html` and sets up Playwright, so the agent finds the build
in its container and has a browser (the `playwright` MCP server) to play it. Its instructions are
[`.github/claude-code/action-system-prompt.md`](../../.github/claude-code/action-system-prompt.md).
