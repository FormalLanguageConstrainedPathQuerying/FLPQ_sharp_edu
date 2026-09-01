# Tools

**Tags:** guide, tools, python, quality, ci, detect-changes, format, build, test, coverage, lint
**Kind:** guide

> **Abstract:** Documents auxiliary Python scripts for project quality control in `tools/`: `detect_changes.py` (detect modified projects), `quality_check.py` (inter-subtask format + build gate), `hard_gate.py` (full format → build → tests + coverage → lint gate). For execution procedures, see the `quality-gates` and `dotnet-tooling` skills. For output conventions, see `tools/README.md`.

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

Inter-subtask quality check: format then build. Used as the commit gate.

**Output:** `tmp/quality-check.txt`

**Steps:**
1. Format: `dotnet fantomas .`
2. Build: `dotnet build FLPQ.slnx -c Debug`

**Exit codes:**
- 0 — STATUS: PASS
- 1 — STATUS: BLOCKED (format or build failed)

**Output format:**
```
QUALITY CHECK SUMMARY
Step 1: Format (dotnet fantomas .)
Step 2: Build (dotnet build)
  Format: OK
  Build: OK (Build succeeded)
STATUS: PASS
--- DETAILED LOG ---
[per-command stdout/stderr]
```

### `hard_gate.py`

Full quality gate for task verification: format → build → tests per project with coverage → coverage verification → lint on changed projects.

**Output:** `tmp/hard-gate.txt`

**Step counter:** Each step shows `<N>/<M>` where `M` is the total number of steps. For tests and linting, one project = one step. Format, build, and coverage are one step each.

**Steps:**
1. Format: `dotnet fantomas . --check`
2. Build: `dotnet build FLPQ.slnx -c Debug`
3. Tests: `dotnet dotnet-coverage collect dotnet test <project> -o tmp/coverage_<project>.cobertura -f cobertura --nologo` for each test project (discovered dynamically via `find_test_packages()`). After all projects, merge coverage files: `dotnet dotnet-coverage merge tmp/coverage_*.cobertura -o tmp/coverage.cobertura -f cobertura` and clean up per-project files.
4. Coverage gate: parse `tmp/coverage.cobertura` XML
   - Per-project and total line-coverage thresholds are defined in `hard_gate.py` (`PER_PROJECT_THRESHOLD`, `TOTAL_THRESHOLD`) — the tool is the source of truth for the numbers
   - Filters to FLPQ source packages only (excludes `*.Tests` and `FLPQ.TestUtilities`)
5. Lint: `dotnet-fsharplint lint` on each project with modified `.fs` files (detected via `detect_changes.py` logic). Uses `DOTNET_ROOT` from environment or `/usr/lib/dotnet`. If no `.fs` files changed, lint is skipped (not counted as a step).

**Test project discovery:** `find_test_packages()` in `common.py` dynamically discovers test projects by scanning `*.fsproj` files ending with `.Tests` (excluding `FLPQ.TestUtilities`). No project names are hardcoded.

**Incremental output:** the output file is flushed after each step. While the gate is running, the file shows `STATUS: IN_PROGRESS`. The final `STATUS: PASS` or `STATUS: BLOCKED` is only written when all steps complete. The exit code is the authoritative signal.

**Exit codes:**
- 0 — STATUS: PASS (all steps passed)
- 1 — STATUS: BLOCKED (any step failed)

**Merge-blocking messages** appended after the detailed log:

| Status | Message |
|--------|---------|
| `IN_PROGRESS` | `HARD GATE IN PROGRESS. DO NOT MERGE. Await completion.` |
| `BLOCKED` | `HARD GATE FAILED. Exit code 1. DO NOT MERGE. Resolve ALL failures and re-run.` |
| `PASS` | (none) |

