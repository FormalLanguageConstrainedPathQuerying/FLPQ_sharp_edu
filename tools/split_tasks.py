#!/usr/bin/env python3
"""Task log formatter and splitter.

Keeps `tasks/tasks.md` (active log, at most 100 entries) and the archive
files `tasks/tasks<N>.md` well-formed Markdown:

1. Parse entry heads — numbered lines (`N. `), strictly increasing; lines
   inside fenced or indented code blocks are skipped.
2. Repair indentation (leading whitespace only, entry text never touched):
   - dedent heads not at column 0 to column 0;
    - shift sub-blocks that escape their entry item back inside it. A line
      escapes when, after a blank line, its indent is below the entry's
      content column `C(N) = len(str(N)) + 2` (digits + dot + space); such a
      line becomes a top-level paragraph or indented code block and breaks
      the ordered list. Fenced blocks and indented code blocks move as
      atomic units; an escaped indented code block shifts by exactly `C(N)`
      (not `C(N) - indent`) so it stays a code block inside the item with
      byte-identical content — a line at column `C(N)` after a blank line is
      paragraph text, and in-item code strips `C(N)+4` while top-level code
      strips 4;
   - split ordered lists at number gaps with a thematic break (`---`),
     because the AST stores only a list's start number and any renderer
     would renumber across a gap.
3. Normalize with mdformat (all installed parser extensions: tasklog,
   frontmatter).
4. Verify structure preservation; abort on any violation:
   - repair stage: changed lines differ only in leading whitespace; added
     lines are exactly the gap separators;
    - format stage: markdown-it-py AST deep equality modulo style (token
      types/sequence, block text with whitespace collapsed, ordered-list
      start numbers, fence bytes compared byte-exact). One canonical rewrite
      is accepted: mdformat converts indented code blocks to fenced ones; the
      code content must pass through byte-identical.
   - structure: every head is a direct item of a top-level ordered list and
     each such list's numbers are consecutive from its start.
5. If `tasks.md` has more than 100 entries, move the oldest 100 to the next
   `tasks/tasks<N>.md` (provenance header) and update the archive-reference
   line in `tasks.md`.

Output: tmp/split-tasks.txt (nothing to console). The exit code is the
authoritative signal: 0 = PASS (including "no changes needed"),
1 = BLOCKED. `--dry-run` performs the full pipeline without writing files.
"""

import os
import re
import sys
from pathlib import Path

# `common` is a sibling module in this directory; it must be imported before
# this directory is removed from sys.path (see below).
from common import ensure_output_dir, remove_output_file, write_output_file

# When run as `python3 tools/split_tasks.py`, this script's directory is
# sys.path[0]. It contains the mdformat_tasklog project directory, which would
# shadow the installed mdformat_tasklog package as a namespace package and
# break mdformat's entry-point loading. Remove it before importing mdformat.
_HERE = os.path.dirname(os.path.abspath(__file__))
sys.path[:] = [p for p in sys.path if os.path.abspath(p or ".") != _HERE]

import mdformat  # noqa: E402
from markdown_it import MarkdownIt  # noqa: E402
from mdformat.plugins import PARSER_EXTENSIONS  # noqa: E402

OUTPUT_FILE = "tmp/split-tasks.txt"
TASKS_DIR = Path("tasks")
MAX_ENTRIES = 100
ACTIVE_LOG = TASKS_DIR / "tasks.md"

MD = MarkdownIt().enable("table")
HEAD_RE = re.compile(r"^(\s*)(\d+)\.\s")
ARCHIVE_REF_RE = re.compile(r"^[-*+] .*tasks\d+\.md")
ARCHIVE_NAME_RE = re.compile(r"^tasks(\d+)\.md$")


def is_blank(line: str) -> bool:
    return line.strip() == ""


def indent_of(line: str) -> int:
    return len(line) - len(line.lstrip(" "))


def code_ranges(lines: list[str]) -> list[tuple[int, int]]:
    """Inclusive (start, end) line ranges of fenced and indented code blocks."""
    tokens = MD.parse("\n".join(lines))
    ranges = []
    for t in tokens:
        if t.type in ("fence", "code_block") and t.map:
            ranges.append((t.map[0], t.map[1] - 1))
    return sorted(ranges)


def code_line_set(ranges: list[tuple[int, int]]) -> set[int]:
    s = set()
    for start, end in ranges:
        s.update(range(start, end + 1))
    return s


