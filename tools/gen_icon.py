"""Generate the app icon: three cards fanned on the green felt.

Run from the project root:  python tools/gen_icon.py
Needs Pillow and the Georgia Bold font (Windows: C:/Windows/Fonts/georgiab.ttf).

Writes:
  sprites/icon/icon.png          1024 px, full square (project + Windows icon)
  sprites/icon/icon_192.png      legacy Android launcher icon
  sprites/icon/adaptive_fg.png   432 px adaptive foreground (cards in the safe zone)
  sprites/icon/adaptive_bg.png   432 px adaptive background (felt)
  sprites/icon/adaptive_mono.png 432 px themed (monochrome) icon
  build/store/icon_512.png       Google Play store icon
"""
import math
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONT = "C:/Windows/Fonts/georgiab.ttf"

NAVY = (24, 32, 48, 255)
FROST = (238, 242, 246, 255)
RED = (184, 19, 0, 255)
BACK_EDGE = (150, 162, 184, 255)

N = 1024          # working size of one layer
SS = 3            # supersampling
# Adaptive layer is 108 dp; the launcher mask shows the central 72 dp and
# guarantees only the central 66 dp circle. Scale the cards to fit there.
ADAPTIVE_SCALE = 0.75


def res(*p):
    return os.path.join(ROOT, *p)


def heart_pts(cx, cy, s, up=False):
    pts = []
    for i in range(400):
        t = 2 * math.pi * i / 400
        x = 16 * math.sin(t) ** 3
        y = -(13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t))
        if up:
            y = -y
        pts.append((cx + x * s, cy + y * s))
    return pts


def draw_spade(d, cx, cy, s, fill):
    d.polygon(heart_pts(cx, cy - 2 * s, s, up=True), fill=fill)
    d.polygon([(cx - 1.2 * s, cy + 4 * s), (cx + 1.2 * s, cy + 4 * s),
               (cx + 5 * s, cy + 17 * s), (cx - 5 * s, cy + 17 * s)], fill=fill)


def draw_heart(d, cx, cy, s, fill):
    d.polygon(heart_pts(cx, cy, s), fill=fill)


def card_mask(w, h):
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, w - 1, h - 1], radius=int(w * 0.08), fill=255)
    return m


def face(w, h, suit, mono=False):
    """Ace face. mono=True gives a white card with the pips cut out."""
    ink = (0, 0, 0, 0) if mono else (NAVY if suit == "spade" else RED)
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    im.paste((255, 255, 255, 255) if mono else FROST, (0, 0), card_mask(w, h))
    pips = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(pips)
    d.text((w * 0.09, h * 0.03), "A", font=ImageFont.truetype(FONT, int(h * 0.2)), fill=255)
    if suit == "spade":
        draw_spade(d, w / 2, h * 0.5, w * 0.022, 255)
    else:
        draw_heart(d, w / 2, h * 0.52, w * 0.022, 255)
    im.paste(ink, (0, 0), pips)
    return im


