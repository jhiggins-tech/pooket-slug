# The house map (house-layout-in-repo; run: python3 docs/design/lobby/house.py docs/design/lobby/house-layout.svg): the den, its upper floor, the loft and the bunks, the tabk
# bay, and the outer rooms behind the doors (the kitchen and the back yard, the basement, the rooftop), drawn to scale in the game's art pixels from what the
# game builds (Core/Lobby/DenMap.cs, BayMap.cs, Ui/Den/DenRoom.cs's stations). Writes an SVG for docs/design/lobby/.
import sys

M = 14            # margin
TOP = 34          # room for the title
BAYX = 334        # where the bay's own x 0 sits (just right of the den's roller door)
H = 344           # the loft's roof, the tallest thing

# the outer rooms: their own screens, each one behind a door, drawn in a band under the house
OH = 180          # a room's height, the den screen's
OW = 420          # a room's width: a screen and a bit, so it scrolls (RoomMap.Width)
WW = 320          # the wintergarden's width: one screen across (RoomMap.WinterWidth)
OGAP = 20         # between two outer rooms
OBAND = 476       # the outer band's top (SVG y), below the key
KITCHEN_X = 0     # the kitchen: out the den's side door
YARD_X = 440      # the back yard: out the kitchen's back door
BASE_X = 880      # the basement: down the trapdoor where the board used to hang
ROOF_X = 1320     # the rooftop: out the loft's far-left door

# The 420-wide rooms' contents are laid out on a 320-wide grid and stretched to the room's width (XS), as their
# paintings were: each came back 1260 wide and was cut to a uniform third. The ways painted inside a room (the
# basement's hole, the bunker's rope) are placed at RoomMap's x, in the room's own pixels.
GRID = 320
XS = OW / GRID

# the bunker's band, right under the basement
BBAND = OBAND + OH + 44
BB2_X = BASE_X

# the loft's own rooms, entered from the loft: its shower block and its wintergarden
LBAND = BBAND + OH + 44
SHOWER_X = 0      # the shower block: out the loft's shower door
WINTER_X = 440    # the wintergarden: out the loft's wintergarden door
WH = 360          # the wintergarden is a tall room: two screens up, a spiral stair to the rose

W = max(BAYX + 993, ROOF_X + OW, WINTER_X + WW) + M
VH = LBAND + WH + 40

def OY(y): return OBAND + OH - y      # an outer room's game y (up) to SVG y (down)
def LY(y): return LBAND + WH - y      # the loft's rooms' game y (up) to SVG y (down)
def SY(y): return LBAND + OH - y      # the shower block's: the shorter room, at the top of the band
def BY(y): return BBAND + OH - y      # the bunker's game y (up) to SVG y (down)

def Y(y): return TOP + H - y          # game y (up) to SVG y (down)

out = []
def rect(x0, y0, x1, y1, cls):
    out.append(f'<rect x="{x0:g}" y="{Y(y1):g}" width="{x1 - x0:g}" height="{y1 - y0:g}" class="{cls}"/>')
def text(x, y, s, cls="t", anchor="middle"):
    s = s.replace("&", "&amp;")
    out.append(f'<text x="{x:g}" y="{Y(y):g}" class="{cls}" text-anchor="{anchor}">{s}</text>')
def label(x0, x1, y, name, note=None):
    text((x0 + x1) / 2, y, name)
    if note: text((x0 + x1) / 2, y - 8, note, "s")
def ladder(x, y0, y1):
    out.append(f'<line x1="{x - 3:g}" y1="{Y(y0):g}" x2="{x - 3:g}" y2="{Y(y1):g}" class="lad"/>')
    out.append(f'<line x1="{x + 3:g}" y1="{Y(y0):g}" x2="{x + 3:g}" y2="{Y(y1):g}" class="lad"/>')
    y = y0 + 4
    while y < y1:
        out.append(f'<line x1="{x - 3:g}" y1="{Y(y):g}" x2="{x + 3:g}" y2="{Y(y):g}" class="lad"/>')
        y += 7
def floor(x0, x1, y):
    out.append(f'<line x1="{x0:g}" y1="{Y(y):g}" x2="{x1:g}" y2="{Y(y):g}" class="fl"/>')

