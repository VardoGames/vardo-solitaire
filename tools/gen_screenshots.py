"""Compose Google Play screenshots (1920 x 1080) from landscape phone captures.

Run from the project root:  python tools/gen_screenshots.py <raw_dir>
<raw_dir> holds <lang>_landscape/<shot>.png captures (2400 x 1080, taken with
`adb exec-out screencap -p`). A phone capture is wider than the 2:1 limit of
Google Play, so it is scaled into the lower part of a 16:9 frame and a caption
band goes on top. Writes build/store/screenshots/<lang>/<shot>.png.
Needs Pillow and the Windows fonts Georgia Bold and Microsoft YaHei Bold.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W, H = 1920, 1080
NAVY = (24, 32, 48)
FROST = (238, 242, 246)
FONTS = {"en": "C:/Windows/Fonts/georgiab.ttf", "ru": "C:/Windows/Fonts/georgiab.ttf",
         "zh": "C:/Windows/Fonts/msyhbd.ttc"}

CAPTIONS = {
    "1_mid": {"en": "Classic Klondike solitaire", "ru": "Классическая косынка", "zh": "经典克朗代克纸牌"},
    "2_end": {"en": "Draw 1 or 3 · Vegas scoring", "ru": "По одной или по три · счёт Вегас",
              "zh": "一张或三张发牌 · 维加斯计分"},
    "3_menu": {"en": "No ads, no purchases, no internet", "ru": "Без рекламы, покупок и интернета",
               "zh": "无广告，无内购，无需联网"},
}


def compose(capture, caption, font_path):
    shot = Image.open(capture).convert("RGB")
    shot = shot.resize((W, round(shot.height * W / shot.width)), Image.LANCZOS)
    band = H - shot.height
    frame = Image.new("RGB", (W, H), NAVY)
    frame.paste(shot, (0, band))
    d = ImageDraw.Draw(frame)
    size = 72
    font = ImageFont.truetype(font_path, size)
    while d.textlength(caption, font=font) > W - 160 and size > 40:
        size -= 2
        font = ImageFont.truetype(font_path, size)
    d.text((W / 2, band / 2 + 4), caption, font=font, fill=FROST, anchor="mm")
    return frame


def main(raw_dir):
    for lang, font in FONTS.items():
        out_dir = os.path.join(ROOT, "build", "store", "screenshots", lang)
        os.makedirs(out_dir, exist_ok=True)
        for shot, text in CAPTIONS.items():
            src = os.path.join(raw_dir, f"{lang}_landscape", shot + ".png")
            if not os.path.exists(src):
                print("missing", src)
                continue
            compose(src, text[lang], font).save(os.path.join(out_dir, shot + ".png"))
            print(lang, shot)


if __name__ == "__main__":
    main(sys.argv[1])
