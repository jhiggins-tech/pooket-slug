"""Makes a room's reference picture: the room drawn to scale as vector art, with
the game's soldier standing in it, for the painter to work from.

The rooms are drawn for the 22 px soldier: the den and the loft look right at
this scale, so a room has to be drawn to it. The layout comes from the house
map (docs/design/lobby/house.py), which is already to scale, and the soldier is
the game's own figure, rendered from its art data.

The reference is a plain vector drawing, in flat colours with hard edges: a
side-on cutaway of the room, the things in it drawn in, the soldier standing
where you stand. Along the bottom is a strip of the den's own furniture,
measured off the den's painting, so the painter can judge the scale.

usage: python3 -I tools/room_ref.py <out.svg> <room>
"""
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

# ---- the scale (docs/design/lobby.md, Den 2.0: Scale) ----
SOLDIER = 22          # the soldier's height: everything is drawn for him

# ---- the den's own furniture, measured off the den's painting, in art pixels ----
DEN_PIECES = [
    ("the den's couch", 82, 14, 98, 29),
    ("the den's TV", 100, 49, 130, 58),
    ("the den's side door", 10, 14, 20, 76),
]

ROOMS = {
    # the kitchen, in art pixels, from the house map: (kind, x0, y0, x1, y1, label).
    # It is wider than a screen: the player scrolls through it as they walk.
    "kitchen": (420, 180, [
        ("den door", 0, 15, 24, 52),
        ("stove", 30, 15, 86, 52),
        ("bench", 100, 15, 148, 44),
        ("coffee machine", 152, 15, 176, 50),
        ("fridge", 180, 15, 216, 58),
        ("eating table", 232, 15, 304, 44),
        ("window", 180, 74, 216, 98),
        ("back door", 394, 15, 420, 52),
    ], [(70, 30, 176), (96, 30, 176), (122, 30, 176)]),
}


def read_rows(path, name):
    text = open(path).read()
    i = text.index(name)
    j = text.index('{', i)
    k = text.index('}', j)
    return re.findall(r'"([^"]*)"', text[j:k])


def hex_rgb(h):
    return ((h >> 16) & 255, (h >> 8) & 255, h & 255)


PAL = {
    'k': 0x201820, 'r': 0xd04030, 'R': 0x902820, 'h': 0x6a4424, 's': 0xf8c090,
    'S': 0xd08c60, 'e': 0x201820, 'c': 0xe0d0a0, 'C': 0xb0a070, 'g': 0x5c8c40,
    'G': 0x3c6428, 'b': 0x4c3828, 'y': 0xf0c840, 'p': 0xc09858, 'P': 0x8c6c3c,
    'w': 0x4a3428, 'm': 0xa8b0c0, 'M': 0x586078, 'W': 0xffffff, 'o': 0xf08830,
    'O': 0xc05020, 'x': 0xffe860, 'd': 0x707888, 'D': 0x454b5c, 't': 0x8a9a4a,
    'T': 0x5a6a30, 'u': 0x3a3a48, 'z': 0xc8d0e0, 'v': 0x6b4fb0, 'V': 0x45307a,
}
OUTLINE = 0x1c1420
HEAD_TOP, BODY_TOP, LEGS_TOP, W, H = 9, 8, 22, 24, 32


def soldier_canvas():
    art = os.path.join(ROOT, "Assets/Scripts/Art/ArtData.cs")
    canvas = [[None] * W for _ in range(H)]

    def put(rows, top):
        for i, row in enumerate(rows):
            for x, ch in enumerate(row):
                if ch in '. ':
                    continue
                if x < W and 0 <= top + i < H:
                    canvas[top + i][x] = PAL.get(ch, OUTLINE)

    put(read_rows(art, "Head"), HEAD_TOP)
    put(read_rows(art, "TorsoUnarmed"), BODY_TOP)
    put(read_rows(art, "LegsIdle"), LEGS_TOP)
    filled = [[canvas[y][x] is not None for x in range(W)] for y in range(H)]
    for y in range(H):
        for x in range(W):
            if filled[y][x]:
                continue
            if ((x > 0 and filled[y][x - 1]) or (x < W - 1 and filled[y][x + 1]) or
                    (y > 0 and filled[y - 1][x]) or (y < H - 1 and filled[y + 1][x])):
                canvas[y][x] = OUTLINE
    return canvas


