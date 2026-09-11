# Tools

**Tags:** guide, tools, python, quality, ci, detect-changes, format, build, test, coverage, lint, mdformat, split-tasks
**Kind:** guide

> **Abstract:** Documents auxiliary Python scripts for project quality control in `tools/`: `detect_changes.py` (detect modified projects), `quality_check.py` (inter-subtask markdown format + format + build gate), `hard_gate.py` (full markdown format → format → build → tests + coverage → lint gate), `split_tasks.py` (task log repair, mdformat normalization, and 100-entry archiving). For execution procedures, see the `quality-gates` and `dotnet-tooling` skills. For output conventions, see `tools/README.md`.

## Contents

- [Tools](#tools)

## Tools

### `detect_changes.py`

Detects which projects have modified `.fs` files relative to the `dev` branch.

**Output:** `tmp/detect-changes.txt`

**Exit codes:**

- 0 — clean or modified (both are valid states)
- 1 — error running git commands

**Output format:**

```
DETECT CHANGES SUMMARY
Branch: <branch>
Found N modified .fs file(s) in M project(s):
  src/FLPQ.Languages/FLPQ.Languages.fsproj
STATUS: MODIFIED (or CLEAN if no changes)
--- DETAILED LOG ---
Changed files (file -> project mapping):
  src/FLPQ.Languages/Sppf.fs -> src/FLPQ.Languages/FLPQ.Languages.fsproj
```

### `quality_check.py`

Inter-subtask quality check: markdown format, then format, then build. Used as the commit gate.

**Output:** `tmp/quality-check.txt`

**Steps:**

1. Markdown format: `mdformat --check` on all tracked `.md` files (`git ls-files '*.md'`)
2. Format: `dotnet fantomas .`
3. Build: `dotnet build FLPQ.slnx -c Debug`

**Exit codes:**

- 0 — STATUS: PASS
- 1 — STATUS: BLOCKED (markdown format, format, or build failed)

**Output format:**

```
QUALITY CHECK SUMMARY
Step 1: Markdown format (mdformat --check)
Step 2: Format (dotnet fantomas .)
Step 3: Build (dotnet build)
  Markdown format: OK
  Format: OK
  Build: OK (Build succeeded)
STATUS: PASS
--- DETAILED LOG ---
[per-command stdout/stderr]
```

### `hard_gate.py`

Full quality gate for task verification: markdown format → format → build → tests per project with coverage → coverage verification → lint on changed projects.

**Output:** `tmp/hard-gate.txt`

**Step counter:** Each step shows `<N>/<M>` where `M` is the total number of steps. For tests and linting, one project = one step. Markdown format, format, build, and coverage are one step each.

**Steps:**

1. Markdown format: `mdformat --check` on all tracked `.md` files (`git ls-files '*.md'`)
2. Format: `dotnet fantomas . --check`
3. Build: `dotnet build FLPQ.slnx -c Debug`
4. Tests: `dotnet dotnet-coverage collect dotnet test <project> -o tmp/coverage_<project>.cobertura -f cobertura --nologo` for each test project (discovered dynamically via `find_test_packages()`). After all projects, merge coverage files: `dotnet dotnet-coverage merge tmp/coverage_*.cobertura -o tmp/coverage.cobertura -f cobertura` and clean up per-project files.
5. Coverage gate: parse `tmp/coverage.cobertura` XML
   - Per-project and total line-coverage thresholds are defined in `hard_gate.py` (`PER_PROJECT_THRESHOLD`, `TOTAL_THRESHOLD`) — the tool is the source of truth for the numbers
   - Filters to FLPQ source packages only (excludes `*.Tests` and `FLPQ.TestUtilities`)
6. Lint: `dotnet-fsharplint lint` on each project with modified `.fs` files (detected via `detect_changes.py` logic). Uses `DOTNET_ROOT` from environment or `/usr/lib/dotnet`. If no `.fs` files changed, lint is skipped (not counted as a step).

**Test project discovery:** `find_test_packages()` in `common.py` dynamically discovers test projects by scanning `*.fsproj` files ending with `.Tests` (excluding `FLPQ.TestUtilities`). No project names are hardcoded.

**Incremental output:** the output file is flushed after each step. While the gate is running, the file shows `STATUS: IN_PROGRESS`. The final `STATUS: PASS` or `STATUS: BLOCKED` is only written when all steps complete. The exit code is the authoritative signal.

**Exit codes:**

- 0 — STATUS: PASS (all steps passed)
- 1 — STATUS: BLOCKED (any step failed)

**Merge-blocking messages** appended after the detailed log:

| Status | Message |
| --- | --- |
| `IN_PROGRESS` | `HARD GATE IN PROGRESS. DO NOT MERGE. Await completion.` |
| `BLOCKED` | `HARD GATE FAILED. Exit code 1. DO NOT MERGE. Resolve ALL failures and re-run.` |
| `PASS` | (none) |

**Output format (mid-execution, after Step 3):**

```
HARD GATE SUMMARY
Step 1/12 (Markdown format): OK
Step 2/12 (Format): OK
Step 3/12 (Build): OK (Build succeeded)

STATUS: IN_PROGRESS

--- DETAILED LOG ---

--- STEP 1: MARKDOWN FORMAT (mdformat --check) ---
(no output)

--- STEP 2: FORMAT (dotnet fantomas . --check) ---
(no output)

--- STEP 3: BUILD (dotnet build) ---
Build succeeded.
...

HARD GATE IN PROGRESS. DO NOT MERGE. Await completion.
```

**Output format (final — PASS):**

```
HARD GATE SUMMARY
Step 1/12 (Markdown format): OK
Step 2/12 (Format): OK
Step 3/12 (Build): OK (Build succeeded)
Step 4-9/12 (Tests):
  Step 4/12 FLPQ.Cli.Tests: OK (0 failed, 0 skipped)
  Step 5/12 FLPQ.GraphAnalysis.Tests: OK (0 failed, 0 skipped)
  Step 6/12 FLPQ.Languages.Tests: OK (0 failed, 0 skipped)
  Step 7/12 FLPQ.LinearAlgebra.Tests: OK (0 failed, 0 skipped)
  Step 8/12 FLPQ.Printers.Tests: OK (0 failed, 0 skipped)
  Step 9/12 FLPQ.RPQ.Tests: OK (0 failed, 0 skipped)
  Test gate: PASS
Step 10/12 (Coverage):
  FLPQ.Languages: 91.2% (4444/4874) — PASS
  TOTAL: 91.8% (7695/8380) (threshold 90%) — PASS
  Coverage gate: PASS
Step 11-12/12 (Lint):
  Step 11/12 src/FLPQ.Languages/FLPQ.Languages.fsproj: 0 warnings — PASS
  Step 12/12 src/FLPQ.Cli/FLPQ.Cli.fsproj: 0 warnings — PASS
  Lint gate: PASS

STATUS: PASS

--- DETAILED LOG ---
[per-step detailed output]
```

**Output format (final — no changed files, PASS):**

```
HARD GATE SUMMARY
Step 1/10 (Markdown format): OK
Step 2/10 (Format): OK
Step 3/10 (Build): OK (Build succeeded)
Step 4-9/10 (Tests):
  Step 4/10 FLPQ.Cli.Tests: OK (0 failed, 0 skipped)
  ...
  Step 9/10 FLPQ.RPQ.Tests: OK (0 failed, 0 skipped)
  Test gate: PASS
Step 10/10 (Coverage):
  FLPQ.Languages: 91.2% (4444/4874) — PASS
  TOTAL: 91.8% (7695/8380) (threshold 90%) — PASS
  Coverage gate: PASS
Lint: SKIP (no changed .fs files)

STATUS: PASS

--- DETAILED LOG ---
[per-step detailed output]
```

**Output format (final — BLOCKED):**

```
HARD GATE SUMMARY
Step 1/12 (Markdown format): OK
Step 2/12 (Format): OK
Step 3/12 (Build): OK (Build succeeded)
Step 4-9/12 (Tests):
  Step 4/12 FLPQ.Cli.Tests: OK (0 failed, 0 skipped)
  ...
  Test gate: PASS
Step 10/12 (Coverage):
  FLPQ.Languages: 91.2% (4444/4874) — PASS
  FLPQ.Cli: 55.0% (438/796) — BLOCKED (below 85%)
  TOTAL: 91.8% (7695/8380) (threshold 90%) — PASS
  Coverage gate: BLOCKED
Step 11-12/12 (Lint):
  Step 11/12 src/FLPQ.Languages/FLPQ.Languages.fsproj: 0 warnings — PASS
  Step 12/12 src/FLPQ.Cli/FLPQ.Cli.fsproj: 0 warnings — PASS
  Lint gate: PASS

STATUS: BLOCKED

--- DETAILED LOG ---
[per-step detailed output]

HARD GATE FAILED. Exit code 1. DO NOT MERGE. Resolve ALL failures and re-run.
```

If a step fails early (e.g., build failure), subsequent steps are not executed and `STATUS: BLOCKED` is written immediately.

### `split_tasks.py`

Task log formatter and splitter. Keeps `tasks/tasks.md` (active log, at most 100 entries) and the archive files `tasks/tasks<N>.md` well-formed Markdown; numbering is continuous across files (`tasks1.md` = 1-100, `tasks2.md` = 101-200, ...). Run it after every task-log change (add entry, mark `[done]`, append USER GUIDANCE) and commit the result.

**Output:** `tmp/split-tasks.txt`

**Pipeline (per file):**

1. Parse entry heads — numbered lines (`N. `), strictly increasing; lines inside fenced or indented code blocks are skipped.
2. Repair indentation (leading whitespace only, entry text never touched): dedent heads not at column 0; shift sub-blocks that escape their entry item back inside it (a line escapes when, after a blank line, its indent is below the entry's content column `C(N) = len(str(N)) + 2`); split ordered lists at number gaps with a thematic break (`---`), because the AST stores only a list's start number and any renderer would renumber across a gap.
3. Normalize with mdformat (all installed parser extensions: tasklog, frontmatter).
4. Verify structure preservation; abort on any violation:
   - repair stage: changed lines differ only in leading whitespace; added lines are exactly the gap separators;
   - format stage: markdown-it-py AST deep equality modulo style (token types/sequence, block text with whitespace collapsed, ordered-list start numbers, fence and code-span bytes compared byte-exact); one canonical rewrite is accepted — mdformat converts indented code blocks to fenced ones with byte-identical content;
   - structure: every head is a direct item of a top-level ordered list and each such list's numbers are consecutive from its start.
5. Split: if `tasks.md` has more than 100 entries, move the oldest 100 to the next `tasks/tasks<N>.md` (provenance header) and update the archive-reference line in `tasks.md`.

**Invariants:** idempotent — on an already-clean state the script makes zero changes; moved blocks are byte-identical to the formatted source; entry numbers partition correctly across files.

**Exit codes:**

- 0 — PASS (including "no changes needed")
- 1 — BLOCKED (a verification stage found a violation; nothing is written)

`--dry-run` performs the full pipeline without writing files.

## Skill Integration

The skills that reference these tools:

- `.opencode/skills/quality-gates/SKILL.md` — commit gate and task verification sections reference `quality_check.py` and `hard_gate.py`
- `.opencode/skills/subtask-loop/SKILL.md` — pre-commit check step references `quality_check.py`; task completion verification references `hard_gate.py`

## See Also

- [tools/README.md](../../../tools/README.md) — output conventions
- [Quality Standards](quality-standards.md) — quality metrics and thresholds