def find_heads(lines: list[str], ranges: list[tuple[int, int]]) -> list[tuple[int, int]]:
    """Entry heads: (line index, number), strictly increasing, outside code."""
    protected = code_line_set(ranges)
    heads = []
    last = 0
    for i, line in enumerate(lines):
        if i in protected:
            continue
        m = HEAD_RE.match(line)
        if m and int(m.group(2)) > last:
            heads.append((i, int(m.group(2))))
            last = int(m.group(2))
    return heads


def hr_lines(lines: list[str]) -> set[int]:
    """Line indices of top-level thematic breaks (list separators).

    An ``hr`` nested inside a list item is entry content, not a separator.
    """
    tokens = MD.parse("\n".join(lines))
    found = set()
    depth = 0
    for t in tokens:
        if t.type == "hr" and t.map and depth == 0:
            found.add(t.map[0])
        elif t.type in ("ordered_list_open", "bullet_list_open"):
            depth += 1
        elif t.type in ("ordered_list_close", "bullet_list_close"):
            depth -= 1
    return found


def repair(lines: list[str]) -> tuple[list[str], dict[int, bool], list[tuple[int, int, int]]]:
    """Dedent heads to column 0 and shift escaped sub-blocks inside their item.

    Returns (repaired_lines, head_dedents, block_shifts) where block_shifts
    are (start, end_inclusive, delta) ranges in original coordinates.
    """
    ranges = code_ranges(lines)
    fence_open = {s: e for s, e in ranges if _is_fence_start(lines, s)}
    code_open = {s: e for s, e in ranges if not _is_fence_start(lines, s)}
    protected = code_line_set(ranges)
    heads = find_heads(lines, ranges)
    hrs = hr_lines(lines)

    rep = list(lines)
    head_dedents: dict[int, bool] = {}
    block_shifts: list[tuple[int, int, int]] = []

    for idx, _num in heads:
        if lines[idx] != lines[idx].lstrip():
            rep[idx] = lines[idx].lstrip()
            head_dedents[idx] = True

    for k, (hidx, hnum) in enumerate(heads):
        content_col = len(str(hnum)) + 2
        end = heads[k + 1][0] if k + 1 < len(heads) else len(lines)
        # A top-level thematic break is a list separator: it ends the region
        # and is never shifted (shifting it would nest it inside the item).
        for hr in hrs:
            if hidx < hr < end:
                end = hr
                break
        i = hidx + 1
        while i < end:
            line = lines[i]
            if is_blank(line):
                i += 1
                continue
            ind = indent_of(line)
            if i in protected:
                if i in fence_open:
                    # Fenced blocks are atomic units.
                    fend = fence_open[i]
                    if ind < content_col:
                        delta = content_col - indent_of(lines[i])
                        for m in range(i, fend + 1):
                            if not is_blank(lines[m]):
                                rep[m] = " " * (indent_of(lines[m]) + delta) + lines[m].lstrip()
                        block_shifts.append((i, fend, delta))
                    i = fend + 1
                    continue
                if i in code_open:
                    # Indented code blocks are atomic units. An escaped one
                    # (first line below C(N), hence top-level in the original
                    # parse) shifts by exactly C(N): a line at column C(N)
                    # after a blank line is paragraph text, and in-item code
                    # strips C(N)+4 while top-level code strips 4, so this
                    # keeps it a code block with byte-identical content.
                    cend = code_open[i]
                    if ind < content_col:
                        for m in range(i, cend + 1):
                            if not is_blank(lines[m]):
                                rep[m] = " " * (indent_of(lines[m]) + content_col) + lines[m].lstrip()
                        block_shifts.append((i, cend, content_col))
                    i = cend + 1
                    continue
            if ind >= content_col:
                i += 1
                continue
            # Escaped sub-block rooted here: extends over following lines with
            # greater indent (blank lines included); fences are atomic.
            delta = content_col - ind
            j = i + 1
            while j < end:
                if is_blank(lines[j]):
                    j += 1
                    continue
                if indent_of(lines[j]) <= ind:
                    break
                if j in fence_open:
                    j = fence_open[j] + 1
                    continue
                j += 1
            for m in range(i, j):
                if not is_blank(lines[m]):
                    rep[m] = " " * (indent_of(lines[m]) + delta) + lines[m].lstrip()
            block_shifts.append((i, j - 1, delta))
            i = j

    return rep, head_dedents, block_shifts


