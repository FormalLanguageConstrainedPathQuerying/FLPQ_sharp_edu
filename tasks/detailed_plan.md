# Detailed Plan — Task 277

## Task description (verbatim)

277. Add branch coverage to the hard gate and add tests to reach it. 1. Extend `tools/hard_gate.py` to parse branch coverage from `tmp/coverage.cobertura` — sum covered/total from `condition-coverage="(n/m)"` on lines within classes of source packages (verified to exactly match coverlet's own package-level `branch-rate`) — and gate it: per project >= 90%, total >= 95%; report per-project branch % alongside line % in the coverage step. Update `docs/developer/guides/tools.md` and the quality-gates skill. 2. Add tests until every source project reaches >= 90% branch coverage and the total reaches >= 95%. Baseline: FLPQ.Languages 90.35% (1872/2072), FLPQ.Cli 87.18% (136/156), FLPQ.Printers 94.16%, FLPQ.LinearAlgebra 100%, FLPQ.GraphAnalysis 100%, FLPQ.RPQ 84.31% (172/204), TOTAL 91.36% (3446/3772). Biggest gaps: BasicSppf.fs (86 uncovered branches), Sppf.fs (44), Automaton.fs (26) in FLPQ.Languages; ValiantTeX.fs (24) in FLPQ.Printers; Helpers.fs (12) in FLPQ.Cli; GraphReader.fs (14) + BelyaninRPQ.fs (12) in FLPQ.RPQ.

## Reuse analysis (reusing skill checklist)

- **Q4 generalize**: extend the existing `run_coverage_gate()` in `tools/hard_gate.py`
  rather than adding a parallel branch-gate function — one parse pass over
  `tmp/coverage.cobertura` computes both line and branch metrics.
- **Reuse constants**: branch thresholds are the same numbers (90/95) as line
  thresholds, so reuse `PER_PROJECT_THRESHOLD` / `TOTAL_THRESHOLD` — no new
  constants.
- **Reuse `common.py`**: `find_source_packages()` already filters to FLPQ source
  packages; no change needed.
- **Tests**: extend existing test files (`GllRunnerTests.fs`,
  `RnglrRunnerTests.fs`, `RPQTests.fs`) with the existing private runner helpers
  (`runGllRunner`, `runRnglrRunner`) and existing NFA/DFA construction patterns;
  no new fixtures needed.
- **Docs**: update the coverage section of `docs/developer/guides/tools.md`
  (step description + three example outputs) and the step table in
  `.opencode/skills/quality-gates/SKILL.md`; no new doc files.

## Current state (measured from the task-276 hard-gate merged coverage,

`tmp/coverage.cobertura`, 2026-09-18)

Branch coverage is already better than the task baseline because the task-276
review rounds added tests:

| Project | Branch % (covered/total) | >= 90%? |
| --- | --- | --- |
| FLPQ.Cli | 89.74% (140/156) | NO — needs +1 |
| FLPQ.GraphAnalysis | 100.00% (16/16) | yes |
| FLPQ.Languages | 95.52% (2132/2232) | yes |
| FLPQ.LinearAlgebra | 100.00% (56/56) | yes |
| FLPQ.Printers | 99.70% (1332/1336) | yes |
| FLPQ.RPQ | 88.24% (180/204) | NO — needs +4 |
| TOTAL | 96.40% (3856/4000) | >= 95% yes |

