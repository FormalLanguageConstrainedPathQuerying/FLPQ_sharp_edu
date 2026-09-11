# Task 268: Improve task log formatting and splitting

## Task description (verbatim)

268. Improve task log formatting and splitting. `tasks/tasks.md` must contain at most 100 tasks; older tasks are archived in `tasks/tasks<N>.md` files (tasks1.md = 1-100, tasks2.md = 101-200, ...); task numbering is continuous, no reset.
     1. Create helper `tools/split_tasks.py`. On every run it must: (a) parse entry heads in `tasks/tasks.md` (numbered lines, strictly increasing; lines inside fenced code blocks skipped); (b) repair head-line indentation — dedent heads not at column 0 to column 0, leading whitespace only, entry text never touched; (c) normalize with mdformat; (d) verify structure preservation and abort on violation — repair stage: changed lines differ only in leading whitespace and are exactly the repaired heads; format stage: markdown-it-py AST deep equality modulo style (token types/sequence, block text, ordered-list start numbers, fence bytes compared; bullet markers and emphasis delimiters excluded) — no renumbering, no list/block flattening, no new nesting or sections, no missing data; marker/emphasis/whitespace normalization allowed; split stage: moved blocks byte-identical to the formatted source; (e) if more than 100 entries, move the oldest 100 to the next `tasks/tasks<N>.md` with a one-line provenance header and update the archive-reference line in `tasks.md`. Output to `tmp/split-tasks.txt`, exit code authoritative, `--dry-run` supported.
     2. Run it on the current files: repair + format `tasks/tasks.md` and `tasks/tasks1.md` in place; create `tasks/tasks2.md` (101-200); leave 201+ in `tasks/tasks.md`. Verify all invariants and idempotency (second run makes no changes).
     3. Extend instructions and skills so task log files stay well-formatted continuously with mdformat — no tricks or workarounds: `tools/quality_check.py` format step gains `mdformat --check tasks/tasks*.md`; AGENTS.md working loop runs the script after every task-log change (add entry, `[done]`, USER GUIDANCE) and commits the result on dev; pre-merge and `[done]` existence checks search all `tasks/tasks*.md`; git-workflow never-checkout rule extended to all `tasks/tasks*.md`; planning/subtask-loop/user-guidance-transfer reference "the task-log file containing the entry"; document the tool in `tools/README.md` and `docs/developer/guides/tools.md`.

## User guidance (2026-09-11)

- mdformat scope is extended beyond `tasks/tasks*.md`: after this one-time
  normalization, **all** tracked `.md` files must stay mdformat-clean
  continuously — "we must just use mdformat to continuously provide consistent
  well-formatted md files without any tricks, workarounds".
- Allowed normalizations: spaces/newlines, text style (bold/italic markers),
  item markers (`*`/`-`). Forbidden: renumbering, data loss, list/block
  flattening, new nesting, new sections.

## Context and analysis

### File scheme

- `tasks/tasks.md` — active log, at most 100 entries.
- `tasks/tasks<N>.md` — archive covering `(N-1)*100+1 .. N*100`.
- Numbering is continuous across files, no reset. Current state:
  `tasks.md` = 168 entries (101-268), `tasks1.md` = 96 entries (1-100,
  numbers 52-55 were never used).

### Empirical parsing rules (markdown-it-py, verified on minimal cases)

mdformat parses with markdown-it-py, so these rules govern both.

1. Entry heads at column 0 (or \<=3 leading spaces) inside an open list are
   always proper list items, with or without blank lines between them.
2. A list marker line indented **< the parent entry's content column**
   `C(N) = len(str(N)) + 1` and \<=3 spaces is treated as a **top-level**
   marker: it becomes a *sibling* item of the outer list (absorbed sub-list).
3. A list marker line indented **>= C(N)** nests inside the entry item.
4. A paragraph line after a blank line, indented < C(N), **escapes the list
   item** and becomes a top-level paragraph — this *breaks* the ordered list.
