"""Make the splash screen's logo from logo C1 (docs/design/art.md): the whole logo, and its parts for the animation.

usage: python3 -I tools/title_logo.py <logo-c1.png> <Assets/Resources/Title>

Scales the logo to the size it shows at on a 1080-pixel-high phone (440 of the canvas's 360-high units, about 1280
pixels wide), then bakes in a thin dark outline (the one keying took away) and a soft drop shadow, so the gold
letters hold up on the colour ramp. Writes, all PNGs as .bytes:
- logo.bytes: the whole logo, for a build without the parts;
- parts.bytes: the ten letters and the tabk cut apart, each with its outline, packed in rows;
- parts-shadow.bytes: each part's shadow, at the same place in its own sheet (drawn under every part, so a letter's
  shadow never darkens its neighbour);
- parts-gold.bytes: each part's gold, white where it's bright, the glint's mask;
- parts.txt: where each part sits in the whole logo and in the sheets (TitleLogoParts reads it).
Needs Pillow and numpy.
"""
import os
import sys
from collections import deque

import numpy as np
from PIL import Image, ImageFilter

WIDTH, PAD = 1280, 24
OUTLINE, SHADOW = (20, 10, 6, 255), (4, 3, 22, 255)
LETTERS = ["P", "O1", "O2", "K", "E", "T", "S", "L", "U", "G"]
SHEET_WIDTH, GUTTER = 1024, 4
GOLD_LUMA = 95


def grown(alpha):
    return alpha.filter(ImageFilter.MaxFilter(7))  # the letters grown by 3 px: about one canvas unit


def shadow_of(outline):
    shadow = Image.new("L", outline.size, 0)
    shadow.paste(outline, (0, 8))
    return shadow.filter(ImageFilter.GaussianBlur(7)).point(lambda v: int(v * 0.7))


def whole(logo):
    size = (logo.width + 2 * PAD, logo.height + 2 * PAD)
    alpha = Image.new("L", size, 0)
    alpha.paste(logo.getchannel("A"), (PAD, PAD))
    outline = grown(alpha)
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out.paste(Image.new("RGBA", size, SHADOW), (0, 0), shadow_of(outline))
    out.paste(Image.new("RGBA", size, OUTLINE), (0, 0), outline)
    letters = Image.new("RGBA", size, (0, 0, 0, 0))
    letters.paste(logo, (PAD, PAD))
    return Image.alpha_composite(out, letters)


def labels(logo):
    """Which part each pixel of the logo belongs to: the bright gold in connected pieces (the dark edges between
    the letters touch, the gold doesn't), named by place, then the dark edges given to the nearest piece."""
    a = np.array(logo).astype(int)
    h, w = a.shape[:2]
    luma = (a[:, :, 0] * 3 + a[:, :, 1] * 6 + a[:, :, 2]) // 10
    bright = (a[:, :, 3] > 128) & (luma > 110)
    comp = -np.ones((h, w), int)
    boxes = []
    for y in range(h):
        for x in range(w):
            if not bright[y, x] or comp[y, x] >= 0:
                continue
            n, box, q = len(boxes), [x, x, y, y, 0], deque([(y, x)])
            comp[y, x] = n
            while q:
                cy, cx = q.popleft()
                box[0], box[1], box[2], box[3], box[4] = min(box[0], cx), max(box[1], cx), min(box[2], cy), max(box[3], cy), box[4] + 1
                for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                    if 0 <= ny < h and 0 <= nx < w and bright[ny, nx] and comp[ny, nx] < 0:
                        comp[ny, nx] = n
                        q.append((ny, nx))
            boxes.append(box)
    # the letters are the ten biggest pieces reaching the bottom, left to right; the tabk is the rest that starts
    # above their tops (the specks lower down go with the letter they're nearest)
    low = sorted((i for i, b in enumerate(boxes) if b[3] > h * 0.8), key=lambda i: -boxes[i][4])[:len(LETTERS)]
    letters = sorted(low, key=lambda i: boxes[i][0])
    top = min(boxes[i][2] for i in letters)
    assert len(letters) == len(LETTERS), f"found {len(letters)} letters"
    part = -np.ones((h, w), int)
    for k, i in enumerate(letters):
        part[comp == i] = k
    for i, b in enumerate(boxes):
        if i not in letters and b[2] < top - 2:
            part[comp == i] = len(LETTERS)
    todo = (a[:, :, 3] > 8) & (part < 0)
    while todo.any():
        grow = part.copy()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            near = np.roll(np.roll(part, dy, 0), dx, 1)
            fill = todo & (grow < 0) & (near >= 0)
            grow[fill] = near[fill]
        if (grow == part).all():
            break
        part = grow
        todo = (a[:, :, 3] > 8) & (part < 0)
    return part