# ---- the den screen: 320 x 180, ground floor (feet at 15) and upper floor (feet at 118) ----
rect(0, 0, 320, 180, "den")
text(4, 172, "THE DEN · one screen, 320 × 180", "h", "start")
floor(0, 320, 15); floor(0, 320, 118)
text(316, 104, "ground floor", "s", "end"); text(316, 164, "upper floor", "s", "end")
# ground floor, left to right
rect(12, 15, 36, 77, "door"); label(12, 36, 58, "side", "door"); text(24, 40, "to the", "s"); text(24, 33, "kitchen →", "s")
rect(36, 15, 82, 60, "st"); label(36, 82, 42, "kie", "bench")
rect(82, 15, 166, 52, "key"); label(82, 166, 34, "COUCH", "pick · ready")
rect(103, 58, 151, 82, "key"); label(103, 151, 72, "TV", "console")
rect(168, 15, 200, 60, "st"); label(168, 200, 42, "tones2", "slushie")
rect(222, 15, 248, 78, "st2"); label(222, 248, 58, "the board", "was here"); text(235, 34, "a nail", "s")
rect(254, 15, 306, 89, "door"); label(254, 306, 56, "ROLLER", "DOOR"); text(280, 34, "to the bay →", "s")
rect(222, 0, 248, 15, "door")   # the trapdoor, in the floor where the board used to hang
# upper floor
rect(12, 118, 108, 160, "key"); label(12, 108, 146, "THE BOARD", "missions"); text(60, 126, "kcaj's string board", "s")
rect(108, 118, 196, 160, "tarp"); label(108, 196, 142, "torikloud", "courtroom")
rect(224, 118, 308, 160, "tarp"); label(224, 308, 142, "larinovsky", "cat bed")
ladder(209, 15, 194)

# ---- the loft: 382 × 164 from x 19 to 401, on top of the den; feet at 194, bunks at 255 ----
rect(19, 180, 401, 344, "loft")
text(23, 336, "THE LOFT · one attic, 382 × 164, pans", "h", "start")
floor(19, 401, 194)
rect(28, 194, 50, 240, "door"); label(28, 50, 222, "door", "rooftop"); text(39, 204, "to the", "s"); text(39, 197, "rooftop →", "s")
rect(52, 194, 86, 236, "st2"); label(52, 86, 220, "bath-", "room")
ladder(88, 194, 255)
rect(95, 194, 139, 245, "door"); text(117, 240, "SHOWERS", "t"); text(117, 232, "→ a room", "s"); text(117, 204, "kiwicore", "s")
rect(142, 194, 165, 240, "tarp"); text(153.5, 230, "laun-", "s"); text(153.5, 223, "dry", "s"); text(153.5, 214, "ciarra", "s")
rect(168, 194, 238, 245, "st"); label(168, 197, 228, "gym", "kit ⓘ"); text(209, 248, "hatch", "s")
rect(241, 194, 260, 245, "st2"); label(241, 260, 228, "tro-", "phies")
rect(262, 194, 289, 236, "st2"); label(262, 289, 220, "server", "cupbd")
rect(291, 194, 362, 245, "door"); text(326, 240, "WINTERGARDEN", "t"); text(326, 232, "→ a room", "s"); text(326, 204, "roses", "s")
rect(366, 194, 395, 245, "tarp"); text(380.5, 232, "vend-", "s"); text(380.5, 225, "ing", "s"); text(380.5, 214, "gary", "s")
# the bunks: a platform in the roof
floor(55, 365, 255)
rect(55, 255, 365, 278, "bunks"); label(55, 365, 268, "THE BUNKS · six bunk beds under the rafters", "crew roster, not yet")
out.append(f'<path d="M19 {Y(255)} L210 {Y(344)} L401 {Y(255)}" class="roof"/>')

# ---- the tabk bay: 993 × 180, through the roller door; feet at 13, top deck at 76 ----
B = BAYX
rect(B, 0, B + 993, 180, "bay")
text(B + 4, 172, "THE TABK BAY · 993 × 180, scrolls sideways (tabks 1.5× here)", "h", "start")
floor(B, B + 993, 13)
rect(B + 0, 13, B + 22, 100, "door"); label(B, B + 22, 64, "←", "den")
rect(B + 92 - 45, 13, B + 92 + 45, 70, "key"); label(B + 47, B + 137, 46, "REPAIR STANDS", "your tabk · crane")
floor(B + 190, B + 706, 66)
for i, x in enumerate([232, 308, 407, 483, 582, 658]):
    rect(B + x - 36, 13, B + x + 36, 48, "space"); text(B + x, 26, f"space {i + 1}", "s")
    rect(B + x - 36, 76, B + x + 36, 111, "space"); text(B + x, 89, f"space {i + 7}", "s")
