#!/usr/bin/env python3
"""Render the selected brand marks to PNG for avatars, icons and social previews."""

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
ORG_OUT = ROOT / "brand" / "org"
PROJECT_OUT = ROOT / "brand" / "project"

SUPERSAMPLE = 4
DARK = (20, 22, 24)
AMBER = (192, 138, 62)
TEXT = (230, 232, 235)
MUTED = (154, 160, 167)
DIM = (107, 112, 119)

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

    draw.polygon(hexagon(cx, cy, 27 * unit), fill=AMBER)
    draw.polygon(hexagon(cx, cy, 24.5 * unit), fill=fill)
    draw.polygon(hexagon(cx, cy, 17 * unit), fill=AMBER)
    draw.polygon(hexagon(cx, cy, 14.5 * unit), fill=fill)
    draw.polygon(hexagon(cx, cy, 8 * unit), fill=AMBER)

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


def load_font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
    ]

    for candidate in candidates:
        if Path(candidate).exists():
            return ImageFont.truetype(candidate, size)

    return ImageFont.load_default()


def render_social_preview() -> Image.Image:
    width, height = 1280, 640
    canvas = Image.new("RGB", (width, height), DARK)
    draw = ImageDraw.Draw(canvas)

    mark = render_strata(190, DARK)
    canvas.paste(mark, (96, 224))

    draw.text((346, 236), "RungerHeimdall", font=load_font(78, bold=True), fill=TEXT)
    draw.text(
        (350, 340),
        "Architecture guard and allocation detection for .NET",
        font=load_font(30),
        fill=MUTED,
    )
    draw.text(
        (350, 392),
        "Compile-time rules. No runtime profiler. No test project.",
        font=load_font(24),
        fill=DIM,
    )

    organization = render_concentric(64, None)
    canvas.paste(organization, (1040, 512), organization)
    draw.text((1116, 528), "Rungerheimdall", font=load_font(22), fill=DIM)

    return canvas


def main() -> None:
    ORG_OUT.mkdir(parents=True, exist_ok=True)
    PROJECT_OUT.mkdir(parents=True, exist_ok=True)

    render_concentric(460, DARK).save(ORG_OUT / "concentric-dark-460.png", "PNG")
    render_concentric(460, None).save(ORG_OUT / "concentric-transparent-460.png", "PNG")
    render_concentric(128, DARK).save(ORG_OUT / "concentric-128.png", "PNG")

    render_strata(460, DARK).save(PROJECT_OUT / "strata-dark-460.png", "PNG")
    render_strata(460, None).save(PROJECT_OUT / "strata-transparent-460.png", "PNG")
    render_strata(128, DARK).save(PROJECT_OUT / "strata-128.png", "PNG")
    render_strata(32, DARK).save(PROJECT_OUT / "strata-32.png", "PNG")

    render_social_preview().save(PROJECT_OUT / "social-preview.png", "PNG")

    print("Brand exports written.")


if __name__ == "__main__":
    main()