5. An ordered list can interrupt a top-level paragraph **only if its start
   number is 1**. After an escaped paragraph, subsequent entries (start != 1)
   are swallowed as lazy continuation lines of that paragraph.
6. A column-0 fenced code block between entries breaks the ordered list.
7. The AST stores only the list `start` attribute + item count — **per-item
   numbers and gaps are lost**. Any compliant renderer renumbers a list with
   a gap (e.g. 1..51, 56..100 renders as 1..96).

### Corruption found in current files

- `tasks.md`: entries 183-215 are swallowed into one giant paragraph of entry
  182 (its "Detailed analysis" sub-block is indented 3 < C(182)=5 and contains
  blank lines -> rule 4, then rule 5). Entry 268 head has 1 leading space.
- `tasks1.md`: entry 9's sub-list indented 2 < C(9)=3 -> absorbed as siblings
  (rule 2); entries 76/78 have column-0 fences -> list breaks (rule 6); the
  line after entry 78's fence is a column-0 paragraph -> entries 79-100
  swallowed (rules 4+5); gap 51->56 would renumber under any renderer (rule 7).

### mdformat behavior (v1.0.0, verified)

- Default ordered-list rendering **destroys numbering**: `101./102./103.` ->
  `101./001./001.`; `1./2./3.` -> `1./1./1.`.
- `--number` flag: consecutive numbering but **zero-padded to the last
  number's width**: `9./10./11.` -> `09./10./11.` — also destructive.
- Therefore a custom mdformat plugin is required: consecutive numbering from
  `start`, no padding. Previous session started `tools/mdformat_tasklog/`
  (entry-point API was wrong, `indent_width` used first number's width — both
  fixed in S1). With the plugin: all verified cases preserve numbers and are
  idempotent (second run = no change).
- YAML frontmatter (all 12 SKILL.md files) requires the `mdformat-frontmatter`
  plugin; tables nested inside list items render correctly.

### Reuse decisions (reusing skill checklist)

- Reuse `tools/common.py`: `run_cmd`, `ensure_output_dir`,
  `remove_output_file`, `write_output_file` in `split_tasks.py`.
- Reuse (fix, do not rewrite) the previous session's `tools/mdformat_tasklog/`
  package.
- Follow existing step pattern of `quality_check.py` / `hard_gate.py` for the
  new mdformat step; follow `tools/README.md` output conventions.
- Canonical tool docs live in `docs/developer/guides/tools.md`;
  `tools/README.md` keeps the table + conventions only (no duplication).

## Subtasks

### S1: mdformat plugin package `tools/mdformat_tasklog` — preserve ordered-list numbering — [done]

**Code:** `tools/mdformat_tasklog/mdformat_tasklog/__init__.py` — fix the
previous session's skeleton: (1) add the required `update_mdit(mdit)` static
method (mdformat loads entry points and calls it; without it mdformat crashes
with AttributeError); (2) compute `indent_width` from the **last** number's
width (`starting_number + len(node.children) - 1`), exactly like upstream's
consecutive branch, so continuation lines stay inside items when digit count
grows (e.g. list 1..51). Keep `RENDERERS = {"ordered_list": ordered_list}`,
`CHANGES_AST = False`, `pyproject.toml` entry point
`mdformat.parser_extension`. Install: `pip install -e tools/mdformat_tasklog`.
**Tests:** no Python test infra in repo — verify by running mdformat on
fixtures and recording results: (a) `101./102./103.` preserved; (b)
`9./10./11.` preserved without zero-padding; (c) nested list under a
multi-digit entry renders canonically; (d) gapless 1..12 with multi-line items
is stable across two runs (idempotency diff empty).
**Docs:** `tools/README.md` — add the plugin to the setup notes (install line).

**Spec:**

- The renderer mirrors mdformat 1.0.0's `consecutive_numbering` branch minus
  zero-padding: marker = `str(start + index) + "."`, no `rjust`.
