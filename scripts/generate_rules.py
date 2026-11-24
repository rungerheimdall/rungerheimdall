#!/usr/bin/env python3
"""Extract rule metadata from the analyzer sources and build the rules browser."""

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "src"
WEBSITE = ROOT / "website"

PATTERN = re.compile(
    r'DiagnosticFactory\.Create\(\s*'
    r'"(?P<id>[^"]+)"\s*,\s*'
    r'"(?P<title>[^"]+)"\s*,\s*'
    r'"(?P<message>[^"]*)"\s*,\s*'
    r'"(?P<category>[^"]+)"\s*,\s*'
    r'DiagnosticSeverity\.(?P<severity>\w+)',
    re.S,
)

CATEGORY_LINKS = {
    "Architecture": "architecture-rules.html",
    "Performance": "performance-rules.html",
}


def parse(path: Path) -> list[dict]:
    text = path.read_text(encoding="utf-8")
    rules = []

    for match in PATTERN.finditer(text):
        rules.append(
            {
                "id": match.group("id"),
                "title": match.group("title"),
                "message": match.group("message"),
                "category": match.group("category"),
                "severity": match.group("severity"),
                "source": path.name,
            }
        )

    return rules


def collect() -> list[dict]:
    rules: list[dict] = []

    for folder in ("RungerHeimdall.Analyzers.Architecture", "RungerHeimdall.Analyzers.Performance"):
        for path in sorted((SRC / folder / "Rules").glob("*.cs")):
            rules.extend(parse(path))

    return sorted(rules, key=lambda rule: rule["id"])


def render_html(rules: list[dict]) -> str:
    rows = []

    for rule in rules:
        link = CATEGORY_LINKS.get(rule["category"], "rules.html")
        rows.append(
            f'<tr data-category="{rule["category"]}" data-severity="{rule["severity"]}">'
            f'<td class="id">{rule["id"]}</td>'
            f'<td>{rule["title"]}</td>'
            f'<td><a href="{link}">{rule["category"]}</a></td>'
            f'<td class="sev">{rule["severity"]}</td>'
            f"</tr>"
        )

    body = "\n".join(rows)

    return f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Rules · RungerHeimdall</title>
<link rel="icon" href="assets/favicon.svg" type="image/svg+xml">
<link rel="stylesheet" href="assets/style.css">
<style>
  .rules-header {{ max-width: 1120px; margin: 0 auto; padding: 4rem 2rem 2rem; }}
  .rules-header h1 {{ margin: 0 0 .75rem; font-size: 2rem; letter-spacing: -.02em; }}
  .rules-header p {{ margin: 0; color: var(--muted); line-height: 1.7; max-width: 680px; }}
  .rules-body {{ max-width: 1120px; margin: 0 auto; padding: 1rem 2rem 4rem; }}
  .filters {{ display: flex; gap: .5rem; margin: 1.5rem 0; flex-wrap: wrap; }}
  .filters button {{
    padding: .4rem .8rem; border: 1px solid var(--border); background: transparent;
    color: var(--muted); font-family: var(--mono); font-size: .72rem; cursor: pointer;
  }}
  .filters button.active {{ border-color: var(--accent); color: var(--accent); }}
  table {{ width: 100%; border-collapse: collapse; font-size: .9rem; }}
  th, td {{ border: 1px solid var(--border); padding: .55rem .75rem; text-align: left; }}
  th {{ background: var(--surface); font-weight: 600; }}
  td.id {{ font-family: var(--mono); white-space: nowrap; }}
  td.sev {{ font-family: var(--mono); }}
</style>
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
<div class="rules-header">
  <h1>Rules</h1>
  <p>Every rule shipped by the analyzers. Severity is the default; both <code>architecture.json</code> and <code>.editorconfig</code> can override it.</p>
</div>
<div class="rules-body">
  <div class="filters" id="filters">
    <button data-filter="all" class="active">All</button>
    <button data-filter="Architecture">Architecture</button>
    <button data-filter="Performance">Performance</button>
    <button data-filter="Warning">Warning</button>
    <button data-filter="Info">Info</button>
  </div>
  <table>
    <thead><tr><th>ID</th><th>Rule</th><th>Family</th><th>Severity</th></tr></thead>
    <tbody id="rows">
{body}
    </tbody>
  </table>
</div>
<footer class="footer">
  <span class="footer-org"><img src="assets/concentric.svg" alt="" width="18" height="18"> Rungerheimdall</span>
  <span>MIT licensed. Copyright © 2026 RungerHeimdall contributors.</span>
</footer>
<script>
  const buttons = document.querySelectorAll('#filters button');
  const rows = document.querySelectorAll('#rows tr');

  buttons.forEach(button => {{
    button.addEventListener('click', () => {{
      buttons.forEach(other => other.classList.remove('active'));
      button.classList.add('active');

      const filter = button.dataset.filter;
      rows.forEach(row => {{
        const matches = filter === 'all'
          || row.dataset.category === filter
          || row.dataset.severity === filter;
        row.style.display = matches ? '' : 'none';
      }});
    }});
  }});
</script>
</body>
</html>
"""


def main() -> None:
    rules = collect()

    (WEBSITE / "rules.json").write_text(
        json.dumps(rules, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    (WEBSITE / "rules.html").write_text(render_html(rules), encoding="utf-8")

    print(f"Generated {len(rules)} rules.")


if __name__ == "__main__":
    main()