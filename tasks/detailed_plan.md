# Detailed Plan: Task 70 — Make nonterminalsOf/terminalsOf public

## Goal

Make `nonterminalsOf` and `terminalsOf` public in `Grammar.fs` and replace duplicates in other modules.

## Changes

1. **`src/FLPQ.Languages/Grammar.fs`**:
   - Change `private nonterminalsOf` → public (remove `private`)
   - Change `private terminalsOf` → public (remove `private`)

2. **`src/FLPQ.Printers/LLTableTeX.fs`**:
   - Remove private `nonterminalsOf` and `terminalsOf` (lines 43-54)
   - Replace usage with `Grammar.nonterminalsOf`/`Grammar.terminalsOf`
   - Note: `Grammar.nonterminalsOf` returns `Set`, `LLTableTeX.nonterminalsOf` returns `list`. Need to convert: `Set.toList`

3. **`tests/FLPQ.Languages.Tests/GrammarTests.fs`**:
   - The private `nonterminalsOfCnf` at line 162 only collects LHS nonterminals. The public `Grammar.nonterminalsOf` collects both LHS and RHS nonterminals. Different semantics. 
   - `nonterminalsOfCnf` should be left as-is or modified to use `Grammar.nonterminalsOf` if semantics match. Check usage...

## Verification
- Build check
- Test run
- Format check
