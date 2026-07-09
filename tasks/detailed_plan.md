# Task 149: Fix empty-body RNGLR tests and underlying algorithmic issues

## Problem

`RnglrTests.fs` lines 175-188 contain three `[<Fact>]` tests with empty bodies `()`. The comments claim "grammar2 with S -> S S creates unbounded DFA states" but:

1. First two tests (`S -> a S b | eps accepts/rejects`) don't involve S -> S S at all
2. Third test has S -> S S, but "right part of production is a valid regular expression, DFA must be constructed correctly by derivatives"

Root cause: `Rnglr.isAccepted` was too permissive — it checked for any PNonterminal entry where `toVertex = vertexCount - 1`, without requiring `fromVertex = 0`. This allowed intermediate nonterminal recognitions (like S matching substring positions 1..3 in "aab") to trigger false acceptance.

The spurious reduction chain: at state 1 after shifting "a", closure-predicted item (S, 0) triggers ε reduction → S recognized at (1,1). Combined with shifts, this creates a partial parse `S → a ε b = ab` spanning (1,3), which produces PNonterminal at [(1,1),(4,3)]. The old `isAccepted` found this because `toVertex = 3 = vertexCount - 1`.

## Fix

Modified `Rnglr.isAccepted` to require `fromVertex = 0` (i.e., `i % vertexCount = 0`) in addition to `toVertex = vertexCount - 1`. This filters out intermediate nonterminal recognitions that don't span the entire input.

## Subtasks

### S1: Fix `isAccepted` to check `fromVertex = 0` [done]
- Modified `Rnglr.isAccepted` to only consider entries where `i % vertexCount = 0`
- Test "S -> a S b | eps rejects a a b" now passes

### S2: (not needed) Kernel item tracking [cancelled]
- Initially attempted but caused 26 test failures. The kernel approach is too strict — it prevents valid ε reductions from closure-predicted items needed for correct parses.
- The `isAccepted` fix alone resolves the false acceptance without affecting the parse engine.

### S3: Make the three tests meaningful [done]
- Test 1: `S -> a S b | eps accepts a a b b` — grammar with two rules, assert acceptance
- Test 2: `S -> a S b | eps rejects a a b` — same grammar, assert rejection
- Test 3: `S -> a S b | eps | S S accepts a b a b` — uses TestGrammars.grammar2, assert acceptance

### S4: Quality gates [done]
- Format: passes (fantomas --check: 0 formatting issues)
- Lint: no new warnings (2 existing FL0067 warnings for K, unchanged)
- Build: succeeds
- Full test suite: 0 failed, 0 skipped across all projects

## Progress
- S1: done
- S2: cancelled (kernel approach too strict, isAccepted fix alone sufficient)
- S3: done
- S4: done