def soldier_svg(x, feet_y, canvas):
    out = []
    for row in range(H):
        for col in range(W):
            c = canvas[row][col]
            if c is None:
                continue
            r, g, b = hex_rgb(c)
            out.append('<rect x="%g" y="%g" width="1" height="1" fill="#%02x%02x%02x"/>' %
                       (x + col - 12, feet_y - (H - row), r, g, b))
    return "\n".join(out)


# ---- the room's furniture, drawn as vector art ----
# Every piece stands on the floor: its base is at FLOOR, the room's floor line.
# The height is the room's, and the room's y is from its floor, so the floor line
# is at h - 15 in the picture's own y.
FLOOR_OFF = 15   # the floor's thickness: the room's floor line is this far up


def base(h):
    """The y of the floor line, in the picture's own y (from the top)."""
    return h - FLOOR_OFF


def stove(h):
    """The stove and the worktop: the cooker on the left, the worktop beside it."""
    f = base(h)
    return """
<rect x="30" y="%(top)s" width="56" height="%(bh)s" fill="#8a9298" stroke="#241c18"/>
<rect x="30" y="%(cook)s" width="24" height="%(ch)s" fill="#6a7278" stroke="#241c18"/>
<rect x="54" y="%(cook)s" width="32" height="12" fill="#6a7278" stroke="#241c18"/>
<rect x="34" y="%(cook)s" width="16" height="7" fill="#2a2622"/>
<rect x="32" y="%(hood)s" width="14" height="9" fill="#2a2622"/>
<path d="M38 %(hood2)s h12 v-9 h14 v9" fill="none" stroke="#241c18" stroke-width="1"/>""" % {
        "top": f - 37, "bh": 37, "cook": f - 30, "ch": 30, "hood": f - 37, "hood2": f - 28}


def bench(h):
    """The cooking bench: a smaller top, a shelf under it, and utensils hanging."""
    f = base(h)
    return """
<rect x="100" y="%(top)s" width="48" height="5" fill="#a07a4e" stroke="#241c18"/>
<rect x="104" y="%(top)s" width="4" height="%(lh)s" fill="#7a5a38" stroke="#241c18"/>
<rect x="140" y="%(top)s" width="4" height="%(lh)s" fill="#7a5a38" stroke="#241c18"/>
<rect x="104" y="%(shelf)s" width="40" height="3" fill="#7a5a38" stroke="#241c18"/>
<rect x="107" y="%(shelf)s" width="7" height="6" fill="#9a8a70" stroke="#241c18"/>
<rect x="116" y="%(shelf)s" width="5" height="5" fill="#8a7a60" stroke="#241c18"/>
<rect x="124" y="%(shelf)s" width="6" height="4" fill="#b0a080" stroke="#241c18"/>
<!-- the utensils, hung on a rail over the bench -->
<rect x="102" y="%(rail)s" width="44" height="2" fill="#5a5a60" stroke="#241c18"/>
<rect x="106" y="%(rail)s" width="2" height="9" fill="#b8bcc4" stroke="#241c18"/>
<rect x="111" y="%(rail)s" width="3" height="7" fill="#b8bcc4" stroke="#241c18"/>
<rect x="117" y="%(rail)s" width="2" height="11" fill="#b8bcc4" stroke="#241c18"/>
<rect x="123" y="%(rail)s" width="4" height="6" fill="#8a8e96" stroke="#241c18"/>
<rect x="131" y="%(rail)s" width="2" height="8" fill="#b8bcc4" stroke="#241c18"/>""" % {
        "top": f - 24, "lh": 24, "shelf": f - 10, "rail": f - 37}


