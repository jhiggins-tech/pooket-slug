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

    # the back yard: the kitchen's back door at the left, the shed, the bins, the
    # drone's pile, and the fence at the end with no way out of it
    "backyard": (320, 180, [
        ("back door", 0, 15, 24, 52),
        ("shed", 56, 15, 124, 62),
        ("bins", 146, 15, 184, 36),
        ("drone pile", 204, 15, 272, 34),
        ("fence", 288, 15, 320, 68),
    ], []),

    # the basement, B1: the trapdoor back up at the left, the PCs along the wall,
    # the arcade, the sofa, and the hole at the far end
    "basement": (320, 180, [
        ("trapdoor", 0, 15, 24, 52),
        ("pcs", 40, 15, 148, 42),
        ("arcade", 158, 15, 196, 66),
        ("sofa", 208, 15, 268, 33),
        ("the hole", 282, 15, 316, 22),
    ], []),

    # the bunker, B2: the hole back up at the right (the rope hangs down from
    # it), the supplies in crates, and the vault door that stays shut
    "bunker": (320, 180, [
        ("crate stack", 40, 15, 120, 56),
        ("barrels", 140, 15, 184, 30),
        ("vault door", 216, 15, 248, 52),
        ("roof hole", 288, 15, 320, 22),
    ], []),

    # the rooftop: the loft's door back at the right, the telescope, the water
    # tank, the aerial, and the parapet over the street at the left end
    "rooftop": (320, 180, [
        ("parapet", 0, 15, 20, 40),
        ("telescope", 44, 15, 88, 56),
        ("water tank", 112, 15, 156, 58),
        ("aerial", 182, 15, 210, 74),
        ("roof door", 296, 15, 320, 52),
    ], []),

    # the shower block: the loft's door back at the left, sinks and a mirror,
    # the cubicles behind swinging doors, and the lockers against the end wall
    "shower": (320, 180, [
        ("door", 0, 15, 24, 52),
        ("sinks", 40, 15, 120, 40),
        ("mirror", 48, 58, 112, 88),
        ("cubicles", 140, 15, 236, 68),
        ("lockers", 252, 15, 304, 56),
    ], []),

    # the wintergarden: a tall room, two screens up. The stair turns up the wall
    # to the landing at the top, where the rose waits under its glass dome
    "wintergarden": (320, 360, [
        ("door", 0, 15, 24, 52),
        ("plants", 60, 15, 110, 40),
        ("stair", 150, 15, 240, 296),
        ("landing", 140, 296, 264, 306),
        ("the rose", 176, 306, 224, 336),
        ("tall window", 268, 60, 308, 180),
    ], []),
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


# the furniture drawers are named after the room items' "kind" above; they are
# all defined below, and FURNITURE maps at the bottom of the file, after them


# ---- the back yard ----

def shed(h):
    """A wooden garden shed with a pitched roof, a door and a small window."""
    f = base(h)
    return """
<rect x="56" y="%(base)s" width="68" height="47" fill="#6a4e38" stroke="#241c18"/>
<path d="M50 %(base)s L90 %(peak)s L130 %(base)s z" fill="#54382a" stroke="#241c18"/>
<rect x="72" y="%(base)s" width="22" height="30" fill="#4a3428" stroke="#241c18"/>
<rect x="108" y="%(base)s" width="10" height="8" fill="#2a4a78" stroke="#241c18"/>""" % {
        "base": f - 47, "peak": f - 62}


def bins(h):
    """Two wheelie bins side by side."""
    f = base(h)
    return """
<rect x="146" y="%(top)s" width="18" height="21" fill="#3c6428" stroke="#241c18"/>
<rect x="168" y="%(top)s" width="16" height="21" fill="#4a5058" stroke="#241c18"/>
<rect x="145" y="%(lid)s" width="20" height="3" fill="#2c4a1e" stroke="#241c18"/>
<rect x="167" y="%(lid)s" width="18" height="3" fill="#3a4048" stroke="#241c18"/>""" % {
        "top": f - 21, "lid": f - 24}