**Output format (mid-execution, after Step 2):**
```
HARD GATE SUMMARY
Step 1/11 (Format): OK
Step 2/11 (Build): OK (Build succeeded)

STATUS: IN_PROGRESS

--- DETAILED LOG ---

--- STEP 1: FORMAT (dotnet fantomas . --check) ---
(no output)

--- STEP 2: BUILD (dotnet build) ---
Build succeeded.
...

HARD GATE IN PROGRESS. DO NOT MERGE. Await completion.
```

**Output format (final — PASS):**
```
HARD GATE SUMMARY
Step 1/11 (Format): OK
Step 2/11 (Build): OK (Build succeeded)
Step 3-8/11 (Tests):
  Step 3/11 FLPQ.Cli.Tests: OK (0 failed, 0 skipped)
  Step 4/11 FLPQ.GraphAnalysis.Tests: OK (0 failed, 0 skipped)
  Step 5/11 FLPQ.Languages.Tests: OK (0 failed, 0 skipped)
  Step 6/11 FLPQ.LinearAlgebra.Tests: OK (0 failed, 0 skipped)
  Step 7/11 FLPQ.Printers.Tests: OK (0 failed, 0 skipped)
  Step 8/11 FLPQ.RPQ.Tests: OK (0 failed, 0 skipped)
  Test gate: PASS
Step 9/11 (Coverage):
  FLPQ.Languages: 91.2% (4444/4874) — PASS
  TOTAL: 91.8% (7695/8380) (threshold 90%) — PASS
  Coverage gate: PASS
Step 10-11/11 (Lint):
  Step 10/11 src/FLPQ.Languages/FLPQ.Languages.fsproj: 0 warnings — PASS
  Step 11/11 src/FLPQ.Cli/FLPQ.Cli.fsproj: 0 warnings — PASS
  Lint gate: PASS

STATUS: PASS

--- DETAILED LOG ---
[per-step detailed output]
```

**Output format (final — no changed files, PASS):**
```
HARD GATE SUMMARY
Step 1/9 (Format): OK
Step 2/9 (Build): OK (Build succeeded)
Step 3-8/9 (Tests):
  Step 3/9 FLPQ.Cli.Tests: OK (0 failed, 0 skipped)
  ...
  Step 8/9 FLPQ.RPQ.Tests: OK (0 failed, 0 skipped)
  Test gate: PASS
Step 9/9 (Coverage):
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
Step 1/11 (Format): OK
Step 2/11 (Build): OK (Build succeeded)
Step 3-8/11 (Tests):
  Step 3/11 FLPQ.Cli.Tests: OK (0 failed, 0 skipped)
  ...
  Test gate: PASS
Step 9/11 (Coverage):
  FLPQ.Languages: 91.2% (4444/4874) — PASS
  FLPQ.Cli: 55.0% (438/796) — BLOCKED (below 85%)
  TOTAL: 91.8% (7695/8380) (threshold 90%) — PASS
  Coverage gate: BLOCKED
Step 10-11/11 (Lint):
  Step 10/11 src/FLPQ.Languages/FLPQ.Languages.fsproj: 0 warnings — PASS
  Step 11/11 src/FLPQ.Cli/FLPQ.Cli.fsproj: 0 warnings — PASS
  Lint gate: PASS

STATUS: BLOCKED

--- DETAILED LOG ---
[per-step detailed output]

HARD GATE FAILED. Exit code 1. DO NOT MERGE. Resolve ALL failures and re-run.
```

If a step fails early (e.g., build failure), subsequent steps are not executed and `STATUS: BLOCKED` is written immediately.

## Skill Integration

The skills that reference these tools:

- `.opencode/skills/quality-gates/SKILL.md` — commit gate and task verification sections reference `quality_check.py` and `hard_gate.py`
- `.opencode/skills/subtask-loop/SKILL.md` — pre-commit check step references `quality_check.py`; task completion verification references `hard_gate.py`

## See Also

- [tools/README.md](../../../tools/README.md) — output conventions
- [Quality Standards](quality-standards.md) — quality metrics and thresholds
