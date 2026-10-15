# Tools

**Tags:** guide, tools, python, quality, ci, detect-changes, format, build, test, coverage, lint, mdformat, split-tasks
**Kind:** guide

> **Abstract:** Documents auxiliary Python scripts in `tools/`: `detect_changes.py` (detect modified projects), `quality_check.py` (inter-subtask markdown format + format + build gate), `hard_gate.py` (full markdown format → format → build → tests + coverage → lint gate), `flpq_data/export.py` (materialize CFPQ reference data from the pinned flpq-data package). For execution procedures, see the `quality-gates` and `dotnet-tooling` skills. For output conventions, see `tools/README.md`.

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
   - Line coverage counts `<line>` entries with `hits > 0`; branch coverage sums the `(n/m)` pair from each line's `condition-coverage` attribute (matches coverlet's package-level `branch-rate`)
   - Both metrics are gated: per-project and total thresholds are defined in `hard_gate.py` (`PER_PROJECT_THRESHOLD`, `TOTAL_THRESHOLD`) — the tool is the source of truth for the numbers
   - Filters to FLPQ source packages only (excludes `*.Tests` and `FLPQ.TestUtilities`)
6. Lint: `dotnet-fsharplint lint` on each project with modified `.fs` files (detected via `detect_changes.py` logic). Uses `DOTNET_ROOT` from environment or `/usr/lib/dotnet`. If no `.fs` files changed, lint is skipped (not counted as a step).
   - **Timing warning:** FSharpLint 0.27.0 bundles FCS 43.9 (.NET 9 era) and is pathologically slow on this machine's net10.0 projects — 10–40+ minutes per large project before the first `Linting <file>` progress line appears (verified pre-existing on unchanged projects, e.g. `FLPQ.RPQ.Tests`). The process burns CPU and grows RSS while working; it is not hung. Budget hours, not minutes, for the lint step when several projects changed, and never kill a run that has not yet printed its first progress line.

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
  FLPQ.Languages: line 96.8% (4718/4874), branch 95.1% (1930/2030) — PASS
  TOTAL: line 97.6% (8181/8380), branch 96.2% (3820/3970) (threshold 95%) — PASS
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
  FLPQ.Languages: line 96.8% (4718/4874), branch 95.1% (1930/2030) — PASS
  TOTAL: line 97.6% (8181/8380), branch 96.2% (3820/3970) (threshold 95%) — PASS
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
  FLPQ.Languages: line 91.2% (4444/4874), branch 93.4% (1896/2030) — PASS
  FLPQ.Cli: line 55.0% (438/796), branch 82.1% (128/156) — BLOCKED (line and branch below 90%)
  TOTAL: line 88.4% (7408/8380), branch 91.0% (3612/3970) (threshold 95%) — BLOCKED (line and branch below 95%)
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

### `flpq_data/export.py`

Materializes CFPQ reference data from the pinned `flpq-data==6.0.0.dev15` package (test.pypi.org, Python ≥ 3.11) into the gitignored `data/cfpq/` directory. For each of the five pinned graphs (generations, travel, skos, sampleproject, univ) it writes:

- `data/cfpq/<graph>/graph/*.mtx` — the effective query graph: the stored edge labels plus the reverse edges with the `_r` label suffix that the package's CFPQ queries run over (`flpq_data.add_reverse_edges`), one 0-based pattern matrix per label;
- `data/cfpq/<graph>/queries/<q>/<q>.cnf` — the grammar template materialized over the graph (indexed symbols expanded to concrete edge labels);
- `data/cfpq/<graph>/queries/<q>/results.mtx` — the package's reference reachability matrix for that graph × grammar;
- `data/cfpq/<graph>/reference.json` — `{grammar: num_reachable_pairs}` from the package's `reachable_pairs.csv` (query_class == cfpq);
- `data/cfpq/manifest.json` — `{version, graphs: {name: {vertex_count, grammars}}}`.

The F# reference tests (`CfpqReferenceTests` in FLPQ.CFPQ.Tests) read the manifest and assert that Azimov's reachability answer equals each `results.mtx` exactly and that the true-cell count equals `num_reachable_pairs`. CI materializes the data before running tests; locally run the tool once after installing the pinned package.

**Setup:**

```bash
pip install --extra-index-url https://test.pypi.org/simple/ "flpq-data==6.0.0.dev15"
python3 tools/flpq_data/export.py
```

**Output:** summary on stdout (per-graph vertex count, grammar count, reachable-pair total) — unlike the quality tools, this is a one-shot data materializer and writes no `tmp/<tool>.txt`.

**Exit codes:**

- 0 — export complete
- 1 — installed flpq-data version differs from the pinned one (the message shows the install command)

**Invariants:** idempotent — re-running regenerates byte-identical files; the output directory is wiped and rebuilt per graph.

## Skill Integration

The skills that reference these tools:

- `.opencode/skills/quality-gates/SKILL.md` — commit gate and task verification sections reference `quality_check.py` and `hard_gate.py`
- `.opencode/skills/subtask-loop/SKILL.md` — pre-commit check step references `quality_check.py`; task completion verification references `hard_gate.py`

## See Also

- [tools/README.md](../../../tools/README.md) — output conventions
- [Quality Standards](quality-standards.md) — quality metrics and thresholds
