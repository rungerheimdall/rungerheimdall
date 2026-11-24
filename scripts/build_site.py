#!/usr/bin/env python3
"""Generate the static documentation site from the Markdown sources."""

import shutil
from pathlib import Path

import markdown

ROOT = Path(__file__).resolve().parent.parent
DOCS = ROOT / "website" / "docs"
BLOG = ROOT / "website" / "blog"
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
<link rel="icon" href="assets/favicon.svg" type="image/svg+xml">
<link rel="stylesheet" href="assets/style.css">
</head>
<body>
<header class="topbar">
  <a class="brand" href="index.html">
    <img src="assets/logo.svg" alt="" width="26" height="26">
    <span>RungerHeimdall</span>
  </a>
  <nav class="topnav">
    <a href="rules.html">Rules</a>
    <a href="playground.html">Playground</a>
    <a href="blog.html">Blog</a>
    <a href="cli.html">CLI</a>
    <a href="brand/">Brand</a>
    <a href="https://github.com/rungerheimdall/rungerheimdall">GitHub</a>
  </nav>
</header>
<div class="layout">
  <aside class="sidebar">
    <p class="sidebar-title">Documentation</p>
    <nav>
{nav}
    </nav>
    <p class="sidebar-meta">0.1.0-preview.4</p>
  </aside>
  <main class="content">
{content}
  </main>
</div>
<footer class="footer">
  <span class="footer-org"><img src="assets/concentric.svg" alt="" width="18" height="18"> Rungerheimdall</span>
  <span>MIT licensed. Copyright © 2026 RungerHeimdall contributors.</span>
</footer>
</body>
</html>
"""

BLOG_TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{title} · RungerHeimdall</title>
<meta name="description" content="{description}">
<link rel="icon" href="{base}assets/favicon.svg" type="image/svg+xml">
<link rel="stylesheet" href="{base}assets/style.css">
</head>
<body>
<header class="topbar">
  <a class="brand" href="{base}index.html">
    <img src="{base}assets/logo.svg" alt="" width="26" height="26">
    <span>RungerHeimdall</span>
  </a>
  <nav class="topnav">
    <a href="{base}rules.html">Rules</a>
    <a href="{base}playground.html">Playground</a>
    <a href="{base}blog.html">Blog</a>
    <a href="{base}cli.html">CLI</a>
    <a href="{base}brand/">Brand</a>
    <a href="https://github.com/rungerheimdall/rungerheimdall">GitHub</a>
  </nav>
</header>
<article class="content blog-content">
{content}
</article>
<footer class="footer">
  <span class="footer-org"><img src="{base}assets/concentric.svg" alt="" width="18" height="18"> Rungerheimdall</span>
  <span>MIT licensed. Copyright © 2026 RungerHeimdall contributors.</span>
</footer>
</body>
</html>
"""


def split_front_matter(text: str) -> tuple[dict, str]:
    if not text.startswith("---"):
        return {}, text

    end = text.find("\n---", 3)
    if end == -1:
        return {}, text

    header = text[3:end].strip()
    body = text[end + 4 :].lstrip("\n")
    metadata = {}

    for line in header.splitlines():
        if ":" in line:
            key, value = line.split(":", 1)
            metadata[key.strip()] = value.strip()

    return metadata, body


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


def render_blog() -> int:
    if not BLOG.exists():
        return 0

    out_dir = OUT / "blog"
    out_dir.mkdir(exist_ok=True)

    posts = []
    for path in sorted(BLOG.glob("*.md")):
        metadata, body = split_front_matter(path.read_text(encoding="utf-8"))
        slug = path.stem
        html = markdown.markdown(
            body,
            extensions=["fenced_code", "tables", "sane_lists"],
        )

        content = (
            f'<p class="blog-meta">{metadata.get("date", "")}</p>\n'
            f'<h1>{metadata.get("title", slug)}</h1>\n'
            f'<p class="blog-lede">{metadata.get("description", "")}</p>\n'
            f"{html}"
        )

        page = BLOG_TEMPLATE.format(
            base="../",
            title=metadata.get("title", slug),
            description=metadata.get("description", ""),
            content=content,
        )
        (out_dir / f"{slug}.html").write_text(page, encoding="utf-8")
        posts.append((slug, metadata))

    items = "\n".join(
        f'<a class="blog-item" href="blog/{slug}.html">'
        f'<h2>{metadata.get("title", slug)}</h2>'
        f'<p>{metadata.get("description", "")}</p>'
        f'<span>{metadata.get("date", "")}</span>'
        f"</a>"
        for slug, metadata in posts
    )

    index = BLOG_TEMPLATE.format(
        base="",
        title="Blog",
        description="Notes on architecture rules, allocation patterns and analyzer design.",
        content=f'<h1>Blog</h1>\n<div class="blog-list">{items}</div>',
    )
    (OUT / "blog.html").write_text(index, encoding="utf-8")

    return len(posts)


def main() -> None:
    if OUT.exists():
        shutil.rmtree(OUT)

    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "assets").mkdir(exist_ok=True)

    shutil.copy2(ASSETS / "style.css", OUT / "assets" / "style.css")
    shutil.copy2(ASSETS / "logo.svg", OUT / "assets" / "logo.svg")
    shutil.copy2(ASSETS / "favicon.svg", OUT / "assets" / "favicon.svg")
    shutil.copy2(ASSETS / "concentric.svg", OUT / "assets" / "concentric.svg")
    shutil.copy2(ASSETS / "index.html", OUT / "index.html")
    shutil.copy2(ROOT / "website" / "rules.html", OUT / "rules.html")
    shutil.copy2(ROOT / "website" / "rules.json", OUT / "rules.json")
    shutil.copy2(ASSETS / "playground.html", OUT / "playground.html")

    brand = ROOT / "brand"
    if brand.exists():
        shutil.copytree(brand, OUT / "brand")

    for source, title, output in PAGES:
        raw = (DOCS / source).read_text(encoding="utf-8")
        body = rewrite_links(split_front_matter(raw)[1])
        (OUT / output).write_text(render_page(title, output, body), encoding="utf-8")

    posts = render_blog()

    print(f"Generated {len(PAGES)} documentation pages and {posts} blog posts in {OUT}")


if __name__ == "__main__":
    main()