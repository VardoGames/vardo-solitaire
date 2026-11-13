"""Generate the Google Play feature graphic (1024 x 500) for each store language.

Run from the project root:  python tools/gen_feature.py
Needs Pillow and the Windows fonts Georgia Bold and Microsoft YaHei Bold.
Writes build/store/feature_{en,ru,zh}.png.
"""
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W, H = 1024, 500
SS = 2
FROST = (238, 242, 246, 255)
GOLD = (233, 200, 120, 255)
SERIF = "C:/Windows/Fonts/georgiab.ttf"
CJK = "C:/Windows/Fonts/msyhbd.ttc"

SUBTITLE = {"en": "Klondike", "ru": "Косынка", "zh": "纸牌接龙"}

# (sprite path, centre x, centre y, angle), bottom card first
FAN = [
    ("Heart/card_13_heart.png", 190, 262, 24),
    ("Spade/card_12_spade.png", 268, 240, 12),
    ("Diamond/card_11_diamond.png", 346, 232, 0),
    ("Clover/card_1_clover.png", 424, 240, -12),
    ("Vardo.png", 502, 262, -24),
]
CARD_H = 300


def res(*p):
    return os.path.join(ROOT, *p)


def felt():
    src = Image.open(res("sprites", "table.jpg")).convert("RGBA")
    scale = max(W * SS / src.width, H * SS / src.height)
    src = src.resize((int(src.width * scale) + 1, int(src.height * scale) + 1), Image.LANCZOS)
    bg = src.crop((0, 0, W * SS, H * SS))
    # soft vignette so the cards and the title stand out
    vig = Image.new("L", bg.size, 0)
    ImageDraw.Draw(vig).ellipse([-W * SS * 0.2, -H * SS * 0.4, W * SS * 1.2, H * SS * 1.4], fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(120 * SS))
    dark = Image.new("RGBA", bg.size, (0, 0, 0, 255))
    return Image.composite(bg, Image.blend(bg, dark, 0.55), vig)


def card(path):
    im = Image.open(res("sprites", "cards", path)).convert("RGBA")
    h = CARD_H * SS
    w = int(im.width * h / im.height)
    im = im.resize((w, h), Image.LANCZOS)
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, w - 1, h - 1], radius=int(w * 0.06), fill=255)
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.paste(im, (0, 0), mask)
    return out


def place(canvas, im, cx, cy, angle):
    rc = im.rotate(angle, resample=Image.BICUBIC, expand=True)
    blur = 10 * SS
    pad = blur * 3
    sh = Image.new("RGBA", (rc.width + 2 * pad, rc.height + 2 * pad), (0, 0, 0, 0))
    shade = Image.new("RGBA", rc.size, (0, 0, 0, 255))
    shade.putalpha(rc.split()[3].point(lambda v: int(v * 0.5)))
    sh.alpha_composite(shade, (pad, pad))
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    x, y = cx * SS, cy * SS
    canvas.alpha_composite(sh, (int(x - sh.width / 2 + 6 * SS), int(y - sh.height / 2 + 10 * SS)))
    canvas.alpha_composite(rc, (int(x - rc.width / 2), int(y - rc.height / 2)))


def text_block(canvas, lang):
    d = ImageDraw.Draw(canvas)
    x = 790 * SS
    title = ImageFont.truetype(SERIF, 64 * SS)
    d.text((x, 205 * SS), "Vardo", font=title, fill=FROST, anchor="ms")
    d.text((x, 280 * SS), "Solitaire", font=title, fill=FROST, anchor="ms")
    sub_font = ImageFont.truetype(CJK if lang == "zh" else SERIF, 40 * SS)
    d.line([(x - 90 * SS, 312 * SS), (x + 90 * SS, 312 * SS)], fill=GOLD, width=2 * SS)
    d.text((x, 368 * SS), SUBTITLE[lang], font=sub_font, fill=GOLD, anchor="ms")


def main():
    os.makedirs(res("build", "store"), exist_ok=True)
    base = felt()
    for path, cx, cy, angle in FAN:
        place(base, card(path), cx, cy, angle)
    for lang in SUBTITLE:
        im = base.copy()
        text_block(im, lang)
        im.resize((W, H), Image.LANCZOS).convert("RGB").save(res("build", "store", f"feature_{lang}.png"))


if __name__ == "__main__":
    main()
