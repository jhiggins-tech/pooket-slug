"""The docs link check (tools/check.sh): every relative link in the repository's Markdown files must point at a file or
folder that exists, and a #heading on a Markdown file at a heading it has (GitHub's slugs). Links to the web aren't
followed. Prints each broken link as <file>:<line>: <link> and exits 1 if there are any.

usage: python3 -I tools/doclinks.py [<repo>]
"""
import os, re, subprocess, sys

root = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else ".")
files = subprocess.run(["git", "-C", root, "ls-files", "--cached", "--others", "--exclude-standard", "*.md"], capture_output=True, text=True, check=True).stdout.split()
link = re.compile(r"\[[^\]]*\]\(([^)\s]+)(?:\s+\"[^\"]*\")?\)")
fence = re.compile(r"^\s*(```|~~~)")

def slug(heading):  # GitHub's: lower case, punctuation dropped (not - or _), spaces to -
    heading = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", heading).strip().lower()
    return re.sub(r"[^\w\- ]", "", heading).replace(" ", "-")

anchors = {}
def anchors_of(path):
    if path not in anchors:
        seen, out, code = {}, set(), False
        for line in open(path, encoding="utf-8"):
            if fence.match(line):
                code = not code
            elif not code and line.startswith("#"):
                s = slug(line.lstrip("#"))
                n = seen.get(s, 0); seen[s] = n + 1
                out.add(s if n == 0 else f"{s}-{n}")
            out.update(re.findall(r'<a (?:name|id)="([^"]+)"', line))
        anchors[path] = out
    return anchors[path]

broken = 0
for rel in files:
    path = os.path.join(root, rel)
    code = False
    for n, line in enumerate(open(path, encoding="utf-8"), 1):
        if fence.match(line):
            code = not code
        if code:
            continue
        for target in link.findall(re.sub(r"`[^`]*`", "", line)):
            if re.match(r"^[a-z][a-z0-9+.-]*:", target, re.I):
                continue  # https:, mailto: and the like
            file, _, anchor = target.partition("#")
            dest = os.path.normpath(os.path.join(os.path.dirname(path), file)) if file else path
            if not os.path.exists(dest):
                problem = "no such file"
            elif anchor and dest.endswith(".md") and anchor.lower() not in anchors_of(dest):
                problem = "no such heading"
            else:
                continue
            print(f"{rel}:{n}: {target} ({problem})")
            broken += 1
print(f"Docs links: {len(files)} files, " + (f"{broken} broken." if broken else "all links resolve."))
sys.exit(1 if broken else 0)