def _is_fence_start(lines: list[str], idx: int) -> bool:
    return lines[idx].lstrip().startswith("```") or lines[idx].lstrip().startswith("~~~")


def apply_ops(
    orig: list[str],
    head_dedents: dict[int, bool],
    block_shifts: list[tuple[int, int, int]],
    inserts: list[tuple[int, list[str]]],
) -> list[str]:
    """Reconstruct the repaired text from original lines plus the ops."""
    mid = list(orig)
    for i in head_dedents:
        mid[i] = orig[i].lstrip()
    for s, e, delta in block_shifts:
        for m in range(s, e + 1):
            if not is_blank(mid[m]):
                mid[m] = " " * (indent_of(mid[m]) + delta) + mid[m].lstrip()
    out = list(mid)
    offset = 0
    for head_idx, sep_lines in inserts:
        pos = head_idx + offset
        out[pos:pos] = sep_lines
        offset += len(sep_lines)
    return out


def top_level_ols(text: str) -> list[tuple[int, int, list[int]]]:
    """Top-level ordered lists: (start_attr, first_item_line, item_numbers)."""
    lines = text.split("\n")
    tokens = MD.parse(text)
    ols = []
    depth = 0
    i = 0
    while i < len(tokens):
        t = tokens[i]
        if t.type in ("ordered_list_open", "bullet_list_open"):
            if t.type == "ordered_list_open" and depth == 0:
                start = (t.attrs or {}).get("start") or 1
                d = 1
                j = i + 1
                item_lines = []
                while j < len(tokens):
                    tj = tokens[j].type
                    if tj in ("ordered_list_open", "bullet_list_open"):
                        d += 1
                    elif tj in ("ordered_list_close", "bullet_list_close"):
                        d -= 1
                        if d == 0:
                            break
                    elif tj == "list_item_open" and d == 1:
                        item_map = tokens[j].map
                        if item_map:
                            item_lines.append(item_map[0])
                    j += 1
                nums = []
                for ln in item_lines:
                    m = HEAD_RE.match(lines[ln])
                    nums.append(int(m.group(2)) if m else -1)
                ols.append((start, item_lines[0] if item_lines else -1, nums))
            depth += 1
        elif t.type in ("ordered_list_close", "bullet_list_close"):
            depth -= 1
        i += 1
    return ols


def insert_gap_separators(lines: list[str]) -> tuple[list[str], list[tuple[int, list[str]]]]:
    """Insert `['', '---']` before each head that continues a broken number run.

    Returns (new_lines, inserts) where inserts are (before_orig_head_index, lines).
    """
    ranges = code_ranges(lines)
    heads = find_heads(lines, ranges)
    ols = top_level_ols("\n".join(lines))
    has_gap = any(
        b != a + 1
        for _s, _f, nums in ols
        if -1 not in nums
        for a, b in zip(nums, nums[1:])
    )
    if not has_gap:
        return lines, []
    inserts: list[tuple[int, list[str]]] = []
    prev = None
    for idx, num in heads:
        if prev is not None and num != prev + 1:
            inserts.append((idx, ["", "---"]))
        prev = num
    return apply_ops(lines, {}, [], inserts), inserts


def normalize_inline(token) -> tuple:
    """Inline structure with markup delimiters dropped and whitespace collapsed."""
    parts = []
    buf = ""

    def flush():
        nonlocal buf
        if buf:
            parts.append(("text", re.sub(r"\s+", " ", buf).strip()))
            buf = ""

    for c in token.children or []:
        if c.type == "text":
            buf += c.content
        elif c.type == "softbreak":
            buf += " "
        elif c.type == "code_inline":
            # Code span content is literal data; compare it byte-exact so a
            # formatter cannot silently alter it (e.g. drop an escape that
            # the table cell splitter consumed).
            flush()
            parts.append(("code_inline", c.content))
        else:
            flush()
            parts.append((c.type, (c.attrs or {}).get("href") if c.attrs else None))
    flush()
    return tuple(parts)


def fingerprint(text: str) -> list[tuple]:
    """Structural token fingerprint: types/sequence, block text, list starts."""
    fp = []
    for t in MD.parse(text):
        if t.type == "inline":
            fp.append(("inline", normalize_inline(t)))
        elif t.type == "fence":
            fp.append(("fence", t.content, (t.attrs or {}).get("info", "")))
        elif t.type == "code_block":
            fp.append(("code_block", t.content))
        elif t.type == "html_block":
            fp.append(("html_block", t.content))
        elif t.type == "ordered_list_open":
            fp.append(("ordered_list_open", (t.attrs or {}).get("start")))
        else:
            fp.append((t.type,))
    return fp