text(B + 448, 122, "TOP DECK", "t"); text(B + 448, 56, "FLOOR PARKING", "t")
rect(B + 716, 13, B + 790, 130, "lift"); label(B + 716, B + 790, 80, "LIFT", "soldiers only")
rect(B + 820, 13, B + 916, 110, "nyet"); label(B + 820, B + 916, 70, "WASH", "off for now")
rect(B + 940, 13, B + 993, 130, "door"); label(B + 940, B + 993, 70, "STREET", "DOOR · shut")
out.append(f'<circle cx="{B + 92}" cy="{Y(150)}" r="6" class="drone"/>'); text(B + 108, 148, "drone", "s", "start")

# ---- the outer rooms: their own screens, each behind a door, in a band under the house ----
out.append(f'<text x="{M}" y="{OBAND - 12:g}" class="h">THE OUTER ROOMS · each its own screen, behind a door</text>')
def orect(x0, y0, x1, y1, cls, ox=0, ymap=None):
    ym = ymap or OY
    out.append(f'<rect x="{ox + x0 * XS:g}" y="{ym(y1):g}" width="{(x1 - x0) * XS:g}" height="{y1 - y0:g}" class="{cls}"/>')
def otext(x, y, s, cls="t", anchor="middle", ox=0, ymap=None):
    ym = ymap or OY
    s = s.replace("&", "&amp;")
    out.append(f'<text x="{ox + x * XS:g}" y="{ym(y):g}" class="{cls}" text-anchor="{anchor}">{s}</text>')
def olabel(x0, x1, y, name, note=None, ox=0, ymap=None):
    otext((x0 + x1) / 2, y, name, ox=ox, ymap=ymap)
    if note: otext((x0 + x1) / 2, y - 8, note, "s", ox=ox, ymap=ymap)
def ofloor(x0, x1, y, ox=0, ymap=None):
    ym = ymap or OY
    out.append(f'<line x1="{ox + x0 * XS:g}" y1="{ym(y):g}" x2="{ox + x1 * XS:g}" y2="{ym(y):g}" class="fl"/>')
def oladder(x, y0, y1, ox=0, ymap=None):
    ym = ymap or OY
    x *= XS
    out.append(f'<line x1="{ox + x - 3:g}" y1="{ym(y0):g}" x2="{ox + x - 3:g}" y2="{ym(y1):g}" class="lad"/>')
    out.append(f'<line x1="{ox + x + 3:g}" y1="{ym(y0):g}" x2="{ox + x + 3:g}" y2="{ym(y1):g}" class="lad"/>')
    y = y0 + 4
    while y < y1:
        out.append(f'<line x1="{ox + x - 3:g}" y1="{ym(y):g}" x2="{ox + x + 3:g}" y2="{ym(y):g}" class="lad"/>')
        y += 7
def obadge(x, y, n, ox=0, ymap=None):
    """A numbered badge: the same number marks the two ends of one way in."""
    ym = ymap or OY
    out.append(f'<circle cx="{ox + x * XS:g}" cy="{ym(y):g}" r="5" class="badge"/>')
    out.append(f'<text x="{ox + x * XS:g}" y="{ym(y) + 2:g}" class="bn">{n}</text>')
def orope(x, y0, y1, ox=0, ymap=None):
    """A rope, hanging at the room's own x (RoomMap gives it in the room's pixels, so it isn't stretched)."""
    ym = ymap or OY
    out.append(f'<line x1="{ox + x:g}" y1="{ym(y0):g}" x2="{ox + x:g}" y2="{ym(y1):g}" class="lad"/>')
def obadge_house(x, y, n):
    """The same badge, in the house above (its y is the game's, measured up)."""
    out.append(f'<circle cx="{x:g}" cy="{Y(y):g}" r="5" class="badge"/>')
    out.append(f'<text x="{x:g}" y="{Y(y) + 2:g}" class="bn">{n}</text>')

