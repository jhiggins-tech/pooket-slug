# The house map (house-layout-in-repo; run: python3 docs/design/lobby/house.py docs/design/lobby/house-layout.svg): the den, its upper floor, the loft and the bunks, and the tabk bay, drawn to
# scale in the game's art pixels from what the game builds (Core/Lobby/DenMap.cs, BayMap.cs, Ui/Den/DenRoom.cs's
# stations). Writes an SVG for docs/design/lobby/.
import sys

M = 14            # margin
TOP = 34          # room for the title
BAYX = 334        # where the bay's own x 0 sits (just right of the den's roller door)
H = 344           # the loft's roof, the tallest thing
W = BAYX + 993 + M
VH = TOP + H + 74

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
rect(12, 15, 36, 77, "nyet"); label(12, 36, 58, "side", "door"); text(24, 40, "kitchen,", "s"); text(24, 33, "not yet", "s")
rect(36, 15, 82, 60, "st"); label(36, 82, 42, "kie", "bench")
rect(82, 15, 166, 52, "key"); label(82, 166, 34, "COUCH", "pick · ready")
rect(103, 58, 151, 82, "key"); label(103, 151, 72, "TV", "console")
rect(168, 15, 200, 60, "st"); label(168, 200, 42, "tones2", "slushie")
rect(222, 15, 248, 78, "key"); label(222, 248, 58, "BOARD", "missions")
rect(254, 15, 306, 89, "door"); label(254, 306, 56, "ROLLER", "DOOR"); text(280, 34, "to the bay →", "s")
# upper floor
rect(12, 118, 108, 160, "tarp"); label(12, 108, 142, "kcaj", "string board")
rect(108, 118, 196, 160, "tarp"); label(108, 196, 142, "torikloud", "courtroom")
rect(224, 118, 308, 160, "tarp"); label(224, 308, 142, "larinovsky", "cat bed")
ladder(209, 15, 194)

# ---- the loft: 382 × 164 from x 19 to 401, on top of the den; feet at 194, bunks at 255 ----
rect(19, 180, 401, 344, "loft")
text(23, 336, "THE LOFT · one attic, 382 × 164, pans", "h", "start")
floor(19, 401, 194)
rect(28, 194, 50, 240, "nyet"); label(28, 50, 222, "door", "rooftop"); text(39, 204, "not yet", "s")
rect(52, 194, 86, 236, "st2"); label(52, 86, 220, "bath-", "room")
ladder(88, 194, 255)
rect(95, 194, 139, 245, "tarp"); label(95, 139, 228, "showers", "kiwicore")
rect(142, 194, 165, 240, "tarp"); text(153.5, 230, "laun-", "s"); text(153.5, 223, "dry", "s"); text(153.5, 214, "ciarra", "s")
rect(168, 194, 238, 245, "st"); label(168, 197, 228, "gym", "kit ⓘ"); text(209, 248, "hatch", "s")
rect(241, 194, 260, 245, "st2"); label(241, 260, 228, "tro-", "phies")
rect(262, 194, 289, 236, "st2"); label(262, 289, 220, "server", "cupbd")
rect(291, 194, 362, 245, "tarp"); label(291, 362, 228, "wintergarden", "roses")
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
ladder(B + 172, 13, 76)
floor(B + 190, B + 706, 66)
for i, x in enumerate([232, 308, 407, 483, 582, 658]):
    rect(B + x - 36, 13, B + x + 36, 48, "space"); text(B + x, 26, f"space {i + 1}", "s")
    rect(B + x - 36, 76, B + x + 36, 111, "space"); text(B + x, 89, f"space {i + 7}", "s")
text(B + 448, 122, "TOP DECK", "t"); text(B + 448, 56, "FLOOR PARKING", "t")
rect(B + 716, 13, B + 790, 130, "lift"); label(B + 716, B + 790, 80, "LIFT", "soldiers only")
rect(B + 820, 13, B + 916, 110, "nyet"); label(B + 820, B + 916, 70, "WASH", "off for now")
rect(B + 940, 13, B + 993, 130, "door"); label(B + 940, B + 993, 70, "STREET", "DOOR · shut")
out.append(f'<circle cx="{B + 92}" cy="{Y(150)}" r="6" class="drone"/>'); text(B + 108, 148, "drone", "s", "start")

# ---- the key ----
ky = TOP + H + 22
keys = [("den", "the den"), ("key", "what you use there"), ("st", "a crewmate's station"), ("st2", "scenery that's in"),
        ("tarp", "tarped until its crewmate moves in"), ("nyet", "there, but not yet"), ("door", "a door"), ("lift", "lift")]
x = M
for cls, name in keys:
    out.append(f'<rect x="{x}" y="{ky}" width="16" height="10" class="{cls}"/>')
    out.append(f'<text x="{x + 20}" y="{ky + 9}" class="k">{name}</text>')
    x += 30 + len(name) * 4.2
out.append(f'<text x="{M}" y="{ky + 34}" class="k">In the game\'s art pixels, to scale. From Core/Lobby/DenMap.cs, BayMap.cs and Ui/Den/DenRoom.cs; the design is docs/design/lobby.md.</text>')

svg = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="{-M} 0 {W + M} {VH}" width="{(W + M) * 1.25:g}" font-family="Verdana, DejaVu Sans, sans-serif">
<style>
.den{{fill:#f6d7a8;stroke:#3a2c26}} .loft{{fill:#efe4d2;stroke:#3a2c26}} .bay{{fill:#dbe6dd;stroke:#3a2c26}}
.key{{fill:#ffe9a8;stroke:#3a2c26}} .st{{fill:#fff6e6;stroke:#3a2c26}} .st2{{fill:#f7f1e8;stroke:#8a7a70}}
.tarp{{fill:#c9ccc4;stroke:#3a2c26;stroke-dasharray:3 2}} .nyet{{fill:#f3d2c8;stroke:#a05040;stroke-dasharray:2 2}}
.door{{fill:#a9b8d0;stroke:#2b3a58}} .lift{{fill:#e8c84a;stroke:#3a2c26}} .space{{fill:#eef4ef;stroke:#7a9080;stroke-dasharray:3 2}}
.bunks{{fill:#e3cfae;stroke:#3a2c26}} .roof{{fill:none;stroke:#3a2c26;stroke-width:1.5}}
.fl{{stroke:#3a2c26;stroke-width:2}} .lad{{stroke:#6a4424;stroke-width:1}} .drone{{fill:#586078;stroke:#1c1420}}
text{{fill:#231a17}} .t{{font-size:7px;font-weight:bold}} .s{{font-size:5.5px;fill:#5a4c44}} .h{{font-size:8px;font-weight:bold;fill:#8a3a5a}} .k{{font-size:7px}}
</style>
<rect x="{-M}" y="0" width="{W + M}" height="{VH}" fill="#fbf8f3"/>
<text x="0" y="18" style="font-size:12px;font-weight:bold;fill:#231a17">The crew's house: the den, the loft and the tabk bay, as the game builds them</text>
{chr(10).join(out)}
</svg>
'''
open(sys.argv[1], "w").write(svg)
