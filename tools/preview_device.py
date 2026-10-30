"""Render a level exactly as the phone would show it.

Mirrors CameraManager.FitCameraToLines (padding 2 units, orthographic fit) and
ProceduralLevelBuilder (stroke width clamp, head scale clamp) on a 1080x2280
canvas. Strokes overlap as they do on device, so crowding is visible.

    python tools/preview_device.py 11 200 500
"""

import sys

from PIL import Image, ImageDraw

import generate_levels as G

W, H = 1080, 2280
PADDING = 2.0
BACKGROUND = (239, 233, 229)
INK = (70, 62, 56)


def parse(entries):
    strokes = []
    for entry in entries:
        start, steps = entry.split(":")
        x, y = (int(v) for v in start.split(","))
        pts = [(x, y)]
        for ch in steps:
            dx, dy = G.DIRS[ch]
            x += dx
            y += dy
            pts.append((x, y))
        strokes.append(pts)
    return strokes


def device_scale(strokes):
    xs = [p[0] for s in strokes for p in s]
    ys = [p[1] for s in strokes for p in s]
    width = (max(xs) - min(xs)) + 2 * PADDING
    height = (max(ys) - min(ys)) + 2 * PADDING
    aspect = W / H
    ortho = max(width / (2 * aspect), height / 2)
    return H / (2 * ortho), (max(xs) - min(xs)), (max(ys) - min(ys))


def render(level, path, cell_px=None):
    built = G.build_level(level, level)
    if built is None:
        raise SystemExit(f"level {level} failed to build")
    name, n, count, entries = built
    strokes = parse(entries)
    px_per_unit, span_x, span_y = device_scale(strokes)

    stroke_units = min(max(0.022 * (max(span_x, span_y) + 4.0), 0.28), 0.55)
    head_units = min(max(stroke_units * 3.0, 0.9), 1.05)

    image = Image.new("RGB", (W, H), BACKGROUND)
    draw = ImageDraw.Draw(image)

    def to_px(p):
        return (W / 2 + p[0] * px_per_unit, H / 2 - p[1] * px_per_unit)

    line_px = stroke_units * px_per_unit
    for pts in strokes:
        draw.line([to_px(p) for p in pts], fill=INK, width=max(2, int(round(line_px))),
                  joint="curve")

    head_h = head_units * px_per_unit
    head_w = head_h * 0.82
    for pts in strokes:
        (x1, y1), (x2, y2) = pts[-2], pts[-1]
        dx, dy = x2 - x1, y2 - y1
        length = (dx * dx + dy * dy) ** 0.5
        ux, uy = dx / length, dy / length
        tx, ty = to_px((x2, y2))
        # sprite tip sits a little past the last point, as on device
        tipx = tx + ux * head_h * 0.35
        tipy = ty - uy * head_h * 0.35
        basex, basey = tipx - ux * head_h, tipy + uy * head_h
        draw.polygon([
            (tipx, tipy),
            (basex - uy * head_w / 2, basey - ux * head_w / 2),
            (basex + uy * head_w / 2, basey + ux * head_w / 2),
        ], fill=INK)

    draw.text((24, 24), f"level {level}  {name}  n={n}  strokes={count}  "
                        f"px/unit={px_per_unit:.1f}", fill=(30, 60, 120))
    image.save(path)
    print(f"level {level:3d} {name:12s} n={n} strokes={count} "
          f"span={span_x}x{span_y} px/unit={px_per_unit:.1f} "
          f"stroke={stroke_units:.2f}u head={head_units:.2f}u -> {path}")


def main():
    levels = [int(a) for a in sys.argv[1:]] or [11, 200, 500]
    for lv in levels:
        render(lv, f"E:/build/shots/device-{lv}.png")


if __name__ == "__main__":
    main()
