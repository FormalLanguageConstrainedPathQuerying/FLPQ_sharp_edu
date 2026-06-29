# Global Plan: Tasks 26—37

## Task Summary

| ID | Description | Type |
|----|-------------|------|
| 26 | Category traits for graphviz tests, CI improvements | Infrastructure |
| 27 | Improve graph/tree visualization tests with -Tplain parsing | Improvement |
| 28 | Grammar refactoring with NonEmptyList from FSharpPlus | Refactoring |
| 29 | Epsilon transitions in automata with NonEmptySet from FSharpPlus | Refactoring |
| 30 | Use non-empty-list/set where required across codebase | Refactoring |
| 31 | Split DFA and NFA on type level | Refactoring |
| 32 | Remove code duplication in visualization steps | Refactoring |
| 33 | Matrix visualization improvements (highlight cells, border/fill submatrix) | New feature |
| 34 | CYK visualization improvements (highlight modified cells per step) | New feature |
| 35 | Valiant steps visualization (boolean decomposition + recomposed matrix) | New feature |
| 36 | Console application with Argu CLI | New feature |
| 37 | TeX compilation tests with LaTeX template | Testing |

## Dependencies

```
26 (categories+CI) ── 27 (Tplain tests)
                      │
28 (NonEmptyList) ── 29 (NonEmptySet) ── 30 (apply NEL/NES)
                                      │
                                      31 (DFA/NFA split) depends on 29
                                      │
32 (dedup viz) ────────────────────── independent
                                      │
33 (Matrix viz improve) ── 34 (CYK viz) ──┬── 36 (Console app)
                          35 (Valiant viz)┘         │
                                                    37 (TeX tests)
```

- **26 must come first**: introduces Trait categories used by subsequent test improvements.
- **27 depends on 26**: uses the category system.
- **28 must come before 29, 30, 31**: FSharpPlus package must be added first.
- **29 depends on 28**: uses FSharpPlus types.
- **30 depends on 28+29**: applies new types across codebase.
- **31 depends on 29**: epsilon transitions are part of the NFA/DFA distinction.
- **32 is independent**: only touches visualization modules.
- **33 is independent**: only touches Matrix module.
- **34 depends on 33**: needs matrix highlight API.
- **35 depends on 33+34**: follows the same visualization pattern.
- **36 depends on 34+35**: console app writes visualization steps.
- **37 depends on 36**: reuses console app output structure.

## Execution Order

1. **Task 26** — Category traits for graphviz tests + CI
2. **Task 27** — Improve visualization tests with -Tplain parsing
3. **Task 28** — Grammar refactoring with NonEmptyList from FSharpPlus
4. **Task 29** — Epsilon transitions in automata with NonEmptySet
5. **Task 30** — Apply non-empty-list/set where required
6. **Task 31** — Split DFA and NFA on type level
7. **Task 32** — Remove code duplication in visualization steps
8. **Task 33** — Matrix visualization improvements
9. **Task 34** — CYK visualization improvements
10. **Task 35** — Valiant steps visualization
11. **Task 36** — Console application with Argu CLI
12. **Task 37** — TeX compilation tests

## Potential Conflicts

- Tasks 28-31 touch Grammar.fs, Automaton.fs, and many downstream files — must be done in sequence.
- Task 29 changes the Automaton type — affects AutomatonVisualizer.fs, LLParser.fs, LRParser.fs.
- Task 31 further changes Automaton type — downstream impact.
- Task 32 changes LLVisualizer.fs and LRVisualizer.fs — these also get touched by 36 (console).
- Tasks 33-35 add new features to Matrix.fs, Cyk.fs, Valiant.fs — potential merge conflicts if done in parallel.
- Task 36 creates new project (console app) with new fsproj and .slnx modification.
- Task 37 creates TeX template files and test files.

## Shared Infrastructure

- **Trait categories** (task 26): `"Graphviz"`, `"TeX"` — used by tests in 27, 37.
- **FSharpPlus** (task 28): shared package across all projects.
- **Matrix highlight** (task 33): used by 34, 35.
- **Common I/O helpers** (task 36): grammar/file reading — could be shared with existing parseGrammarFromFile, Tokenizer.

## Architecture Alignment

### Task 26
- Add `[<Trait("Category", "Graphviz")>]` to all existing graphviz-dependent tests.
- Add GitHub Actions workflow (.github/workflows/ci.yml) with graphviz install on ubuntu.
- Split test runs: all tests (excluding Graphviz) on all OS, Graphviz tests on ubuntu only.

### Task 27
- Enhance `TestUtils.checkDotCompiles` to return parsed -Tplain info.
- Add assertions on node/edge counts in existing tests.

### Task 28
- Add FSharpPlus NuGet package to both src projects and test projects.
- Change `Rule.rhs` from `Symbol<'t,'nt> list` to `NonEmptyList<Symbol<'t,'nt>> option` where `None` means epsilon.
- Actually, re-reading the task: "Right part of rule is non empty list of symbols or epsilon."
  Better: `type Rhs<'t,'nt> = Rhs of NonEmptyList<Symbol<'t,'nt>> | EpsilonRhs`
  Or keep `Rule<'t,'nt> = { lhs: Nonterminal<'nt>; rhs: Rhs<'t,'nt> }`

### Task 29
- Add epsilon transition explicitly: `transitions` becomes a record with epsilon edges.
- Or: change `transitions: Matrix<Set<'t>>` to use `NonEmptySet<'t>` instead of `Set<'t>`.
- Actually the task says "Introduce epsilon transition in finite automata explicitly. Similarly to grammars." — so add explicit epsilon transitions field.

### Task 30
- Scan codebase for places where lists/sets should never be empty.
- Apply NonEmptyList/NonEmptySet where semantically required.

### Task 31
- Create separate types: `NFA<'t,'s>` and `DFA<'t,'s>`.
- DFA: exactly one start state, no epsilon transitions, deterministic transitions.
- NFA: set of start states, epsilon transitions allowed.

### Task 32
- LLParser.parse and LLVisualizer.visualizeSteps share parsing logic — consolidate.
- LRParser.parse and LRVisualizer.visualizeSteps share parsing logic — consolidate.
- Collect visualization info during regular parse execution.

### Task 33
- Add `toTeX` overload with highlighting: list of (row, col, color) tuples.
- Add `toTeX` overload with submatrix border/fill: (startRow, startCol, rows, cols, borderColor, fillColor).
- Use nicematrix `\Block` commands for submatrices.

### Task 34
- In `Cyk.tableTrace`, track which cells were modified at each step.
- Use new Matrix.toTeX highlight API to highlight modified cells.
- Add `Cyk.toTeXWithHighlight` or modify existing `tableToTeX`.

### Task 35
- Add `Valiant.parseWithTrace` returning step-by-step visualization data.
- Each step shows: boolean decomposition matrices (with true=1, false=·), recomposed matrix.
- Highlight processed submatrices with colors.

### Task 36
- New project: `src/FLPQ.Cli/FLPQ.Cli.fsproj` (console app).
- Add Argu package reference.
- CLI interface: algorithm name, input files, output directory.
- For each algorithm: read grammar file, read input string file, write visualization steps to subdirectories.
- Common helpers: read grammar from file, read string from file, write TeX/Dot files.

### Task 37
- Create LaTeX template file in `tests/` or `data/`.
- Add tests that compile TeX files using pdflatex/lualatex.
- Mark with `[<Trait("Category", "TeX")>]`.
- These tests run only locally, not in CI.