def parts(logo):
    a = np.array(logo)
    part = labels(logo)
    names = LETTERS + ["tabk"]
    cut = []
    for k, name in enumerate(names):
        ys, xs = np.where(part == k)
        x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
        piece = a.copy()
        piece[part != k] = 0
        piece = Image.fromarray(piece).crop((x0, y0, x1, y1))
        size = (piece.width + 2 * PAD, piece.height + 2 * PAD)
        alpha = Image.new("L", size, 0)
        alpha.paste(piece.getchannel("A"), (PAD, PAD))
        outline = grown(alpha)
        body = Image.new("RGBA", size, (0, 0, 0, 0))
        body.paste(Image.new("RGBA", size, OUTLINE), (0, 0), outline)
        gold = Image.new("RGBA", size, (0, 0, 0, 0))
        gold.paste(piece, (PAD, PAD))
        body = Image.alpha_composite(body, gold)
        shadow = Image.new("RGBA", size, (0, 0, 0, 0))
        shadow.paste(Image.new("RGBA", size, SHADOW), (0, 0), shadow_of(outline))
        g = np.array(gold).astype(int)
        bright = ((g[:, :, 0] * 3 + g[:, :, 1] * 6 + g[:, :, 2]) // 10 > GOLD_LUMA) & (g[:, :, 3] > 128)
        mask = Image.fromarray(np.dstack([np.full(bright.shape, 255, np.uint8)] * 3 + [(bright * 255).astype(np.uint8)]))
        # where it sits in the whole logo (whose own padding is PAD too)
        cut.append(dict(name=name, x=int(x0), y=int(y0), body=body, shadow=shadow, gold=mask))
    return cut


def pack(cut):
    x = y = row = 0
    for c in cut:
        w, h = c["body"].size
        if x + w > SHEET_WIDTH:
            x, y, row = 0, y + row + GUTTER, 0
        c["u"], c["v"] = x, y
        x, row = x + w + GUTTER, max(row, h)
    return SHEET_WIDTH, y + row


def main(src_path, out_dir):
    src = Image.open(src_path).convert("RGBA")
    logo = src.resize((WIDTH, round(src.height * WIDTH / src.width)), Image.LANCZOS)
    full = whole(logo)
    full.save(os.path.join(out_dir, "logo.bytes"), format="PNG", optimize=True)
    cut = parts(logo)
    size = pack(cut)
    lines = [f"logo {full.width} {full.height}", f"sheet {size[0]} {size[1]}"]
    for layer in ("body", "shadow", "gold"):
        sheet = Image.new("RGBA", size, (0, 0, 0, 0))
        for c in cut:
            sheet.paste(c[layer], (c["u"], c["v"]))
        name = {"body": "parts", "shadow": "parts-shadow", "gold": "parts-gold"}[layer]
        sheet.save(os.path.join(out_dir, name + ".bytes"), format="PNG", optimize=True)
    for c in cut:
        w, h = c["body"].size
        lines.append(f"{c['name']} {c['x']} {c['y']} {w} {h} {c['u']} {c['v']}")
    with open(os.path.join(out_dir, "parts.txt"), "w") as f:
        f.write("# The title logo's parts (tools/title_logo.py): name, place in the whole logo (x y, from its top left),\n"
                "# size (w h) and place in the parts sheets (u v, from their top left), in pixels.\n")
        f.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main(*sys.argv[1:3])
