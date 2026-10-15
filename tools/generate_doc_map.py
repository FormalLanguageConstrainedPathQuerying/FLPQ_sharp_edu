#!/usr/bin/env python3
"""Generate the documentation navigation map in docs/main.md from per-page metadata.

Every page under docs/ (except docs/main.md itself) must carry a metadata block
with **Tags:**, **Kind:**, and an Abstract line (**Abstract:** inside a blockquote).
The tool regenerates the list sections of docs/main.md between the
``<!-- doc-map:<section> -->`` / ``<!-- /doc-map:<section> -->`` marker pairs.
Hand-editing a list item between markers is forbidden: CI runs this tool in
--check mode and fails on any drift or missing metadata.

Grouping is derived from the page path (relative to docs/):

- ``project/*.md``           -> project section
- ``developer/guides/*.md``  -> guides section
- ``developer/FLPQ.*.md``    -> hubs section
- ``developer/*.md``         -> modules section (alphabetical)
- ``user/*.md``              -> user section

Entry format: ``- [Title](relative link) — <first sentence of the Abstract>``.
The title is the page's H1; the description is the first sentence of its
Abstract, so the page itself stays the single source of truth for both.
"""

import argparse
import difflib
import re
import sys
from pathlib import Path

DOCS_DIR = Path("docs")
MAIN_MD = DOCS_DIR / "main.md"
SECTIONS = ("project", "guides", "hubs", "modules", "user")


def parse_page(path: Path):
    """Extract (title, tags, kind, abstract) from a doc page.

    Returns a dict with possibly-None fields; the caller reports what is missing.
    """
    lines = path.read_text(encoding="utf-8").splitlines()
    title = None
    tags = None
    kind = None
    abstract_parts = []
    in_abstract = False
    for line in lines:
        if title is None and line.startswith("# "):
            title = line[2:].strip()
        elif line.startswith("**Tags:**"):
            tags = line[len("**Tags:**"):].strip()
        elif line.startswith("**Kind:**"):
            kind = line[len("**Kind:**"):].strip()
        elif line.startswith("> **Abstract:**"):
            in_abstract = True
            abstract_parts.append(line[len("> **Abstract:**"):].strip())
        elif in_abstract:
            if line.startswith("> "):
                abstract_parts.append(line[2:].strip())
            else:
                in_abstract = False
    abstract = " ".join(part for part in abstract_parts if part).strip()
    return {"title": title, "tags": tags, "kind": kind, "abstract": abstract or None}


def first_sentence(text: str) -> str:
    """First sentence: up to the first '. ' (or the whole text), no trailing period."""
    match = re.search(r"\. ", text)
    sentence = text[: match.start()] if match else text
    return sentence.rstrip().rstrip(".")


def section_for(relpath: Path):
    """Map a docs/-relative page path to its map section, or None for main.md."""
    parts = relpath.parts
    if len(parts) == 1:
        return None
    top, rest = parts[0], parts[1:]
    if top == "project":
        return "project"
    if top == "user":
        return "user"
    if top == "developer":
        if len(rest) > 1 and rest[0] == "guides":
            return "guides"
        if rest[0].startswith("FLPQ."):
            return "hubs"
        return "modules"
    return None


def collect_pages():
    """Return (sections, errors): sections maps name -> sorted entry list."""
    sections = {name: [] for name in SECTIONS}
    errors = []
    for path in sorted(DOCS_DIR.rglob("*.md")):
        rel = path.relative_to(DOCS_DIR)
        if rel == MAIN_MD.relative_to(DOCS_DIR):
            continue
        meta = parse_page(path)
        missing = [
            field
            for field, value in (
                ("title", meta["title"]),
                ("tags", meta["tags"]),
                ("kind", meta["kind"]),
                ("abstract", meta["abstract"]),
            )
            if not value
        ]
        if missing:
            errors.append(f"{rel}: missing metadata field(s): {', '.join(missing)}")
            continue
        section = section_for(rel)
        if section is None:
            errors.append(f"{rel}: no map section for this path (top-level docs/ file)")
            continue
        sections[section].append(
            (meta["title"], str(rel).replace("\\", "/"), first_sentence(meta["abstract"]))
        )
    for name in SECTIONS:
        sections[name].sort(key=lambda entry: entry[1].lower())
    return sections, errors


def render_section(entries) -> str:
    return "\n".join(f"- [{title}]({link}) — {desc}" for title, link, desc in entries)


def apply_sections(main_text: str, sections):
    """Replace the content between each marker pair with the generated list."""
    out = main_text
    for name in SECTIONS:
        open_marker = f"<!-- doc-map:{name} -->"
        close_marker = f"<!-- /doc-map:{name} -->"
        start = out.find(open_marker)
        end = out.find(close_marker)
        if start == -1 or end == -1 or end < start:
            raise SystemExit(f"error: missing marker pair for section '{name}' in {MAIN_MD}")
        # Blank lines inside the markers are part of the mdformat-clean form.
        block = (
            open_marker
            + "\n\n"
            + render_section(sections[name])
            + "\n\n"
            + close_marker
        )
        out = out[:start] + block + out[end + len(close_marker):]
    return out


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument(
        "--check",
        action="store_true",
        help="verify the map is up to date instead of writing it (CI mode)",
    )
    args = parser.parse_args()

    if not DOCS_DIR.is_dir():
        print(f"error: {DOCS_DIR}/ not found — run from the repository root", file=sys.stderr)
        return 2

    sections, errors = collect_pages()
    if errors:
        for error in errors:
            print(f"metadata error: {error}", file=sys.stderr)
        return 1

    current = MAIN_MD.read_text(encoding="utf-8")
    expected = apply_sections(current, sections)

    if args.check:
        if expected != current:
            print(f"docs map is stale — run `python3 tools/generate_doc_map.py` ({MAIN_MD})", file=sys.stderr)
            for line in difflib.unified_diff(
                current.splitlines(),
                expected.splitlines(),
                fromfile=f"{MAIN_MD} (current)",
                tofile=f"{MAIN_MD} (generated)",
                lineterm="",
            ):
                print(line, file=sys.stderr)
            return 1
        print(f"docs map up to date ({MAIN_MD})")
        return 0

    MAIN_MD.write_text(expected, encoding="utf-8")
    total = sum(len(entries) for entries in sections.values())
    print(f"wrote {MAIN_MD}: {total} pages across {len(SECTIONS)} sections")
    return 0


if __name__ == "__main__":
    sys.exit(main())