def drone_pile(h):
    """The pile the drone makes of the tabks it moves: a heap of parts and treads."""
    f = base(h)
    return """
<path d="M204 %(f)s L216 %(p1)s L230 %(f)s z" fill="#586078" stroke="#241c18"/>
<path d="M226 %(f)s L244 %(p2)s L262 %(f)s z" fill="#8a9298" stroke="#241c18"/>
<path d="M240 %(f)s L254 %(p1)s L272 %(f)s z" fill="#902820" stroke="#241c18"/>
<rect x="212" y="%(f2)s" width="18" height="5" fill="#3a3a48" stroke="#241c18"/>
<rect x="248" y="%(f2)s" width="16" height="5" fill="#3a3a48" stroke="#241c18"/>""" % {
        "f": f, "f2": f - 5, "p1": f - 20, "p2": f - 26}


def fence(h):
    """The fence at the end of the yard: planks and posts, no way out of it."""
    f = base(h)
    out = ""
    for x in range(288, 320, 6):
        out += '<rect x="%d" y="%d" width="5" height="53" fill="#7a5a3a" stroke="#241c18"/>' % (x, f - 53)
    out += '<rect x="288" y="%d" width="32" height="4" fill="#5c4230" stroke="#241c18"/>' % (f - 46)
    return out


# ---- the basement and the bunker ----

def pcs(h):
    """The LAN: desks with PCs along the wall, their screens glowing."""
    f = base(h)
    out = '<rect x="40" y="%d" width="108" height="4" fill="#7a5a3a" stroke="#241c18"/>' % (f - 26)
    out += '<rect x="44" y="%d" width="4" height="26" fill="#5c4230"/>' % (f - 26)
    out += '<rect x="140" y="%d" width="4" height="26" fill="#5c4230"/>' % (f - 26)
    for x in (52, 82, 112):
        out += ('<rect x="%d" y="%d" width="18" height="13" fill="#2a2630" stroke="#241c18"/>'
                '<rect x="%d" y="%d" width="14" height="9" fill="#4a8a58"/>'
                '<rect x="%d" y="%d" width="10" height="3" fill="#8a8e96"/>' % (
                    x, f - 39, x + 2, f - 37, x + 4, f - 24))
    return out


def arcade(h):
    """The arcade cabinet: a tall box with a screen and a marquee."""
    f = base(h)
    return """
<rect x="158" y="%(top)s" width="38" height="51" fill="#543a6a" stroke="#241c18"/>
<rect x="163" y="%(screen)d" width="28" height="18" fill="#2a4a78" stroke="#241c18"/>
<rect x="163" y="%(marq)s" width="28" height="7" fill="#f0c14b" stroke="#241c18"/>
<rect x="166" y="%(screen)d" width="22" height="4" fill="#3a3a3c"/>""" % {
        "top": f - 51, "marq": f - 51, "screen": f - 34}


def sofa(h):
    """A worn sofa against the wall."""
    f = base(h)
    return """
<rect x="208" y="%(top)s" width="60" height="10" fill="#6a3a5a" stroke="#241c18"/>
<rect x="208" y="%(seat)s" width="60" height="10" fill="#8a4a72" stroke="#241c18"/>
<rect x="208" y="%(top)s" width="6" height="18" fill="#5a3050" stroke="#241c18"/>
<rect x="262" y="%(top)s" width="6" height="18" fill="#5a3050" stroke="#241c18"/>""" % {
        "top": f - 18, "seat": f - 10}


def the_hole(h):
    """The hole dug through the floor: dark earth round it."""
    f = base(h)
    return """
<rect x="282" y="%d" width="34" height="4" fill="#241c18"/>
<rect x="286" y="%d" width="26" height="3" fill="#0d0a08"/>""" % (f - 4, f - 4)


def crate_stack(h):
    """The supplies: crates stacked two high."""
    f = base(h)
    return """
<rect x="40" y="%(t)s" width="36" height="22" fill="#8a6a42" stroke="#241c18"/>
<rect x="80" y="%(t)s" width="36" height="22" fill="#8a6a42" stroke="#241c18"/>
<rect x="58" y="%(u)s" width="36" height="22" fill="#7a5a38" stroke="#241c18"/>
<rect x="44" y="%(m)s" width="28" height="3" fill="#5c4230"/>
<rect x="84" y="%(m)s" width="28" height="3" fill="#5c4230"/>""" % {
        "t": f - 22, "u": f - 44, "m": f - 11}


