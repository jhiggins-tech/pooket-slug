"""Makes the gallery page for the plate generations: every image the painter makes
lands in the wip folder, and this page lists them so they can be looked over in a
browser.

Each generation is one section: the full-size picture and its cut down to the
game's size, side by side, newest generation first. The vector reference the
pictures are made from is shown once at the top, on its own.

The page is rewritten every run, so a new picture in the folder is picked up by
running the script again.

usage: python3 -I tools/plates_gallery.py
"""
import os
import re
import struct

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
PLATES = os.path.join(ROOT, "docs/design/lobby/plates")
WIP = os.path.join(PLATES, "wip")
OUT = os.path.join(PLATES, "gen-index.html")

PICTURES = (".png", ".jpg", ".jpeg", ".webp")
VECTORS = (".svg",)

STYLE = """  :root { color-scheme: dark; }
  body { margin: 0; padding: 24px; background: #12100e; color: #e8e0d4;
         font: 14px/1.6 ui-sans-serif, system-ui, sans-serif; }
  h1 { font-size: 20px; margin: 0 0 4px; }
  p.lede { color: #a89c8c; margin: 0 0 20px; max-width: 70ch; }
  p.lede b { color: #f0c14b; }
  h2 { font-size: 15px; color: #f0c14b; margin: 28px 0 8px; font-weight: 600; }
  h3 { font-size: 13px; color: #a89c8c; margin: 20px 0 6px; font-weight: 600; }
  .note { color: #a89c8c; font-size: 13px; max-width: 70ch; }
  .pair { display: flex; gap: 20px; align-items: flex-start; flex-wrap: wrap; }
  figure { margin: 0; min-width: 0; max-width: 100%; }
  figure img { display: block; image-rendering: pixelated; border: 1px solid #2c2622; }
  figure.full img { max-width: 100%; max-height: 72vh; width: auto; height: auto; }
  figure.game img { width: 420px; max-width: 100%; }
  figcaption { color: #a89c8c; font-size: 12px; margin-top: 4px; }
  .ref img { max-width: 100%; max-height: 40vh; border: 1px solid #2c2622; }
"""


def picture_size(path):
    """The picture's width and height in pixels, read from the PNG's header.
    Zero if it isn't a PNG or the header can't be read."""
    try:
        with open(path, "rb") as f:
            head = f.read(24)
        if head[:8] != b"\x89PNG\r\n\x1a\n":
            return (0, 0)
        return struct.unpack(">II", head[16:24])
    except OSError:
        return (0, 0)


def take_number(name):
    """The take's number from a name like take2-kitchen-1260x540.png, or None."""
    m = re.search(r"take(\d+)", name)
    return int(m.group(1)) if m else None


def group_key(name):
    """The generation a picture belongs to: its name without the size and
    without the -game that marks the cut down to the game's size."""
    stem = os.path.splitext(name)[0]
    stem = re.sub(r"-\d+x\d+$", "", stem)
    stem = re.sub(r"-game$", "", stem)
    return stem


def is_game_cut(name):
    return name.endswith("-game" + os.path.splitext(name)[1])


def scan():
    """The pictures in the wip folder, and the vector reference, newest first."""
    pictures = []
    vectors = []
    for name in sorted(os.listdir(WIP)):
        path = os.path.join(WIP, name)
        if not os.path.isfile(path):
            continue
        ext = os.path.splitext(name)[1].lower()
        if ext in PICTURES:
            pictures.append(name)
        elif ext in VECTORS:
            vectors.append(name)
    return pictures, vectors


def group(pictures):
    """The generations: each group is one generation's pictures, newest group
    first. A wip- picture is the latest, then the takes by number, then
    anything else by when it was made."""
    groups = {}
    for name in pictures:
        groups.setdefault(group_key(name), []).append(name)

    out = []
    for key, names in groups.items():
        # in a group, the full-size picture comes before its game-size cut
        names.sort(key=lambda n: (is_game_cut(n), n))
        mtime = max(os.path.getmtime(os.path.join(WIP, n)) for n in names)
        take = next((t for t in (take_number(n) for n in names) if t is not None), None)
        if any(n.startswith("wip") for n in names):
            order = (0, 0, -mtime)
        elif take is not None:
            order = (1, -take, -mtime)
        else:
            order = (2, 0, -mtime)
        out.append((order, key, names))
    out.sort()
    return [(key, names) for _, key, names in out]


def group_title(key, names):
    """The heading for a generation: what a reader calls it."""
    if any(n.startswith("wip") for n in names):
        return "The latest"
    take = next((t for t in (take_number(n) for n in names) if t is not None), None)
    if take is not None:
        return "Take %d" % take
    return key


def picture_html(name, kind, alt):
    """One picture with its caption, naming the file."""
    return (
        '<figure class="%s">\n'
        '  <img src="wip/%s" alt="%s">\n'
        '  <figcaption>%s</figcaption>\n'
        '</figure>' % (kind, name, alt, name)
    )


def build_page(pictures, vectors):
    s = []
    s.append("<!doctype html>")
    s.append('<html lang="en">')
    s.append("<head>")
    s.append('<meta charset="utf-8">')
    s.append('<meta name="viewport" content="width=device-width, initial-scale=1">')
    s.append("<title>Pooket Slug: the plate generations</title>")
    s.append("<style>")
    s.append(STYLE)
    s.append("</style>")
    s.append("</head>")
    s.append("<body>")
    s.append("<h1>The plate generations</h1>")
    s.append('<p class="lede">Every generation of the room plates as it lands, '
             'newest first: the picture the painter made, and the same picture cut '
             'down to the <b>game&#39;s size</b>, so the detail can be judged at '
             'the size the player sees it.</p>')

    if vectors:
        s.append("<h2>The reference this was made from</h2>")
        for name in vectors:
            s.append('<div class="ref"><img src="wip/%s" alt="%s"></div>' %
                     (name, "The vector reference the plates are painted from"))
        s.append('<p class="note">The vector reference the painter works from: '
                 'the room drawn to scale, with the soldier in it. Every picture '
                 'below is made from it.</p>')

    for key, names in group(pictures):
        s.append("<h2>%s</h2>" % group_title(key, names))
        s.append('<div class="pair">')
        for name in names:
            kind = "game" if is_game_cut(name) else "full"
            alt = "%s: the plate, %s" % (name, "at the game's size" if kind == "game"
                                          else "full size")
            s.append(picture_html(name, kind, alt))
        s.append("</div>")

    s.append("</body>")
    s.append("</html>")
    return "\n".join(s) + "\n"


def main():
    pictures, vectors = scan()
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(build_page(pictures, vectors))
    print("wrote %s (%d generations, %d reference)" %
          (os.path.relpath(OUT, ROOT), len(group(pictures)), len(vectors)))


if __name__ == "__main__":
    main()