def otitle(ox, name, where, ymap=None, top=172):
    """A room's heading: its name, then where it's reached from, on a line of its own."""
    otext(4, top, name, "h", "start", ox, ymap)
    otext(4, top - 10, where, "s", "start", ox, ymap)

# the kitchen: out the den's side door, on the ground floor
K = KITCHEN_X
orect(0, 0, GRID, OH, "kitchen", K)
otitle(K, "THE KITCHEN · a screen and a bit, 420 × 180", "out the den's side door")
ofloor(0, GRID, 15, K)
orect(0, 15, 24, 52, "door", K); olabel(0, 24, 40, "←", "den", K)
orect(30, 15, 86, 52, "st", K); olabel(30, 86, 40, "STOVE", "worktop", K)
orect(92, 15, 146, 50, "key", K); olabel(92, 146, 38, "TABLE", "eat here", K)
orect(152, 15, 196, 58, "st", K); olabel(152, 196, 46, "FRIDGE", None, K)
orect(152, 74, 196, 98, "st2", K); olabel(152, 196, 90, "WINDOW", "night", K)
orect(294, 15, 320, 52, "door", K); olabel(294, 320, 40, "→", "back door", K)
# the empty shelves, up above the worktop
for sy in (78, 104, 130):
    ofloor(30, 290, sy, K)
    out.append(f'<line x1="{K + 30:g}" y1="{OY(sy - 4):g}" x2="{K + 290:g}" y2="{OY(sy - 4):g}" class="shelvedge"/>')

# the back yard: out the kitchen's back door
Yd = YARD_X
orect(0, 0, GRID, OH, "yard", Yd)
otitle(Yd, "THE BACK YARD · a screen and a bit, 420 × 180", "out the kitchen's back door")
ofloor(0, GRID, 15, Yd)
orect(0, 15, 24, 52, "door", Yd); olabel(0, 24, 40, "←", "kitchen", Yd)
orect(30, 15, 104, 64, "nyet", Yd); olabel(30, 104, 50, "SHED", "shut", Yd)
orect(110, 15, 160, 48, "st2", Yd); olabel(110, 160, 38, "BINS", None, Yd)
orect(166, 15, 236, 56, "st", Yd); olabel(166, 236, 44, "THE PILE", "the drone's", Yd)
orect(266, 15, 320, 74, "st2", Yd); olabel(266, 320, 60, "FENCE", "no way out", Yd)

# the basement: two levels, down the trapdoor where the board used to hang
B = BASE_X
orect(0, 0, GRID, OH, "base", B)
otitle(B, "B1 · THE BASEMENT · a screen and a bit, 420 × 180", "down the trapdoor where the board used to hang")
ofloor(0, GRID, 15, B)
oladder(14, 15, 58, B); olabel(0, 34, 46, "↑", "den", B)
# B1: the gaming den, a long room with the PCs and the arcade
orect(30, 15, 120, 60, "st", B); olabel(30, 120, 48, "THE PCs", "the LAN", B)
orect(126, 15, 186, 56, "key", B); olabel(126, 186, 44, "ARCADE", "Pooket Tabks", B)
orect(192, 15, 250, 54, "st2", B); olabel(192, 250, 42, "SOFA", None, B)
# the hole through the floor to the bunker, the rope hanging down it (RoomMap.WayX: x 372)
HOLE = 372 / XS
orect(HOLE - 22, 0, HOLE + 22, 15, "door", B); olabel(HOLE - 22, HOLE + 22, 9, "THE HOLE", "rope down", B)

# B2: the bunker, below the basement, down the rope through the hole in B1's floor (RoomMap.RopeX: x 351)
BB = BB2_X
orect(0, 0, GRID, OH, "bunker", BB, BY)
otitle(BB, "B2 · THE BUNKER · a screen and a bit, 420 × 180", "down the rope through the hole in B1's floor", BY)
ofloor(0, GRID, 15, BB, BY)
ROPE = 351
orope(ROPE, 15, OH, BB, BY); olabel(ROPE / XS - 30, ROPE / XS - 6, 120, "↑", "rope to B1", BB, BY)
orect(30, 15, 120, 66, "nyet", BB, BY); olabel(30, 120, 52, "EARTH", "not dug", BB, BY)
orect(126, 15, 220, 58, "st2", BB, BY); olabel(126, 220, 46, "SUPPLIES", "the crates", BB, BY)
orect(226, 15, 290, 56, "st2", BB, BY); olabel(226, 290, 44, "THE DOOR", "shut", BB, BY)