def coffee_machine(h):
    """A fancy barista coffee machine: a tall body, a group head, a jug on top."""
    f = base(h)
    return """
<rect x="152" y="%(top)s" width="24" height="%(bh)s" fill="#c8ccd0" stroke="#241c18"/>
<rect x="152" y="%(top)s" width="24" height="6" fill="#a8adb4" stroke="#241c18"/>
<circle cx="164" cy="%(dial)s" r="3" fill="#f0c14b" stroke="#241c18"/>
<rect x="156" y="%(head)s" width="16" height="5" fill="#8a8e96" stroke="#241c18"/>
<rect x="162" y="%(spout)s" width="3" height="6" fill="#4a4a50" stroke="#241c18"/>
<rect x="158" y="%(cup)s" width="10" height="5" fill="#e8e4d8" stroke="#241c18"/>
<rect x="154" y="%(jug)s" width="8" height="6" fill="#d8d4c8" stroke="#241c18"/>
<rect x="166" y="%(jug)s" width="7" height="6" fill="#d8d4c8" stroke="#241c18"/>
<rect x="152" y="%(gauge)s" width="24" height="2" fill="#6a6e76" stroke="#241c18"/>""" % {
        "top": f - 35, "bh": 35, "dial": f - 25, "head": f - 15, "spout": f - 10,
        "cup": f - 5, "jug": f - 41, "gauge": f - 31}


def eating_table(h):
    """A long dining table with a cloth on it, six chairs, and coffee cups.
    Its top is below the soldier's waist: about 18 px off the floor."""
    f = base(h)
    X = 232   # the table's left edge, clear of the fridge
    chairs = ""
    # three chairs behind the table, three in front of it
    for cx in (X + 2, X + 24, X + 46):
        chairs += ('<rect x="%d" y="%g" width="11" height="3" fill="#8a7458" stroke="#241c18"/>\n'
                   '<rect x="%d" y="%g" width="11" height="3" fill="#8a7458" stroke="#241c18"/>\n'
                   '<rect x="%d" y="%g" width="2" height="8" fill="#6a5a44" stroke="#241c18"/>\n' % (
                       cx, f - 18, cx, f - 27, cx + 9, f - 27))
    cups = ""
    for cx in (X + 4, X + 16, X + 28, X + 40, X + 52, X + 64):
        cups += '<rect x="%d" y="%g" width="4" height="4" fill="#e8e4d8" stroke="#241c18"/>' % (cx, f - 22)
    return """
<!-- the chairs, behind the table -->
%(chairs)s
<!-- the table, with the cloth hanging over the front -->
<rect x="%(x)s" y="%(top)s" width="72" height="4" fill="#b8a888" stroke="#241c18"/>
<rect x="%(x)s" y="%(top)s" width="72" height="7" fill="#b04a3a" stroke="#241c18"/>
<rect x="%(x)s" y="%(top)s" width="72" height="2" fill="#c86a52" stroke="#241c18"/>
<rect x="%(l1)s" y="%(top)s" width="3" height="%(lh)s" fill="#8a7458" stroke="#241c18"/>
<rect x="%(l2)s" y="%(top)s" width="3" height="%(lh)s" fill="#8a7458" stroke="#241c18"/>
<!-- the coffee cups, on the table -->
%(cups)s""" % {"chairs": chairs, "cups": cups, "x": X, "l1": X + 5, "l2": X + 64,
                "top": f - 18, "lh": 18}


def fridge(h):
    f = base(h)
    return """
<rect x="180" y="%(top)s" width="36" height="43" fill="#c8ccd0" stroke="#241c18"/>
<rect x="180" y="%(split)s" width="36" height="1" fill="#241c18"/>
<rect x="210" y="%(h1)s" width="3" height="9" fill="#707880"/>
<rect x="210" y="%(h2)s" width="3" height="9" fill="#707880"/>""" % {
        "top": f - 43, "split": f - 15, "h1": f - 35, "h2": f - 12}


def window_(h):
    f = base(h)
    return """
<rect x="180" y="%(top)s" width="36" height="24" fill="#2a4a78" stroke="#241c18"/>
<rect x="196" y="%(top)s" width="3" height="24" fill="#241c18"/>
<rect x="180" y="%(mid)s" width="36" height="3" fill="#241c18"/>
<circle cx="206" cy="%(moon)s" r="4" fill="#e8e4d0"/>""" % {
        "top": f - 83, "mid": f - 73, "moon": f - 75}


def door(x0, x1, h):
    # the door's opening, in the room's own y (from the floor)
    f = base(h)
    return """
<rect x="%g" y="%d" width="%g" height="37" fill="#5a3f2a" stroke="#241c18"/>
<rect x="%g" y="%d" width="%g" height="37" fill="#1a1512"/>""" % (
        x0, f - 37, x1 - x0, x0 + 2, f - 37, x1 - x0 - 4)


