# Detailed Plan: Task 72 — LL(2) parsing tests

## Goal

Add tests for LL(2) parsing with a grammar that requires 2-token lookahead.

## Grammar

```
S -> S1 | S2
S1 -> a b S c
S1 -> eps 
S2 -> a x S y
S2 -> eps
```

LL(1) detects a conflict (both S1 and S2 start with 'a'), LL(2) works.

Accept: "", abc, axy, ababcc, axaxyy, axabcy, abaxyc
Reject: a, x, y, c, axc, aby, axab, abaxy, axabc, axaby

## Changes

1. TestGrammars.fs: add grammar definition + accept/reject lists
2. LLParserTests.fs: add LL(1) conflict test, LL(2) no-conflict test, LL(2) accept/reject tests, leaf matching test
