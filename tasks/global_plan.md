# Global Plan: Tasks 52—55

## Task Summary

| ID | Description | Type |
|----|-------------|------|
| 52 | Modified Valiant algorithm (V-shaped layers, batched multiplications) | Feature |
| 53 | RSM (Recursive State Machine) type | Feature (new type) |
| 54 | EBNF grammar reading and RSM construction (Brzozowski derivatives) | Feature |
| 55 | DFA to CFG and RSM to BNF grammar conversion | Feature |

## Dependencies

```
Task 52 (Modified Valiant)     independent
Task 53 (RSM type)             independent, prerequisite for 54, 55
Task 54 (EBNF parsing + RSM)  depends on 53
Task 55 (RSM to BNF)           depends on 53, 54
```

## Potential Conflicts

| Task | Files Modified/Created | Conflicts With |
|------|----------------------|----------------|
| 52 | `Valiant.fs` (add new functions), `ValiantTests.fs` (add tests), `TexCompilationTests.fs` (add TeX tests) | None |
| 53 | New `RSM.fs` (new module), `FLPQ.Languages.fsproj` (add Compile), `Automaton.fs` (no changes expected) | None |
| 54 | New `EbnfParser.fs`, modifications to `RSM.fs`, `FLPQ.Languages.fsproj` (add Compile + NuGet ref for FParsec) | Minor: 53 created `RSM.fs` |
| 55 | New `RsmToGrammar.fs` or modifications to `RSM.fs`, `FLPQ.Languages.fsproj`, `TestGrammars.fs` (add EBNF grammars) | 53, 54 created RSM modules |

## Shared Infrastructure

- All tasks use the existing `Grammar` and `Automaton` types
- Task 52 uses existing `BooleanDecomposition` and `Valiant.Submatrix` helpers
- Task 53 reuses `DFA<'t,'s>` from `Automaton.fs`
- Task 54 uses FParsec (new NuGet dependency) and Brzozowski derivatives
- Task 55 uses `RSM` type (from 53), `DFA` type (from Automaton), `Grammar` type, and right-linear grammar construction pattern from the book
- All test infrastructure (xUnit, FsCheck, FSharpPlus, TeX compilation) already exists

## Execution Order

1. **Task 52** — Modified Valiant (independent, no dependencies on 53-55)
2. **Task 53** — RSM type (prerequisite for 54 and 55)
3. **Task 54** — EBNF parsing and RSM construction (needs RSM type from 53)
4. **Task 55** — RSM to BNF conversion (needs RSM from 53 and EBNF parser from 54)

## Task Details

### Task 52: Modified Valiant
- **Source**: Book Chapter 7, `02_Valiant.tex`, subsection "Модифицированный алгоритм"
- **Reference paper**: `10.1007/978-3-030-63061-4_17`
- **Key design**: V-shaped layers of disjoint equal-size submatrices. `constructLayer(i)` builds layer of size 2^i. `completeVLayer(M)` processes all submatrices in a layer with batched multiplications.
- **Reuse**: Boolean decomposition (`BooleanDecomposition.decompose/recompose`), Submatrix helpers (bottomSubmatrix, leftSubmatrix, rightSubmatrix, topSubmatrix, rightGrounded, leftGrounded, sshift, extractSlice, writeSlice), performMultiplications (already accepts list of tasks).
- **New types/functions**: `parseModified`, `parseModifiedWithTable`, `parseModifiedWithTrace` (returning trace steps with submatrix layer information for visualization).
- **Visualization**: Each layer shows the T matrix with disjoint submatrices highlighted. Use different colors per submatrix. Show both bool decomposition and recomposed matrices.
- **Tests**: Property-based — for any grammar and input string, standard Valiant and modified Valiant must return identical results (acceptance status and final table). TeX compilation tests for visualization.

### Task 53: RSM Type
- **Source**: Book Chapter 6, `03_RecursiveAutomata.tex`
- **Type**: `RSM<'t, 'nt when 't: comparison and 'nt: comparison>` with blocks, start block, sets of terminals/nonterminals/states/start states.
- **Block type**: `RsmBlock<'t, 'nt>` wrapping a DFA over `Σ ∪ Q_S` (terminals + start states of other blocks).
- **Reuse**: `DFA<'t,'s>` from Automaton.fs for block internals. `Nonterminal<'nt>`, `Terminal<'t>` from Grammar.fs.
- **New types**: `RsmTransitionSymbol` or similar to represent the alphabet `Σ ∪ Q_S`, `RsmBlock`, `RSM`.

### Task 54: EBNF Parsing and RSM Construction
- **Source**: Book Chapter 6, `02_EBNF.tex`
- **Input format**: `.ebnf` files. Nonterminals in PascalCase, terminals in camelCase. Operators: `|`, `*`, `+`, `?`, `(…)`, `eps`.
- **Stages**:
  1. Parse EBNF using FParsec into regex AST (Epsilon, Terminal, Nonterminal, Concatenate, Alternative, Star, Plus, Optional)
  2. Group rules by nonterminal: join right-hand sides with `|`
  3. For each nonterminal, build DFA using Brzozowski derivatives (reference: `https://github.com/gsvgit/CFPQ_GLL/blob/Parsing/CFPQ_GLL/RsmBuilder.fs`)
  4. Relabel nonterminal transitions to use block start states
  5. Assemble RSM
- **New NuGet dependency**: FParsec
- **Tests**: Grammar `S -> eps` (1 block, 1 state, no transitions, start=final), `S -> a*`, `S -> a b`, `S -> (a S b)*`, expression grammar, etc. Dot visualization compilation for RSM blocks.

### Task 55: RSM to BNF Conversion
- **Source**: Book Chapter 5, `06_LinearGrammars.tex` (DFA→right-linear grammar) + Chapter 6, `02_EBNF.tex` (Theorem `thm:ebnf_cfg`)
- **Algorithm**:
  1. For each RSM block, convert DFA to right-linear grammar
  2. Transitions on terminals → right-linear rules
  3. Transitions on block start states → rules with referenced nonterminal
  4. Final states → epsilon rules
  5. Identify block start nonterminal with grammar nonterminal
- **Tests**: Round-trip property tests: grammar loaded as BNF vs. EBNF-converted-to-RSM-converted-back-to-BNF must produce identical parsing results (CYK, Valiant, Modified Valiant, LL, LR). Dyck language round-trip test.

## Files to Create/Modify

### Created:
- `src/FLPQ.Languages/RSM.fs` (tasks 53, 54, 55)
- `src/FLPQ.Languages/EbnfParser.fs` (task 54) 
- `src/FLPQ.Languages/RsmToGrammar.fs` (task 55) — or integrate into `RSM.fs`

### Modified:
- `src/FLPQ.Languages/Valiant.fs` — add `parseModified`, `parseModifiedWithTable`, `parseModifiedWithTrace`
- `src/FLPQ.Languages/FLPQ.Languages.fsproj` — add new .fs files, add FParsec package reference
- `tests/FLPQ.Languages.Tests/ValiantTests.fs` — add modified Valiant tests
- `tests/FLPQ.Languages.Tests/TexCompilationTests.fs` — add modified Valiant TeX tests
- `tests/FLPQ.Languages.Tests/TestGrammars.fs` — add EBNF grammars

### New Test Files:
- `tests/FLPQ.Languages.Tests/RSMTests.fs`
- `tests/FLPQ.Languages.Tests/EbnfParserTests.fs`
- `tests/FLPQ.Languages.Tests/RsmToGrammarTests.fs`