def dresser(h):
    """A chest of drawers, with a couple of things on top."""
    f = base(h)
    return """
<rect x="280" y="%(top)s" width="50" height="31" fill="#6a4e38" stroke="#241c18"/>
<rect x="282" y="%(d1)s" width="46" height="8" fill="#7a5a40" stroke="#241c18"/>
<rect x="282" y="%(d2)s" width="46" height="8" fill="#7a5a40" stroke="#241c18"/>
<rect x="282" y="%(d3)s" width="46" height="8" fill="#7a5a40" stroke="#241c18"/>
<rect x="300" y="%(h1)s" width="10" height="2" fill="#c0a060" stroke="#241c18"/>
<rect x="300" y="%(h2)s" width="10" height="2" fill="#c0a060" stroke="#241c18"/>
<rect x="300" y="%(h3)s" width="10" height="2" fill="#c0a060" stroke="#241c18"/>
<!-- a bowl and a jug on top -->
<rect x="288" y="%(bowl)s" width="12" height="6" fill="#a89070" stroke="#241c18"/>
<rect x="308" y="%(jug)s" width="8" height="9" fill="#8a9a8a" stroke="#241c18"/>""" % {
        "top": f - 31, "d1": f - 29, "d2": f - 20, "d3": f - 11,
        "h1": f - 27, "h2": f - 18, "h3": f - 9, "bowl": f - 37, "jug": f - 40}


FURNITURE = {
    "stove": stove, "fridge": fridge, "window": window_,
    "bench": bench, "coffee machine": coffee_machine, "eating table": eating_table,
}


