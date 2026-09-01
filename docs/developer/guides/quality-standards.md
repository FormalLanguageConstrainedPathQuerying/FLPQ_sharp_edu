# Quality Standards

**Tags:** guide, quality, lint, format, coverage, tests, ci, gates
**Kind:** guide

> **Abstract:** Defines the project's non-negotiable quality gates: zero lint warnings, zero formatting differences, build with zero errors, all tests pass with zero skipped, equivalence tests for all algorithm variants, and the line-coverage threshold enforced by `tools/hard_gate.py`. A quality gate failure is a blocker — do not commit, do not merge, do not weaken tests. For execution procedures (commands, output capture), see the `quality-gates` and `dotnet-tooling` skills.

## Contents

- [Why quality standards matter](#why-quality-standards-matter)
- [What our quality metrics are](#what-our-quality-metrics-are)
- [What happens when a gate fails](#what-happens-when-a-gate-fails)

## See Also

- [Coding Conventions](coding-conventions.md) | [Design Guides](design-guides.md)
- [Documentation Conventions](documentation-conventions.md)
- [Tools](tools.md) — Python quality tools
- [Test Categories](test-categories.md) — Graphviz/TeX/Summary test categories

## Why quality standards matter

This project's algorithms are reference implementations for a textbook. Bugs in reference code propagate to readers who trust the book's correctness. Quality gates are a non-negotiable baseline — they catch regressions before they reach the reader.

For execution of quality gates (commands, output capture, verification), see the `quality-gates` skill. For individual dotnet commands, see the `dotnet-tooling` skill.

## What our quality metrics are

### Zero lint warnings

The lint configuration (`fsharplint.json`) enforces naming conventions, idiomatic patterns, and detects ~100 common anti-patterns (e.g., `List.head (List.sort x)` → `List.min x`, `x = true` → `x`).

**Why**: Lint warnings are compiler-detectable code smells. A single warning means code that violates our naming conventions or contains a knowable improvement. Zero is absolute — pre-existing warnings are not exempt. The threshold is zero because nonzero thresholds normalize warning accumulation.

### Zero formatting differences

All code is formatted by fantomas with project-level settings. Format check runs in CI and before every commit.

**Why**: Consistent formatting eliminates style debates in code review and ensures that diffs show only semantic changes, not whitespace reformatting. A single formatting difference blocks commit because formatting is automated — there is no reason for it to drift.

### Build with zero errors

**Why**: A build error may cascade from an innocent-seeming change. Building the full solution after every change catches type mismatches, missing references, and breaking API changes immediately.

### All tests pass, zero skipped

**Why**: A skipped test is a test that was recognized as necessary but not implemented. Zero skipped tests means every planned test either passes or doesn't exist — there is no grey area of "we'll fix it later." A single failing test means the algorithm produces incorrect results for at least one input.

### Equivalence tests for all variants

Every algorithm variant must include property-based equivalence tests proving it returns identical results to at least one existing reference implementation. Examples:

- Standard Valiant ≡ Modified Valiant
- Belyanin RPQ ≡ Arroyuelo RPQ ≡ Kronecker+MS-BFS

**Why**: Variants are alternative algorithms for the same problem. Without equivalence tests, a variant could produce different results and no one would know. Property-based tests generate random inputs and verify both implementations agree, giving statistical confidence in correctness.

### Line coverage

Total line coverage across all `FLPQ.*` source packages (excluding `*.Tests`) must meet the threshold enforced by the hard gate. The threshold is defined in, and checked by, the tool — run `python3 tools/hard_gate.py` to measure and verify coverage; there is no separate coverage number documented here.

**Why**: A single tool is the source of truth for the coverage threshold, so the metric cannot drift out of sync with its enforcement.

## What happens when a gate fails

A quality gate failure is a blocker. Do not commit. Do not merge. Do not comment out or weaken failing tests. The change must be fixed until all gates pass.

If a problem cannot be resolved to 100% correctness, STOP, report concretely (which tests fail, why, what was tried), and ask for guidance. Never ship known-incorrect reference code.

## See Also

- [Coding Conventions](coding-conventions.md)
- [Design Guides](design-guides.md)
- [Tools](tools.md) — quality check and hard gate scripts
- [Test Categories](test-categories.md) — external tool dependencies
