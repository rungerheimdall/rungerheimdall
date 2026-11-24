#!/usr/bin/env python3
"""Render the selected brand marks to PNG for GitHub avatars and NuGet icons."""

import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "brand" / "org"

SIZE = 460
SUPERSAMPLE = 4
DARK = (20, 22, 24)
AMBER = (192, 138, 62)


def hexagon(cx: float, cy: float, radius: float) -> list[tuple[float, float]]:
    points = []
    for index in range(6):
        angle = math.radians(60 * index - 90)
        points.append((cx + radius * math.cos(angle), cy + radius * math.sin(angle)))
    return points


def render_concentric(background: tuple[int, int, int, int] | None) -> Image.Image:
    width = SIZE * SUPERSAMPLE
    mode = "RGBA" if background is None else "RGB"
    fill = (0, 0, 0, 0) if background is None else background

    canvas = Image.new(mode, (width, width), fill)
    draw = ImageDraw.Draw(canvas)

    cx = cy = width / 2
    unit = width / 64

    outer = hexagon(cx, cy, 27 * unit)
    outer_inner = hexagon(cx, cy, 24.5 * unit)
    middle = hexagon(cx, cy, 17 * unit)
    middle_inner = hexagon(cx, cy, 14.5 * unit)
    inner = hexagon(cx, cy, 8 * unit)

    draw.polygon(outer, fill=AMBER)
    draw.polygon(outer_inner, fill=fill)
    draw.polygon(middle, fill=AMBER)
    draw.polygon(middle_inner, fill=fill)
    draw.polygon(inner, fill=AMBER)

    return canvas.resize((SIZE, SIZE), Image.LANCZOS)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)

    dark = render_concentric(DARK)
    dark.save(OUT / "concentric-dark-460.png", "PNG")

    transparent = render_concentric(None)
    transparent.save(OUT / "concentric-transparent-460.png", "PNG")

    print(f"Wrote {OUT / 'concentric-dark-460.png'}")
    print(f"Wrote {OUT / 'concentric-transparent-460.png'}")


if __name__ == "__main__":
    main()