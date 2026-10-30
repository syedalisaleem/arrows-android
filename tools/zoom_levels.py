"""Render a few levels large, to judge a single figure closely.

    python tools/zoom_levels.py 11 19 22 17
"""

import sys

from PIL import Image, ImageDraw

import generate_levels as G
from preview_levels import draw_level

CELL = 54
PAD = 40
COLS = 2


def main():
    levels = [int(a) for a in sys.argv[1:]] or [11, 19, 21, 17]
    built = []
    for lv in levels:
        res = G.build_level(lv, lv)
        if res is None:
            print("FAILED", lv)
            continue
        name, n, count, entries = res
        built.append((lv, name, n, entries))
        print(f"{lv:4d} {name:12s} n={n} strokes={count}")

    size = (max(b[2] for b in built) + 2) * CELL + 2 * PAD
    rows = (len(built) + COLS - 1) // COLS
    sheet = Image.new("RGB", (COLS * size, rows * size), (239, 233, 229))
    draw = ImageDraw.Draw(sheet)
    for idx, (lv, name, n, entries) in enumerate(built):
        cx = (idx % COLS) * size + size / 2
        cy = (idx // COLS) * size + size / 2 + 12
        draw_level(draw, cx, cy, entries, n, cell=CELL)
        draw.text(((idx % COLS) * size + 8, (idx // COLS) * size + 8),
                  f"{lv} {name} n={n}", fill=(30, 60, 120))
    sheet.save("E:/build/shots/zoom.png")
    print("saved E:/build/shots/zoom.png", sheet.size)


if __name__ == "__main__":
    main()
