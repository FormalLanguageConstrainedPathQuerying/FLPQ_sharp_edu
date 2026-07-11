# Detailed Plan: Task 166 — Fix RNGLR SPPF DOT visualization

## Problem Analysis

RNGLR builds `PIntermediate` entries in different cells than `PNonterminal` entries.
When `processReduction` adds `PNonterminal(S)` to cell `(globalStart, vPre)→(finalRsmState, vEnd)`,
the intermediate decomposition entries are stored in intermediate cells along the `productBfs`
traversal path. The SPPF builder only sees `PNonterminal` in the root cell and cannot build
the tree because the intermediate entries are in other cells.

GLL correctly adds `PIntermediate` entries to the **same cell** as `PNonterminal`, enabling
recursive SPPF decomposition.

## S1: Add PIntermediate entries to block start cell in productBfs

**Code:** Modify `src/FLPQ.Languages/Rnglr.fs` — in `productBfs`, when a predecessor
at the start state is found, add `PIntermediate(globalCurrInv, vCurr)` to the block
start cell `(globalBlockStart, vNext)→(globalEnd, endVertex)`

**Tests:** New test in `tests/FLPQ.Languages.Tests/RnglrTests.fs` — SPPF DOT check for
`S -> a S b | S S | eps` with input `a a b a b b`, verifying that terminals a[0,1], a[2,3],
b[3,4], a[4,5], b[5,6] are present in the SPPF DOT output

**Docs:** No docs changes needed (algorithm bug fix)