def format_text(text: str) -> str:
    return mdformat.text(text, extensions=tuple(PARSER_EXTENSIONS.keys()))


def verify_repair(
    orig: list[str],
    rep: list[str],
    head_dedents: dict[int, bool],
    block_shifts: list[tuple[int, int, int]],
    inserts: list[tuple[int, list[str]]],
) -> list[str]:
    """Reconstruction must match; every change is leading-whitespace-only."""
    violations = []
    expected = apply_ops(orig, head_dedents, block_shifts, inserts)
    if expected != rep:
        for i, (e, r) in enumerate(zip(expected, rep)):
            if e != r:
                violations.append(f"line {i + 1}: repair output differs from op reconstruction:\n  {e!r}\n  {r!r}")
                break
        if len(expected) != len(rep):
            violations.append(f"repair changed line count: {len(orig)} -> {len(rep)}")
        return violations
    shifted = {}
    for s, e, delta in block_shifts:
        for m in range(s, e + 1):
            shifted[m] = delta
    insert_map = {h: sep for h, sep in inserts}
    j = 0
    for i, o in enumerate(orig):
        for sl in insert_map.get(i, []):
            if j >= len(rep) or rep[j] != sl:
                violations.append(f"gap separator before line {i + 1} missing or wrong: expected {sl!r}")
            j += 1
        if j >= len(rep):
            break
        r = rep[j]
        if o != r and o.lstrip() != r.lstrip():
            violations.append(f"line {i + 1}: not a leading-whitespace-only change:\n  {o!r}\n  {r!r}")
        elif o != r and i not in head_dedents and i not in shifted:
            violations.append(f"line {i + 1}: changed but not a repaired head or shifted block line")
        j += 1
    if j != len(rep):
        violations.append(f"trailing lines in repaired output: {rep[j:j + 3]!r}")
    for head_idx, sep_lines in inserts:
        if any(l not in ("", "---") for l in sep_lines):
            violations.append(f"gap insert before line {head_idx + 1} is not ['', '---']: {sep_lines!r}")
    return violations


def _norm_fp(fp: list[tuple]) -> list[tuple]:
    """mdformat rewrites indented code blocks as fenced ones, preserving the
    content byte-for-byte. Map both forms to a common key so that this
    canonical rewrite is accepted (any other change still fails)."""
    out = []
    for item in fp:
        if item[0] == "code_block":
            out.append(("code", item[1]))
        elif item[0] == "fence" and item[2] == "":
            out.append(("code", item[1]))
        else:
            out.append(item)
    return out


def verify_format(rep_text: str, fmt_text: str) -> list[str]:
    """AST deep equality modulo style; entry numbers preserved."""
    violations = []
    fa, fb = _norm_fp(fingerprint(rep_text)), _norm_fp(fingerprint(fmt_text))
    if fa != fb:
        for k, (x, y) in enumerate(zip(fa, fb)):
            if x != y:
                violations.append(f"format changed AST at token {k}:\n  {x!r}\n  {y!r}")
                break
        if len(fa) != len(fb):
            violations.append(f"format changed token count: {len(fa)} -> {len(fb)}")
    heads_a = [n for _i, n in find_heads(rep_text.split("\n"), code_ranges(rep_text.split("\n")))]
    heads_b = [n for _i, n in find_heads(fmt_text.split("\n"), code_ranges(fmt_text.split("\n")))]
    if heads_a != heads_b:
        violations.append(f"format changed entry numbers: {heads_a[:5]}... -> {heads_b[:5]}...")
    return violations


def check_structure(text: str) -> list[str]:
    """Every head is a direct item of a top-level ordered list; runs consecutive."""
    lines = text.split("\n")
    heads = find_heads(lines, code_ranges(lines))
    head_nums = {n for _i, n in heads}
    violations = []
    item_nums = set()
    for start, first_line, nums in top_level_ols(text):
        if -1 in nums:
            violations.append(f"ordered list at line {first_line + 1}: item without a number marker")
            continue
        expected = list(range(start, start + len(nums)))
        if nums != expected:
            violations.append(
                f"ordered list at line {first_line + 1}: numbers {nums[:8]}{'...' if len(nums) > 8 else ''} "
                f"are not consecutive from start={start}"
            )
        item_nums.update(n for n in nums if n != -1)
    missing = head_nums - item_nums
    if missing:
        violations.append(f"entries not direct list items (swallowed): {sorted(missing)[:10]}")
    extra = item_nums - head_nums
    if extra:
        violations.append(f"list items that are not entry heads: {sorted(extra)[:10]}")
    return violations


