# Global Plan: Tasks 161–163

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|--------------|
| 161 | SPPF refactoring — invariant enforcement, node key types, edge restructuring | Code + Tests | None |
| 162 | Create `tools/` directory with Python quality control tools | Tooling + Docs | None |
| 163 | Improve test coverage for projects below threshold | Tests | 162 (needs coverage tool) |

## Dependencies Graph

```
Task 162 → (independent, tooling)
Task 161 → (independent, SPPF code)
Task 163 → Task 162 (needs coverage reporting tool to identify gaps)
```

## Execution Order

1. **Task 162** — Create auxiliary tools in `tools/`
2. **Task 161** — SPPF refactoring
3. **Task 163** — Test coverage improvement

## Rationale

- Task 162 creates the coverage analysis tool needed by Task 163
- Task 161 is independent SPPF code changes; placing it before 163 allows the new test invariants (path index nonterminal check) to contribute to coverage
- Task 163 depends on 162 for coverage measurement; 161 may add tests that help coverage

## Conflict Analysis

- **162 vs 161/163**: Task 162 only creates files in `tools/` and updates skill `.md` files. Skill files are not touched by 161 or 163. No conflicts.
- **161 vs 163**: Task 161 modifies `Sppf.fs`, `GllTests.fs`, `RnglrTests.fs`. Task 163 may add new tests to existing test files. Sequential execution prevents conflicts.

## Shared Infrastructure

- `src/FLPQ.Languages/Sppf.fs` — SPPF types and build/extraction functions (modified by 161)
- `tests/FLPQ.Languages.Tests/GllTests.fs` — GLL tests (modified by 161, 163)
- `tests/FLPQ.Languages.Tests/RnglrTests.fs` — RNGLR tests (modified by 161, 163)
- `.opencode/skills/quality-gates/SKILL.md` — quality gate procedures (modified by 162)
- `.opencode/skills/subtask-loop/SKILL.md` — subtask loop procedures (modified by 162)

## Task 161: SPPF Refactoring

Three deep changes to SPPF data structures and extraction logic:

1. **Path index invariant check**: For all GLL and RNGLR tests, verify that each cell of the path index contains at most one `PNonterminal` or `PEpsilonNonterminal`. This must hold even for rejected inputs.
2. **Typed node keys**: Replace `Dictionary<string, int>` (string-based node deduplication) with typed dictionaries keyed by discriminated union subtypes (`TerminalNodeKey`, `NonterminalNodeKey`, `IntermediateNodeKey`, etc.).
3. **Nonterminal edge restructuring**: Nonterminal nodes are not alternatives of range nodes. If a range cell contains a nonterminal, it contains exactly one nonterminal. The nonterminal node becomes a predecessor of its corresponding range node: `Intermediate → Nonterminal → Range` (instead of current `Intermediate → Range` with `Nonterminal` as an alternative child).

## Task 162: Auxiliary Tools

Create `tools/` directory at project root with Python scripts for quality control:

1. **`tools/detect_changes.py`** — determines which projects have modified `.fs` files
2. **`tools/quality_check.py`** — inter-subtask check: format → build solution
3. **`tools/hard_gate.py`** — full gate: format → build → all tests with coverage (80% line min, 75% per-project min) → fsharplint on changed projects
4. Each tool writes results to `tmp/<tool-name>.txt` with structured output (summary first, detailed logs after)
5. Update `quality-gates` and `subtask-loop` skills to reference these tools

## Task 163: Test Coverage Improvement

After running the coverage tool from Task 162:

1. Identify all FLPQ source projects (excluding `*.Tests`) with line coverage below 75%
2. Add focused tests to bring each below-threshold project up to at least 75% line coverage
3. Verify total coverage meets 80% threshold
4. Do NOT modify projects already above threshold
