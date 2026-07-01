# Detailed Plan: Task 67 — Merge modified Valiant trace + table

## Goal

Eliminate code duplication between `parseModifiedWithTable` and `parseModifiedWithTrace`.
The trace version already produces a table at each step. Extract the final table from the last trace step.

## Changes

### Modified Valiant
1. Extract shared initialization into a private helper (already partially exists as `initValiant`)
2. Create single private `parseModifiedInternal` that does computation and collects trace
3. `parseModifiedWithTable` calls internal, extracts last step's table, computes acceptance from it
4. `parseModified` calls `parseModifiedWithTable` and returns `snd`
5. `parseModifiedWithTrace` calls internal and returns trace steps

### Standard Valiant
Apply the same pattern:
1. `parseWithTable` calls trace version, extracts last step's table, computes acceptance
2. `parse` calls `parseWithTable`
3. Keep `parseWithTrace` as primary computation (it already has traces)

Actually for standard Valiant the trace is via `ResizeArray` option, which is already a clean pattern.
The main duplication is in modified Valiant. Let me focus there and also clean up standard Valiant.

## Acceptance computation from table

The final trace step contains `table: Matrix<Set<Nonterminal<'nt>>>` (n×n).
Acceptance: `Set.contains cnfStart table.data.[0, n-1]`

For the empty string case: handle separately as before.
