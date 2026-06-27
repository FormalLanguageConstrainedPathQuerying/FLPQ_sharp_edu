# Detailed Plan: Task 011 — Add Arithmetic Expression Grammars

## Overview
Add 3 CFGs for arithmetic expressions (all specifying the same language). These serve as test data for parsing algorithms (CYK, Valiant, LL, LR).

## Grammars

1. **Ambiguous expression grammar**: `S -> x | S+S | S*S | (S)`
2. **Left-recursive precedence grammar**: `E->E+T|T`, `T->T*F|F`, `F->(E)|x`
3. **Right-recursive precedence grammar**: `E->T+E|T`, `T->F*T|F`, `F->(E)|x`

All three accept: `x`, `(x)`, `(x)*x`, `x+x`, `x+x*x`, `x*(x+x)`, `(x*(x+x))`
All three reject: `""`, `()`, `+x`, `x+`, `x+()`

## Changes

### `tests/.../TestGrammars.fs`
- Add `grammar6`, `grammar7`, `grammar8` (pre-parsed)
- Add `exprAccept`, `exprReject` lists

### `tests/.../CykTests.fs`
- Add `Grammar6Tests` module: 2 Facts testing all 3 grammars against accept/reject lists

## Files
| File | Action |
|------|--------|
| `tests/.../TestGrammars.fs` | Add grammars + data |
| `tests/.../CykTests.fs` | Add tests |
| `tasks/tasks.md` | Mark done |