def barrels(h):
    """Two water barrels."""
    f = base(h)
    return """
<rect x="140" y="%(t)s" width="20" height="15" fill="#4a5058" stroke="#241c18"/>
<rect x="164" y="%(t)s" width="20" height="15" fill="#4a5058" stroke="#241c18"/>
<rect x="140" y="%(m)s" width="20" height="2" fill="#3a4048"/>
<rect x="164" y="%(m)s" width="20" height="2" fill="#3a4048"/>""" % {
        "t": f - 15, "m": f - 8}


def shut_door(h):
    """The bunker's door: metal, riveted, and it stays shut."""
    f = base(h)
    return """
<rect x="216" y="%(t)s" width="32" height="37" fill="#586078" stroke="#241c18"/>
<rect x="220" y="%(t)s" width="24" height="33" fill="#485068" stroke="#241c18"/>
<rect x="240" y="%(h)s" width="5" height="5" fill="#2a2a34"/>""" % {
        "t": f - 37, "h": f - 20}


# ---- the rooftop ----

def parapet(h):
    """The parapet over the street, at the roof's left end."""
    f = base(h)
    return """
<rect x="0" y="%(t)s" width="20" height="25" fill="#8a9298" stroke="#241c18"/>
<rect x="0" y="%(t)s" width="20" height="4" fill="#6a7278" stroke="#241c18"/>""" % {"t": f - 25}


def telescope(h):
    """The telescope on its tripod, which replays the missions."""
    f = base(h)
    return """
<path d="M60 %(f)s L66 %(t)s L72 %(f)s z" fill="#5a5a60" stroke="#241c18"/>
<rect x="48" y="%(b)s" width="40" height="9" fill="#2a2a34" stroke="#241c18" transform="rotate(-12 68 %(m)s)"/>
<rect x="52" y="%(c)s" width="8" height="6" fill="#f0c14b"/>""" % {
        "f": f, "t": f - 28, "b": f - 30, "m": f - 26, "c": f - 24}


def water_tank(h):
    """The water tank on legs."""
    f = base(h)
    return """
<rect x="118" y="%(t)s" width="34" height="26" fill="#8a9298" stroke="#241c18"/>
<rect x="118" y="%(t)s" width="34" height="4" fill="#6a7278" stroke="#241c18"/>
<rect x="122" y="%(l)s" width="3" height="32" fill="#5a5a60"/>
<rect x="145" y="%(l)s" width="3" height="32" fill="#5a5a60"/>""" % {
        "t": f - 58, "l": f - 32}


