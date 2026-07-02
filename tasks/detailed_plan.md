# Detailed Plan: Task 90 — Improve LL and LR stacks

## Goal

Remove symbol duplication from `LLStackFrame`. Derivation tree nodes already carry their symbol, so storing both Symbol and DerivationTree is redundant.

## Changes

### 1. `src/FLPQ.Languages/VisualizationTypes.fs`
- Change `LLStackFrame` from `LLFrame of Symbol<'t,'nt> * DerivationTree<'t,'nt>` to `LLFrame of DerivationTree<'t,'nt>`
- Update `LLStackFrame.symbol` to extract from tree: `DerivationTree.rootSymbol tree`
- Update `LLStackFrame.tree` to just return the tree
- Update `LLStackFrame.create` to `LLFrame(Leaf sym)`

### 2. `src/FLPQ.Languages/LLParser.fs`
- Pattern matching: match on tree node pattern to extract symbol info
- `LLFrame(T _ as sym, tree)` → `LLFrame(Leaf(T _ as sym) as tree)`
- `LLFrame(Epsilon, tree)` → `LLFrame(Leaf(Epsilon) as tree)`  
- `LLFrame(N nt, _)` → `LLFrame(Node(nt, _))`
- Frame creation: `LLFrame(sym, Leaf sym)` → `LLFrame(Leaf sym)`
- Init stack: `LLFrame(N g.start, Node(g.start, []))` → `LLFrame(Node(g.start, []))`

### 3. `src/FLPQ.Printers/LLStepVisualizer.fs`
- No changes needed (already uses `LLStackFrame.tree` which returns the tree)

## Verification
- All tests pass
- Check formatting
