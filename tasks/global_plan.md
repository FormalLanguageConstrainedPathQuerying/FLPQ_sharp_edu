# Global Plan: Tasks 57—63

## Task Summary

| ID | Description | Type |
|----|-------------|------|
| 57 | Linear-algebra based multiple-source BFS (MS-BFS) | Feature |
| 58 | Matrix operations for MS-BFS and RPQ algorithms | Feature |
| 59 | Belyanin's LARPQ algorithm (BFS-based single-source RPQ) | Feature |
| 60 | Arroyuelo's RPQ algorithm (Matrix-based regex evaluation) | Feature |
| 61 | Kronecker-based RPQ algorithm with MS-BFS filtering | Feature |
| 62 | Graph reading from file | Feature |
| 63 | Property-based tests for all three RPQ algorithms | Feature (Tests) |

## Dependencies

```
Task 57 (MS-BFS) ───────────────┐
                                 ├──> Task 61 (Kronecker RPQ)
Task 58 (Matrix operations) ────┤
                                 ├──> Task 59 (Belyanin RPQ)
                                 ├──> Task 60 (Arroyuelo RPQ)
Task 62 (Graph reader) ─────────┤
                                 └──> Task 63 (Property-based tests)
```

- Tasks 57 and 58 are independent of each other but both go into the LinearAlgebra project
- Tasks 59, 60, 61 depend on 57, 58 (matrix operations) and 62 (graph reading for tests)
- Task 63 depends on all of 57-62

## Potential Conflicts

| Task | Files Modified/Created | Conflicts With |
|------|----------------------|----------------|
| 57+58 | New `MsBfs.fs`, modify `FLPQ.LinearAlgebra.fsproj` | None |
| 59 | New `BelyaninRPQ.fs`, modify `FLPQ.Languages.fsproj` | None |
| 60 | New `ArroyueloRPQ.fs`, modify `FLPQ.Languages.fsproj` | None |
| 61 | New `KroneckerRPQ.fs`, modify `FLPQ.Languages.fsproj` | Uses MS-BFS from 57 |
| 62 | New `GraphReader.fs`, modify `FLPQ.Languages.fsproj` | None |
| 63 | New/modified test files, modify test `.fsproj` | None |

All tasks create new files — no conflicts with existing code.

## Shared Infrastructure

- Tasks 57-58 share the `Matrix` and `LinearAlgebra` modules from `FLPQ.LinearAlgebra`
- Tasks 59, 60, 61 all use per-label boolean matrix decomposition (`BooleanDecomposition.decompose/recompose`)
- Tasks 59, 61 use the automaton type (`DFA<'t,'s>`) for query representation
- Task 60 reuses the `Regexp` AST from `EbnfParser.fs`
- Task 61 uses MS-BFS from task 57
- Task 62 provides graph reading infrastructure used by tests for all RPQ algorithms
- All test infrastructure (xUnit, FsCheck, TeX compilation) already exists

## Execution Order

1. **Tasks 57+58** (combined) — MS-BFS and supporting matrix operations in LinearAlgebra project
2. **Task 62** — Graph reader (independent, needed for tests)
3. **Task 59** — Belyanin's LARPQ
4. **Task 60** — Arroyuelo's RPQ 
5. **Task 61** — Kronecker-based RPQ (uses MS-BFS from 57)
6. **Task 63** — Property-based tests for all three algorithms

## New Files to Create

### Source files:
- `src/FLPQ.LinearAlgebra/MsBfs.fs` — MS-BFS, Boolean semiring ops, Mask semiring ops (tasks 57-58)
- `src/FLPQ.Languages/GraphReader.fs` — Graph file reading (task 62)
- `src/FLPQ.Languages/BelyaninRPQ.fs` — Belyanin's algorithm (task 59)
- `src/FLPQ.Languages/ArroyueloRPQ.fs` — Arroyuelo's algorithm (task 60)
- `src/FLPQ.Languages/KroneckerRPQ.fs` — Kronecker-based algorithm (task 61)

### Test files:
- `tests/FLPQ.LinearAlgebra.Tests/MsBfsTests.fs` — MS-BFS and matrix operations tests
- `tests/FLPQ.Languages.Tests/RPQTests.fs` — RPQ algorithms tests (all three + property-based)

### Documentation:
- `docs/msbfs.md` — MS-BFS module
- `docs/graph-reader.md` — Graph reader module
- `docs/belyanin-rpq.md` — Belyanin's RPQ
- `docs/arroyuelo-rpq.md` — Arroyuelo's RPQ
- `docs/kronecker-rpq.md` — Kronecker-based RPQ

### Modified files:
- `src/FLPQ.LinearAlgebra/FLPQ.LinearAlgebra.fsproj` — add `MsBfs.fs`
- `src/FLPQ.Languages/FLPQ.Languages.fsproj` — add new source files
- `tests/FLPQ.LinearAlgebra.Tests/FLPQ.LinearAlgebra.Tests.fsproj` — add test file
- `tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj` — add test file
- `docs/main.md` — add links to new docs
- `docs/architecture.md` — update with new modules
