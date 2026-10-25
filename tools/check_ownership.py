"""The ownership check: every file in the repository belongs to exactly one domain.

Reads .claude/hierarchy/OWNERSHIP.txt and checks two things:

  1. No file is claimed by two domains (a file with two owners has no owner).
  2. Every source file in the repository is claimed by some domain (a file with no
     owner is a hole: nobody maintains it).

The second check covers the game's code and its tests, the scenes, and the tools the
cutscene and den domains use. Files that are nobody's business (the build, the CI, the
docs) are listed in the UNTRACKED section and are not required to be claimed.

usage: python3 -I tools/check_ownership.py [<repo>]
"""
import os
import subprocess
import sys

root = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else ".")
map_path = os.path.join(root, ".claude/hierarchy/OWNERSHIP.txt")

# The domains, and the prefixes that must be covered. A prefix ending in / covers everything under it.
DOMAINS = {"gameplay", "den", "cutscene", "sprites", "shell", "net", "story", "audio", "architect"}

# The paths whose files must each have an owner. A prefix ending in / covers everything under it.
COVERED = [
    "Assets/Scripts/",
    "Assets/Tests/",
    "Assets/Resources/",
    "Assets/Plugins/",
    "Assets/Photon/",
    "Assets/Editor/",
    "scenes/",
    "sandbox/",
    "tools/scenecheck/",
    "docs/design/lobby/",
    "docs/design/cutscenes/",
    "docs/scenes.md",
    "docs/design/lobby.md",
    "docs/design/story.md",
    "docs/design/music.md",
    "docs/design/player.md",
    "docs/design/kits.md",
    "docs/design/levels.md",
    "docs/design/team-supers.md",
    "docs/design/tanks.md",
    "docs/design/tabks/",
    "docs/design/art.md",
    "CLAUDE.md",
    "docs/design/README.md",
    "docs/research/",
]


def git_ls_files(pattern):
    # A trailing / means "everything under here". git ls-files needs the glob magic for
    # that, because a bare directory path only matches the directory itself.
    if pattern.endswith("/"):
        pattern = ":(glob)" + pattern + "**"
    out = subprocess.run(
        ["git", "-C", root, "ls-files", "--", pattern], capture_output=True, text=True, check=True
    ).stdout
    return [p for p in out.split() if not p.endswith(".meta")]


def load_claims():
    """The claims: (domain, path) pairs, and the prefixes, from OWNERSHIP.txt."""
    claims = []
    with open(map_path, encoding="utf-8") as f:
        for n, line in enumerate(f, 1):
            line = line.split("#", 1)[0].strip()
            if not line:
                continue
            parts = line.split()
            if len(parts) != 2:
                raise SystemExit(f"OWNERSHIP.txt:{n}: expected '<domain> <path>', got {line!r}")
            domain, path = parts
            if domain not in DOMAINS:
                raise SystemExit(f"OWNERSHIP.txt:{n}: unknown domain {domain!r}")
            claims.append((domain, path, n))
    return claims


def owners_of(path, claims):
    """Every domain claiming the path, by exact path or by a prefix directory."""
    out = []
    for domain, claim, n in claims:
        if claim == path:
            out.append((domain, n, claim))
        elif claim.endswith("/") and path.startswith(claim):
            out.append((domain, n, claim))
    return out


def main():
    claims = load_claims()

    # The files that must have an owner.
    required = []
    for pattern in COVERED:
        if pattern.endswith("/"):
            # Everything under the directory, at any depth.
            required.extend(git_ls_files(pattern))
        else:
            required.extend(git_ls_files(pattern))
    required = sorted(set(required))

    problems = 0

    # 1. A file claimed by two domains.
    for path in required:
        found = owners_of(path, claims)
        domains = {d for d, _, _ in found}
        if len(domains) > 1:
            where = ", ".join(f"{d} (line {n})" for d, n, _ in found)
            print(f"{path}: claimed by {len(domains)} domains: {where}")
            problems += 1

    # 2. A file claimed by none.
    for path in required:
        if not owners_of(path, claims):
            print(f"{path}: no domain owns it")
            problems += 1

    # 3. A claim that matches nothing: a stale entry after a file moved.
    all_files = set(required)
    for domain, claim, n in claims:
        if claim.endswith("/"):
            if not any(p.startswith(claim) for p in all_files):
                print(f"OWNERSHIP.txt:{n}: {claim} matches nothing (stale?)")
                problems += 1
        elif claim not in all_files:
            print(f"OWNERSHIP.txt:{n}: {claim} is not a file in the repository (stale?)")
            problems += 1

    total = len(required)
    print(
        f"Ownership: {total} files, {len(claims)} claims, "
        + (f"{problems} problems." if problems else "every file has exactly one owner.")
    )
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
