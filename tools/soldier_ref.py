"""Renders the game's soldier, from the game's own art data, as a picture.

The room plates are drawn for this figure: the den and the loft look right at
this scale, so a room has to be drawn to it. This writes the figure out at the
size it's drawn in the game (22 px tall), and bigger, so it can be put in a
reference picture.

usage: python3 -I tools/soldier_ref.py <out.png> [scale]
"""
import os
import re
import struct
import sys
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

# the shared figure's palette (Assets/Scripts/Art/Art.cs, s_Pal)
PAL = {
    'k': 0x201820, 'r': 0xd04030, 'R': 0x902820, 'h': 0x6a4424, 's': 0xf8c090,
    'S': 0xd08c60, 'e': 0x201820, 'c': 0xe0d0a0, 'C': 0xb0a070, 'g': 0x5c8c40,
    'G': 0x3c6428, 'b': 0x4c3828, 'y': 0xf0c840, 'p': 0xc09858, 'P': 0x8c6c3c,
    'w': 0x4a3428, 'm': 0xa8b0c0, 'M': 0x586078, 'W': 0xffffff, 'o': 0xf08830,
    'O': 0xc05020, 'x': 0xffe860, 'd': 0x707888, 'D': 0x454b5c, 't': 0x8a9a4a,
    'T': 0x5a6a30, 'u': 0x3a3a48, 'z': 0xc8d0e0, 'v': 0x6b4fb0, 'V': 0x45307a,
}
OUTLINE = 0x1c1420
HEAD_TOP = 9
BODY_TOP = 8
LEGS_TOP = 22
W, H = 24, 32


def read_rows(path, name):
    """The rows of a `public static readonly string[] <name>` in a C# file."""
    text = open(path).read()
    i = text.index(name)
    j = text.index('{', i)
    k = text.index('}', j)
    return re.findall(r'"([^"]*)"', text[j:k])


def hex_rgb(h):
    return ((h >> 16) & 255, (h >> 8) & 255, h & 255)


def png_write(path, w, h, rows):
    raw = bytearray()
    for row in rows:
        raw.append(0)
        for px in row:
            raw += bytes(px)
    def chunk(kind, body):
        return (struct.pack(">I", len(body)) + kind + body +
                struct.pack(">I", zlib.crc32(kind + body) & 0xffffffff))
    out = b"\x89PNG\r\n\x1a\n"
    out += chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
    out += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    out += chunk(b"IEND", b"")
    open(path, "wb").write(out)


def main():
    out = sys.argv[1]
    scale = int(sys.argv[2]) if len(sys.argv) > 2 else 8
    art = os.path.join(ROOT, "Assets/Scripts/Art/ArtData.cs")

    head = read_rows(art, "Head")
    torso = read_rows(art, "TorsoUnarmed")
    legs = read_rows(art, "LegsIdle")

    canvas = [[None for _ in range(W)] for _ in range(H)]

    def put(rows, top, x0=0):
        for i, row in enumerate(rows):
            for x, ch in enumerate(row):
                if ch == '.' or ch == ' ':
                    continue
                if x0 + x >= W or top + i >= H or top + i < 0:
                    continue
                canvas[top + i][x0 + x] = hex_rgb(PAL.get(ch, OUTLINE))

    put(head, HEAD_TOP)
    put(torso, BODY_TOP)
    put(legs, LEGS_TOP)

    # the outline: a dark pixel on every empty one that touches a filled one
    filled = [[canvas[y][x] is not None for x in range(W)] for y in range(H)]
    for y in range(H):
        for x in range(W):
            if filled[y][x]:
                continue
            edge = ((x > 0 and filled[y][x - 1]) or (x < W - 1 and filled[y][x + 1]) or
                    (y > 0 and filled[y - 1][x]) or (y < H - 1 and filled[y + 1][x]))
            if edge:
                canvas[y][x] = hex_rgb(OUTLINE)

    rows = []
    for y in range(H):
        line = []
        for x in range(W):
            line += [canvas[y][x] or (0, 0, 0)] * scale
        for _ in range(scale):
            rows.append(list(line))
    png_write(out, W * scale, H * scale, rows)
    print("wrote %s  %d x %d (the figure is 22 px tall in the game)" % (out, W * scale, H * scale))


if __name__ == "__main__":
    main()
