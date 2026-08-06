# Global Plan: CNF Cleanup, Table Rendering, BasicSPPF (Tasks 240, 241, 242)

## Tasks

| ID | Title | Summary |
|----|-------|---------|
| 240 | Fix toCNF function | Add non-generating + unreachable nonterminal cleanup; add test functions and registry-wide tests |
| 241 | Improve CYK and Valiant table rendering | Render nonempty cells as `(nonterm, split_point, prod_id)` tuples |
| 242 | Improve BasicSPPF creation and visualization | Build only from start NT cell; remove edge labels; production stores split point; NT label format |

## Dependencies

- **240 is independent** — touches only `Grammar.fs` (toCNF + new cleanup helpers) and grammar tests.
- **241 depends on 240** only weakly — if 240 adds unreachable nonterminals cleanup, CNF tables may shrink, but SPPF table rendering is independent of cleanup logic. Tasks can proceed independently.
- **242 depends on 241** only weakly — 241 changes table rendering format; 242 changes SPPF construction from the same SPPF table. They touch different aspects of the same data but can be done independently.

## Execution Order

1. **Task 240** — simplest, self-contained, establishes test infrastructure for cleanup checks
2. **Task 241** — medium, adds trace types + rendering changes for CYK/Valiant
3. **Task 242** — medium, restructures SPPF node/edge types and rendering

All three are largely independent. Order chosen for incremental risk: fix core algorithm first (240), then improve visualization bottom-up (241 → 242).

## Overlapping Files

| File | 240 | 241 | 242 |
|------|-----|-----|-----|
| `Grammar.fs` | +removeNonGenerating, +removeUnreachable | — | — |
| `Cyk.fs` | — | +SppfCykTraceStep, +parseWithSppfTrace | — |
| `Valiant.fs` | — | +SppfValiantTraceStep, +parseWithSppfTrace | — |
| `ParsingTable.fs` | — | — (reuses SppfParsingTable) | — |
| `CykTeX.fs` | — | +sppfTableToTeXStyled (uses sppfEntryCellToTeX) | — |
| `ValiantTeX.fs` | — | +sppfStepToTeX, +sppfModifiedStepToTeX | — |
| `CykRunner.fs` | — | use sppfTrace instead of trace for rendering | — |
| `ValiantRunner.fs` | — | use sppfTrace for rendering | — |
| `BasicSppf.fs` | — | — | fromParsingTable (start NT only); change Prod type |
| `BasicSppfDot.fs` | — | — | new labels, no edge labels |
| `CykTests.fs` / `ValiantTests.fs` | — | test new trace functions | update fromParsingTable tests |
| `SppfPropertyTests.fs` | — | — | update SPPF construction tests |
| `BasicSppfDotTests.fs` | — | — | update golden tests |
| `GrammarTests.fs` or new test file | +cleanup tests | — | — |

## Reuse Analysis

### Task 240
- `Grammar.nonterminalsOf` — existing, used to enumerate all nonterminals
- `Grammar.terminalsOf` — existing
- `Grammar.computeNullable` — existing, can be reused for computing generating set
- No new external deps needed

### Task 241
- `ParsingTableTeX.sppfEntryCellToTeX` — **already exists** at `ParsingTableTeX.fs:35`, renders `(nt, k, prodIdx)` tuples. Task 241 just switches CYK/Valiant rendering from `ntCellToTeX` to `sppfEntryCellToTeX`.
- `SppfParsingTable<'nt>` — **already exists** at `ParsingTable.fs:17`
- `Cyk.parseWithSppfInfo` — **already exists**, returns `SppfParsingTable`
- `Valiant.parseWithSppfInfo` — **already exists**
- Need new SPPF-aware trace collection in CYK and Valiant

### Task 242
- `BasicSppf.fromParsingTable` — **already exists**, needs modification
- `BasicSppfDot.toDot` — **already exists**, needs modification
- `BasicSppfNodeInfo`, `BasicSppfEdgeLabel`, `BasicSPPF` — **already exist**
- Tree extraction functions rely on edge labels — need update when labels are removed
