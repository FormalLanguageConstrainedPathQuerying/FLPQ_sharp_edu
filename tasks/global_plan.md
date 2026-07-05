
# Global Plan: Tasks 122--124

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|--------------|
| 122 | Make `Matrix.data` private, add `get`/`set` functions, replace color strings with semantic labels | Refactor | None |
| 123 | Deduplicate miscellaneous helpers: `readIfExists`, `collectSteps`, `termPrinter`, `escapeLabel`, unused definitions, exception handling, `AlgorithmKind` | Refactor | None |
| 124 | Rename `Rhs.toList`→`toListWithEpsilon`, `Rhs.toSymbols`→`toNonEpsilonList` | Rename | None |

## Dependencies Graph

```
No interdependencies between tasks. They touch mostly disjoint sets of files.
Execution order: 124 (simplest) → 123 (collection of small changes) → 122 (largest, most files affected)
```

## Execution Order

1. **Task 124** — Rename `Rhs.toList`/`toSymbols`
2. **Task 123** — Deduplicate miscellaneous helpers
3. **Task 122** — Matrix.data privatization + semantic labels

## Shared Infrastructure / Potential Conflicts

Tasks 122 and 123 both touch `src/FLPQ.Languages/Grammar.fs`, `src/FLPQ.Languages/Cyk.fs`, `src/FLPQ.Printers/SummaryTeX.fs`, `src/FLPQ.Cli/Helpers.fs`, `src/FLPQ.Cli/Summary.fs`, and test files. However, they modify different parts of those files, so conflicts are minimal. Execution order avoids rework.

## Task 122: Matrix.data Privatization

### Files to modify (source)
- `src/FLPQ.LinearAlgebra/Matrix.fs` — make `data` private, add `get`/`set`/`unsafeGet`/`unsafeSet`, add semantic label types, rename `Highlight`/`SubmatrixBlock`
- `src/FLPQ.LinearAlgebra/BooleanDecomposition.fs` — update direct `.data` accesses
- `src/FLPQ.LinearAlgebra/LinearAlgebra.fs` — update direct `.data` accesses
- `src/FLPQ.Languages/Automaton.fs` — update direct `.data` accesses
- `src/FLPQ.Languages/Cyk.fs` — update `.data` accesses + replace color strings with semantic labels
- `src/FLPQ.Languages/Valiant.fs` — update `.data` accesses
- `src/FLPQ.Languages/LRParser.fs` — update `.data` accesses
- `src/FLPQ.GraphAnalysis/Graph.fs` — update `.data` accesses
- `src/FLPQ.GraphAnalysis/MsBfs.fs` — update `.data` accesses
- `src/FLPQ.RPQ/ArroyueloRPQ.fs` — update `.data` accesses
- `src/FLPQ.RPQ/KroneckerRPQ.fs` — update `.data` accesses
- `src/FLPQ.RPQ/BelyaninRPQ.fs` — update `.data` accesses
- `src/FLPQ.Printers/MatrixTeX.fs` — update `.data` accesses + use semantic label→color mapping
- `src/FLPQ.Printers/ValiantTeX.fs` — replace color strings with semantic labels
- `src/FLPQ.Printers/AutomatonDot.fs` — update `.data` accesses
- `src/FLPQ.Printers/AutomatonTikz.fs` — update `.data` accesses
- `tests/FLPQ.GraphAnalysis.Tests/MsBfsTests.fs` — update `.data` accesses
- `tests/FLPQ.RPQ.Tests/RPQTests.fs` — update `.data` accesses
- `tests/FLPQ.RPQ.Tests/StressTests.fs` — update `.data` accesses
- `tests/FLPQ.Languages.Tests/ValiantTests.fs` — update `.data` accesses

### Semantic label design
```fsharp
type HighlightLabel = CurrentCell

type SubmatrixBlockLabel = SubmatrixRegion

// Color mapping in FLPQ.Printers only
let highlightColor : HighlightLabel -> string = function
    | CurrentCell -> "yellow"

let submatrixColors : SubmatrixBlockLabel -> string option * string option = function
    | SubmatrixRegion -> None, None  // colors determined by printer logic
```

### Equivalence
- All matrix operations must produce identical results
- Rendering output must be identical

## Task 123: Deduplicate Miscellaneous Helpers

### Items
1. `readIfExists`: Keep single public copy in `FLPQ.Printers`, remove from `FLPQ.Cli/Helpers.fs`, update CLI tests
2. `collectSteps`: Move to `FLPQ.Printers` as public function, use from both `SummaryTeX` and CLI `Helpers`
3. `termPrinter`: Extract into shared visualizer helper module
4. `escapeLabel`: Make public in `DerivationTreeDot`, use in `AutomatonDot`
5. `Grammar.nonterminalsOf`/`terminalsOf`: Already public, just update `code_review.md` to remove stale note
6. Unused `Automaton.buildDfaMatrix`: Already gone (skip)
7. Remove `GrammarTests.nonterminalsOfCnf`
8. Remove unused `StringArb` from Generators.fs
9. Remove unused `MyGen`/`MyArb` imports from test files (RsmToGrammarTests, EbnfParserTests) — but these come via `open FLPQ.TestUtilities`, so need to check if they're actually used for anything else
10. `ExternalToolsTests`: Replace `try...with _ -> ()` with `printfn` warnings
11. `Summary.AlgorithmKind`: Simplify to 3-case DU with `toString` member

### Equivalence
- CLI behavior identical after refactoring
- All golden tests pass

## Task 124: Rename Rhs Functions

### Renames
- `Rhs.toList` → `Rhs.toListWithEpsilon` (returns `[Epsilon]` for epsilon)
- `Rhs.toSymbols` → `Rhs.toNonEpsilonList` (returns `[]` for epsilon)

### Call sites to update
- `Grammar.fs` (3 call sites for each)
- `FirstFollow.fs` (3 call sites)
- `LLParser.fs` (2 call sites)
- `LRParser.fs` (12 call sites)
- `Valiant.fs` (2 call sites)
- `LLTableTeX.fs` (1 call site)
- `LRTableTeX.fs` (1 call site)
- `GrammarTests.fs` (3 call sites for toList, 2 for length)

### Equivalence
- Behavior identical after rename
