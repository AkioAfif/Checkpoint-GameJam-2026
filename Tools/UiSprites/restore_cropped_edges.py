# The 2D UI kit (commit "feat: Added 2D UI elements") was exported with its left and top edges cut off: outlines run
# straight into the image's left/top border while the right/bottom keep their full outline (and rounded corners).
# This rebuilds the lost strip of each frame-like sprite and writes the result to Art/Sprites/UI/Restored/ (same sub-folder
# and file name); the originals are left untouched.
#
# How much was cut is measured, not guessed: an intact bottom-right corner is the mirror image of the cut corners, so near
# the right edge "first opaque row from the top" vs "from the bottom", and near the bottom "first opaque column from the
# left" vs "from the right", differ by exactly the cut. The lost strip is then filled with the mirrored outermost band of the
# opposite edge (the outline, plus the corner arcs).
#
# Usage (from the repo root): python -I Tools/UiSprites/restore_cropped_edges.py
import os
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
SOURCE = os.path.join(ROOT, "Assets", "_Project", "Art", "Sprites", "UI")
TARGET = os.path.join(SOURCE, "Restored")
ALPHA = 40
# the kit's frames (icons and other art with open silhouettes are untouched)
FILES = [
    "Common/ui_common_panel_paper.png",
    "Common/ui_common_hintbar_bg.png",
    "Common/ui_common_button_gold.png",
    "Common/ui_common_button_green.png",
    "Banners/ui_banner_debt_record.png",
    "Banners/ui_banner_new_item.png",
    "Banners/ui_banner_planted.png",
    "Banners/ui_banner_seed_ready.png",
    "Banners/ui_banner_settings.png",
    "Input/ui_common_key_e_white_idle.png",
    "Input/ui_common_key_r_white_idle.png",
    "Input/ui_common_key_esc_white_idle.png",
    "Input/ui_common_mouse_left.png",
    "QTE/ui_qte_hold_ring.png",
    "Loading/ui_loading_bar_unfilled.png",
    "Loading/ui_loading_bar_filled.png",
]


def first_opaque(alpha, coords):
    for i, xy in enumerate(coords):
        if alpha[xy] >= ALPHA:
            return i
    return None


def measure_cut(image, horizontal):
    # horizontal: compare first-opaque-from-left vs from-right along rows near the bottom edge; else top vs bottom along
    # columns near the right edge. Only rows/columns where the cut side doesn't start at the border count.
    w, h = image.size
    alpha = image.getchannel("A").load()
    diffs = []
    lines = range(h - 1, h - 1 - h // 3, -1) if horizontal else range(w - 1, w - 1 - w // 3, -1)
    for line in lines:
        if horizontal:
            near = first_opaque(alpha, [(x, line) for x in range(w)])
            far = first_opaque(alpha, [(x, line) for x in range(w - 1, -1, -1)])
        else:
            near = first_opaque(alpha, [(line, y) for y in range(h)])
            far = first_opaque(alpha, [(line, y) for y in range(h - 1, -1, -1)])
        if near is None or far is None or near == 0:
            continue
        diffs.append(far - near)
    if not diffs:
        return 0
    diffs.sort()
    return max(0, diffs[len(diffs) // 2])


def edge_is_cut(image, horizontal):
    # the key caps' frame is a half-transparent shadow (alpha 128), so "cut" = art clearly present on the border
    w, h = image.size
    alpha = image.getchannel("A").load()
    edge = [(0, y) for y in range(h)] if horizontal else [(x, 0) for x in range(w)]
    return max(alpha[xy] for xy in edge) >= 100


def outline_difference(image, horizontal):
    # fallback where the far corner isn't a mirror (the paper's folded corner): the dark outline's thickness across the
    # middle, far side minus the cut side
    w, h = image.size
    pixels = image.load()
    line = [(x, h // 2) for x in range(w)] if horizontal else [(w // 2, y) for y in range(h)]

    def dark_run(points):
        i = 0
        while i < len(points) and pixels[points[i]][3] < ALPHA:
            i += 1
        start = i
        while i < len(points) and pixels[points[i]][3] >= ALPHA and sum(pixels[points[i]][:3]) < 200:
            i += 1
        return i - start

    return max(0, dark_run(line[::-1]) - dark_run(line))


def measure(image, horizontal):
    if not edge_is_cut(image, horizontal):
        return 0
    cut = measure_cut(image, horizontal)
    return cut if cut > 0 else outline_difference(image, horizontal)


def restore(path):
    image = Image.open(os.path.join(SOURCE, path)).convert("RGBA")
    w, h = image.size
    cut_left = measure(image, True)
    # a banner's top arc and its ribbon-tail bottom aren't mirror images: only its sides are measured
    cut_top = 0 if "/ui_banner_" in path else measure(image, False)
    width, height = w + cut_left, h + cut_top
    restored = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    restored.paste(image, (cut_left, cut_top))
    pixels = restored.load()
    # the lost strips: mirrored from the intact opposite edge (top-left block from the bottom-right corner)
    for y in range(height):
        for x in range(width):
            if x >= cut_left and y >= cut_top:
                continue
            source_x = width - 1 - x if x < cut_left else x
            source_y = height - 1 - y if y < cut_top else y
            pixels[x, y] = pixels[source_x, source_y]
    out = os.path.join(TARGET, path)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    restored.save(out)
    print(f"{path}: {w}x{h} -> {width}x{height} (left +{cut_left}, top +{cut_top})")


if __name__ == "__main__":
    for file in FILES:
        restore(file)
