# Detailed Plan: Task 296 — Fix split_tasks.py [done]-entry handling

## Task description (verbatim)

```
296. Fix tools/split_tasks.py so repair() treats `[done] N. ` paragraph-form entries as region boundaries. Root cause: HEAD_RE does not match the `[done] N. ` paragraph form of completed entries, so repair() treated such an entry following a visible list-item head as an 'escaped sub-block' of the previous entry and shifted it by +5 spaces; mdformat then converted the resulting indented code block into a fenced code block (entries 277/280-287 were corrupted this way; the log is repaired in a subsequent docs(tasks) commit on dev). Fix: add DONE_HEAD_RE (`^\s*\[done\]\s+\d+\.\s`) and make repair()'s region scan end the region at such a line without shifting it. Verify: (a) the old tool corrupts a synthetic pending-head + [done]-paragraph case (2 blocks shifted) while the fixed tool leaves it unchanged; (b) legitimate escaped sub-blocks after a visible head are still shifted by the fixed tool (regression check); (c) the repaired tasks/tasks.md is byte-stable under the fixed tool (0 shifts, format unchanged, STATUS: PASS).
```

## Context

- The task log uses two entry conventions: old `NNN. [done] desc` (ordered-list
  items, matched by `HEAD_RE`) and new `[done] NNN. desc` (top-level paragraphs,
  invisible to `HEAD_RE`). Entries 277–296 use the paragraph form.
- `repair()` scans each visible head's region for "escaped sub-blocks" (lines
  below the content column after a blank line) and shifts them into the item.
  A `[done]` paragraph following a visible head matched that condition and was
  shifted +5; mdformat then promoted the indented block to a fenced code block.
- `dev` already carries a separate, complementary `repair()` fix (origin/dev
  `b1fbea3`): the region now ends at the earliest top-level thematic break
  (`end = min(region_hrs)`) instead of trusting set iteration order. This task
  adds the `[done]`-paragraph boundary; both fixes are required and coexist in
  the same loop.
- The log corruption this bug caused (entries 277/280–287 nested inside list
  items 276/279) is repaired by a subsequent whitespace-only `docs(tasks)`
  commit on dev; this task fixes the tool so the corruption cannot recur.

## Reuse checklist results

1. **Reuse existing stuff:** the fix extends the existing `repair()` region-scan
   loop; the boundary pattern mirrors the in-loop thematic-break (`hrs`)
   handling (a separator ends the region and is never shifted). No new module
   or helper file.
2. **Reference existing docs:** `docs/developer/guides/tools.md` is the
   canonical per-tool doc — its pipeline step 2 description is updated in place
   (no new doc file). The script docstring remains the canonical algorithm text.
3. **Call existing functions:** `indent_of`, `is_blank`, `protected`,
   `fence_open`/`code_open` are all reused; the new check uses only the
   loop-local `line`.
4. **Generalize:** n/a (bug fix, not a feature).
5. **Extract shared helper:** not needed — the boundary check is 3 lines local
   to the loop.

## Subtasks

### S1: Fix repair() to treat [done] entries as region boundaries [done]

**Code:** `tools/split_tasks.py` — add `DONE_HEAD_RE = re.compile(r"^\s*\[done\]\s+\d+\.\s")`
next to `HEAD_RE`; in `repair()`'s region-scan loop, after the code-block
handling and before the escape check, break out of the loop (end the region)
when a line matches `DONE_HEAD_RE` — such a line is never shifted. Update the
module docstring's repair bullet to state the exception. The change lands on top
of dev's existing `min(region_hrs)` fix; both are kept.
**Tests:** no Python test framework exists for the tools; verification is by
running the tool on synthetic and real inputs (scenarios a/b below, executed in
S2). Before committing, run scenario (a) with both old and new tool versions to
confirm the fix changes exactly the intended behavior.
**Docs:** `docs/developer/guides/tools.md` — pipeline step 2: add that a
`[done] N. ` line (completed-entry paragraph form) is never shifted; it starts
a new top-level entry and ends the preceding head's region.

**Spec:**