def build_svg(room, clean=False):
    """The room's reference. With clean, the annotation is left out: no labels, no
    screen outline, no scale note. That's the version the painter works from."""
    width, height, items, shelves = ROOMS[room]
    soldier = soldier_canvas()
    STRIP = 0    # the scale strip is its own picture now
    s = []
    s.append('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" '
             'preserveAspectRatio="xMidYMid meet" shape-rendering="crispEdges">' % (width, height))
    s.append('<style>text{font:5px sans-serif;fill:#e8e0d4}</style>')
    # the back wall, with a brick course, and the floor
    s.append('<rect width="%d" height="%d" fill="#4a3a30"/>' % (width, height))
    for y in range(0, height - 15, 6):
        s.append('<rect x="0" y="%d" width="%d" height="1" fill="#3a2c24"/>' % (y, width))
    s.append('<rect x="0" y="%d" width="%d" height="15" fill="#5c4230"/>' % (height - 15, width))
    s.append('<rect x="0" y="%d" width="%d" height="2" fill="#2a1f18"/>' % (height - 15, width))

    for kind, x0, y0, x1, y1 in items:
        if kind == "den door" or kind == "back door":
            s.append(door(x0, x1, height))
        else:
            s.append(FURNITURE[kind](height))
        if not clean:
            s.append('<text x="%g" y="%g" font-size="4" text-anchor="middle">%s</text>' %
                     ((x0 + x1) / 2, height - y0 + 8, kind))

    # the shelves, up above the worktop
    for y, x0, x1 in shelves:
        s.append('<rect x="%g" y="%g" width="%g" height="3" fill="#7a5a3a" stroke="#241c18"/>' %
                 (x0, height - y, x1 - x0))
        s.append('<rect x="%g" y="%g" width="3" height="7" fill="#5c4230"/>' % (x0 + 6, height - y))
        s.append('<rect x="%g" y="%g" width="3" height="7" fill="#5c4230"/>' % (x1 - 9, height - y))

    # the things that make a kitchen a kitchen, at this scale: a hanging lamp, a
    # kettle and a pan on the worktop, a chair, a bin, a pipe, a skirting board
    s.append('<line x1="120" y1="0" x2="120" y2="18" stroke="#241c18" stroke-width="1"/>')
    s.append('<path d="M112 18 h16 l-3 6 h-10 z" fill="#f0c14b" stroke="#241c18" stroke-width="1"/>')
    s.append('<rect x="66" y="%d" width="9" height="7" fill="#c8ccd0" stroke="#241c18" stroke-width="1"/>' % (height - 44))
    s.append('<rect x="70" y="%d" width="2" height="3" fill="#241c18"/>' % (height - 50))
    s.append('<ellipse cx="40" cy="%d" rx="7" ry="2" fill="#3a3a3c" stroke="#241c18" stroke-width="1"/>' % (height - 50))
    s.append('<rect x="0" y="%d" width="%d" height="3" fill="#3a2c24"/>' % (height - 15, width))
    # a bin in the corner
    s.append('<rect x="212" y="%d" width="11" height="14" fill="#4a5058" stroke="#241c18" stroke-width="1"/>' % (height - 29))
    # a pipe along the wall, above the window
    s.append('<rect x="0" y="%d" width="%d" height="3" fill="#6a6258" stroke="#241c18" stroke-width="1"/>' % (height - 108, width))

    # the soldier, on the floor by the door: the model's guide to the scale
    s.append(soldier_svg(12, height - 15, soldier))

    if not clean:
        # the scale, spelled out
        s.append('<line x1="212" y1="%d" x2="212" y2="%d" stroke="#f0c14b" stroke-width="1"/>' %
                 (height - 15 - SOLDIER, height - 15))
        s.append('<text x="215" y="%g" fill="#f0c14b">the soldier: %d px</text>' %
                 (height - 15 - SOLDIER + 4, SOLDIER))

    # the one screen the room is seen in: what the player sees at a time. A room
    # wider or taller than a screen scrolls, and this shows the first screen.
    # It's drawn last, over the room, so it can be seen.
    if not clean:
        screen_w = min(width, 320)
        screen_h = min(height, 180)
        top = height - screen_h
        s.append('<rect x="1" y="%g" width="%g" height="%g" fill="none" stroke="#5aa0e0" stroke-width="1.5" '
                 'stroke-dasharray="5 4"/>' % (top + 1, screen_w - 2, screen_h - 2))
        s.append('<rect x="2" y="%g" width="86" height="12" fill="#0d1a26"/>' % (top + 2))
        s.append('<text x="4" y="%g" fill="#7cc0f0" font-size="5">one screen: %d x %d</text>' %
                 (top + 11, screen_w, screen_h))
    if width > min(width, 320) and not clean:
        s.append('<text x="%g" y="%g" fill="#7cc0f0" font-size="5">the room is %d wide: the player scrolls through it</text>' %
                 (screen_w + 8, top + 11, width))

    # the den's own furniture, along the bottom, to judge the scale against
    s.append('</svg>')
    return "\n".join(s)


def build_strip_svg():
    """The den's own furniture, on its own, to judge the scale against."""
    s = []
    s.append('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 320 56" width="320" height="56" '
             'shape-rendering="crispEdges">')
    s.append('<rect width="320" height="56" fill="#241e1a"/>')
    x = 6
    for what, dx0, dy0, dx1, dy1 in DEN_PIECES:
        w, h = dx1 - dx0, dy1 - dy0
        s.append('<rect x="%g" y="%g" width="%g" height="%g" fill="#7a6a58" stroke="#241c18" stroke-width="1"/>' %
                 (x, 44 - h, w, h))
        s.append('<text x="%g" y="53" font-size="4" fill="#e8e0d4" text-anchor="middle">%s</text>' % (x + w / 2, what))
        x += w + 26
    s.append('</svg>')
    return "\n".join(s)


def main():
    out = sys.argv[1]
    room = sys.argv[2] if len(sys.argv) > 2 else "kitchen"
    clean = "--clean" in sys.argv
    open(out, "w").write(build_svg(room, clean))
    width, height, items, shelves = ROOMS[room]
    if not clean:
        strip = os.path.splitext(out)[0] + "-scale.svg"
        open(strip, "w").write(build_strip_svg())
        print("wrote %s and %s (the room is %d x %d art pixels; the soldier is %d px tall)" %
              (out, strip, width, height, SOLDIER))
    else:
        print("wrote %s (the room is %d x %d art pixels; the soldier is %d px tall; no annotation)" %
              (out, width, height, SOLDIER))


if __name__ == "__main__":
    main()
