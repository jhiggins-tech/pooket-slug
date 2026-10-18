"""Make the splash screen's logo, Assets/Resources/Title/logo.bytes (a PNG), from logo C1 (docs/design/art.md).

usage: python3 -I tools/title_logo.py <logo-c1.png> <out.png>

Scales the logo to the size it shows at on a 1080-pixel-high phone (440 of the canvas's 360-high units, about 1280
pixels wide), then bakes in a thin dark outline (the one keying took away) and a soft drop shadow, so the gold
letters hold up on the colour ramp. Needs Pillow.
"""
import sys

from PIL import Image, ImageFilter

WIDTH, PAD = 1280, 24
OUTLINE, SHADOW = (20, 10, 6, 255), (4, 3, 22, 255)


def main(src_path, out_path):
    src = Image.open(src_path).convert("RGBA")
    logo = src.resize((WIDTH, round(src.height * WIDTH / src.width)), Image.LANCZOS)
    size = (logo.width + 2 * PAD, logo.height + 2 * PAD)
    alpha = Image.new("L", size, 0)
    alpha.paste(logo.getchannel("A"), (PAD, PAD))
    outline = alpha.filter(ImageFilter.MaxFilter(7))  # the letters grown by 3 px: about one canvas unit
    shadow = Image.new("L", size, 0)
    shadow.paste(outline, (0, 8))
    shadow = shadow.filter(ImageFilter.GaussianBlur(7)).point(lambda v: int(v * 0.7))
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out.paste(Image.new("RGBA", size, SHADOW), (0, 0), shadow)
    out.paste(Image.new("RGBA", size, OUTLINE), (0, 0), outline)
    letters = Image.new("RGBA", size, (0, 0, 0, 0))
    letters.paste(logo, (PAD, PAD))
    Image.alpha_composite(out, letters).save(out_path, format="PNG", optimize=True)


if __name__ == "__main__":
    main(*sys.argv[1:3])
