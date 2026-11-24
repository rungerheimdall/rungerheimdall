#!/usr/bin/env python3
"""Generate the static documentation site from the Markdown sources."""

import shutil
from pathlib import Path

import markdown

ROOT = Path(__file__).resolve().parent.parent
DOCS = ROOT / "website" / "docs"
ASSETS = ROOT / "website" / "assets"
OUT = ROOT / "website" / "build"

PAGES = [
    ("intro.md", "Overview", "overview.html"),
    ("getting-started.md", "Getting started", "getting-started.html"),
    ("rules/architecture.md", "Architecture rules", "architecture-rules.html"),
    ("rules/performance.md", "Performance rules", "performance-rules.html"),
    ("configuration.md", "Configuration", "configuration.html"),
    ("baseline.md", "Baseline", "baseline.html"),
    ("cli.md", "CLI", "cli.html"),
    ("ci.md", "CI", "ci.html"),
]

LINK_MAP = {
    "intro.md": "overview.html",
    "getting-started.md": "getting-started.html",
    "rules/architecture.md": "architecture-rules.html",
    "rules/performance.md": "performance-rules.html",
    "configuration.md": "configuration.html",
    "baseline.md": "baseline.html",
    "cli.md": "cli.html",
    "ci.md": "ci.html",
}

NAV = [
    ("Overview", "overview.html"),
    ("Getting started", "getting-started.html"),
    ("Architecture rules", "architecture-rules.html"),
    ("Performance rules", "performance-rules.html"),
    ("Configuration", "configuration.html"),
    ("Baseline", "baseline.html"),
    ("CLI", "cli.html"),
    ("CI", "ci.html"),
]

TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{title} · RungerHeimdall</title>
<meta name="description" content="Compiler-level architecture guard and allocation detection for .NET.">
<link rel="icon" href="assets/logo.svg" type="image/svg+xml">
<link rel="stylesheet" href="assets/style.css">
</head>
<body>
<header class="topbar">
  <a class="brand" href="index.html">
    <img src="assets/logo.svg" alt="" width="22" height="22">
    <span>RungerHeimdall</span>
  </a>
  <nav class="topnav">
    <a href="architecture-rules.html">Rules</a>
    <a href="cli.html">CLI</a>
    <a href="https://github.com/rungerheimdall/rungerheimdall">GitHub</a>
  </nav>
</header>
<div class="layout">
  <aside class="sidebar">
    <p class="sidebar-title">Documentation</p>
    <nav>
{nav}
    </nav>
    <p class="sidebar-meta">0.1.0-preview.3</p>
  </aside>
  <main class="content">
{content}
  </main>
</div>
<footer class="footer">
  <span>MIT licensed. Copyright © 2026 RungerHeimdall contributors.</span>
</footer>
</body>
</html>
"""


def strip_front_matter(text: str) -> str:
    if text.startswith("---"):
        end = text.find("\n---", 3)
        if end != -1:
            return text[end + 4 :].lstrip("\n")
    return text


def rewrite_links(text: str) -> str:
    for source, target in LINK_MAP.items():
        text = text.replace(f"]({source})", f"]({target})")
    return text


def render_nav(current: str) -> str:
    lines = []
    for label, href in NAV:
        active = ' class="active"' if href == current else ""
        lines.append(f'      <a href="{href}"{active}>{label}</a>')
    return "\n".join(lines)


def render_page(title: str, output: str, body: str) -> str:
    content = markdown.markdown(
        body,
        extensions=["fenced_code", "tables", "sane_lists"],
    )
    return TEMPLATE.format(title=title, nav=render_nav(output), content=content)


def main() -> None:
    if OUT.exists():
        shutil.rmtree(OUT)

    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "assets").mkdir(exist_ok=True)

    shutil.copy2(ASSETS / "style.css", OUT / "assets" / "style.css")
    shutil.copy2(ASSETS / "logo.svg", OUT / "assets" / "logo.svg")
    shutil.copy2(ASSETS / "index.html", OUT / "index.html")

    brand = ROOT / "brand"
    if brand.exists():
        shutil.copytree(brand, OUT / "brand")

    for source, title, output in PAGES:
        raw = (DOCS / source).read_text(encoding="utf-8")
        body = rewrite_links(strip_front_matter(raw))
        (OUT / output).write_text(render_page(title, output, body), encoding="utf-8")

    print(f"Generated {len(PAGES)} documentation pages in {OUT}")


if __name__ == "__main__":
    main()