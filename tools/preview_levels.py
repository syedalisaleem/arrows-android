"""Render sample levels to a contact sheet so the shapes can be eyeballed.

Strokes are centred on the origin, so everything is drawn around a panel centre.
"""

import sys

from PIL import Image, ImageDraw

import generate_levels as G

SAMPLES = [11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
           25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37,
           60, 120, 200, 300, 400, 500]

CELL = 26
PAD = 30
COLS = 6


def draw_level(draw, cx, cy, entries, grid, cell=None):
    """Draw a level with its board centre at pixel (cx, cy)."""
    cell = cell or CELL

    def to_px(p):
        return (cx + p[0] * cell, cy - p[1] * cell)

    for entry in entries:
        start, steps = entry.split(":")
        x, y = (int(v) for v in start.split(","))
        pts = [(x, y)]
        for ch in steps:
            dx, dy = G.DIRS[ch]
            x += dx
            y += dy
            pts.append((x, y))
        pix = [to_px(p) for p in pts]
        draw.line(pix, fill=(70, 62, 56), width=max(3, cell // 3), joint="curve")

        (x1, y1), (x2, y2) = pts[-2], pts[-1]
        hx, hy = x2 - x1, y2 - y1          # unit step in grid coords
        hx_px, hy_px = hx * cell, -hy * cell
        size = cell * 0.5
        tipx, tipy = to_px((x2, y2))
        draw.polygon([
            (tipx + hx_px * size / cell, tipy + hy_px * size / cell),
            (tipx - hy_px * size / cell * 0.8 - hx_px * 0.1, tipy + hx_px * size / cell * 0.8 - hy_px * 0.1),
            (tipx + hy_px * size / cell * 0.8 - hx_px * 0.1, tipy - hx_px * size / cell * 0.8 - hy_px * 0.1),
        ], fill=(70, 62, 56))


def main():
    levels = []
    for lv in SAMPLES:
        built = G.build_level(lv, lv)
        if built is None:
            print("FAILED", lv)
            continue
        name, n, count, entries = built
        levels.append((lv, name, n, entries))

    cols = COLS
    rows = (len(levels) + cols - 1) // cols
    board = (max(l[2] for l in levels) + 2) * CELL + 2 * PAD
    sheet = Image.new("RGB", (cols * board, rows * board), (239, 233, 229))
    draw = ImageDraw.Draw(sheet)
    for idx, (lv, name, n, entries) in enumerate(levels):
        cx = (idx % cols) * board + board / 2
        cy = (idx // cols) * board + board / 2 + 10
        draw_level(draw, cx, cy, entries, n)
        draw.text(((idx % cols) * board + 8, (idx // cols) * board + 6),
                  f"{lv} {name} ({len(entries)})", fill=(30, 60, 120))
    sheet.save("E:/build/shots/level-preview.png")
    print("saved E:/build/shots/level-preview.png", sheet.size)


if __name__ == "__main__":
    main()
