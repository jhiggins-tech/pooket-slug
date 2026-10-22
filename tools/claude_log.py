"""Print Claude Code's stream-json as it arrives, so the Actions log shows the agent working.

Claude Code writes one JSON object per line when run with --output-format stream-json. This reads them
from stdin and prints each one as a readable block: the agent's own words, the tools it calls, and the
result at the end. The tool calls are grouped, so the log can be folded away while a long run goes.

usage: claude -p --output-format stream-json --verbose ... | python3 -I tools/claude_log.py

Prints the final result to stdout's own file (nothing special: this only writes to stderr and stdout for
the human reading) and exits 1 if the run failed, so the workflow stops.
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
for line in sys.stdin:
    line = line.strip()
    if not line:
        continue
    try:
        event = json.loads(line)
    except json.JSONDecodeError:
        print(line)
        continue
    kind = event.get("type")
    if kind == "system":
        subtype = event.get("subtype")
        if subtype == "init":
            print(f"▶ Claude Code {event.get('claude_code_version', '')} on {event.get('model', '?')}")
        elif subtype == "api_retry":
            print(f"↻ retrying: {event.get('error', '?')}")
        else:
            print(f"· {subtype}")
    elif kind == "assistant":
        show_assistant(event)
    elif kind == "user":
        show_user(event)
    elif kind == "result":
        if event.get("is_error"):
            failed = True
            print(f"✗ the run failed: {event.get('subtype', '?')}")
        else:
            turns = event.get("num_turns", "?")
            cost = event.get("total_cost_usd")
            money = f", ${cost:.2f}" if isinstance(cost, (int, float)) else ""
            print(f"✓ done in {turns} turns{money}")
            result = event.get("result")
            if result:
                print(result.strip())

sys.exit(1 if failed else 0)
