# Global Plan: Coverage Thresholds and Branch Coverage (Tasks 276, 277)

## Tasks

| ID | Title | Summary |
| --- | --- | --- |
| 276 | Raise line coverage thresholds + tests | Raise hard-gate line-coverage thresholds to 90% per project / 95% total; add tests until all source projects and the total meet them. |
| 277 | Add branch coverage gate + tests | Extend the hard gate with a branch-coverage check (90% per project / 95% total) parsed from the merged cobertura file; add tests until all source projects and the total meet them. |

## Interpretation Notes

- **"Instruction coverage" = line coverage.** .NET coverage tooling
  (`dotnet-coverage` + coverlet) reports line and branch coverage; there is no
  separate instruction-level metric. The existing gate already gates line
  coverage (85% per project / 90% total); task 276 raises those numbers.
- **Branch coverage IS available** (verified 2026-09-16): the cobertura output
  of `dotnet-coverage collect` contains `condition-coverage="(n/m)"` on every
  branch line. Summing n/m over lines of source packages exactly reproduces
  coverlet's own package-level `branch-rate` attribute (verified against
  `FLPQ.LinearAlgebra`: 58/80 = 0.725 both ways). Task 277 uses this.

## Baseline (full-solution merged coverage, all 6 test projects)

| Package | Line % | Branch % |
| --- | --- | --- |
| FLPQ.Languages | 88.65% (6810/7682) | 90.35% (1872/2072) |
| FLPQ.Cli | 95.30% (1054/1106) | 87.18% (136/156) |
| FLPQ.Printers | 93.29% (4172/4472) | 94.16% (1194/1268) |
| FLPQ.LinearAlgebra | 100% (236/236) | 100% (56/56) |
| FLPQ.GraphAnalysis | 100% (98/98) | 100% (16/16) |
| FLPQ.RPQ | 93.01% (266/286) | 84.31% (172/204) |
| **TOTAL** | **91.04% (12636/13880)** | **91.36% (3446/3772)** |

Gaps to close:

- **Line (276):** FLPQ.Languages needs +104 lines for its own 90%; TOTAL needs
  +550 lines for 95%. Biggest uncovered files: `Sppf.fs` (~150 unique lines),
  `BasicSppf.fs` (~90), `PathIndex.fs` (~90), `EbnfParser.fs` (~30),
  `LRParser.fs` (~21) in FLPQ.Languages; `MatrixTeX.fs` (~40), `SummaryTeX.fs`
  (~30), `ExternalTools.fs` (~20), `RnglrTableTeX.fs` (~18) in FLPQ.Printers.
- **Branch (277):** FLPQ.Cli needs +5 branches for its own 90%; FLPQ.RPQ needs
  +12; TOTAL needs +138 for 95%. Biggest uncovered branch files:
  `BasicSppf.fs` (86), `Sppf.fs` (44), `Automaton.fs` (26) in FLPQ.Languages;
  `ValiantTeX.fs` (24) in FLPQ.Printers; `Helpers.fs` (12) in FLPQ.Cli;
  `GraphReader.fs` (14) + `BelyaninRPQ.fs` (12) in FLPQ.RPQ.

## Dependencies

- **276 is independent** — threshold change + line-coverage tests.
- **277 depends on 276 being merged** — both modify `tools/hard_gate.py` and
  add tests; sequential execution avoids conflicts. Tests added in 276 also
  improve branch coverage, shrinking 277's test work.

## Execution Order

1. **Task 276** — raise line thresholds, close the line-coverage gap
2. **Task 277** — add the branch gate, close the branch-coverage gap

## Overlapping Files

| File | 276 | 277 |
| --- | --- | --- |
| `tools/hard_gate.py` | thresholds 85→90 / 90→95 | + branch parsing & gate |
| `docs/developer/guides/tools.md` | threshold references, example output | + branch metric in coverage step |
| `.opencode/skills/quality-gates/SKILL.md` | if thresholds mentioned | + branch metric |
| `tests/*/*.fs` | new tests (line gaps) | new tests (branch gaps) |

## Reuse Analysis

- Coverage parsing: extend the existing per-package loop in
  `hard_gate.py:run_coverage_gate` — one loop collects line and branch counts
  together; no new script.
- Tests: reuse existing test infrastructure — `FLPQ.TestUtilities`
  (`LanguageRegistry`, `Generators`, `ParsingTestCases`, `TestHelpers`),
  existing golden/property-based patterns per the `tests-writer` skill. New
  tests go into the test project mirroring the source project.
- No new shared modules needed; both tasks reuse the same coverage pipeline.

## Risk Assessment

- **Task 276**: Low risk — additive tests + threshold bump. Some uncovered
  lines may be defensive/error paths that are hard to reach; if a clause of
  the 95% total proves unreachable without contrived tests, report per the
  blocked-work protocol instead of weakening the gate.
- **Task 277**: Medium risk — F# emits many compiler-generated branches
  (match-failure checks, option handling). Baseline shows branch coverage is
  close to line coverage in this codebase (91.36% vs 91.04%), so 95% total /
  90% per project is plausible, but the tail (BasicSppf/Sppf/Automaton) needs
  real test input, not just more of the same.
