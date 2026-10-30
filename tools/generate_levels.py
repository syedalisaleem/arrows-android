"""
Generate Resources/Levels/procedural_levels.txt for levels 11-500.

Every level is a recognisable figure (heart, star, ring, letter, ...) instead of
the old random clusters.

How a level is built:
  1. rasterise the figure into a lattice mask,
  2. drop a few vertical strokes inside it (these become the blockers),
  3. fill every remaining row of the figure with horizontal strokes, split into
     short arrows, so the silhouette is covered edge to edge,
  4. search for a head direction per stroke that makes the board solvable.

Solvability rule (matches the game): a stroke may be tapped when no other stroke
has a lattice point on the ray starting at its head and continuing in its head
direction. Tapping strokes one at a time under that rule must clear the board.

Output format, one level per line:
    <level>|<startX>,<startY>:<U|D|L|R steps>;<startX>,<startY>:<steps>;...
"""

import random

from collections import deque

DIRS = {"U": (0, 1), "D": (0, -1), "L": (-1, 0), "R": (1, 0)}
OPPOSITE = {"U": "D", "D": "U", "L": "R", "R": "L"}

FIRST_LEVEL = 11
LAST_LEVEL = 500

# ---------------------------------------------------------------- figures

FONT = {
    "A": ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
    "S": ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
    "M": ["10001", "11011", "10101", "10001", "10001", "10001", "10001"],
    "N": ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
    "H": ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
    "X": ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
    "U": ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
    "Z": ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
    "K": ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
    "C": ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
    "O": ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    "T": ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
    "Y": ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
    "E": ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
    "L": ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
}

LETTERS = sorted(FONT)

POLYGONS = {
    # right-pointing arrow: shaft, then a head about twice as tall
    "arrow": [(-1, -0.30), (0.25, -0.30), (0.25, -0.62), (1, 0),
              (0.25, 0.62), (0.25, 0.30), (-1, 0.30)],
    # lightning bolt
    "bolt": [(-0.35, 1), (0.55, 1), (0.05, 0.15), (0.65, 0.15), (-0.55, -1), (-0.05, -0.15), (-0.6, -0.15)],
    # house
    "house": [(-0.8, -0.9), (0.8, -0.9), (0.8, 0.25), (0, 0.95), (-0.8, 0.25)],
    # shield with a soft point so the bottom row is not a single cell
    "shield": [(-0.85, 0.88), (0.85, 0.88), (0.85, -0.15), (0.6, -0.6),
               (0, -0.95), (-0.6, -0.6), (-0.85, -0.15)],
    # crown
    "crown": [(-0.95, -0.7), (0.95, -0.7), (0.95, 0.35), (0.5, -0.1), (0, 0.85), (-0.5, -0.1), (-0.95, 0.35)],
}

GEOMETRIC = ["heart", "star", "ring", "circle", "diamond", "triangle",
             "hexagon", "cross", "arrow", "house", "shield", "crown"]

# figures with one-cell-wide points that need thickening before they are filled
THIN_SHAPES = {"star", "triangle", "crown"}


def point_in_poly(u, v, poly):
    inside = False
    n = len(poly)
    for i in range(n):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % n]
        if (y1 > v) != (y2 > v):
            xint = (x2 - x1) * (v - y1) / (y2 - y1) + x1
            if u < xint:
                inside = not inside
    return inside


def inside_shape(name, u, v):
    import math
    r = math.hypot(u, v)
    if name == "heart":
        x, y = u * 1.12, v * 1.12 + 0.06
        return (x * x + y * y - 1) ** 3 - x * x * y ** 3 <= 0 and r <= 1.05
    if name == "star":
        theta = math.atan2(v, u) + math.pi / 2
        seg = math.pi / 5
        a = abs((theta % (2 * seg)) - seg) / seg
        return r <= (1 - 0.56 * a) * 0.98
    if name == "ring":
        return 0.52 <= r <= 0.98
    if name == "circle":
        return r <= 0.98
    if name == "diamond":
        return abs(u) + abs(v) <= 0.98
    if name == "triangle":
        return -1 <= v <= 1 and abs(u) <= (1 - v) * 0.52
    if name == "hexagon":
        return max(abs(u), abs(u) * 0.5 + abs(v) * 0.866) <= 0.9 and abs(v) <= 0.86
    if name == "cross":
        return (abs(u) <= 0.30 and abs(v) <= 0.95) or (abs(v) <= 0.30 and abs(u) <= 0.95)
    if name == "spiral":
        if r > 0.98:
            return False
        theta = (math.atan2(v, u) + math.pi) / (2 * math.pi)
        return ((r * 1.9 - theta) % 1.0) < 0.55 and r > 0.12
    if name == "flower":
        theta = math.atan2(v, u)
        return r <= 0.55 + 0.33 * math.cos(5 * theta)
    if name in POLYGONS:
        return point_in_poly(u, v, POLYGONS[name])
    raise ValueError(name)


