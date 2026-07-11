# Detailed Plan — Task 164

**Task**: GLL fail with infinite loop when handling grammar `S -> a S b | S S | eps` and string `a b`. Fix this problem. Add respective test.

## Root Cause Analysis

For grammar `S -> a S b | S S | eps`, the RSM DFA for block S has a nonterminal transition S (from `S S` rule) that points back to the same block. When S can match epsilon (both `eps` alternative and `S S` produce empty), this creates an infinite descriptor chain:

1. State qX in block S processes nonterminal transition S → creates GSS edge → call descriptor enters start of S
2. Start of S reaches final (epsilon) → saves matched range to storedPops
3. Continuation descriptor arrives at state qY in block S (same block)
4. At qY, nonterminal transition S fires again → addEdge returns storedPops → another continuation
5. Each iteration creates a new descriptor with a different MatchedRange but same vertex → infinite loop

The `handled` set uses full `Descriptor` including `MatchedRange` as key, and each iteration extends `MatchedRange` differently, so the set never blocks the cycle.

## Fix Strategy

Add epsilon-cycle tracking: maintain a `HashSet<int * int * int>` of `(RsmState, Vertex, GssIdx)` for descriptors carrying a NonEmpty MatchedRange. Before enqueuing such a descriptor, check if the triple was already seen — if so, skip.

This is safe because two descriptors with the same `(RsmState, Vertex, GssIdx)` represent the same caller context. Processing the same triple again without advancing the vertex would only duplicate information already in the path index.

## Subtasks

### S1: Fix GLL infinite loop

**Code:** `src/FLPQ.Languages/Gll.fs` — add epsilon-cycle tracking in `buildPathIndex`
**Tests:** None (verified by S2)
**Docs:** None

**Spec:**
- Add `let handledNonEmpty = HashSet<int * int * int>()` alongside existing `handled`
- In `tryEnqueue`, before `handled.Add(d)`, check if the descriptor's MatchedRange is NonEmptyRange. If yes, check `handledNonEmpty.Add(d.RsmState, d.Vertex, d.GssIdx)` — if returns false, skip (already processed this triple with a non-empty range)
- The check must not apply to EmptyRange descriptors (call descriptors at block start must always be enqueued)
- All existing tests must pass

### S2: Add test for the problematic case

**Code:** `tests/FLPQ.Languages.Tests/GllTests.fs` — add [<Fact>] test
**Tests:** New test case
**Docs:** None

**Spec:**
- Add test: `S -> a S b | S S | eps rejects a b`
- Grammar: `TestGrammars.grammar2` (which is `S -> a S b | S S | eps`)
- Input: `["a"; "b"]`
- Expected: `gllAccepts g ["a"; "b"]` = false (string "ab" is not in the language)
- The test must terminate (verifies the infinite loop is fixed)
