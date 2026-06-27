# Detailed Plan: Task 005 — CNF Transformation

## Overview
Transform a BNF grammar into Chomsky Normal Form. CNF rules:
- A -> BC (two nonterminals)
- A -> a (one terminal)
- S0 -> eps (only start symbol, only if language contains epsilon)

## 1. Implementation in `Grammar.fs`

Add functions to the `Grammar` module:

### `toCnf: Grammar<string, string> -> Grammar<string, string>`

Steps:
1. **START**: Add new start nonterminal S0 -> S_old if needed
2. **TERM**: Replace terminals in rules with length > 1: for each terminal `t`, create Nt -> t, replace t with Nt in all rules where rhs length > 1
3. **BIN**: Eliminate long rules: replace A -> X1 X2 ... Xk (k > 2) with binary productions
4. **DEL**: Eliminate epsilon-productions (except for start)
5. **UNIT**: Eliminate unit productions
6. **Remove unreachable and unproductive nonterminals**

Order (standard Hopcroft-Ullman): START → DEL → UNIT → TERM → BIN → cleanup

### Helper functions

- `eliminateEpsilon`: Remove eps rules. Compute nullable nonterminals, then for each rule with nullable symbols on RHS, add all combinations.
- `eliminateUnit`: Remove unit productions. For each A → B, add all rules B → α.
- `replaceTerminals`: For each terminal in rules with |rhs| > 1, create new nonterminal.
- `binarize`: Break rules with |rhs| > 2 into binary rules.
- `freshNonterminal`: Generate unique nonterminal names.

## 2. Files

| File | Action |
|------|--------|
| `src/FLPQ.Core/Grammar.fs` | Modify — add CNF transformation functions |
| `tests/FLPQ.Core.Tests/GrammarTests.fs` | Modify — add CNF tests |
| `docs/grammar.md` | Modify — document CNF transformation |
| `tasks/tasks.md` | Mark task 5 done |