# the rooftop: out the loft's far-left door
R = ROOF_X
orect(0, 0, GRID, OH, "roofroom", R)
ofloor(0, GRID, 15, R)
orect(294, 15, 320, 52, "door", R); olabel(294, 320, 40, "→", "loft", R)
orect(190, 15, 258, 56, "key", R); olabel(190, 258, 44, "TELESCOPE", "replays", R)
orect(120, 15, 178, 62, "st2", R); olabel(120, 178, 50, "WATER TANK", None, R)
orect(60, 15, 110, 58, "st2", R); olabel(60, 110, 46, "AERIAL", None, R)
orect(0, 15, 52, 60, "st2", R); olabel(0, 52, 48, "PARAPET", "the street", R)
# the far end: the roof ends at a parapet, so there is nothing more to walk on
orect(0, 60, 52, OH, "wall", R)
otitle(R, "THE ROOFTOP · a screen and a bit, 420 × 180", "out the loft's far-left door")   # over the wall, so it reads

# ---- the loft's own rooms, entered from the loft: its shower block and its wintergarden ----
out.append(f'<text x="{M}" y="{LBAND - 12:g}" class="h">THE LOFT\'S OWN ROOMS · the loft is the way in</text>')

# the shower block: out the loft's shower door, its cubicles behind swinging saloon doors
S = SHOWER_X
orect(0, 0, GRID, OH, "shower", S, SY)
otitle(S, "THE SHOWER BLOCK · a screen and a bit, 420 × 180", "out the loft's shower door", SY)
ofloor(0, GRID, 15, S, SY)
orect(0, 15, 24, 52, "door", S, SY); olabel(0, 24, 40, "←", "loft", S, SY)
orect(30, 15, 92, 56, "st2", S, SY); olabel(30, 92, 44, "SINKS", "the mirror", S, SY)
orect(98, 15, 150, 74, "st", S, SY); olabel(98, 150, 56, "SHOWER", "the cubicle", S, SY)
orect(156, 15, 208, 74, "st", S, SY); olabel(156, 208, 56, "SHOWER", "the cubicle", S, SY)
orect(214, 15, 266, 70, "st2", S, SY); olabel(214, 266, 52, "TOILET", "the cubicle", S, SY)
orect(272, 15, 308, 46, "st2", S, SY); olabel(272, 308, 36, "LOCKERS", None, S, SY)
# the end wall: the room stops here, so the lockers don't look cut off
orect(308, 0, 320, OH, "wall", S, SY)
# the swinging saloon doors: a stall across the front of each cubicle, with a pivot at each end
for cx in (124, 182, 240):
    c, h = S + cx * XS, 26 * XS
    out.append(f'<path d="M{c - h:g} {SY(64):g} L{c - h:g} {SY(30):g} M{c + h:g} {SY(64):g} L{c + h:g} {SY(30):g}" class="saloon"/>')
    out.append(f'<circle cx="{c - h:g}" cy="{SY(64):g}" r="1.6" class="saloondot"/>')
    out.append(f'<circle cx="{c + h:g}" cy="{SY(64):g}" r="1.6" class="saloondot"/>')
    out.append(f'<path d="M{c - h:g} {SY(64):g} L{c + h:g} {SY(64):g}" class="saloon"/>')