- A list with a gap in its numbers is unrecoverable from the AST (rule 7);
  the plugin renders it consecutively — preventing gaps is the script's job
  (S2), not the plugin's.

### S2: `tools/split_tasks.py` — repair + format + verify + split pipeline — [done]

**Code:** New `tools/split_tasks.py` (reuses `tools/common.py`). Processes
`tasks/tasks.md` and every `tasks/tasks<N>.md`. Pipeline per file:

1. **Parse**: markdown-it-py (with `table`) -> fenced-code line ranges; entry
   heads = lines matching `^\s*(\d+)\.\s` outside fences whose number is
   strictly greater than the previous head's.
2. **Repair** (leading-whitespace-only changes, entry text never touched):
   - Heads not at column 0 -> dedent to column 0.
   - Each non-head line with indent < C(parent head) belongs to a sub-block
     that escapes/absorbs (rules 2, 4); shift the whole block right by
     `C - indent` so every line sits >= C. Fenced blocks are atomic units
     (opening marker + content + closing marker shifted together).
   - Gap handling (extension of the committed spec, required by rule 7): if a
     top-level ordered list would contain non-consecutive entry numbers,
     insert a thematic break (`---` surrounded by blank lines) between the two
     entries so each consecutive run is its own list. Without this, any
     renderer renumbers across the gap (56 -> 52 in tasks1.md).
3. **Format**: `mdformat.text(text, extensions=tuple(PARSER_EXTENSIONS.keys()))`
   — the API does not auto-load plugins (only the CLI does), so all installed
   parser extensions (tasklog, frontmatter) are passed explicitly.
4. **Verify** — abort (exit 1) on any violation:
   - Repair stage: every changed line differs from the original only in
     leading whitespace; changed lines are exactly heads + shifted block
     lines; added lines are exactly gap separators.
   - Format stage: structural AST equality between repaired and formatted —
     flat token sequence of `(type, content, ordered-list start)` compared
     with whitespace collapsed in inline content (paragraph re-wrapping is
     allowed); fence `content` compared byte-exact; the entry-number sequence
     (regex on both texts) is identical.
   - Structure invariants: every head is a direct `list_item` of some
     top-level ordered list; each top-level ordered list's direct item numbers
     are consecutive from its `start`; no head occurs inside a paragraph.
   - Split stage (when splitting): moved blocks byte-identical to the
     formatted source lines; target file = provenance header + moved lines;
     number sets partition correctly; chunk alignment — `tasks<N>.md` contains
     exactly `(N-1)*100+1 .. N*100`.
5. **Split**: if `tasks.md` has > 100 entries, move the oldest 100 to
   `tasks/tasks<N>.md` where N = (max existing archive number) + 1, with a
   one-line provenance header (`- Archived from tasks.md by tools/split_tasks.py:  entries <first>-<last>.`); regenerate the archive-reference line in
   `tasks.md` — the bullet line matching `^[-*+] .*tasks\d+\.md` (currently
   `* First part of tasks located in tasks1.md`) becomes
   `- Archived: 1-100 in tasks1.md, 101-200 in tasks2.md.` (inserted after the
   header bullets if absent). Bullets are written as `-` because mdformat
   normalizes `*`/`+` to `-`.

Output per `tools/README.md` conventions: `tmp/split-tasks.txt` (header
`PASS`/`BLOCKED`, `--- DETAILED LOG ---`, body), nothing to console, exit code
authoritative (0 = ok incl. "no split needed", 1 = blocked). `--dry-run`:
full pipeline, no file writes.

**Tests:** fixture runs in /tmp covering each corruption mode (indented head;
escaped sub-block with blank lines; column-0 fence between entries; gap), plus
idempotency (second run reports no changes). The script's own verify stage is
the primary test; fixture results recorded in the commit message.
**Docs:** none yet (tool docs land in S5).

**Spec:**

- The script never rewrites entry text: repair touches leading whitespace only;
  format and split are verified against it (format-stage AST equality,
  split-stage byte identity).