def archive_files() -> dict[int, Path]:
    found = {}
    for p in TASKS_DIR.iterdir():
        m = ARCHIVE_NAME_RE.match(p.name)
        if m:
            found[int(m.group(1))] = p
    return found


def do_split(fmt_lines: list[str]) -> tuple[list[str], str, list[str], list[str]]:
    """Split the formatted active log.

    Returns (rest_lines, target_name, target_lines, moved_lines).
    """
    heads = find_heads(fmt_lines, code_ranges(fmt_lines))
    if len(heads) <= MAX_ENTRIES:
        raise ValueError("do_split called with at most 100 entries")
    first_head_line = heads[0][0]
    cut_line = heads[MAX_ENTRIES][0]
    moved = fmt_lines[first_head_line:cut_line]
    rest = fmt_lines[:first_head_line] + fmt_lines[cut_line:]
    first_num, last_num = heads[0][1], heads[MAX_ENTRIES - 1][1]
    archives = archive_files()
    n = (max(archives) if archives else 0) + 1
    if first_num != (n - 1) * MAX_ENTRIES + 1 or last_num != n * MAX_ENTRIES:
        raise ValueError(
            f"chunk misalignment: moving entries {first_num}-{last_num} but tasks{n}.md "
            f"must cover {(n - 1) * MAX_ENTRIES + 1}-{n * MAX_ENTRIES}"
        )
    target_lines = [
        f"- Archived from tasks.md by tools/split_tasks.py: entries {first_num}-{last_num}.",
        "",
        *moved,
    ]
    ref_parts = ", ".join(
        f"{(k - 1) * MAX_ENTRIES + 1}-{k * MAX_ENTRIES} in tasks{k}.md" for k in range(1, n + 1)
    )
    ref_line = f"- Archived: {ref_parts}."
    replaced = False
    for i, line in enumerate(rest):
        if ARCHIVE_REF_RE.match(line):
            rest[i] = ref_line
            replaced = True
            break
    if not replaced:
        first_rest_head = find_heads(rest, code_ranges(rest))[0][0]
        rest[first_rest_head:first_rest_head] = [ref_line, ""]
    return rest, f"tasks{n}.md", target_lines, moved


def verify_split(
    fmt_lines: list[str],
    rest_lines: list[str],
    target_lines: list[str],
    moved: list[str],
) -> list[str]:
    """Moved blocks byte-identical; number sets partition; both outputs well-formed."""
    violations = []
    heads = find_heads(fmt_lines, code_ranges(fmt_lines))
    first_head_line = heads[0][0]
    cut_line = heads[MAX_ENTRIES][0]
    if moved != fmt_lines[first_head_line:cut_line]:
        violations.append("moved block is not byte-identical to the formatted source")
    expected_rest = fmt_lines[:first_head_line] + fmt_lines[cut_line:]
    ref_idx = next((i for i, l in enumerate(expected_rest) if ARCHIVE_REF_RE.match(l)), None)
    if ref_idx is not None:
        diffs = [i for i, (a, b) in enumerate(zip(expected_rest, rest_lines)) if a != b]
        if len(rest_lines) != len(expected_rest) or len(diffs) != 1 or diffs[0] != ref_idx:
            violations.append(f"rest differs from source beyond the archive-reference line: {diffs[:3]}")
        elif not rest_lines[ref_idx].startswith("- Archived:"):
            violations.append(f"archive-reference line not regenerated: {rest_lines[ref_idx]!r}")
    else:
        # do_split inserts [ref_line, ""] before the first remaining head.
        head_idxs = [i for i, l in enumerate(rest_lines) if HEAD_RE.match(l)]
        h = head_idxs[0] if head_idxs else -1
        ref_line = rest_lines[h - 2] if h >= 2 else ""
        without_ref = rest_lines[: h - 2] + rest_lines[h:] if h >= 2 else []
        if (
            h < 2
            or not ref_line.startswith("- Archived:")
            or rest_lines[h - 1] != ""
            or len(rest_lines) != len(expected_rest) + 2
            or without_ref != expected_rest
        ):
            violations.append("rest differs from source beyond the inserted archive-reference line")
    fmt_nums = [n for _i, n in heads]
    rest_nums = [n for _i, n in find_heads(rest_lines, code_ranges(rest_lines))]
    target_nums = [n for _i, n in find_heads(target_lines, code_ranges(target_lines))]
    if sorted(rest_nums + target_nums) != sorted(fmt_nums):
        violations.append("entry numbers do not partition between rest and archive")
    violations += [f"rest: {v}" for v in check_structure("\n".join(rest_lines))]
    violations += [f"archive: {v}" for v in check_structure("\n".join(target_lines))]
    return violations


