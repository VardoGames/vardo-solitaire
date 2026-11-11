"""Generate the 52 card faces in sprites/cards/.

Run from the project root:  python tools/gen_cards.py
Needs Pillow, the Georgia Bold font (Windows: C:/Windows/Fonts/georgiab.ttf)
and Godot 4.6 to render the court card SVGs (set GODOT to the editor .exe
if it is not at the default path below).

Number cards and aces are drawn here. Jacks, queens and kings use the
picture from tools/court-art (public domain, see the README there). Every
card gets a large index in the top-left corner so that rank and suit stay
readable when cards overlap in a tableau column.
"""
import math
import os
import subprocess
import tempfile

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GODOT = os.environ.get("GODOT", "D:/dev/Godot_v4.6.2-stable_mono_win64/Godot_v4.6.2-stable_mono_win64_console.exe")
FONT = "C:/Windows/Fonts/georgiab.ttf"

W, H = 400, 600   # card texture size (same 2:3 shape as the back)
SS = 3            # supersampling
PAPER = (254, 253, 248, 255)
EDGE = (190, 190, 192, 255)
RED = (196, 18, 20, 255)
BLACK = (20, 20, 24, 255)

SVG_SCALE = 3.0                  # court SVGs render at 668 x 971
FRAME = (88, 88, 581, 884)       # picture frame inside the rendered court card
PIC_TOP = 122                    # court picture starts below the index

SUITS = {"H": ("Heart", "heart"), "D": ("Diamond", "diamond"),
         "C": ("Clover", "clover"), "S": ("Spade", "spade")}
COURT = {11: "J", 12: "Q", 13: "K"}
INDEX = {1: "A", 11: "J", 12: "Q", 13: "K"}

# Pip positions on a 3-column x 7-row grid (row 0 at the top).
LAYOUT = {
    2: [(1, 0), (1, 6)],
    3: [(1, 0), (1, 3), (1, 6)],
    4: [(0, 0), (2, 0), (0, 6), (2, 6)],
    5: [(0, 0), (2, 0), (1, 3), (0, 6), (2, 6)],
    6: [(0, 0), (2, 0), (0, 3), (2, 3), (0, 6), (2, 6)],
    7: [(0, 0), (2, 0), (1, 1.5), (0, 3), (2, 3), (0, 6), (2, 6)],
    8: [(0, 0), (2, 0), (1, 1.5), (0, 3), (2, 3), (1, 4.5), (0, 6), (2, 6)],
    9: [(0, 0), (2, 0), (0, 2), (2, 2), (1, 3), (0, 4), (2, 4), (0, 6), (2, 6)],
    10: [(0, 0), (2, 0), (1, 1), (0, 2), (2, 2), (0, 4), (2, 4), (1, 5), (0, 6), (2, 6)],
}


def heart_pts(s, up=False):
    pts = []
    for i in range(360):
        t = 2 * math.pi * i / 360
        x = 16 * math.sin(t) ** 3
        y = -(13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t))
        pts.append((x * s, (-y if up else y) * s))
    return pts


def pip(d, suit, cx, cy, size, col, flip=False):
    """Draw one suit symbol about `size` wide centred at (cx, cy)."""
    s = size / 34.0
    f = -1 if flip else 1

    def poly(pts):
        d.polygon([(cx + x, cy + f * y) for x, y in pts], fill=col)

    if suit == "H":
        poly(heart_pts(s))
    elif suit == "S":
        poly([(x, y - 2 * s) for x, y in heart_pts(s, up=True)])
        poly([(-1.2 * s, 4 * s), (1.2 * s, 4 * s), (5 * s, 17 * s), (-5 * s, 17 * s)])
    elif suit == "D":
        a, b, n = size * 0.42, size * 0.56, 40
        pts = []
        for (x0, y0), (x1, y1) in [((0, -b), (a, 0)), ((a, 0), (0, b)), ((0, b), (-a, 0)), ((-a, 0), (0, -b))]:
            nx, ny = -(y1 - y0), (x1 - x0)
            length = math.hypot(nx, ny)
            for i in range(n):
                t = i / n
                bend = -math.sin(math.pi * t) * size * 0.035
                pts.append((x0 + (x1 - x0) * t - nx / length * bend, y0 + (y1 - y0) * t - ny / length * bend))
        poly(pts)
    elif suit == "C":
        r = size * 0.235
        for x, y in [(0, -0.95 * r), (-1.0 * r, 0.55 * r), (1.0 * r, 0.55 * r)]:
            d.ellipse([cx + x - r, cy + f * y - r, cx + x + r, cy + f * y + r], fill=col)
        d.ellipse([cx - 0.6 * r, cy + f * 0.2 * r - 0.6 * r, cx + 0.6 * r, cy + f * 0.2 * r + 0.6 * r], fill=col)
        poly([(-0.18 * r, 0.3 * r), (0.18 * r, 0.3 * r), (0.75 * r, 2.25 * r), (-0.75 * r, 2.25 * r)])