# the wintergarden: out the loft's wintergarden door. A tall room, two screens up:
# a spiral stair turns up the wall to the rose at the top, and you have to climb to see it
XS = 1            # one screen across: not stretched
Wn = WINTER_X
orect(0, 0, WW, WH, "winter", Wn, LY)
otitle(Wn, "THE WINTERGARDEN · one tall room, 320 × 360", "out the loft's wintergarden door", LY, 346)
ofloor(0, WW, 15, Wn, LY)
orect(0, 15, 24, 52, "door", Wn, LY); olabel(0, 24, 40, "←", "loft", Wn, LY)
orect(30, 15, 106, 60, "st", Wn, LY); olabel(30, 106, 48, "ROSES", "the beds", Wn, LY)
orect(112, 15, 168, 58, "st2", Wn, LY); olabel(112, 168, 46, "PLANTERS", None, Wn, LY)
orect(242, 15, WW, 64, "st2", Wn, LY); olabel(242, 320, 50, "GREENHOUSE", "the cold", Wn, LY)
# the spiral stair, turning up the wall to the rose at the top: a post, and treads winding round it.
# The treads are a step apart (about 8 px, for the 22 px soldier), so the stair is climbable.
sx = 205
RISER = 8
treads = 34
for i in range(treads):
    t = i / (treads - 1.0)
    rx = 26 - 8 * t                       # the stair tightens as it rises
    ty = 22 + i * RISER
    out.append(f'<ellipse cx="{Wn + sx:g}" cy="{LY(ty):g}" rx="{rx:g}" ry="{rx * 0.3:g}" class="spiral"/>')
    out.append(f'<line x1="{Wn + sx - rx:g}" y1="{LY(ty):g}" x2="{Wn + sx + rx:g}" y2="{LY(ty):g}" class="spiral"/>')
out.append(f'<line x1="{Wn + sx:g}" y1="{LY(15):g}" x2="{Wn + sx:g}" y2="{LY(294):g}" class="spiralpost"/>')
for rx, ry in ((46, 30), (64, 26), (84, 31), (130, 28), (150, 32)):
    out.append(f'<circle cx="{Wn + rx:g}" cy="{LY(ry):g}" r="2.4" class="rose"/>')
# the landing at the top: a platform wide enough for two crew to stand on it
LAND = 296
orect(158, LAND, 256, LAND + 8, "fl2", Wn, LY)
orect(158, LAND, 166, LAND + 8, "fl2", Wn, LY)
out.append(f'<line x1="{Wn + 158:g}" y1="{LY(LAND + 8):g}" x2="{Wn + 256:g}" y2="{LY(LAND + 8):g}" class="fl"/>')
# the rose on the landing, under its glass dome, up where only the stair reaches
out.append(f'<circle cx="{Wn + 232:g}" cy="{LY(LAND + 16):g}" r="7" class="rose"/>')
out.append(f'<circle cx="{Wn + 232:g}" cy="{LY(LAND + 16):g}" r="11" class="dome"/>')
out.append(f'<text x="{Wn + 258:g}" y="{LY(LAND + 18):g}" class="t" text-anchor="start">THE ROSE</text>')
out.append(f'<text x="{Wn + 258:g}" y="{LY(LAND + 10):g}" class="s" text-anchor="start">under glass, at the top</text>')

# ---- the ways in, matched by number: the badge on the door in the house, and the badge on the door in the room
out.append(f'<text x="{BB2_X}" y="{BBAND - 12:g}" class="h">B2 · below the basement</text>')
obadge_house(24, 90, 1)     # the den's side door
obadge_house(235, 8, 3)    # the trapdoor, where the board used to hang
obadge_house(39, 252, 4)    # the loft's far-left door
obadge_house(117, 232, 5)   # the loft's shower door
obadge_house(326, 232, 6)   # the loft's wintergarden door
XS = OW / GRID    # the 420-wide rooms again
obadge(12, 64, 1, K)        # the kitchen's door back to the den
obadge(308, 64, 2, K)       # the kitchen's back door
obadge(12, 64, 2, Yd)       # the yard's door back to the kitchen
obadge(12, 70, 3, B)        # the basement's ladder up to the den
obadge(HOLE, 26, 7, B)      # the hole down to the bunker
obadge(ROPE / XS, 70, 7, BB, BY)  # the bunker's rope up to B1
obadge(308, 64, 4, R)       # the rooftop's door back to the loft
obadge(12, 64, 5, S, SY)    # the shower block's door back to the loft
XS = 1
obadge(12, 64, 6, Wn, LY)   # the wintergarden's door back to the loft

# ---- the key ----
ky = TOP + H + 22
keys = [("den", "the den"), ("key", "what you use there"), ("st", "a crewmate's station"), ("st2", "scenery that's in"),
        ("tarp", "tarped until its crewmate moves in"), ("nyet", "there, but not yet"), ("door", "a door"), ("lift", "lift")]
x = M
for cls, name in keys:
    out.append(f'<rect x="{x}" y="{ky}" width="16" height="10" class="{cls}"/>')
    out.append(f'<text x="{x + 20}" y="{ky + 9}" class="k">{name}</text>')
    x += 30 + len(name) * 4.2
