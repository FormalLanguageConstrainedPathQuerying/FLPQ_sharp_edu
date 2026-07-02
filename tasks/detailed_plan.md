# Detailed Plan: Task 91 — Improve LL and LR steps visualization

## Goal

Do not split trees and stack. Render combined stack-tree structure directly as returned from parsing algorithm.

## Changes

### 1. `src/FLPQ.Languages/VisualizationTypes.fs`
- Remove `tree` field from `LLParsingStep` — it now has only `stack` and `input`
- Remove `tree` field from `LRParsingStep` — it now has only `stack` and `input`

### 2. `src/FLPQ.Languages/LLParser.fs`
- Simplify `recordStep` — no longer compute `currentTree` from `completed`
- Step record: `{ stack = stack; input = { tokens = tokens; position = pos } }`

### 3. `src/FLPQ.Languages/LRParser.fs`
- Simplify `recordStep` — no longer extract trees from LRSymbol and build synthetic `currentTree`
- Step record: `{ stack = stack; input = { tokens = tokens; position = pos } }`

### 4. `src/FLPQ.Printers/DerivationTreeDot.fs`
- Replace `toDotWithStack` with `toDotWithLLStack` that takes only `LLStackFrame list` (no separate tree param)
  - For each LLFrame, render its tree node (Leaf or Node with children)
  - Connect frames via dashed chain, set rank=same
- Replace `toDotWithLRStack` signature: remove `tree` param, take only `LRStackFrame list`
  - For each LRSymbol, render its full subtree
  - For each LRState, render labeled box
  - Connect all frames via dashed chain, set rank=same

### 5. `src/FLPQ.Printers/LLStepVisualizer.fs`
- Update to use `toDotWithLLStack` with just the stack

### 6. `src/FLPQ.Printers/LRStepVisualizer.fs`
- Update to use updated `toDotWithLRStack` with just the stack

## Verification
- All tests pass
- Check formatting
