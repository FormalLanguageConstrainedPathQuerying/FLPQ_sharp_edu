# Global Plan: dev History Integration (Tasks 294–296)

## Context

On 2026-10-14 `git push origin dev` was rejected: the remote carried 8 commits
of parallel Oct-8 work that local dev did not have — their task 292 (deduplicate
merged Arroyuelo reachability edge labels), their task 293 (CFPQ project, Azimov
matrix-based algorithm), and a separate `split_tasks.py` fix (`b1fbea3`: region
ends at the earliest top-level thematic break). Task numbers 292/293 collided
between the sides.

Decision (user-approved): rebase + renumber ours — their pushed 292/293 keep
their numbers; our completed work was renumbered to 294 (CI configuration +
FSharpLint pin), 295 (tool/system dependency migration), 296 (`split_tasks.py`
[done]-entry handling). The pre-integration local dev is preserved as branch
`backup/local-dev-pre-integration`.

## Tasks

| ID | Title | Status |
| --- | --- | --- |
| 294 | Configure CI properly and fix issue #1 (pin FSharpLint) | merged to dev (`aebb38d`), [done] |
| 295 | Migrate to latest tool/system dependencies | merged to dev (`d1333f0`), [done] |
| 296 | Fix `split_tasks.py` [done]-entry handling | merged to dev (`c1a668b`), [done] |

## Integration Commits (on dev, on top of origin/dev `d082242`)

1. `3e0e60c` docs(tasks): add task 294
2. `aebb38d` feat(294): CI configuration + FSharpLint pin — integrated
    `ci.yml` = our manifest-based rewrite + their CFPQ reference-data export
    step + Python 3.12
3. `fea73b0` docs(tasks): mark task 294 [done]
4. `aa551c1` docs(tasks): add task 295
5. `d1333f0` feat(295): dotnet-coverage 18.12.0 + nicematrix 7 compatibility
6. `fea941d` docs(tasks): mark task 295 [done]
7. `e66c085` docs(tasks): add task 296
8. `c1a668b` feat(296): `split_tasks.py` — `DONE_HEAD_RE` region boundary
    (complements their `min(region_hrs)` fix) + tools.md + plan + review
9. `827db70` docs(tasks): repair corrupted entries 277/280–287 (whitespace-only)
10. `59e3f5f` docs(tasks): mark task 296 [done]

## Verification State

- `split_tasks.py` scenarios re-run on the integrated tree (`detailed_plan.md`):
  old tool (origin/dev `d082242`) reproduces the corruption (2 blocks shifted;
  the corrupted state is a fixed point); merged tool shifts 0 with
  byte-identical output; legitimate escapes still shift +5.
- Repaired log: mdformat clean; STATUS: PASS, 0 shifts, all files byte-identical
  under the merged tool (verified pre-repair, post-repair, and after the
  `[done] 296.` mark).
- Hard gate on the integrated tree (incl. `FLPQ.CFPQ.Tests` with materialized
  `data/cfpq/`): STATUS: PASS — markdown, Fantomas, build, all seven test
  projects 0 failed / 0 skipped, coverage TOTAL line 98.4% / branch 96.5%,
  lint SKIP (no `.fs` changes vs dev).
- `data/cfpq/` materialized via flpq-data 6.0.0.dev15 in a Python 3.13 venv
  (`/tmp/opencode/venv-cfpq`) — the package requires `<3.14`, system Python is
  3.14; CI uses Python 3.12 via `setup-python`.

## CI Test-Step Fix (first push, run 38558343273)

The first integrated push failed at the Test step: the runner image has
neither `dot` (Graphviz) nor `lualatex` (TeX Live), and task 293's CI rewrite
had dropped the historical category filter — so all tool-dependent tests ran
and failed (148 failures: 77 TeX, 39 Graphviz, 28 Summary, 4 untagged). No
CI run had ever reached the Test step since July (every earlier run died at
tool install), so this was latent.

Fix (committed on dev as integration follow-up):

- Main job Test step: `--filter "Category!=Graphviz&Category!=TeX&Category!=Summary"`
  — the documented selective-execution filter
  (`docs/developer/guides/test-categories.md`).
- Restored the historical `graphviz-tests` job (apt Graphviz, runs only
  `Category=Graphviz`); TeX/Summary tests stay local (hard gate).
- Added missing category traits to 6 untagged tool-dependent tests:
  `GssDotTests` stored-pop fill (Graphviz), `RsmTikzTests` epsilon label
  (TeX), 4× `SummaryTests.buildSummary ... PDF when *.dot exists` (Graphviz).
- `test-categories.md`: "Affected tests" column updated to the actual file
  lists (was stale); Summary row now includes `AzimovCfpqCliTests`.

Verified locally: main filter 1404 tests / 0 failed; Graphviz filter
48 tests / 0 failed.

## Remaining Work

1. Push `dev` to origin and watch GitHub Actions CI until all steps pass —
   the first fully working run (manifest tool restore, CFPQ reference-data
   export on Python 3.12, markdown/format/lint/build/test + graphviz-tests).
2. Deliver the FSharpLint upstream issue text (issue #1's ≤5-min acceptance
   is unmet — 17m01s vs ~22m baseline on `FLPQ.Printers.Tests`; per the
   issue's own step 5, file upstream at `fsprojects/FSharpLint`).

## Notes

- The two `split_tasks.py` fixes are complementary and both required:
  `min(region_hrs)` (theirs, `b1fbea3`) + `DONE_HEAD_RE` boundary (ours,
  task 296).
- Do not run `split_tasks.py` from a checkout older than `c1a668b`: dev's tool
  before that commit lacks the [done]-boundary fix and would re-corrupt
  `[done] N. ` paragraphs following a visible head.