def back(w, h, mono=False):
    m = card_mask(w, h)
    src = Image.open(res("sprites", "cards", "Vardo.png")).convert("RGBA").resize((w, h), Image.LANCZOS)
    if mono:
        # White card with the wagon mark cut out (the thin frame line is left out).
        mark = src.convert("L").point(lambda v: 255 if v > 140 else 0)
        keep = Image.new("L", (w, h), 0)
        keep.paste(mark.crop((int(w * 0.12), int(h * 0.3), int(w * 0.88), int(h * 0.7))), (int(w * 0.12), int(h * 0.3)))
        im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        im.paste((255, 255, 255, 255), (0, 0), ImageChops.subtract(m, keep))
        return im
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    im.paste(src, (0, 0), m)
    ImageDraw.Draw(im).rounded_rectangle([0, 0, w - 1, h - 1], radius=int(w * 0.08),
                                         outline=BACK_EDGE, width=max(3, w // 70))
    return im


def cards(k, mono=False):
    """List of (card image, centre x, centre y, angle), bottom card first."""
    w = int(340 * k * SS)
    h = int(w * 1.5)
    c = N * SS / 2
    off = 140 * k * SS
    return [
        (face(w, h, "heart", mono), c - off, c + 30 * k * SS, 18),
        (face(w, h, "spade", mono), c, c, 0),
        (back(w, h, mono), c + off, c + 30 * k * SS, -18),   # the Vardo card on top
    ]


def compose(k):
    layer = Image.new("RGBA", (N * SS, N * SS), (0, 0, 0, 0))
    blur = int(12 * k * SS)
    for card, cx, cy, angle in cards(k):
        rc = card.rotate(angle, resample=Image.BICUBIC, expand=True)
        pad = blur * 3
        sh = Image.new("RGBA", (rc.width + 2 * pad, rc.height + 2 * pad), (0, 0, 0, 0))
        shade = Image.new("RGBA", rc.size, (0, 0, 0, 255))
        shade.putalpha(rc.split()[3].point(lambda v: int(v * 0.45)))
        sh.alpha_composite(shade, (pad, pad))
        sh = sh.filter(ImageFilter.GaussianBlur(blur))
        layer.alpha_composite(sh, (int(cx - sh.width / 2 + blur * 0.6), int(cy - sh.height / 2 + blur * 1.2)))
        layer.alpha_composite(rc, (int(cx - rc.width / 2), int(cy - rc.height / 2)))
    return layer


def compose_mono(k):
    """White silhouettes with a transparent gap around each upper card."""
    alpha = Image.new("L", (N * SS, N * SS), 0)
    gap = int(14 * k * SS)
    for card, cx, cy, angle in cards(k, mono=True):
        rc = card.rotate(angle, resample=Image.BICUBIC, expand=True)
        pos = (int(cx - rc.width / 2), int(cy - rc.height / 2))
        a = rc.split()[3]
        outline = Image.new("L", alpha.size, 0)
        outline.paste(a, pos)
        outline = outline.filter(ImageFilter.MaxFilter(2 * gap + 1))
        alpha = ImageChops.subtract(alpha, outline)
        full = Image.new("L", alpha.size, 0)
        full.paste(a, pos)
        alpha = ImageChops.lighter(alpha, full)
        # cut the pips of this card out again (they are transparent in rc)
        solid = Image.new("L", alpha.size, 0)
        solid.paste(card_mask(*card.size).rotate(angle, resample=Image.BICUBIC, expand=True), pos)
        holes = ImageChops.subtract(solid, full)
        alpha = ImageChops.subtract(alpha, holes)
    out = Image.new("RGBA", alpha.size, (255, 255, 255, 0))
    out.putalpha(alpha)
    return out


def felt():
    src = Image.open(res("sprites", "table.jpg")).convert("RGBA")
    side = min(src.size)
    return src.crop((0, 0, side, side)).resize((N * SS, N * SS), Image.LANCZOS)


def down(im, size):
    return im.resize((size, size), Image.LANCZOS)


def main():
    os.makedirs(res("sprites", "icon"), exist_ok=True)
    os.makedirs(res("build", "store"), exist_ok=True)

    full = felt()
    full.alpha_composite(compose(1.0))
    down(full, 1024).save(res("sprites", "icon", "icon.png"))
    down(full, 192).save(res("sprites", "icon", "icon_192.png"))
    down(full, 512).convert("RGB").save(res("build", "store", "icon_512.png"))

    down(compose(ADAPTIVE_SCALE), 432).save(res("sprites", "icon", "adaptive_fg.png"))
    down(felt(), 432).convert("RGB").save(res("sprites", "icon", "adaptive_bg.png"))
    down(compose_mono(ADAPTIVE_SCALE), 432).save(res("sprites", "icon", "adaptive_mono.png"))


if __name__ == "__main__":
    main()
