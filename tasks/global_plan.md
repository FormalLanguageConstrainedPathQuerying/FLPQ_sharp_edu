# Global Plan: Tasks 81--82

## Task Summary

| ID | Description | Type | Status |
|----|-------------|------|--------|
| 81 | Implement automata acceptance algorithm (NFA + DFA) | Feature | Pending |
| 82 | Implement two automaton intersection (Kronecker + MS-BFS + filtering) | Feature | Pending |

## Dependencies

```
Task 81 ── independent, uses existing NFA/DFA types
Task 82 ── depends on Task 80 (diagonal filtering, already done), uses MS-BFS, Kronecker product, Graph.filterOutgoing/Incoming
```

Tasks 81 and 82 are fully independent of each other. Task 82 uses infrastructure from already-completed tasks 79 (Graph), 80 (diagonal filtering), and 57 (MS-BFS).

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 81 | `Automaton.fs`, `AutomatonTests.fs` | None |
| 82 | `Automaton.fs` (add intersection function), `FLPQ.Languages.fsproj` (possibly add new file), `AutomatonTests.fs` | Task 81 (same files) |

Both tasks modify `Automaton.fs` and `AutomatonTests.fs`, but the modifications are in distinct sections: Task 81 adds `accept` functions, Task 82 adds `intersect` function.

## Execution Order

1. **Task 81** — Automata acceptance:
   - Add `Config` type in `Automaton.fs`
   - Implement `Nfa.accept` (NFA with epsilon transitions, working set of configurations)
   - Implement `Dfa.accept` (simple DFA, no epsilon)
   - Add 12 test cases from task specification

2. **Task 82** — Automaton intersection:
   - Implement `Nfa.intersect` using Kronecker product + MS-BFS + filtering
   - The algorithm operates on NFAs without epsilon transitions (as stated in the task)
   - Steps: Kronecker product of transition matrices → forward MS-BFS from start pairs → backward MS-BFS from final pairs (on transposed Kronecker) → intersect forward and backward visited → filter edges
   - Property-based tests: intersection accepts strings accepted by both and rejects strings rejected by at least one

## Shared Infrastructure

- Both tasks use the existing `NFA` and `DFA` types from `Automaton.fs`
- Task 82 uses `LinearAlgebra.kron`, `MsBfs.msBfs`, `BooleanDecomposition.decomposeNonEmptySet`, `Graph.filterOutgoing`/`Graph.filterIncoming`, `Matrix.transpose`
- No new shared infrastructure needed

## Architecture Alignment

- **Acceptance** (Task 81): Classical textbook algorithm. The Config type captures (state, inputPosition). The working set processes configurations one at a time, expanding epsilon closures without advancing input.
- **Intersection** (Task 82): Linear-algebra approach using Kronecker product (same pattern as KroneckerRPQ). The result is an NFA without epsilon transitions — the product construction filtered by reachability from start and co-reachability to final states.
