"""Render the raw figure masks (before stroke layout) to judge silhouettes."""

import sys

from PIL import Image, ImageDraw

import generate_levels as G

CELL = 20
PAD = 24
COLS = 7


def level_name(level):
    families = G.GEOMETRIC + [f"letter:{c}" for c in G.LETTERS]
    return families[(level - G.FIRST_LEVEL) % len(families)]


def grid_size(level):
    span = (level - G.FIRST_LEVEL) / (G.LAST_LEVEL - G.FIRST_LEVEL)
    n = int(round(15 + span * 10))
    return n + 1 if n % 2 == 0 else n


def main():
    levels = [int(a) for a in sys.argv[1:]] or list(range(11, 38))
    panels = []
    for lv in levels:
        name = level_name(lv)
        n = grid_size(lv)
        panels.append((lv, name, n, G.figure_mask(name, n)))

    n_max = max(p[2] for p in panels)
    tile = (n_max + 2) * CELL + 2 * PAD
    rows = (len(panels) + COLS - 1) // COLS
    sheet = Image.new("RGB", (COLS * tile, rows * tile), (239, 233, 229))
    draw = ImageDraw.Draw(sheet)

    for idx, (lv, name, n, mask) in enumerate(panels):
        ox = (idx % COLS) * tile + PAD
        oy = (idx // COLS) * tile + PAD + 12
        draw.text((ox, oy - 20), f"{lv} {name} n{n} ({len(mask)})", fill=(30, 60, 120))
        for (x, y) in mask:
            px = ox + x * CELL
            py = oy + (n - 1 - y) * CELL
            draw.rectangle([px + 1, py + 1, px + CELL - 1, py + CELL - 1],
                           fill=(70, 62, 56))
    sheet.save("E:/build/shots/shape-masks.png")
    print("saved E:/build/shots/shape-masks.png", sheet.size)


if __name__ == "__main__":
    main()
