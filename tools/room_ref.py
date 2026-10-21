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
    # the kitchen, in art pixels, from the house map: (kind, x0, y0, x1, y1, label)
    "kitchen": (320, 180, [
        ("den door", 0, 15, 24, 52),
        ("stove", 30, 15, 86, 52),
        ("table", 92, 15, 146, 50),
        ("fridge", 152, 15, 196, 58),
        ("window", 152, 74, 196, 98),
        ("back door", 294, 15, 320, 52),
    ], [(78, 30, 290), (104, 30, 290), (130, 30, 290)]),
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
# each is a function that takes the room's height and returns SVG
def stove(h):
    return """
<rect x="30" y="%(top)s" width="56" height="37" fill="#8a9298" stroke="#241c18"/>
<rect x="30" y="%(cook)s" width="24" height="30" fill="#6a7278" stroke="#241c18"/>
<rect x="54" y="%(cook)s" width="32" height="12" fill="#6a7278" stroke="#241c18"/>
<rect x="34" y="%(cook)s" width="16" height="7" fill="#2a2622"/>
<rect x="32" y="%(top)s" width="14" height="9" fill="#2a2622"/>
<path d="M38 %(top2)s h12 v-9 h14 v9" fill="none" stroke="#241c18" stroke-width="1"/>""" % {
        "top": h - 52, "cook": h - 45, "top2": h - 43}


def table(h):
    return """
<rect x="92" y="%(top)s" width="54" height="6" fill="#a07a4e" stroke="#241c18"/>
<rect x="96" y="%(top)s" width="5" height="35" fill="#7a5a38" stroke="#241c18"/>
<rect x="137" y="%(top)s" width="5" height="35" fill="#7a5a38" stroke="#241c18"/>
<rect x="96" y="%(mid)s" width="46" height="4" fill="#7a5a38" stroke="#241c18"/>""" % {
        "top": h - 44, "mid": h - 26}


def fridge(h):
    return """
<rect x="152" y="%(top)s" width="44" height="43" fill="#c8ccd0" stroke="#241c18"/>
<rect x="152" y="%(split)s" width="44" height="1" fill="#241c18"/>
<rect x="188" y="%(h1)s" width="3" height="9" fill="#707880"/>
<rect x="188" y="%(h2)s" width="3" height="9" fill="#707880"/>""" % {
        "top": h - 58, "split": h - 30, "h1": h - 50, "h2": h - 27}


def window_(h):
    return """
<rect x="152" y="%(top)s" width="44" height="24" fill="#2a4a78" stroke="#241c18"/>
<rect x="172" y="%(top)s" width="3" height="24" fill="#241c18"/>
<rect x="152" y="%(mid)s" width="44" height="3" fill="#241c18"/>
<circle cx="186" cy="%(moon)s" r="4" fill="#e8e4d0"/>""" % {
        "top": h - 98, "mid": h - 88, "moon": h - 90}


def door(x0, x1, h):
    # the door's opening, in the room's own y (from the floor)
    return """
<rect x="%g" y="%d" width="%g" height="37" fill="#5a3f2a" stroke="#241c18"/>
<rect x="%g" y="%d" width="%g" height="37" fill="#1a1512"/>""" % (
        x0, h - 52, x1 - x0, x0 + 2, h - 52, x1 - x0 - 4)


FURNITURE = {
    "stove": stove, "table": table, "fridge": fridge, "window": window_,
}


def build_svg(room):
    width, height, items, shelves = ROOMS[room]
    soldier = soldier_canvas()
    STRIP = 50   # the scale strip along the bottom
    s = []
    s.append('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" width="%d" height="%d" '
             'shape-rendering="crispEdges">' % (width, height + STRIP, width * 3, (height + STRIP) * 3))
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
        s.append('<text x="%g" y="%g" font-size="4">%s</text>' % ((x0 + x1) / 2, height - y0 + 9, kind))

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

    # the soldier, on the floor by the door
    s.append(soldier_svg(12, height - 15, soldier))

    # the scale, spelled out
    s.append('<line x1="212" y1="%d" x2="212" y2="%d" stroke="#f0c14b" stroke-width="1"/>' %
             (height - 15 - SOLDIER, height - 15))
    s.append('<text x="215" y="%g" fill="#f0c14b">the soldier: %d px</text>' %
             (height - 15 - SOLDIER + 4, SOLDIER))

    # the den's own furniture, along the bottom, to judge the scale against
    strip = height
    s.append('<rect x="0" y="%d" width="%d" height="50" fill="#241e1a"/>' % (strip, width))
    x = 4
    for what, dx0, dy0, dx1, dy1 in DEN_PIECES:
        w, h = dx1 - dx0, dy1 - dy0
        s.append('<rect x="%g" y="%g" width="%g" height="%g" fill="#7a6a58" stroke="#241c18" stroke-width="1"/>' %
                 (x, strip + 38 - h, w, h))
        s.append('<text x="%g" y="%g" font-size="4" text-anchor="middle">%s</text>' % (x + w / 2, strip + 47, what))
        x += w + 22
    s.append('</svg>')
    return "\n".join(s)


def main():
    out = sys.argv[1]
    room = sys.argv[2] if len(sys.argv) > 2 else "kitchen"
    open(out, "w").write(build_svg(room))
    width, height, items, shelves = ROOMS[room]
    print("wrote %s (the room is %d x %d art pixels; the soldier is %d px tall)" %
          (out, width, height, SOLDIER))


if __name__ == "__main__":
    main()
