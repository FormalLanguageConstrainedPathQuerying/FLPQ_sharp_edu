# Detailed Plan: Tasks 138–140 — RNGLR Implementation and Refactoring

## Goal
Task 138: Implement RNGLR (Right-Nulled Generalized LR) for RSM.
Task 139: Fix issues in RNGLR — regex tests, fixpoint logic, extended RSM.
Task 140: Refactor findPredecessors to classical automata intersection.

## Completed

### Task 138 — RNGLR Implementation
- RnglrTypes.fs: RnglrItem, RnglrAction, RnglrTable, RnglrGssVertex, RnglrGssEdge, RnglrGSS
- RnglrLR.fs: LR(0) table construction over RSM blocks (closure, goto, buildLR0Table)
- Rnglr.fs: buildPathIndex with recursive cascade, product BFS, isAccepted
- RSM.fs: RsmStateInfo, FlattenedRsm, flattenRsm, extendWithStart
- 25 tests: acceptance (13), equivalence (4), right-nullable (3), reduction cascade (1), regex equivalence (4)
- Reuses SPPF/PathIndex types from GllTypes.fs

### Task 139 — Fixes
1. RSM.extendWithStart — creates augmented start block S' -> S (done in task 138)
2. Fixpoint logic — recursive processNode cascade with depth limit (done in task 138)
3. Regex-DFA equivalence tests — enabled all 4 ([<Property(MaxTest = 50)>])

### Task 140 — Refactoring
1. Precompute inverted RSM blocks with `invertRsmTransitions` returning `Map<(int * sym), int list>` for efficient BFS lookup (supports multiple incoming transitions per symbol)
2. Extracted `productBfs` function — standard automaton product BFS over (gssIdx, invState) pairs
3. Replaced `storedReductions` with `storedStates` array (`Set<(Nonterminal<'nt>, int)>` per GSS vertex)
4. Dedup via per-vertex `processedGotos` array
5. Simplified `RnglrGSS.addEdge` to return only `Set<Nonterminal * int>`
6. Shift-time storedStates consumption uses productBfs (replaces old extendProduct)

### Design decisions
- `invertRsmTransitions` uses `int list` values because inverted DFA transitions can be non-deterministic (multiple states may have incoming transitions on the same symbol)
- `productBfs` explores (gssIdx, invState) pairs starting from given start pairs; stores intermediates via storedStates; returns both predecessors (invState = block start) and visited intermediates
- Dedup is per (gotoGssIdx, nt, gssIdxPre) — uses Array of Sets indexed by linear GSS vertex index
- `processReduction` separates gotoVertex (where the reduction result is placed) from intermediateVertex (for PIntermediate annotation)

### Test results
All 634 tests pass (0 failures) across the full solution.
All 25 RNGLR tests pass (0 skipped, 4 regex property tests enabled).
