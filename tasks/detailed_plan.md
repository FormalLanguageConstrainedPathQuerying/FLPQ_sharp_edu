# Detailed Plan: Task 49 — Unified LR stack

## Goal

Replace two separate stacks (stateStack + treeStack) in the LR parser with a single unified stack where each frame is either a state or a symbol+tree.

## Design

### New Type

```fsharp
/// Frame on the unified LR parser stack.
[<Struct>]
type LRStackFrame<'t, 'nt> =
    | LRState of int
    | LRSymbol of Symbol<'t,'nt> * DerivationTree<'t,'nt>
```

### Stack layout (cons-based, head = top)

Initially: `[LRState 0]`

After shift of symbol X from state s to state s':
`[LRState s', LRSymbol(X, Leaf(X)), LRState s, ...]`

After reduce by A → β (|β| = k):
- Pop 2k items (k LRState + k LRSymbol frames)
- Exposed top is `LRState(s_prev)` — the state before β
- Children extracted from LRSymbol frames, reversed to RHS order
- Push `LRSymbol(N A, Node(A, children))`, then `LRState(goto(s_prev, A))`

### Changes

1. **VisualizationTypes.fs**: Add `LRStackFrame<'t,'nt>` DU
2. **VisualizationTypes.fs**: Change `LRParsingStep.stateStack: int list` to `stack: LRStackFrame<'t,'nt> list`
3. **LRParser.fs**: Refactor `parseWithSteps` to use unified stack
4. **LRVisualizer.fs**: Extract state numbers from unified stack for rendering
5. **LRParserTests.fs**: Update any direct references to `stateStack`

## Files

| File | Action |
|------|--------|
| `src/FLPQ.Languages/VisualizationTypes.fs` | Add LRStackFrame, update LRParsingStep |
| `src/FLPQ.Languages/LRParser.fs` | Refactor parseWithSteps |
| `src/FLPQ.Languages/LRVisualizer.fs` | Extract states from unified stack |
| `docs/visualization-types.md` | Update |
