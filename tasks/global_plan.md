
# Global Plan: Tasks 111--113

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|-------------|
| 111 | Fix Valiant Seq.head crash on grammar without binary rules. Deduplicate Valiant initialization. Deduplicate empty-input epsilon-acceptance check. Equivalence: modified Valiant results must be identical to standard Valiant. | Refactor | None |
| 112 | Replace `string` (`obj.ToString()`) with printer-function parameters in `SymbolTeX.terminalContent` / `nonterminalContent` / `toLaTeX`. Cascade to all call sites. Golden tests must produce identical output. | Refactor | None |
| 113 | Deduplicate CYK core algorithm: extract shared parameterized helper with `onCellFound` callback. Replace mutable `HashSet<Nonterminal<'nt>>` with immutable `Set`. Equivalence: `cykTable` and `tableTrace` must produce identical acceptance results. | Refactor | None |

## Dependencies Graph

```
Task 111 (Valiant fixes + Grammar.isEpsilonAccepted) ── independent, touches Grammar.fs and Valiant.fs
Task 112 (SymbolTeX printer-function params)           ── independent, touches SymbolTeX.fs + all callers
Task 113 (CYK dedup + immutable Set)                   ── independent, touches Cyk.fs only
```

All three tasks are independent and can be done in any order. Recommended order: 111 → 112 → 113 because:

- Task 111 adds `Grammar.isEpsilonAccepted` — a small, isolated change to Grammar.fs + Valiant.fs.
- Task 112 is the largest cross-cutting change (SymbolTeX → 10+ files). Doing it second avoids conflicts with task 113's Cyk.fs changes.
- Task 113 modifies Cyk.fs internals — done last to avoid any conflict with the other two.

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 111 | `src/FLPQ.Languages/Grammar.fs` (add `isEpsilonAccepted`), `src/FLPQ.Languages/Valiant.fs` (dedup init, fix Seq.head, reuse `isEpsilonAccepted`), `src/FLPQ.Languages/Cyk.fs` (reuse `isEpsilonAccepted`) | Task 113 (both modify Cyk.fs) |
| 112 | `src/FLPQ.Printers/SymbolTeX.fs`, `src/FLPQ.Printers/GrammarTeX.fs`, `src/FLPQ.Printers/ParsingTableTeX.fs`, `src/FLPQ.Printers/CykTeX.fs`, `src/FLPQ.Printers/ValiantTeX.fs`, `src/FLPQ.Printers/LRAutomatonTikz.fs`, `src/FLPQ.Printers/LLTableTeX.fs`, `src/FLPQ.Printers/LRTableTeX.fs`, `src/FLPQ.Printers/LLStepVisualizer.fs`, `src/FLPQ.Printers/LRStepVisualizer.fs`, `src/FLPQ.Printers/TeXRenderer.fs`, `src/FLPQ.Printers/AutomatonTikz.fs`, `src/FLPQ.Printers/AutomatonDot.fs`, `src/FLPQ.Cli/CykRunner.fs`, `src/FLPQ.Cli/ValiantRunner.fs`, `src/FLPQ.Cli/LRRunner.fs`, `src/FLPQ.Cli/LLRunner.fs` | Task 111 (CykRunner.fs, ValiantRunner.fs may change) |
| 113 | `src/FLPQ.Languages/Cyk.fs` | Task 111 (both modify Cyk.fs) |

## Execution Order

1. **Task 111** — Valiant fixes + Grammar.isEpsilonAccepted (independent)
2. **Task 112** — SymbolTeX printer-function params (independent)
3. **Task 113** — CYK dedup + immutable Set (independent)

## Shared Infrastructure

- `Grammar.isEpsilonAccepted` added by task 111 will be reusable by all tasks that need epsilon-acceptance checks.
- `initValiant` (made reusable in task 111) eliminates duplicated initialization logic.
- Task 112's printer-function parameters will make SymbolTeX more generic and reusable.

## Detailed Task Plans

### Task 111 (Valiant fixes, dedup init, dedup epsilon check)

**Issue 1 — Seq.head crash on grammar without binary rules**:
- Location: `Valiant.fs` line 127: `let mat = pByPair.Values |> Seq.head`
- When grammar has no binary rules (e.g., `S -> a | b`), `pByPair` is empty, `Seq.head` crashes.
- Fix: handle the empty case in `performMultiplications` — if pairs list is empty, skip the multiplication loop.

**Issue 2 — Deduplicate Valiant init**:
- `parseModifiedWithTrace` lines 411-444 duplicate the same ~35 lines as `initValiant` (lines 320-362).
- Make `parseModifiedWithTrace` reuse `initValiant` by calling it for initialization and extracting the fields it needs.

**Issue 3 — Deduplicate epsilon-acceptance check**:
- Add `Grammar.isEpsilonAccepted` function:
  ```fsharp
  let isEpsilonAccepted (cnf: Grammar<'t, 'nt>) : bool =
      cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && Rhs.isEpsilon r.rhs)
  ```
- Replace 4 duplicate blocks in `Cyk.fs` (2 places) and `Valiant.fs` (2 places).

**Equivalence requirement**: All existing Valiant tests must pass after refactoring. Modified Valiant results must be identical to standard Valiant.

### Task 112 (SymbolTeX printer-function params)

**Change SymbolTeX signatures**:
```fsharp
let terminalContent (terminalPrinter: 't -> string) (Terminal t) : string = terminalPrinter t
let nonterminalContent (nonterminalPrinter: 'nt -> string) (Nonterminal nt) : string = nonterminalPrinter nt
let toLaTeX (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (sym: Symbol<'t, 'nt>) : string =
    match sym with
    | T t -> terminalContent terminalPrinter t
    | N nt -> nonterminalContent nonterminalPrinter nt
    | Epsilon -> @"\varepsilon"
```

**Cascade to all call sites** (preserving `string` for `string`-based usage):

1. `GrammarTeX.fs`:
   - `grammarToTeX`/`grammarToTeXWithNumbers`: pass `id` (or `string`) as printer functions
   - `nonterminalContent` call → add printer param

2. `ParsingTableTeX.fs`:
   - `ntCellToTeX`: add `nonterminalPrinter` parameter
   - Update `CykTeX`, `ValiantTeX` callers

3. `CykTeX.fs`:
   - `tableToTeXStyled`/`tableToTeX`: add printer params, pass to `ntCellToTeX`

4. `ValiantTeX.fs`:
   - `stepToTeX`, `boolDecompToTeX`, `modifiedStepToTeX`: add printer params

5. `LRAutomatonTikz.fs`:
   - `renderRhsWithDot`, `renderLR1StateContent`, `lr0AutomatontoTikz`, `lr1AutomatontoTikz`: add printer params

6. `LRRunner.fs`: pass `string`/`id` as printer functions
7. `LLRunner.fs`: pass `string`/`id` as printer functions
8. `CykRunner.fs`: pass `string`/`id` as printer functions
9. `ValiantRunner.fs`: pass `string`/`id` as printer functions

**Golden tests**: Must produce identical output after refactoring.

### Task 113 (CYK dedup + immutable Set)

**Deduplicate CYK core**: Extract a parameterized helper:
```fsharp
let private cykCore (cnf: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) (onCellFound: int -> int -> Set<Nonterminal<'nt>> -> unit) : unit
```
Both `cykTable` and `tableTrace` can be expressed in terms of this core.

**Replace mutable HashSet with immutable Set**: Use `Set.fold` or `Set.union` for accumulation instead of `HashSet.Add`.

**Equivalence**: `cykTable` and `tableTrace` must produce identical acceptance results after refactoring.
