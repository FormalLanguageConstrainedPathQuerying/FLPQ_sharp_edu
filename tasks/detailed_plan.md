# Detailed Plan: Task 73 — LL(2) parsing tests (grammar with S -> eps directly)

## Goal

Add tests for LL(2) parsing with a grammar where `S -> eps` is on the top-level nonterminal
(rather than on S1 and S2 separately). This grammar specifies the same language as grammar9.

## Grammar

```
S -> S1
S -> S2
S1 -> a b S c
S -> eps
S2 -> a x S y
```

The key difference from grammar9 (task 72): instead of `S1 -> eps` and `S2 -> eps`,
only `S -> eps` is present. S1 and S2 are non-nullable directly; they derive their
terminals only through S.

## Language Analysis

Both grammar9 and grammar10 specify the same language (balanced sequences where
`ab` pairs with `c` and `ax` pairs with `y`). The accept/reject string lists from
grammar9 are reused.

### LL(2) analysis

FIRST_1(S) = {a, eps} (S -> S1/S2 give 'a', S -> eps)
FIRST_2(S1) = {aa, ab} (from "a b S c": S gives eps→"ab", S gives a...→"aa")
FIRST_2(S2) = {aa, ax} (from "a x S y": S gives eps→"ax", S gives a...→"aa")

LL(2) table for S:
- S -> S1: lookahead = {aa, ab}
- S -> S2: lookahead = {aa, ax}
- Conflict on "aa" — grammar10 is NOT LL(2)

Both LL(1) and LL(2) tables will detect a conflict.

## Changes

1. TestGrammars.fs:
   - Add `grammar10` grammar definition
   - Add `grammar10Accept` (alias for grammar9Accept, same strings)
   - Add `grammar10Reject` (alias for grammar9Reject, same strings)
   - Add `augGrammar10`

2. LLParserTests.fs (FactTests module):
   - `LL(1) table for grammar10 detects conflict`
   - `LL(2) table for grammar10 also detects conflict`
   - `Valiant and CYK agree on grammar10 acceptance`
   - `Valiant parseWithTable for grammar10 returns correct dimension`