- `DONE_HEAD_RE` matches `[done] N. ` with optional leading whitespace, any
  number of digits, dot, trailing space — the exact paragraph form used by
  completed entries (e.g. `[done] 295. Migrate ...`).
- The check sits after the fenced/indented code-block branches (code blocks are
  atomic and handled first) and before the `ind >= content_col` skip and the
  escaped-sub-block shift; on match it `break`s the region loop, so lines after
  the `[done]` entry belong to no visible head's region.
- Legitimate escapes (plain paragraphs after a blank line, below the content
  column) must still be shifted — the new branch only intercepts `[done] N. `
  lines.
- Docstring and tools.md wording must match the implemented behavior exactly
  (single source of truth: the docstring; tools.md summarizes).

### S2: Verification evidence [done]

**Code:** none (verification only).
**Tests:** run and record three scenarios in this plan:
(a) synthetic regression — a pending head followed by a `[done]` paragraph +
USER GUIDANCE line: old tool shifts 2 blocks (corrupts), fixed tool
shifts 0 (stable);
(b) legitimate-escape regression — a plain escaped paragraph after a visible
head: fixed tool still shifts it (+5 for 3-digit numbers);
(c) real log stability — `tasks/tasks.md` + archives under the fixed tool:
0 heads dedented, 0 blocks shifted, format unchanged, STATUS: PASS, files
byte-identical.
**Docs:** record the results (commands + outputs) in this plan's
"Verification results" section below.

**Spec:**

- Scenario (a) input: `- Archived: 1-100 in tasks1.md.` header line, blank,
  `294. Pending task still open. Some description here.`, blank,
  `[done] 295. Newer task completed out of order. Description with details.`,
  `**[USER GUIDANCE]**: Guidance for the newer task.` — old tool (origin/dev
  `d082242`, which has the hrs fix but not this one) must report
  `2 block(s) shifted`; fixed tool must report `0 block(s) shifted` and leave
  every line at column 0.
- Scenario (b) input: same header, `294. Pending task with a description.`,
  blank, two plain paragraph lines — fixed tool must shift both by +5.
- Scenario (c): run on the real repo; expect STATUS: PASS and byte-identical
  files (diff against pre-run copies). Re-run after the log-repair commit to
  confirm the repaired log is stable as well.

## Verification results

All scenarios re-run on the integrated tree (2026-10-14) with the merged tool
(dev's `min(region_hrs)` fix + this task's `DONE_HEAD_RE` boundary); "old tool"
is origin/dev `d082242`.

### (a) Synthetic regression — `[done]` paragraph after a pending head

Input: archive-reference line, `294. Pending task still open. Some description here.`, blank, `[done] 295. Newer task completed out of order. Description with details.`, `**[USER GUIDANCE]**: Guidance for the newer task.`

- Old tool (origin/dev `d082242`): `repair: 0 head(s) dedented, 2 block(s) shifted (3 lines)` — the `[done]` entry and its USER GUIDANCE line are shifted +5
  (corruption reproduced; the corrupted state is a fixed point — a second run
  reports 0 shifts and leaves the corruption in place).
- Fixed tool: `repair: 0 head(s) dedented, 0 block(s) shifted (0 lines)` — every line stays at column 0; output file byte-identical to the input.

### (b) Legitimate-escape regression — plain paragraph after a visible head

Input: archive-reference line, `294. Pending task with a description.`, blank,
two plain paragraph lines.

- Fixed tool: `repair: 0 head(s) dedented, 2 block(s) shifted (3 lines)` — both
  lines shifted to column 5 (`C(294) = 5`), exactly as before the fix. The new
  boundary branch only intercepts `[done] N. ` lines.

### (c) Real log stability

Run on the repo: `STATUS: PASS`; `tasks/tasks.md` (79 entries),
`tasks/tasks1.md` (96), `tasks/tasks2.md` (100) — all report `0 head(s) dedented, 0 block(s) shifted`, `format: unchanged`; all three files byte-identical to pre-run copies. Re-run after the log-repair commit confirms the same result on the repaired log.