Uncovered-branch map (each line appears in two class entries; the gate sums
per entry, matching coverlet's package-level `branch-rate`):

- `FLPQ.Cli/Helpers.fs:104,123,142,161,180,199` — `None -> failwithf` fallbacks
  of `List.tryFind File.Exists`; unreachable (all six templates exist in
  `data/`, same finding as task 276 Design Notes). Not coverable.
- `FLPQ.Cli/GllRunner.fs:75`, `RnglrRunner.fs:72` — `if accepted then "Accepted" else "Rejected"`; all existing runner tests use accepted inputs,
  so the `"Rejected"` side is uncovered. **Coverable (S2).**
- `FLPQ.RPQ/BelyaninRPQ.fs:23` — `if vCount = 0` true side; unreachable
  (documented in task 276). Not coverable.
- `FLPQ.RPQ/BelyaninRPQ.fs:39` — zero-iteration case of
  `for KeyValue(label, gMat) in perLabel` (graph with no transitions).
  **Coverable (S3).**
- `FLPQ.RPQ/BelyaninRPQ.fs:61` — zero-iteration case of
  `for qf in dfa.FinalStates` (DFA with no final states). **Coverable (S3).**
- `FLPQ.RPQ/BelyaninRPQ.fs:65` — range-loop branches tied to the unreachable
  `vCount = 0` path. Partially coverable via S3 inputs.
- `FLPQ.RPQ/GraphReader.fs:40,55,70,82` — empty/whitespace-only text, first
  line not all numbers (no explicit start vertices), edge list empty,
  `vertexCount = 0`. **Coverable (S3).**

## Subtasks

### S1: Add branch-coverage parsing and gating to tools/hard_gate.py [done — d63336f]

**Code:** `tools/hard_gate.py` — extend `run_coverage_gate()`: in the same
per-package loop that counts line hits, also read each `<line>` element's
`condition-coverage` attribute, extract `(n/m)` with regex `\((\d+)/(\d+)\)`,
and accumulate per-package and total branch covered/valid. Report both metrics
on each project line and the TOTAL line; a project (or the total) is BLOCKED
if either its line % or its branch % is below the threshold. Reuse
`PER_PROJECT_THRESHOLD` / `TOTAL_THRESHOLD`. Update the module docstring step 5
to "per-project >= 90% line and branch, total >= 95% line and branch".

**Tests:** No Python test framework exists for `tools/`; verify by running the
extended `run_coverage_gate()` standalone against the existing
`tmp/coverage.cobertura` and checking the reported numbers match the measured
table above (Cli 89.74% BLOCKED, RPQ 88.24% BLOCKED, TOTAL 96.40% PASS).

**Docs:** `docs/developer/guides/tools.md` — step 5 description (line + branch,
`condition-coverage` source) and all three example outputs (add branch figures,
keep them arithmetically consistent); `.opencode/skills/quality-gates/SKILL.md`
— coverage row of the step table ("Per-project + total line and branch
threshold check").

**Spec:**

- Cobertura format: `<line number="N" hits="H" branch="True" condition-coverage="NN% (n/m)">`; sum `n` into covered, `m` into valid.
- Lines without a `condition-coverage` attribute contribute 0/0 to branches.
- Report format per project:
  `  <name>: line <pct>% (<c>/<v>), branch <pct>% (<c>/<v>) — PASS|BLOCKED`.
- TOTAL line: `  TOTAL: line <pct>% (<c>/<v>), branch <pct>% (<c>/<v>) (thresholds 90%/95%) — PASS|BLOCKED` (exact wording may keep the existing
  single-threshold style; both metrics are gated by the same constants).
- BLOCKED reason must name the failing metric, e.g. `BLOCKED (branch below 90%)`.
- The `under_threshold` return list gains branch-failing package names.

### S2: Cover FLPQ.Cli GllRunner/RnglrRunner "Rejected" branches [done — cd01c66]

**Code:** none (tests only).

**Tests:** `tests/FLPQ.Cli.Tests/GllRunnerTests.fs` — add a test running
`runGllRunner "S -> a S b | eps" "a a a"` (unbalanced input → rejected) and
assert the runner output contains `Rejected`;
`tests/FLPQ.Cli.Tests/RnglrRunnerTests.fs` — same shape with
`runRnglrRunner "S -> a S b | eps" "a a a"`. This covers the `"Rejected"` side
of `GllRunner.fs:75` and `RnglrRunner.fs:72` (4 branch entries).

**Docs:** none.

**Spec:**

- Reuse the existing private helpers `runGllRunner` / `runRnglrRunner`
  (temp-dir + file writing already handled).
- Assert on the printed status line (`GLL: Rejected ...` /
  `RNGLR: Rejected ...`) so the test fails if the branch regresses.
- Target: FLPQ.Cli branch >= 90% (144/156 = 92.3%).

### S3: Cover FLPQ.RPQ Belyanin and GraphReader edge branches [done — 1afd4f7]

**Code:** none (tests only).

**Tests:** `tests/FLPQ.RPQ.Tests/RPQTests.fs`:

- Belyanin on an NFA with vertices but no transitions (empty `perLabel` →
  zero-iteration branch at `BelyaninRPQ.fs:39`); assert the result matrix is
  all false.
- Belyanin with a DFA whose `FinalStates` set is empty (zero-iteration branch
  at `BelyaninRPQ.fs:61`); assert all-false result.
- `GraphReader.parseGraph` on whitespace-only text (empty line list →
  branches at `GraphReader.fs:40,70,82`); assert vertex count 0 / empty NFA.
- `GraphReader.parseGraph` on text whose first line is not all numbers (e.g.
  an edge line as the first line) → `else` branch at `GraphReader.fs:52/55`;
  assert start states default to all vertices.

**Docs:** none.

**Spec:**

- Reuse existing NFA/DFA construction patterns already present in RPQTests.fs.
- Each test asserts a concrete result (matrix values / vertex count / start
  states), not just "does not throw".
- Target: FLPQ.RPQ branch >= 90% (>= 184/204).

### S4: Full verification — hard gate PASS [done — STATUS: PASS]

**Code:** none unless a subtask target is missed.

**Tests:** run the full hard gate (`tools/hard_gate.py`, async procedure from
the quality-gates skill); require `STATUS: PASS` with every project passing
both line and branch gates and TOTAL >= 95% on both metrics. If any project is
below threshold, add tests in the corresponding S2/S3-style increment and
re-run.

**Docs:** none (numbers in tools.md examples are illustrative, not live).

**Spec:**

- No production-code changes in this task; if a branch proves unreachable
  (like `Helpers.fs` failwith fallbacks or `BelyaninRPQ.fs:23`), record it in
  Design Notes instead of forcing coverage.

## Design Notes

### FLPQ.RPQ is at its maximum achievable branch coverage (184/204 = 90.20%)

After S3, the per-file branch breakdown (RPQ-only run, `tmp/cov-s3-final.cobertura`)
is:

| File | Branch % | Status |
| --- | --- | --- |
| ArroyueloRPQ.fs | 100.00% (40/40) | fully covered |
| BelyaninRPQ.fs | 86.11% (62/72) | at ceiling — 5 unique branches unreachable |
| GraphReader.fs | 89.29% (50/56) | at ceiling — 3 unique branches unreachable |
| KroneckerRPQ.fs | 88.89% (32/36) | at ceiling — 2 unique branches unreachable |
| TOTAL | 90.20% (184/204) | >= 90% PASS |

Each source line appears in two `<class>` entries (the module class and a nested
local-function class), so the gate sums per entry; the "unique" counts above are
per distinct source line. All 10 remaining unique uncovered branches are
compiler-generated artifacts that no test input can reach:

- `BelyaninRPQ.fs:23` — true side of `if vCount = 0`; unreachable because the
  NFA is always built from a non-empty vertex list (vCount >= 1). Same finding
  as task 276.
- `BelyaninRPQ.fs:39,61` — each line has a now-covered zero-iteration branch
  (S3 tests) plus a finally-block `isinst IDisposable` branch; F# `Map`/`Set`
  sequence adapters never return an `IDisposable` enumerator, so that side is
  dead.
- `BelyaninRPQ.fs:65` — two empty-range sides of the array comprehension, tied
  to the unreachable vCount = 0 path.
- `GraphReader.fs:40,55` — null-guard on a boxed string array; `String.Split`
  never returns null.
- `GraphReader.fs:82` — empty-range side inside the branch guarded by
  `vertexCount > 0`.
- `KroneckerRPQ.fs:37` — null-guard on `Array.map` over the source list.
- `KroneckerRPQ.fs:48` — finally-block `isinst IDisposable` artifact.

### Reachability methodology (reproducible)

Coverlet attributes compiler-generated branches (null guards on boxed arrays,
`isinst IDisposable` in the finally block of `for ... in seq` loops) to source
lines. To decide whether an uncovered branch is reachable, dump the IL with
sequence points and inspect the branch targets:

```bash
export DOTNET_ROOT=/usr/lib/dotnet
export PATH="$PATH:$HOME/.dotnet/tools"   # ilspycmd via `dotnet tool install -g ilspycmd`
ilspycmd --il-sequence-points -t "FLPQ.RPQ.BelyaninRPQ" \
  src/FLPQ.RPQ/bin/Debug/net10.0/FLPQ.RPQ.dll
```

A branch is unreachable when its target is only reachable through a guard that
the type system or construction invariants make false (vCount >= 1, non-null
`Split` result, non-`IDisposable` F# collection enumerator). No production code
was changed to force these; they are recorded here per the task spec.

### S3 outcome

The six S3 tests raised FLPQ.RPQ from 88.24% (180/204) to 90.20% (184/204).
Only the GraphReader backward-edge test added net new branches (+4, fixing
`GraphReader.fs:70`); the other five are meaningful edge-case regression guards
on paths that were already branch-covered but lacked an asserting test. Since
coverage is a union, the merged hard-gate run (S4) reports FLPQ.RPQ at least
184/204.