- Idempotency is a first-class invariant: on an already-clean file the script
  makes zero changes.

### S3: Run the script on current task files — split tasks.md, create tasks2.md — [done]

**Code:** script run + resulting data-file changes. Two script fixes were
required by the real files (found only outside fixtures):

- `python3 tools/split_tasks.py` puts `tools/` on `sys.path[0]`, where the
  `mdformat_tasklog` project directory shadows the installed package as a
  namespace package and breaks mdformat entry-point loading; the script now
  removes its own directory from `sys.path` after importing `common`.
- mdformat canonically rewrites indented code blocks as fenced ones (content
  passes through byte-identical); `verify_format` now accepts exactly that
  rewrite (`_norm_fp` maps `code_block`/empty-info `fence` to one key).
  **Tests:** script verify stage PASS; idempotency — second run makes no
  changes; grep checks: `tasks.md` = 68 heads (201-268), `tasks2.md` = 100 heads
  (101-200), `tasks1.md` = 96 heads (1-100 minus 52-55); numbering continuous
  across files; `mdformat --check tasks/tasks*.md` passes.
  **Docs:** none.

**Spec:**

- `tasks/tasks.md`: repaired + formatted in place, entries 101-200 moved out,
  archive-reference line updated.
- `tasks/tasks1.md`: repaired + formatted in place; `---` inserted between
  entries 51 and 56 (the historical gap); provenance header line preserved.
- `tasks/tasks2.md`: created — provenance header + entries 101-200
  byte-identical to the formatted source blocks.

### S4: One-time mdformat normalization of all tracked .md files — [done]

**Code:** mdformat run over `git ls-files '*.md'` (76 files). Three things
were required beyond the plain run (found only on the real corpus):

- **GFM tables**: mdformat parses with markdown-it's default preset, which
  has no table support — a table row is parsed as a paragraph and `\|` cell
  escapes are unescaped, silently destroying tables. The tasklog plugin now
  enables the `table` rule in `update_mdit` and provides the renderers
  mdformat ships none for (`table`/`thead`/`tbody`/`tr`/`th`/`td`, separator
  row synthesized from header alignment). Literal pipes are re-escaped in
  cell text and inside code spans (the parser's line-level cell splitter
  consumes `\|` before inline parsing, so a code span containing a pipe only
  survives re-parsing with the escape kept; rendered content is unchanged).
- **Frontmatter**: 9 SKILL.md files had invalid YAML (`description:` values
  with unquoted colons); mdformat-frontmatter warned and passed them through.
  Values are now double-quoted (parsed value identical, warnings gone).
- **Fingerprint hardening**: `normalize_inline` now compares code-span
  content byte-exact (previously invisible — the table splitter's backslash
  consumption could have slipped through undetected).

**Tests:** per-file structural AST equality before/after (token sequence
`(type, content, start)`, whitespace collapsed in inline content, fence bytes
exact) — one-off check, not committed; afterwards `mdformat --check` on all
files passes with zero warnings; second run makes zero changes.
**Docs:** none.

**Spec:**

- All tracked `.md` files (docs/, AGENTS.md, README.md, skills, tools/) become
  mdformat-clean so the continuous gate (S5) starts green.
- Only formatting changes: no word added/removed/reordered (AST equality),
  no list/block structure change, no new sections.

### S5: Continuous enforcement — quality gates + CI + tool docs — [done]

**Code:**

- `tools/quality_check.py`: new Step 1 "Markdown format" —
  `mdformat --check` over all tracked `.md` files (from `git ls-files`);
  renumber existing steps; BLOCKED on failure.
- `tools/hard_gate.py`: same step added to the gate sequence (before dotnet
  format), progress counter updated.
