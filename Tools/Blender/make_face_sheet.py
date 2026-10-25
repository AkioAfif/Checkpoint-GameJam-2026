# 2D face sheet for the main character (villager-style swappable eyes + mouths). Plain Python + Pillow:
#   python Tools/Blender/make_face_sheet.py
# Writes Exports/MC_Meadow/MC_Face.png: 1024 px, 4x4 cells of 256 px, transparent background.
# Rows 0-1 = eyes, drawn as the eye on the VIEWER'S LEFT (the right eye mirrors the UVs), inner corner = cell's right side.
# Rows 2-3 = mouths. Cell order must match EYES / MOUTHS in build_mc_meadow.py.
import math
import os

from PIL import Image, ImageDraw

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(REPO, "Exports", "MC_Meadow", "MC_Face.png")

EYES = ["Open", "HalfBlink", "Closed", "Happy", "Surprised", "Angry", "Sad", "Squint"]
MOUTHS = ["Smile", "TalkSmall", "TalkWide", "Grin", "Frown", "Flat", "Shock", "Wavy"]

SS = 4  # supersampling for anti-aliased edges
CELL = 256 * SS
INK = (46, 38, 34, 255)          # eye colour, near the farm's M_Metal
SHINE = (255, 250, 238, 255)
LIP = (150, 52, 40, 255)         # mouth line, near M_Roof_Dark
INSIDE = (96, 28, 26, 255)
TONGUE = (226, 110, 96, 255)
LINE = 15 * SS


def box(cx, cy, w, h):
    return (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)


def arc_line(d, cx, cy, w, h, start, end, color, width=LINE):
    d.arc(box(cx, cy, w, h), start, end, fill=color, width=width)
    # round caps (Pillow draws the stroke inside the box, so the caps sit half a stroke in)
    for a in (start, end):
        x = cx + (w - width) / 2 * math.cos(math.radians(a))
        y = cy + (h - width) / 2 * math.sin(math.radians(a))
        d.ellipse(box(x, y, width, width), fill=color)


def polyline(d, pts, color, width=LINE):
    d.line(pts, fill=color, width=width, joint="curve")
    for x, y in (pts[0], pts[-1]):
        d.ellipse(box(x, y, width, width), fill=color)


def eye_open(img, d, c, w=.34, h=.6, cut=None):
    """Tall oval eye with a shine. cut = (y_outer, y_inner) as cell fractions: everything above that line is erased (eyelid)."""
    cx = cy = c / 2
    d.ellipse(box(cx, cy, c * w, c * h), fill=INK)
    d.ellipse(box(cx + c * .055, cy - c * .14, c * .1, c * .12), fill=SHINE)
    if cut:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).polygon([(0, 0), (c, 0), (c, c * cut[1]), (0, c * cut[0])], fill=(255, 255, 255, 255))
        img.paste((0, 0, 0, 0), (0, 0), layer)


def draw_eye(name):
    img = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c, m = CELL, CELL / 2
    if name == "Open":
        eye_open(img, d, c)
    elif name == "HalfBlink":
        eye_open(img, d, c, cut=(.5, .5))
        d.line([(c * .32, c * .5), (c * .68, c * .5)], fill=INK, width=LINE)
    elif name == "Closed":
        arc_line(d, m, c * .46, c * .4, c * .16, 20, 160, INK)  # soft downward curve
    elif name == "Happy":
        arc_line(d, m, c * .58, c * .42, c * .38, 200, 340, INK)  # ^ shape
    elif name == "Surprised":
        d.ellipse(box(m, m, c * .5, c * .64), fill=SHINE)
        d.ellipse(box(m, m, c * .5, c * .64), outline=INK, width=LINE)
        d.ellipse(box(m, m + c * .02, c * .2, c * .24), fill=INK)
    elif name == "Angry":
        eye_open(img, d, c, cut=(.22, .5))  # lid slopes down toward the nose
        d.line([(c * .22, c * .26), (c * .82, c * .46)], fill=INK, width=LINE)
    elif name == "Sad":
        eye_open(img, d, c, cut=(.5, .26))  # lid slopes down toward the cheek
        d.line([(c * .18, c * .45), (c * .78, c * .27)], fill=INK, width=LINE)
    elif name == "Squint":
        polyline(d, [(c * .32, c * .3), (c * .66, c * .5), (c * .32, c * .7)], INK)  # ">" pointing at the nose
    return img


def draw_mouth(name):
    img = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c, m = CELL, CELL / 2
    if name == "Smile":
        arc_line(d, m, c * .4, c * .36, c * .26, 25, 155, LIP)
    elif name == "TalkSmall":
        d.ellipse(box(m, m, c * .24, c * .2), fill=INSIDE)
        d.chord(box(m, m + c * .05, c * .16, c * .1), 0, 180, fill=TONGUE)
    elif name == "TalkWide" or name == "Grin":
        w, h = (.42, .34) if name == "TalkWide" else (.6, .44)
        top = m - c * h * .35
        d.chord((m - c * w / 2, top - c * h, m + c * w / 2, top + c * h), 0, 180, fill=INSIDE)  # flat top, round bottom (D)
        d.chord(box(m, top + c * h * .82, c * w * .55, c * h * .45), 180, 360, fill=TONGUE)
        d.chord((m - c * w / 2, top - c * h, m + c * w / 2, top + c * h), 0, 180, outline=LIP, width=LINE // 2)
    elif name == "Frown":
        arc_line(d, m, c * .62, c * .34, c * .24, 205, 335, LIP)
    elif name == "Flat":
        polyline(d, [(c * .38, m), (c * .62, m)], LIP)
    elif name == "Shock":
        d.ellipse(box(m, m, c * .26, c * .34), fill=INSIDE)
        d.ellipse(box(m, m, c * .26, c * .34), outline=LIP, width=LINE // 2)
    elif name == "Wavy":
        pts = [(c * (.3 + .4 * i / 24), m + c * .045 * math.sin(i / 24 * math.pi * 3)) for i in range(25)]
        polyline(d, pts, LIP, width=LINE - 2 * SS)
    return img


def main():
    sheet = Image.new("RGBA", (CELL * 4, CELL * 4), (0, 0, 0, 0))
    for i, name in enumerate(EYES):
        sheet.paste(draw_eye(name), ((i % 4) * CELL, (i // 4) * CELL))
    for i, name in enumerate(MOUTHS):
        sheet.paste(draw_mouth(name), ((i % 4) * CELL, (2 + i // 4) * CELL))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.resize((1024, 1024), Image.LANCZOS).save(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