out.append(f'<circle cx="{x + 8:g}" cy="{ky + 5:g}" r="7" class="badge"/>')
out.append(f'<text x="{x + 8:g}" y="{ky + 7.5:g}" class="bn">1</text>')
out.append(f'<text x="{x + 20:g}" y="{ky + 9}" class="k">a way in: the same number on both ends</text>')
x += 30 + 40 * 4.2
out.append(f'<text x="{M}" y="{ky + 30}" class="k">In the game\'s art pixels, to scale. From Core/Lobby/DenMap.cs, BayMap.cs and Ui/Den/DenRoom.cs; the design is docs/design/lobby.md.</text>')
out.append(f'<text x="{M}" y="{ky + 42}" class="k">The outer rooms are their own screens, each one behind a door; the badges mark the two ends of each way in. The bay has no ladder: the lift is the only way up to the top deck.</text>')

svg = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="{-M} 0 {W + M} {VH}" width="{(W + M) * 1.25:g}" font-family="Verdana, DejaVu Sans, sans-serif">
<style>
.den{{fill:#f6d7a8;stroke:#3a2c26}} .loft{{fill:#efe4d2;stroke:#3a2c26}} .bay{{fill:#dbe6dd;stroke:#3a2c26}}
.key{{fill:#ffe9a8;stroke:#3a2c26}} .st{{fill:#fff6e6;stroke:#3a2c26}} .st2{{fill:#f7f1e8;stroke:#8a7a70}}
.tarp{{fill:#c9ccc4;stroke:#3a2c26;stroke-dasharray:3 2}} .nyet{{fill:#f3d2c8;stroke:#a05040;stroke-dasharray:2 2}}
.door{{fill:#a9b8d0;stroke:#2b3a58}} .lift{{fill:#e8c84a;stroke:#3a2c26}} .space{{fill:#eef4ef;stroke:#7a9080;stroke-dasharray:3 2}}
.wall{{fill:#5a4a42;stroke:#3a2c26}}
.bunks{{fill:#e3cfae;stroke:#3a2c26}} .roof{{fill:none;stroke:#3a2c26;stroke-width:1.5}}
.fl{{stroke:#3a2c26;stroke-width:2}} .lad{{stroke:#6a4424;stroke-width:1}} .drone{{fill:#586078;stroke:#1c1420}}
.fl2{{fill:#8a6a48;stroke:#3a2c26}}
.kitchen{{fill:#f2e0c4;stroke:#3a2c26}} .yard{{fill:#dfe6c8;stroke:#3a2c26}} .base{{fill:#d8cfc4;stroke:#3a2c26}}
.roofroom{{fill:#c8d4e6;stroke:#3a2c26}} .earth{{fill:#a89078;stroke:#3a2c26}}
.bunker{{fill:#c8bcae;stroke:#3a2c26}}
.shower{{fill:#dce8ec;stroke:#3a2c26}} .winter{{fill:#e2ecdc;stroke:#3a2c26}}
.saloon{{stroke:#3a2c26;stroke-width:1;fill:none}} .saloondot{{fill:#3a2c26}}
.rose{{fill:#c0396b;stroke:#7a2038}} .dome{{fill:none;stroke:#2b3a58;stroke-width:1}}
.spiral{{fill:none;stroke:#6a4424;stroke-width:1.2}} .spiralpost{{stroke:#6a4424;stroke-width:1.6}}
.shelvedge{{stroke:#8a7a70;stroke-width:1}}
.badge{{fill:#8a3a5a;stroke:#fbf8f3}} .bn{{font-size:7px;font-weight:bold;fill:#ffffff}}
text{{fill:#231a17}} .t{{font-size:7px;font-weight:bold}} .s{{font-size:5.5px;fill:#5a4c44}} .h{{font-size:8px;font-weight:bold;fill:#8a3a5a}} .k{{font-size:7px}}
</style>
<rect x="{-M}" y="0" width="{W + M}" height="{VH}" fill="#fbf8f3"/>
<text x="0" y="18" style="font-size:12px;font-weight:bold;fill:#231a17">The crew's house: the den, the loft, the tabk bay and the outer rooms, as the game builds them</text>
{chr(10).join(out)}
</svg>
'''
open(sys.argv[1], "w").write(svg)