def process_file(path: Path, log: list[str]) -> tuple[list[str], list[str]]:
    """Run the pipeline on one task file. Returns (violations, final_text_lines)."""
    orig = path.read_text().split("\n")
    log.append(f"--- {path} ---")
    log.append(f"entries: {len(find_heads(orig, code_ranges(orig)))}")

    rep, head_dedents, block_shifts = repair(orig)
    n_dedent = len(head_dedents)
    n_shift_lines = sum(e - s + 1 for s, e, _d in block_shifts)
    log.append(
        f"repair: {n_dedent} head(s) dedented, "
        f"{len(block_shifts)} block(s) shifted ({n_shift_lines} lines)"
    )

    rep2, inserts = insert_gap_separators(rep)
    if inserts:
        log.append(f"gap separators inserted before entries: {[h + 1 for h, _l in inserts]}")

    violations = verify_repair(orig, rep2, head_dedents, block_shifts, inserts)

    fmt_text = format_text("\n".join(rep2))
    changed = fmt_text != "\n".join(rep2)
    log.append(f"format: {'changed' if changed else 'unchanged'}")

    violations += verify_format("\n".join(rep2), fmt_text)
    violations += check_structure(fmt_text)
    for v in violations:
        log.append(f"VIOLATION: {v}")
    return violations, fmt_text.split("\n")


def main() -> None:
    dry_run = "--dry-run" in sys.argv
    ensure_output_dir("tmp")
    remove_output_file(OUTPUT_FILE)

    log: list[str] = []
    all_violations: list[str] = []

    if not ACTIVE_LOG.exists():
        all_violations.append(f"{ACTIVE_LOG} not found")
    else:
        files = [ACTIVE_LOG, *(TASKS_DIR / f"tasks{n}.md" for n in sorted(archive_files()))]
        results: dict[str, list[str]] = {}
        for path in files:
            violations, fmt_lines = process_file(path, log)
            all_violations += [f"{path.name}: {v}" for v in violations]
            results[path.name] = fmt_lines

        split_info = None
        if not all_violations:
            active_lines = results[ACTIVE_LOG.name]
            heads = find_heads(active_lines, code_ranges(active_lines))
            if len(heads) > MAX_ENTRIES:
                try:
                    split_info = do_split(active_lines)
                    rest, target_name, target_lines, moved = split_info
                    log.append("--- split ---")
                    log.append(f"moving {MAX_ENTRIES} oldest entries to tasks/{target_name}")
                    all_violations += [f"split: {v}" for v in verify_split(active_lines, rest, target_lines, moved)]
                    if not any(v.startswith("split:") for v in all_violations):
                        results[ACTIVE_LOG.name] = rest
                        results[target_name] = target_lines
                except ValueError as e:
                    all_violations.append(str(e))

        if not dry_run and not all_violations:
            for name, lines in results.items():
                target = ACTIVE_LOG if name == ACTIVE_LOG.name else TASKS_DIR / name
                text = "\n".join(lines)
                if not text.endswith("\n"):
                    text += "\n"
                target.write_text(text)
                log.append(f"wrote {target}")

    status = "BLOCKED" if all_violations else "PASS"
    lines = [
        "SPLIT TASKS SUMMARY",
        "",
        f"STATUS: {status}",
        f"dry-run: {dry_run}",
        "",
        "--- DETAILED LOG ---",
        "",
        *log,
    ]
    if all_violations:
        lines.append("")
        lines.append("--- VIOLATIONS ---")
        lines.extend(all_violations)
    write_output_file(OUTPUT_FILE, lines)
    sys.exit(1 if all_violations else 0)


if __name__ == "__main__":
    main()