def aerial(h):
    """The TV aerial: a mast with its cross pieces."""
    f = base(h)
    out = '<rect x="194" y="%d" width="3" height="59" fill="#5a5a60" stroke="#241c18"/>' % (f - 59)
    for i, w in enumerate((22, 18, 14)):
        out += '<rect x="%d" y="%d" width="%d" height="2" fill="#5a5a60"/>' % (196 - w // 2 - 1, f - 52 + i * 7, w)
    return out


# ---- the shower block ----

def sinks(h):
    """A row of sinks with a mirror over them."""
    f = base(h)
    out = ""
    for x in (40, 68, 96):
        out += ('<rect x="%d" y="%d" width="22" height="8" fill="#e8e4d8" stroke="#241c18"/>'
                '<rect x="%d" y="%d" width="3" height="12" fill="#b8bcc4"/>' % (x, f - 18, x + 9, f - 10))
    return out


def mirror(h):
    return """
<rect x="48" y="%(t)s" width="64" height="30" fill="#a8b0c0" stroke="#241c18"/>
<rect x="52" y="%(t)s" width="56" height="22" fill="#c8d0e0" stroke="#241c18"/>""" % {"t": base(h) - 43}


def cubicles(h):
    """The shower and toilet cubicles behind swinging saloon doors."""
    f = base(h)
    out = '<rect x="140" y="%d" width="96" height="4" fill="#7a5a3a" stroke="#241c18"/>' % (f - 53)
    for x in (140, 172, 204):
        # the swinging door, half open
        out += ('<rect x="%d" y="%d" width="26" height="34" fill="#a07a4e" stroke="#241c18"/>'
                '<rect x="%d" y="%d" width="8" height="34" fill="#8a6a40" stroke="#241c18"/>' % (
                    x + 4, f - 49, x + 4, f - 49))
    return out


def lockers(h):
    """The lockers against the end wall."""
    f = base(h)
    out = '<rect x="252" y="%d" width="52" height="41" fill="#4a5a6a" stroke="#241c18"/>' % (f - 41)
    for x in (256, 274, 292):
        out += ('<rect x="%d" y="%d" width="16" height="37" fill="#3c4c5c" stroke="#241c18"/>'
                '<rect x="%d" y="%d" width="2" height="6" fill="#8a9298"/>' % (x, f - 39, x + 12, f - 30))
    return out


# ---- the wintergarden ----

def plants(h):
    """Potted plants along the wall: a fern and two pots."""
    f = base(h)
    return """
<rect x="64" y="%(p)s" width="14" height="12" fill="#a05a3a" stroke="#241c18"/>
<path d="M60 %(p)s L71 %(t)s L82 %(p)s z" fill="#3c6428" stroke="#241c18"/>
<rect x="92" y="%(p)s" width="12" height="10" fill="#8a6a4a" stroke="#241c18"/>
<circle cx="98" cy="%(b)s" r="8" fill="#5c8c40" stroke="#241c18"/>""" % {
        "p": f - 12, "t": f - 30, "b": f - 20}


def stair(h):
    """The spiral stair turning up the wall: treads a step apart for the soldier."""
    f = base(h)
    out = '<rect x="150" y="0" width="90" height="3" fill="#6a6258" stroke="#241c18"/>'  # the rail at the top
    for i in range(14):
        y = f - 20 - i * 20          # a tread every 20 px: climbable
        x = 150 + (i % 4) * 22       # four treads to a turn
        out += '<rect x="%d" y="%d" width="20" height="3" fill="#7a5a3a" stroke="#241c18"/>' % (x, y)
    return out


def landing(h):
    """The landing at the top of the stair, wide enough for two crew. Its floor
    is at LandingY (296) above the room's own floor."""
    y = h - 296
    return """
<rect x="140" y="%d" width="124" height="6" fill="#7a5a3a" stroke="#241c18"/>
<rect x="140" y="%d" width="124" height="3" fill="#5c4230"/>""" % (y, y)


def the_rose(h):
    """The rose under its glass dome, on the landing."""
    y = h - 296   # the landing's floor
    return """
<rect x="176" y="%d" width="48" height="3" fill="#b8bcc4" stroke="#241c18"/>
<path d="M180 %d v-28 h40 v28" fill="none" stroke="#8aa8c0" stroke-width="1"/>
<path d="M180 %d h40" stroke="#8aa8c0" stroke-width="1"/>
<rect x="194" y="%d" width="12" height="6" fill="#5c4230" stroke="#241c18"/>
<circle cx="200" cy="%d" r="6" fill="#c04060" stroke="#241c18"/>
<circle cx="200" cy="%d" r="3" fill="#d06080"/>""" % (
        y, y, y, y - 6, y - 12, y - 12)


def tall_window(h):
    """A tall window down the wintergarden's wall."""
    f = base(h)
    return """
<rect x="268" y="%(t)s" width="40" height="120" fill="#2a4a78" stroke="#241c18"/>
<rect x="286" y="%(t)s" width="3" height="120" fill="#241c18"/>
<rect x="268" y="%(m)s" width="40" height="3" fill="#241c18"/>""" % {
        "t": f - 165, "m": f - 105}


def hatch(h):
    """The trapdoor back up to the den, open in the ceiling of the basement."""
    return """
<rect x="0" y="14" width="24" height="4" fill="#5c4230" stroke="#241c18"/>
<rect x="2" y="18" width="20" height="3" fill="#3a2c24"/>"""


FURNITURE = {
    "stove": stove, "fridge": fridge, "window": window_,
    "bench": bench, "coffee machine": coffee_machine, "eating table": eating_table,
    "shed": shed, "bins": bins, "drone pile": drone_pile, "fence": fence,
    "pcs": pcs, "arcade": arcade, "sofa": sofa, "the hole": the_hole,
    "crate stack": crate_stack, "barrels": barrels, "shut door": shut_door,
    "parapet": parapet, "telescope": telescope, "water tank": water_tank, "aerial": aerial,
    "sinks": sinks, "mirror": mirror, "cubicles": cubicles, "lockers": lockers,
    "plants": plants, "stair": stair, "landing": landing, "the rose": the_rose,
    "tall window": tall_window, "trapdoor": hatch,
}


def build_svg(room, clean=False, gen=False):
    """The room's reference. With clean, the annotation is left out: no labels, no
    screen outline, no scale note. That's the version the painter works from.
    With gen (and not clean), the labels and the scale note stay in — the model
    reads them — but the blue screen outline goes, and the soldier is stamped
    at several spots along the floor, so the scale cue repeats across the frame."""
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
        if kind.endswith("door") and kind in ("den door", "back door", "roof door", "door"):
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

    # the soldier, on the floor by the door: the model's guide to the scale. The
    # gen version stamps him at several spots, so the scale cue repeats
    if gen:
        for sx in (12, 120, 228, 336):
            s.append(soldier_svg(sx, height - 15, soldier))
    else:
        s.append(soldier_svg(12, height - 15, soldier))

    if not clean:
        # the scale, spelled out
        s.append('<line x1="212" y1="%d" x2="212" y2="%d" stroke="#f0c14b" stroke-width="1"/>' %
                 (height - 15 - SOLDIER, height - 15))
        s.append('<text x="215" y="%g" fill="#f0c14b">the soldier: %d px</text>' %
                 (height - 15 - SOLDIER + 4, SOLDIER))
        if gen:
            s.append('<text x="260" y="%g" fill="#f0c14b">the door is 37 px tall: under twice him. '
                     'keep every thing this size</text>' % (height - 15 - SOLDIER + 10,))

    # the one screen the room is seen in: what the player sees at a time. A room
    # wider or taller than a screen scrolls, and this shows the first screen.
    # It's drawn last, over the room, so it can be seen. The gen version leaves
    # it out: the model would copy the blue line
    if not clean and not gen:
        screen_w = min(width, 320)
        screen_h = min(height, 180)
        top = height - screen_h
        s.append('<rect x="1" y="%g" width="%g" height="%g" fill="none" stroke="#5aa0e0" stroke-width="1.5" '
                 'stroke-dasharray="5 4"/>' % (top + 1, screen_w - 2, screen_h - 2))
        s.append('<rect x="2" y="%g" width="86" height="12" fill="#0d1a26"/>' % (top + 2))
        s.append('<text x="4" y="%g" fill="#7cc0f0" font-size="5">one screen: %d x %d</text>' %
                 (top + 11, screen_w, screen_h))
    if width > min(width, 320) and not clean and not gen:
        s.append('<text x="%g" y="%g" fill="#7cc0f0" font-size="5">the room is %d wide: the player scrolls through it</text>' %
                 (screen_w + 8, top + 11, width))

    # the den's own furniture, along the bottom, to judge the scale against
    s.append('</svg>')
    return "\n".join(s)


def build_blocks_svg(room):
    """The room as a layout diagram only: flat abstract blocks, labelled, on a
    plain wall — no furniture art at all. The painter gets the den's and the
    loft's paintings alongside it as the style, so the diagram only has to say
    where each thing goes and how big it is, not what it looks like."""
    width, height, items, _ = ROOMS[room]
    soldier = soldier_canvas()
    floor = height - 15
    s = []
    s.append('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" '
             'preserveAspectRatio="xMidYMid meet" shape-rendering="crispEdges">' % (width, height))
    s.append('<style>text{font:5px sans-serif;fill:#e8e0d4}</style>')
    # a plain wall and floor: no texture, no brick, no props of our own
    s.append('<rect width="%d" height="%d" fill="#3a3230"/>' % (width, height))
    s.append('<rect x="0" y="%d" width="%d" height="15" fill="#2c2624"/>' % (floor, width))
    s.append('<rect x="0" y="%d" width="%d" height="2" fill="#1c1816"/>' % (floor, width))

    for kind, x0, y0, x1, y1 in items:
        if kind == "roof hole":
            # the way in: a hole broken up through the ceiling, with the rope
            # hanging down from it — the same hole the basement's floor opens
            s.append('<rect x="%g" y="0" width="%g" height="4" fill="#0d0a08" stroke="#241c18" stroke-width="0.5"/>' %
                     (x0, x1 - x0))
            s.append('<rect x="%g" y="0" width="%g" height="1.5" fill="#241c18"/>' % (x0 - 2, x1 - x0 + 4))
            s.append('<line x1="%g" y1="4" x2="%g" y2="%g" stroke="#a8926a" stroke-width="1.5"/>' %
                     ((x0 + x1) / 2, (x0 + x1) / 2, floor))
            for ry in range(10, int(floor) - 4, 10):
                s.append('<line x1="%g" y1="%d" x2="%g" y2="%d" stroke="#a8926a" stroke-width="1"/>' %
                         ((x0 + x1) / 2 - 2, ry, (x0 + x1) / 2 + 2, ry))
            s.append('<text x="%g" y="12" font-size="4" text-anchor="end">hole up through the roof, rope down</text>' %
                     (x1 - 2))
            continue
        if kind == "vault door":
            # a circular vault door, not a door leaf: round steel door in a
            # ring frame, wheel handle, rivets round the rim
            cx, r = (x0 + x1) / 2, (y1 - y0) / 2
            cy = floor - r
            s.append('<circle cx="%g" cy="%g" r="%g" fill="#586078" stroke="#241c18" stroke-width="1"/>' % (cx, cy, r))
            s.append('<circle cx="%g" cy="%g" r="%g" fill="#485068" stroke="#241c18" stroke-width="0.5"/>' % (cx, cy, r - 4))
            s.append('<circle cx="%g" cy="%g" r="%g" fill="none" stroke="#241c18" stroke-width="0.5"/>' % (cx, cy, r - 8))
            s.append('<circle cx="%g" cy="%g" r="3" fill="#2a2a34" stroke="#241c18" stroke-width="0.5"/>' % (cx, cy))
            for ang in range(0, 360, 30):
                import math
                wx = cx + (r - 6) * math.cos(ang * math.pi / 180)
                wy = cy + (r - 6) * math.sin(ang * math.pi / 180)
                s.append('<circle cx="%g" cy="%g" r="1" fill="#2a2a34"/>' % (wx, wy))
            s.append('<text x="%g" y="%g" font-size="4" text-anchor="middle">vault door</text>' % (cx, floor - y1 - 2))
            continue
        if kind == "trapdoor":
            # the hatch is in the ceiling, and the ladder hangs under it: drawn
            # as a floor-level block, the painter read the trapdoor as a door
            # and tucked the ladder away behind the computers
            s.append('<rect x="%g" y="0" width="%g" height="2" fill="#1c1816"/>' % (x0, x1 - x0))
            s.append('<rect x="%g" y="2" width="%g" height="7" fill="#6a6460" stroke="#241c18" stroke-width="1"/>' %
                     (x0, x1 - x0))
            s.append('<rect x="%g" y="3" width="%g" height="5" fill="#1c1816"/>' % (x0 + 3, x1 - x0 - 6))
            for lx in (x0 + 2, x1 - 4):
                s.append('<rect x="%g" y="9" width="2" height="%d" fill="#8a8480"/>' % (lx, floor - 9))
            for ly in range(12, floor, 9):
                s.append('<rect x="%g" y="%d" width="%g" height="1" fill="#8a8480"/>' % (x0 + 2, ly, x1 - x0 - 4))
            s.append('<text x="%g" y="20" font-size="4" text-anchor="start">trapdoor and ladder</text>' % (x1 + 3))
            continue
        if kind == "stair":
            # the treads are the structure: without them the block reads as a
            # solid wall, and the stair is what the player climbs
            for i in range(14):
                ty = floor - 20 - i * 20
                tx = x0 + (i % 4) * 22
                s.append('<rect x="%g" y="%g" width="20" height="3" fill="#8a8480" stroke="#241c18" stroke-width="0.5"/>' %
                         (tx, ty))
            s.append('<text x="%g" y="%g" font-size="4" text-anchor="start">spiral stair, treads 20 px apart</text>' %
                     (x0 + 2, floor - 6))
            continue
        if kind == "the rose":
            # a dome on the landing: the block said slab, the painter needs the
            # glass shape it actually is
            ry = height - 296
            s.append('<rect x="%g" y="%g" width="%g" height="3" fill="#8a8480" stroke="#241c18" stroke-width="0.5"/>' %
                     (x0, ry - 3, x1 - x0))
            s.append('<path d="M%g %g v-14 h%g v14" fill="none" stroke="#8a8480" stroke-width="1"/>' %
                     (x0 + 4, ry - 3, x1 - x0 - 8))
            s.append('<path d="M%g %g a%g %g 0 0 1 %g 0" fill="none" stroke="#8a8480" stroke-width="1"/>' %
                     (x0 + 4, ry - 17, (x1 - x0 - 8) / 2, (x1 - x0 - 8) / 2, x1 - x0 - 8))
            s.append('<text x="%g" y="%g" font-size="4" text-anchor="middle">the rose under glass</text>' %
                     ((x0 + x1) / 2, ry - 26))
            continue
        if kind == "the hole":
            # a gap broken down through the floor, not a thing standing on it:
            # dark opening in the floor strip, rubble round the lip
            s.append('<rect x="%g" y="%g" width="%g" height="15" fill="#0d0a08" stroke="#241c18" stroke-width="0.5"/>' %
                     (x0, floor, x1 - x0))
            s.append('<rect x="%g" y="%g" width="%g" height="2" fill="#241c18"/>' % (x0 - 2, floor - 2, x1 - x0 + 4))
            s.append('<text x="%g" y="%g" font-size="4" text-anchor="end">hole down through the floor</text>' %
                     (x0 - 4, floor - 4))
            continue
        if kind == "cubicles":
            # three doors, not one slab: the divisions are the structure
            w3 = (x1 - x0) / 3
            for i in range(3):
                cx0 = x0 + i * w3
                s.append('<rect x="%g" y="%g" width="%g" height="%g" fill="#6a6460" stroke="#241c18" stroke-width="1"/>' %
                         (cx0, floor - y1, w3 - 2, y1 - y0))
            s.append('<text x="%g" y="%g" font-size="4" text-anchor="middle">cubicles: three doors</text>' %
                     ((x0 + x1) / 2, floor - y1 + 8))
            continue
        if kind.endswith("door") and kind in ("den door", "back door", "roof door", "door", "shut door"):
            # a doorway: dark opening with the block as its frame, so the
            # painter reads it as a way through, not a cabinet
            s.append('<rect x="%g" y="%g" width="%g" height="%g" fill="#6a6460" stroke="#241c18" stroke-width="1"/>' %
                     (x0, floor - y1, x1 - x0, y1 - y0))
            s.append('<rect x="%g" y="%g" width="%g" height="%g" fill="#14100e"/>' %
                     (x0 + 4, floor - y1 + 4, x1 - x0 - 8, y1 - y0 - 4))
            s.append('<text x="%g" y="%g" font-size="4" text-anchor="middle">%s</text>' %
                     ((x0 + x1) / 2, floor - y1 + 8, kind))
            continue
        w, h = x1 - x0, y1 - y0
        # every block the same grey, with a dark edge: shape and place only
        s.append('<rect x="%g" y="%g" width="%g" height="%g" fill="#6a6460" stroke="#241c18" stroke-width="1"/>' %
                 (x0, floor - y1, w, h))
        s.append('<text x="%g" y="%g" font-size="4" text-anchor="middle">%s</text>' %
                 ((x0 + x1) / 2, floor - y1 + 8, kind))

    # the soldier, stamped along the floor: the scale cue, as in the --gen mode
    for sx in (12, 120, 228, width - 20):
        s.append(soldier_svg(sx, floor, soldier))
    # the scale, spelled out, on a chip in the free band above the blocks — at
    # the right when something hangs at the top left (the basement's ladder),
    # at the left otherwise (the wintergarden's rose sits top middle)
    chip_right = any(kind == "trapdoor" for kind, *_ in items)
    cx = width - 154 if chip_right else 4
    s.append('<rect x="%d" y="4" width="150" height="26" fill="#0d1a26"/>' % cx)
    s.append('<line x1="%d" y1="10" x2="%d" y2="26" stroke="#f0c14b" stroke-width="1"/>' % (cx + 6, cx + 6))
    s.append('<text x="%d" y="14" fill="#f0c14b">the soldier: %d px tall</text>' % (cx + 10, SOLDIER))
    s.append('<text x="%d" y="22" fill="#f0c14b">the door: 37 px, under twice him.</text>' % (cx + 10))
    s.append('<text x="%d" y="28" fill="#f0c14b">keep every thing this size</text>' % (cx + 10))
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
    gen = "--gen" in sys.argv
    blocks = "--blocks" in sys.argv
    if blocks:
        open(out, "w").write(build_blocks_svg(room))
        width, height, items, shelves = ROOMS[room]
        print("wrote %s (the room is %d x %d art pixels; the soldier is %d px tall; blocks only)" %
              (out, width, height, SOLDIER))
        return
    open(out, "w").write(build_svg(room, clean, gen))
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