- `.github/workflows/ci.yml`: new step — setup-python,
  `pip install mdformat mdformat-frontmatter`,
  `pip install -e tools/mdformat_tasklog`, then `mdformat --check` over all
  tracked `.md` files.
  **Tests:** run `tools/quality_check.py` -> PASS (files are clean after S4);
  CI YAML sanity check (no local CI runner).
  **Docs:** `tools/README.md` — add `split_tasks.py` row + mdformat toolchain
  setup section; `docs/developer/guides/tools.md` — new `split_tasks.py` section
  (pipeline, invariants, output format) and mdformat toolchain section (plugins,
  install, why the tasklog plugin exists).

**Spec:**

- The gate checks **all** tracked `.md` files (user guidance: continuous
  well-formatted md, no workarounds) — a superset of the committed spec's
  `tasks/tasks*.md`.
- Any agent modifying an `.md` file must run mdformat on it before committing
  (instruction lands in S6).

### S6: Instructions and skills updates — [done]

**Code:** none (docs-only).
**Tests:** skip (docs-only).
**Docs:**

- `AGENTS.md`: Project Structure table — tasks/ row describes the
  `tasks.md` + `tasks<N>.md` scheme; working loop step 2a — after any
  task-log change (add entry, `[done]`, USER GUIDANCE) run
  `python3 tools/split_tasks.py` and commit the result on dev; Git Safety —
  never-checkout/restore rule extended to all `tasks/tasks*.md`, with the
  exception that a task whose deliverable is the task log itself (like 268)
  commits restructured files on the feature branch; new rule — all `.md`
  files must be mdformat-clean, run mdformat on any `.md` you modify.
- `.opencode/skills/git-workflow/SKILL.md`: pre-merge existence check greps
  `tasks/tasks*.md`; never-checkout rule extended to all `tasks/tasks*.md` +
  the restructuring exception; pre-commit tasks.md check annotated with the
  exception.
- `.opencode/skills/planning/SKILL.md`: verbatim section quotes the entry from
  "the task-log file containing it" (`tasks/tasks.md` or `tasks/tasks<N>.md`).
- `.opencode/skills/subtask-loop/SKILL.md`: re-read spec / mark `[done]` in
  "the task-log file containing the entry"; pre-commit check annotated.
- `.opencode/skills/user-guidance-transfer/SKILL.md`: append guidance to "the
  task-log file containing the entry".
- `.opencode/skills/quality-gates/SKILL.md`: document the new mdformat step in
  quality_check and hard_gate sequences.
- `.opencode/skills/documentation/SKILL.md`: after editing any `.md` file, run
  mdformat on it (gate enforces).

**Spec:**

- Every "does entry NNN exist" check searches all `tasks/tasks*.md` — archived
  tasks must not look lost.
- One source of truth: the file scheme is described in AGENTS.md; skills
  reference it, do not restate it.

### Code Review (2026-09-11) — [done]

**Code:** `tools/split_tasks.py` — two findings from the post-subtask review:

- Escaped indented code blocks were shifted by `C(N) - indent`, landing the
  first line exactly at column `C(N)` — where a line after a blank line is
  parsed as paragraph text. An escaped code block under an entry with N >= 100
  was thus silently converted to a paragraph (code formatting lost) while all
  verifications passed. Indented code blocks are now atomic units like fences,
  and an escaped one shifts by exactly `C(N)`: in-item code strips `C(N)+4`
  while top-level code strips 4, so the block stays a code block with
  byte-identical content (the format stage then fences it).
- `chr(10).join` inside an f-string expression in `process_file` (a
  pre-3.12-backslash workaround) — replaced with a local binding.

**Tests:** seven synthetic scenarios through the full pipeline (escaped code
single/multi-line/mixed-indent, in-item code untouched, escaped fence, flat
paragraph with blank lines, escaped nested list); a 105-entry synthetic log
exercises repair + gap separator + split end-to-end; the misaligned-chunk
guard correctly BLOCKs a gapped log; real files: dry-run PASS with zero
repairs, full run byte-identical (idempotent).
**Docs:** `tasks/code_review.md` — Task 268 section.

**Spec:**

- The S2 repair description above is superseded for indented code blocks:
  atomic shift by exactly `C(N)`, not `C(N) - indent`.