def court_picture(png_path):
    """The framed picture of a rendered court card, without its corner pip."""
    src = Image.open(png_path).convert("RGBA")
    bg = Image.new("RGBA", src.size, (255, 255, 255, 255))
    bg.alpha_composite(src)
    fr = bg.crop(FRAME).convert("RGB")
    # The big suit pip in the top-left of the frame repeats our index; flood
    # it with white (it sits on white paper) and also clear its soft edge.
    seed = min(((x, y) for x in range(40, 150, 4) for y in range(60, 160, 4) if sum(fr.getpixel((x, y))) < 450),
               key=lambda q: (q[0] - 90) ** 2 + (q[1] - 110) ** 2)
    marked = fr.copy()
    ImageDraw.floodfill(marked, seed, (0, 255, 0), thresh=140)
    mask = ImageChops.difference(marked, fr).convert("L").point(lambda v: 255 if v else 0)
    fr.paste((255, 255, 255), (0, 0), mask.filter(ImageFilter.MaxFilter(7)))
    return fr.convert("RGBA")


def card(rank, suit, court_png=None):
    big = Image.new("RGBA", (W * SS, H * SS), PAPER)
    d = ImageDraw.Draw(big)
    col = RED if suit in "HD" else BLACK

    if court_png:
        fr = court_picture(court_png)
        ph = int((H - PIC_TOP - 14) * SS)
        pw = int(fr.width * ph / fr.height)
        big.paste(fr.resize((pw, ph), Image.LANCZOS), ((W * SS - pw) // 2, PIC_TOP * SS))
    elif rank == 1:
        pip(d, suit, W * SS / 2, H * SS * 0.55, W * SS * 0.5, col)
    else:
        xs = [0.29, 0.5, 0.71]
        y0, y1 = 0.30, 0.86
        for c, r in LAYOUT[rank]:
            pip(d, suit, W * SS * xs[c], H * SS * (y0 + (y1 - y0) * r / 6), W * SS * 0.2, col, flip=r > 3)

    # Large index: rank and suit side by side, readable in an overlapped column.
    text = INDEX.get(rank, str(rank))
    font = ImageFont.truetype(FONT, int(96 * SS))
    tw = d.textlength(text, font=font)
    x0, y0, ps, bh = 8 * SS, 8 * SS, 58 * SS, 104 * SS
    d.text((x0 + 10 * SS, y0 + bh / 2), text, font=font, fill=col, anchor="lm")
    pip(d, suit, x0 + 10 * SS + tw + 16 * SS + ps / 2, y0 + bh / 2, ps, col)

    d.rectangle([0, 0, W * SS - 1, H * SS - 1], outline=EDGE, width=3 * SS)
    return big.resize((W, H), Image.LANCZOS)


def render_courts(out_dir):
    subprocess.run([GODOT, "--headless", "--script", os.path.join(ROOT, "tools", "svg2png.gd"), "--",
                    os.path.join(ROOT, "tools", "court-art"), out_dir, str(SVG_SCALE)],
                   check=True, stdout=subprocess.DEVNULL)


def main():
    with tempfile.TemporaryDirectory() as tmp:
        render_courts(tmp)
        for suit, (folder, name) in SUITS.items():
            for rank in range(1, 14):
                court = os.path.join(tmp, COURT[rank] + suit + ".png") if rank in COURT else None
                out = os.path.join(ROOT, "sprites", "cards", folder, f"card_{rank}_{name}.png")
                card(rank, suit, court).save(out)
                print(out)


if __name__ == "__main__":
    main()
