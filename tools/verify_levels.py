"""Re-read the shipped level file and prove every level is clearable."""

import sys

from generate_levels import DIRS, ray_free

PATH = "Assets/_Game/Resources/Levels/procedural_levels.txt"


def parse(entry):
    start, steps = entry.split(":")
    x, y = (int(v) for v in start.split(","))
    points = [(x, y)]
    for ch in steps:
        dx, dy = DIRS[ch]
        x += dx
        y += dy
        points.append((x, y))
    return points


def solvable(strokes):
    cells = [set(s) for s in strokes]
    remaining = set(range(len(strokes)))
    while remaining:
        progress = False
        for i in list(remaining):
            others = set()
            for j in remaining:
                if j != i:
                    others |= cells[j]
            if ray_free(strokes[i], False, others):
                remaining.discard(i)
                progress = True
                break
        if not progress:
            return False
    return True


def main():
    rows = [r.strip() for r in open(PATH, encoding="utf-8") if r.strip() and not r.startswith("#")]
    problems = []
    totals = []
    for row in rows:
        level, body = row.split("|", 1)
        entries = [e for e in body.split(";") if e]
        strokes = [parse(e) for e in entries]
        for s in strokes:
            if len(s) < 2:
                problems.append((level, "stroke shorter than 2 points"))
            for a, b in zip(s, s[1:]):
                if abs(a[0] - b[0]) + abs(a[1] - b[1]) != 1:
                    problems.append((level, "non unit step"))
        # overlapping cells between two strokes would look broken
        seen = {}
        for idx, s in enumerate(strokes):
            for p in s:
                if p in seen:
                    problems.append((level, f"overlap at {p}"))
                seen[p] = idx
        if not solvable(strokes):
            problems.append((level, "not solvable"))
        totals.append((int(level), len(strokes)))

    levels = [l for l, _ in totals]
    print(f"levels parsed: {len(totals)}  range {min(levels)}..{max(levels)}")
    print(f"missing: {sorted(set(range(11, 501)) - set(levels))}")
    print(f"strokes per level: min={min(c for _, c in totals)} max={max(c for _, c in totals)} "
          f"avg={sum(c for _, c in totals) / len(totals):.1f}")
    if problems:
        print(f"PROBLEMS ({len(problems)}):")
        for p in problems[:20]:
            print("  ", p)
        return 1
    print("all levels solvable, no overlaps")
    return 0


if __name__ == "__main__":
    sys.exit(main())
