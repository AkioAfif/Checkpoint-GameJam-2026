# Lays the NPC concept renders (build_npc_concepts.py) out as one labelled proposal sheet per villager + an overview.
#   python Tools/Blender/make_npc_sheets.py
import json
import os
import textwrap

from PIL import Image, ImageDraw, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DIR = os.path.join(REPO, "Exports", "NPC_Concepts")
INK, MUTED, PAPER, CARD = (52, 42, 36), (120, 106, 96), (250, 245, 236), (255, 252, 246)


def font(size, bold=False):
    return ImageFont.truetype("segoeuib.ttf" if bold else "segoeui.ttf", size)


def sheet(npc, spec):
    W, PAD = 1500, 40
    body = Image.open(os.path.join(DIR, f"{npc}_body.png")).convert("RGB")
    img = Image.new("RGB", (W, 1560), PAPER)
    d = ImageDraw.Draw(img)
    d.text((PAD, 28), spec["name"], fill=INK, font=font(46, True))
    d.text((PAD, 92), f"{spec['persona']}  ·  three archetypes (Meadow on the left for scale)", fill=MUTED, font=font(24))
    img.paste(body, (0, 140))
    # name tags under each figure in the lineup (Meadow at x 0.13, variants spaced evenly to the right)
    for x, label in zip((.135, .395, .605, .815), ["Meadow (MC)"] + [f"{v['letter']}  {v['title']}" for v in spec["variants"]]):
        d.text((int(x * W), 140 + 790), label, fill=INK if label[0] != "M" else MUTED, font=font(24, label[0] != "M"), anchor="mm")
    y = 980
    cw = (W - PAD * 4) // 3
    for i, v in enumerate(spec["variants"]):
        x = PAD + i * (cw + PAD)
        d.rounded_rectangle((x, y, x + cw, y + 545), 18, fill=CARD, outline=(232, 222, 206), width=2)
        face = Image.open(os.path.join(DIR, f"{npc}_{v['letter']}_face.png")).convert("RGB").resize((300, 300))
        img.paste(face, (x + (cw - 300) // 2, y + 20))
        d.text((x + 24, y + 335), f"{v['letter']} · {v['title']}", fill=INK, font=font(28, True))
        for j, line in enumerate(textwrap.wrap(v["blurb"], 40)):
            d.text((x + 24, y + 378 + j * 30), line, fill=INK, font=font(21))
        d.text((x + 24, y + 510), f"{v['height']:.2f} m tall  ·  {v['tris']:,} tris", fill=MUTED, font=font(19))
    path = os.path.join(DIR, f"{npc}_proposal.png")
    img.save(path)
    return path


def overview(specs):
    rows = []
    for npc, spec in specs.items():
        body = Image.open(os.path.join(DIR, f"{npc}_body.png")).convert("RGB").crop((260, 120, 1500, 760)).resize((930, 480))
        row = Image.new("RGB", (1240, 480), PAPER)
        row.paste(body, (310, 0))
        d = ImageDraw.Draw(row)
        d.text((30, 170), spec["name"], fill=INK, font=font(38, True))
        for j, v in enumerate(spec["variants"]):
            d.text((30, 230 + j * 34), f"{v['letter']}  {v['title']}", fill=MUTED, font=font(24))
        rows.append(row)
    img = Image.new("RGB", (1240, 480 * len(rows)), PAPER)
    for i, r in enumerate(rows):
        img.paste(r, (0, i * 480))
    path = os.path.join(DIR, "All_NPCs_overview.png")
    img.save(path)
    return path


if __name__ == "__main__":
    specs = json.load(open(os.path.join(DIR, "specs.json"), encoding="utf-8"))
    for npc, spec in specs.items():
        print(sheet(npc, spec))
    print(overview(specs))
