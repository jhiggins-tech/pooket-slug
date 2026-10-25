"""Print Claude Code's stream-json as it arrives, so the Actions log shows the agent working.

Claude Code writes one JSON object per line when run with --output-format stream-json. This reads them
from stdin and prints each one as a readable block: the agent's own words, the tools it calls, and the
result at the end. The tool calls are grouped, so the log can be folded away while a long run goes.

usage: claude -p --output-format stream-json --verbose ... | python3 -I tools/claude_log.py

With --result <file>, it reads that file instead of stdin and prints only the agent's final answer (what
it said at the end), which is what goes in a pull request's body.

Exits 1 if the run failed, so the workflow stops.
"""
import json
import sys

def shorten(text, limit=600):
    text = " ".join(str(text).split())
    return text if len(text) <= limit else text[:limit] + f"… (+{len(text) - limit} more)"

def show_assistant(event):
    for block in event.get("message", {}).get("content", []):
        kind = block.get("type")
        if kind == "text" and block.get("text", "").strip():
            print("💬", block["text"].strip())
        elif kind == "tool_use":
            name = block.get("name", "tool")
            args = block.get("input", {})
            if name == "Bash":
                what = args.get("description") or shorten(args.get("command", ""))
            elif name in ("Read", "Write", "Edit"):
                what = args.get("file_path", "")
            else:
                what = shorten(json.dumps(args))
            print(f"🔧 {name}: {what}")
        elif kind == "thinking":
            thought = block.get("thinking", "").strip()
            if thought:
                print("💭", shorten(thought, 300))

def show_user(event):
    for block in event.get("message", {}).get("content", []) or []:
        if block.get("type") != "tool_result":
            continue
        body = block.get("content")
        if isinstance(body, list):
            body = " ".join(b.get("text", "") for b in body if isinstance(b, dict))
        body = shorten(body or "")
        if body:
            print("   ↳", body)

failed = False
result_only = False
source = sys.stdin
if len(sys.argv) > 1 and sys.argv[1] == "--result":
    # The agent's final answer only, from a stream already written to a file.
    result_only = True
    path = sys.argv[2] if len(sys.argv) > 2 else "-"
    source = open(path, encoding="utf-8") if path != "-" else sys.stdin

for line in source:
    line = line.strip()
    if not line:
        continue
    try:
        event = json.loads(line)
    except json.JSONDecodeError:
        if not result_only:
            print(line)
        continue
    kind = event.get("type")
    if kind == "result":
        if event.get("is_error"):
            failed = True
            if not result_only:
                print(f"✗ the run failed: {event.get('subtype', '?')}")
        elif result_only:
            result = event.get("result")
            if result:
                print(result.strip())
        else:
            turns = event.get("num_turns", "?")
            cost = event.get("total_cost_usd")
            money = f", ${cost:.2f}" if isinstance(cost, (int, float)) else ""
            print(f"✓ done in {turns} turns{money}")
            result = event.get("result")
            if result:
                print(result.strip())
    elif result_only:
        continue
    elif kind == "system":
        subtype = event.get("subtype")
        if subtype == "init":
            print(f"▶ Claude Code {event.get('claude_code_version', '')} on {event.get('model', '?')}")
        elif subtype == "api_retry":
            print(f"↻ retrying: {event.get('error', '?')}")
        elif subtype == "thinking_tokens":
            pass # one of these per thinking block: they say nothing the agent's own words don't
        else:
            print(f"· {subtype}")
    elif kind == "assistant":
        show_assistant(event)
    elif kind == "user":
        show_user(event)

sys.exit(1 if failed else 0)
