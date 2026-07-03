# Detailed Plan: Task 101 — LL Trees Visualization

## Problem

LL parser visualization currently shows only the stack frontier. Completed subtrees (parts of the tree that have already been matched and popped from the stack) are not visualized. The final derivation tree is flat (`Node(start, leafChildren)`), not properly nested.

## Goal

1. Build properly nested hierarchical derivation trees in the LL parser (not flat `Node` with leaf children)
2. Track completed subtrees in visualization step data
3. Render both stack frames AND completed subtrees in the DOT visualization

## Design Decisions

### Unified Stack with Markers

Introduce a marker variant in `LLStackFrame` to track nonterminal boundaries on the stack:

```fsharp
type LLStackFrame<'t, 'nt> =
    | LLTree of DerivationTree<'t, 'nt>
    | LLMarker of Nonterminal<'nt> * int  // nonterminal, expected child count
```

When expanding `A -> α1 α2 ... αm`:
- Push `LLTree(Leaf αi)` for each αi
- Push `LLMarker(A, m)` below them

When `LLMarker(nt, n)` is at the top of the stack:
- Pop it from stack
- Pop `n` trees from completed
- Build `Node(nt, reversed_popped_trees)`
- Add to completed

### Completed List Tracking

The `LLParsingStep` type gains a `completed` field containing roots of fully-built subtrees.

### DOT Visualization

The DOT renderer will show:
- Completed subtrees rendered as full trees (all children included) in a `cluster_completed` subgraph
- Stack frames rendered as before (dashed chain, same rank)
- Markers rendered as gray boxes

## Implementation Checklist

### 1. `VisualizationTypes.fs` — Type Changes
- [x] Change `LLStackFrame` from single-case DU to two-case DU with `LLTree` and `LLMarker`
- [x] Update `LLStackFrame` module helpers (symbol, tree, create)
- [x] Add `completed` field to `LLParsingStep`

### 2. `LLParser.fs` — Parser Logic
- [x] Modify `parseLoop` to push markers when expanding nonterminals
- [x] Handle `LLMarker` at top of stack: pop from completed, build Node
- [x] Handle terminal match: add `Leaf` to completed
- [x] Handle epsilon: add `Leaf(Epsilon)` to completed
- [x] Record `completed` in step data
- [x] Final result: completed.Head (properly nested tree)
- [x] Extract `expandNonterminal` helper

### 3. `DerivationTreeDot.fs` — DOT Rendering
- [x] Update `toDotWithLLStack` signature to accept `completed` list
- [x] Render each completed tree root as a full recursive tree inside `cluster_completed`
- [x] Render stack frames with chain and same-rank constraint
- [x] Render markers as gray boxes

### 4. `LLStepVisualizer.fs` — Visualizer Update
- [x] Pass `completed` from step data to DOT renderer

### 5. `LLRunner.fs` — CLI
- [x] No changes needed (uses `LLStepVisualizer.renderSteps` which now passes `completed` automatically)

### 6. Tests — `LLParserTests.fs`
- [x] Update `LL(1) tree structure for simple parse` to verify 4 children
- [x] Add `LL(1) tree is properly nested with intermediate nonterminals` test

### 7. Tests — `LLVisualizerTests.fs`
- [x] Add `LL step visualization includes completed subtrees` test (checks for cluster_completed)
- [x] Add `LL step visualization stack includes LLMarker boxes` test (checks for lightgray/compile)
- [x] Add `LL step visualization tree is properly nested` test

### 8. Documentation
- [x] Update `docs/ll-parser.md`
- [x] Update `docs/visualization-types.md`

### 9. Final Checks
- [x] Format: `dotnet fantomas . --check` - PASSED
- [x] Build: `dotnet build FLPQ.slnx -c Release` - PASSED
- [x] Tests: `dotnet test` - ALL 421 PASSED
- [x] Duplication check - clean
- [x] Generics check - all generic over `'t, 'nt`
- [x] Separation check - algorithm logic in Languages, rendering in Printers
- [x] Equivalence test - LL vs Valiant equivalence test still passes

## Verification

- Grammar `S -> a S b S | eps`, input `a b`: tree is `Node(S, [Leaf(T("a")), Node(S, [Leaf(Epsilon)]), Leaf(T("b")), Node(S, [Leaf(Epsilon)])])` — properly nested
- DOT output contains `cluster_completed` with completed subtrees, and `lightgray` marker boxes
- All existing tests pass (283 Languages, 66 Printers, +4 new tests)
