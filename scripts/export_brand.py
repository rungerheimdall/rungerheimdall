#!/usr/bin/env python3
"""Render the selected brand marks to PNG for GitHub avatars, NuGet icons and favicons."""

import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
ORG_OUT = ROOT / "brand" / "org"
PROJECT_OUT = ROOT / "brand" / "project"

SUPERSAMPLE = 4
DARK = (20, 22, 24)
AMBER = (192, 138, 62)

STRATA_BLOCKS = [
    (27, 6, 10, 12, 1.0),
    (16, 22, 10, 12, 1.0),
    (38, 22, 10, 12, 1.0),
    (5, 38, 10, 12, 1.0),
    (49, 38, 10, 12, 1.0),
    (27, 38, 10, 12, 0.35),
]


def hexagon(cx: float, cy: float, radius: float) -> list[tuple[float, float]]:
    points = []
    for index in range(6):
        angle = math.radians(60 * index - 90)
        points.append((cx + radius * math.cos(angle), cy + radius * math.sin(angle)))
    return points


def blend(foreground: tuple[int, int, int], background: tuple[int, int, int], alpha: float) -> tuple[int, int, int]:
    return tuple(int(foreground[i] * alpha + background[i] * (1 - alpha)) for i in range(3))


def render_concentric(size: int, background: tuple[int, int, int] | None) -> Image.Image:
    width = size * SUPERSAMPLE
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

    return canvas.resize((size, size), Image.LANCZOS)


def render_strata(size: int, background: tuple[int, int, int] | None) -> Image.Image:
    width = size * SUPERSAMPLE
    mode = "RGBA" if background is None else "RGB"
    fill = (0, 0, 0, 0) if background is None else background

    canvas = Image.new(mode, (width, width), fill)
    draw = ImageDraw.Draw(canvas)
    unit = width / 64

    for x, y, w, h, alpha in STRATA_BLOCKS:
        if background is None:
            color = (AMBER[0], AMBER[1], AMBER[2], int(255 * alpha))
        else:
            color = AMBER if alpha >= 1 else blend(AMBER, background, alpha)

        draw.rectangle(
            [x * unit, y * unit, (x + w) * unit - 1, (y + h) * unit - 1],
            fill=color,
        )

    return canvas.resize((size, size), Image.LANCZOS)


def main() -> None:
    ORG_OUT.mkdir(parents=True, exist_ok=True)
    PROJECT_OUT.mkdir(parents=True, exist_ok=True)

    render_concentric(460, DARK).save(ORG_OUT / "concentric-dark-460.png", "PNG")
    render_concentric(460, None).save(ORG_OUT / "concentric-transparent-460.png", "PNG")

    render_strata(460, DARK).save(PROJECT_OUT / "strata-dark-460.png", "PNG")
    render_strata(460, None).save(PROJECT_OUT / "strata-transparent-460.png", "PNG")
    render_strata(128, DARK).save(PROJECT_OUT / "strata-128.png", "PNG")
    render_strata(32, DARK).save(PROJECT_OUT / "strata-32.png", "PNG")

    print("Brand exports written to brand/org and brand/project.")


if __name__ == "__main__":
    main()