# Global Plan: Tasks 21, 22, 23, 24, 25

## Task Summary

| ID | Description | Type |
|----|-------------|------|
| 21 | Make LL and LR parsers generic over symbol types | Refactoring |
| 22 | Automata visualization via Graphviz dot | New feature |
| 23 | Derivation tree visualization via Graphviz dot | New feature |
| 24 | LL parser step visualization (tree + stack TeX + input TeX) | New feature |
| 25 | LR parser step visualization (tree + stack TeX + input TeX) | New feature |

## Dependencies

```
21 (generics refactoring) ─────────────────────────┐
                                                    │
22 (automata viz) ──┐                                │
                    ├── 24 (LL step viz)              │
23 (tree viz) ──────┘                                │
                    │                                  │
                    └── 25 (LR step viz)              │
```

- **21 must come first**: it changes parser signatures (Grammar types, table types). Visualizations in 24-25 must work with the final generic types.
- **22 and 23 are independent** of each other and can be done after 21.
- **24 depends on 22 + 23**: needs automata dot rendering and tree dot rendering.
- **25 depends on 22 + 23**: same as 24 but for LR.

## Execution Order

1. **Task 21** — Make LL/LR parsers generic over `'t`, `'nt`
2. **Task 22** — Automata dot visualization
3. **Task 23** — Derivation tree dot visualization
4. **Task 24** — LL parser step visualization
5. **Task 25** — LR parser step visualization

## Potential Conflicts

- Tasks 24 and 25 both introduce a struct type for visualization results — could be shared.
- Tasks 22 and 23 both use Graphviz dot — shared infrastructure (e.g., a `DotHelper` module for common patterns, file output, compilation testing).
- Task 21 touches LLParser.fs and LRParser.fs — these same files are touched by tasks 24 and 25 for visualization.

## Architecture Alignment

### Task 21 (generics)
- `LLParser.fs`: make `buildTable`, `parse` generic over `Grammar<'t,'nt>`
- `LRParser.fs`: make all table builders and `parse` generic over `Grammar<'t,'nt>`
- Tokenization: parameterize by `string -> 't` mapping

### Task 22 (automata viz)
- New module `AutomatonVisualizer` in `src/FLPQ.Languages/`
- Function: `toDot: (int -> 's -> string) -> Automaton<'t,'s> -> string`
- New test file `tests/FLPQ.Languages.Tests/AutomatonVisualizationTests.fs`
- Verification: write dot to temp file, run `dot -Tplain` to check compilation

### Task 23 (tree viz)
- New function in `DerivationTree` module: `toDot: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> string`
- New test file `tests/FLPQ.Languages.Tests/DerivationTreeVisualizationTests.fs`
- Verification: same dot compilation check

### Task 24 (LL step viz)
- New module `LLVisualizer` in `src/FLPQ.Languages/`
- Struct type `LLStep`, function `visualizeSteps` returning `LLStep list`
- Each step contains: tree dot string, stack TeX, input TeX with cursor
- New test file: `tests/FLPQ.Languages.Tests/LLVisualizerTests.fs`

### Task 25 (LR step viz)
- New module `LRVisualizer` in `src/FLPQ.Languages/`
- Struct type `LRStep`, function `visualizeSteps` returning `LRStep list`
- Same structure as task 24
- New test file: `tests/FLPQ.Languages.Tests/LRVisualizerTests.fs`

## Files to Create/Modify per Task

### Task 21
| File | Action |
|------|--------|
| `src/FLPQ.Languages/LLParser.fs` | Generic over `Grammar<'t,'nt>` |
| `src/FLPQ.Languages/LRParser.fs` | Generic over `Grammar<'t,'nt>` |
| `tests/FLPQ.Languages.Tests/LLParserTests.fs` | Update for generic API |
| `tests/FLPQ.Languages.Tests/LRParserTests.fs` | Update for generic API |

### Task 22
| File | Action |
|------|--------|
| `src/FLPQ.Languages/AutomatonVisualizer.fs` | NEW |
| `src/FLPQ.Languages/FLPQ.Languages.fsproj` | Add compile entry |
| `tests/FLPQ.Languages.Tests/AutomatonVisualizationTests.fs` | NEW |
| `tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj` | Add compile entry |
| `docs/automaton.md` | Update |

### Task 23
| File | Action |
|------|--------|
| `src/FLPQ.Languages/DerivationTree.fs` | Add `toDot` function |
| `tests/FLPQ.Languages.Tests/DerivationTreeVisualizationTests.fs` | NEW |
| `tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj` | Add compile entry |
| `docs/derivation-tree.md` | Update |

### Task 24
| File | Action |
|------|--------|
| `src/FLPQ.Languages/LLVisualizer.fs` | NEW |
| `src/FLPQ.Languages/FLPQ.Languages.fsproj` | Add compile entry |
| `tests/FLPQ.Languages.Tests/LLVisualizerTests.fs` | NEW |
| `tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj` | Add compile entry |

### Task 25
| File | Action |
|------|--------|
| `src/FLPQ.Languages/LRVisualizer.fs` | NEW |
| `src/FLPQ.Languages/FLPQ.Languages.fsproj` | Add compile entry |
| `tests/FLPQ.Languages.Tests/LRVisualizerTests.fs` | NEW |
| `tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj` | Add compile entry |