def dilate(mask, rounds=1):
    for _ in range(rounds):
        grown = set(mask)
        for (x, y) in mask:
            for dx, dy in DIRS.values():
                grown.add((x + dx, y + dy))
        mask = grown
    return mask


def shape_cells(name, n):
    half = (n - 1) / 2.0
    cells = set()
    for j in range(n):
        for i in range(n):
            u = (i - half) / half
            v = (j - half) / half
            if inside_shape(name, u, v):
                cells.add((i, j))
    return cells


def letter_cells(ch, n):
    scale = max(1, min(3, (n - 1) // 7))
    width, height = 5 * scale, 7 * scale
    x0 = (n - width) // 2
    y0 = (n - height) // 2
    cells = set()
    for r, row in enumerate(FONT[ch]):
        for c, bit in enumerate(row):
            if bit != "1":
                continue
            for dy in range(scale):
                for dx in range(scale):
                    cells.add((x0 + c * scale + dx, y0 + (6 - r) * scale + dy))
    return dilate(cells, 1)


# ---------------------------------------------------------------- layout

def build_strokes(mask, rng, span):
    """Vertical blockers first, then a horizontal fill of every remaining row."""
    xs = sorted({c[0] for c in mask})
    ys = sorted({c[1] for c in mask})
    occupied = set()
    strokes = []

    # --- vertical strokes, which later block the horizontal arrows.
    # They live strictly inside the figure (2 cells of clearance on every side),
    # so the silhouette that makes the shape readable is never broken.
    columns = xs[:]
    rng.shuffle(columns)
    vertical_count = max(2, min(6, len(xs) // 5))

    def wide_enough(x, y):
        return all((x + dx, y) in mask for dx in (-2, -1, 0, 1, 2))

    for x in columns[:vertical_count]:
        safe = sorted(y for (xx, y) in mask if xx == x and wide_enough(x, y))
        if len(safe) < 5:
            continue

        stretches = [[safe[0]]]
        for a, b in zip(safe, safe[1:]):
            if b == a + 1:
                stretches[-1].append(b)
            else:
                stretches.append([b])

        stretch = max(stretches, key=len)
        usable = stretch[1:-1]              # keep clear of the outline above/below
        if len(usable) < 3:
            continue
        length = rng.randint(3, min(9, len(usable)))
        start = rng.randint(0, len(usable) - length)
        points = [(x, y) for y in usable[start:start + length]]
        strokes.append(points)
        occupied |= set(points)

    # --- horizontal fill
    max_seg = 7 if span < 0.6 else 6
    for y in ys:
        row = sorted(x for (x, yy) in mask if yy == y)
        runs = []
        current = []
        for x in row:
            if (x, y) in occupied:
                if current:
                    runs.append(current)
                    current = []
                continue
            if current and x != current[-1] + 1:
                runs.append(current)
                current = []
            current.append(x)
        if current:
            runs.append(current)

        for run in runs:
            if len(run) < 2:
                continue
            i = 0
            while i < len(run):
                remaining = len(run) - i
                if remaining <= max_seg:
                    take = remaining
                else:
                    take = rng.randint(3, max_seg)
                    if remaining - take == 1:
                        take -= 1
                seg = run[i:i + take]
                strokes.append([(x, y) for x in seg])
                i += take

    return strokes


# ---------------------------------------------------------------- solver

def head_of(points, flipped):
    pts = points[::-1] if flipped else points
    (x1, y1), (x2, y2) = pts[-2], pts[-1]
    for d, (dx, dy) in DIRS.items():
        if x2 - x1 == dx and y2 - y1 == dy:
            return (x2, y2), d
    raise ValueError("stroke segment is not axis aligned")


def ray_free(points, flipped, others):
    head, d = head_of(points, flipped)
    dx, dy = DIRS[d]
    x, y = head[0] + dx, head[1] + dy
    for _ in range(90):
        if (x, y) in others:
            return False
        x += dx
        y += dy
    return True


def solve(strokes, flips):
    """Greedy removal order; returns (order, stuck) - order is None when stuck."""
    cells = [set(s) for s in strokes]
    remaining = set(range(len(strokes)))
    order = []
    while remaining:
        moved = False
        for i in list(remaining):
            others = set()
            for j in remaining:
                if j != i:
                    others |= cells[j]
            if ray_free(strokes[i], flips[i], others):
                remaining.discard(i)
                order.append(i)
                moved = True
                break
        if not moved:
            return None, remaining
    return order, set()


def repair(strokes, rng, rounds=600):
    """Flip head directions until the board can be cleared."""
    flips = [rng.random() < 0.5 for _ in strokes]
    cells = [set(s) for s in strokes]

    for _ in range(rounds):
        order, stuck = solve(strokes, flips)
        if order is not None:
            return flips

        best = None
        for i in stuck:
            flips[i] = not flips[i]
            gain = 0
            head, d = head_of(strokes[i], flips[i])
            dx, dy = DIRS[d]
            x, y = head[0] + dx, head[1] + dy
            others = set()
            for j in range(len(strokes)):
                if j != i:
                    others |= cells[j]
            while gain < 90 and (x, y) not in others:
                gain += 1
                x += dx
                y += dy
            flips[i] = not flips[i]
            if best is None or gain > best[0]:
                best = (gain, i)
        if best is None:
            return None
        flips[best[1]] = not flips[best[1]]

    return None


# ---------------------------------------------------------------- output

def to_entry(points, flipped):
    pts = points[::-1] if flipped else points
    steps = []
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        for d, (dx, dy) in DIRS.items():
            if x2 - x1 == dx and y2 - y1 == dy:
                steps.append(d)
                break
        else:
            raise ValueError("non unit step")
    return f"{pts[0][0]},{pts[0][1]}:{''.join(steps)}"


def figure_mask(name, n):
    """Lattice cells for a figure, thickened where a shape has thin points."""
    if name.startswith("letter:"):
        return letter_cells(name.split(":")[1], n)
    mask = shape_cells(name, n)
    if name in THIN_SHAPES:
        mask = dilate(mask, 1)
    return mask


def build_level(level, seed):
    span = (level - FIRST_LEVEL) / (LAST_LEVEL - FIRST_LEVEL)
    n = int(round(15 + span * 10))
    if n % 2 == 0:
        n += 1

    families = GEOMETRIC + [f"letter:{c}" for c in LETTERS]
    name = families[(level - FIRST_LEVEL) % len(families)]
    mask = figure_mask(name, n)
    if not name.startswith("letter:"):
        if len({c[0] for c in mask}) < 6 or len({c[1] for c in mask}) < 6:
            return None

    if len(mask) < 24:
        return None

    for attempt in range(60):
        rng = random.Random(seed * 1000 + attempt)
        strokes = build_strokes(mask, rng, span)
        if len(strokes) < 8:
            continue

        # centre the figure on the origin
        xs = [p[0] for s in strokes for p in s]
        ys = [p[1] for s in strokes for p in s]
        cx = (min(xs) + max(xs)) // 2
        cy = (min(ys) + max(ys)) // 2
        strokes = [[(x - cx, y - cy) for (x, y) in s] for s in strokes]

        flips = repair(strokes, rng)
        if flips is None:
            continue
        entries = [to_entry(s, f) for s, f in zip(strokes, flips)]
        return name, n, len(entries), entries

    return None


def main():
    lines = [
        "# Arrows procedural levels (11-500).",
        "# Generated by tools/generate_levels.py - every level draws a figure and is",
        "# solver-verified in reverse construction order.",
        "# Format: level|startX,startY:UDLR;startX,startY:UDLR",
    ]

    stats = {}
    failures = []
    for level in range(FIRST_LEVEL, LAST_LEVEL + 1):
        built = build_level(level, level)
        if built is None:
            failures.append(level)
            continue
        name, n, count, entries = built
        stats[name] = stats.get(name, 0) + 1
        lines.append(f"{level}|" + ";".join(entries))

    with open("Assets/_Game/Resources/Levels/procedural_levels.txt", "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(lines) + "\n")

    print(f"wrote {LAST_LEVEL - FIRST_LEVEL + 1 - len(failures)} levels, failures={failures[:20]} ({len(failures)})")
    for k in sorted(stats):
        print(f"  {k}: {stats[k]}")


if __name__ == "__main__":
    main()
