# Detailed Plan: Task 108 — Rework LL Tree Building and Rendering

## Problem

Currently the LL parser uses an immutable `DerivationTree` type with `LLMarker` frames and a separate `completed` list to build trees bottom-up. This is complex and involves two separate data structures.

The task requires:
1. Make derivation tree mutable — leafs stored in stack frames can be updated (children added when nonterminal leaf is popped and RHS pushed)
2. Rework steps rendering — draw combined stack-tree structure where some leaves of partial trees are stack frames
3. Resulting tree must be converted to current immutable version

## Design

### 1. MutableTree type (`DerivationTree.fs`)

```fsharp
type MutableTree<'t, 'nt>(sym: Symbol<'t, 'nt>) =
    member val Symbol = sym with get, set
    member val Children: MutableTree<'t, 'nt> list = [] with get, set
    member val Parent: MutableTree<'t, 'nt> option = None with get, set

    member this.ToImmutable() =
        match this.Symbol with
        | N nt when not (List.isEmpty this.Children) ->
            Node(nt, this.Children |> List.map (_.ToImmutable()))
        | _ -> Leaf this.Symbol

    member this.GetPath() : int list =
        let rec go (n: MutableTree<'t,'nt>) acc =
            match n.Parent with
            | None -> acc
            | Some parent ->
                let idx =
                    parent.Children
                    |> List.findIndex (fun c -> obj.ReferenceEquals(c, n))
                go parent (idx :: acc)
        go this []
```

Design rationale:
- Class with mutable properties — allows in-place tree construction
- `Parent` pointer enables computing the path from root to leaf (needed for rendering)
- `ToImmutable()` converts to standard `DerivationTree` once construction is complete
- Unexpanded nonterminal leaves have `Children = []`

### 2. Simplified LL Parser (`LLParser.fs`)

No more `LLMarker`, no more `completed` list. Stack is `MutableTree<'t,'nt> list`.

Algorithm:
```
root = MutableTree(N startSymbol)
stack = [root]

While stack not empty:
    record step
    top = stack.Head
    match top.Symbol:
        Terminal t: if matches input[pos], pop, advance pos
        Epsilon: pop (no input consumed)
        Nonterminal nt: look up table, set top.Children to RHS nodes, push RHS nodes (in order) onto stack

If stack empty and pos == input.Length: success
```

Tree building: when a nonterminal expands, the existing mutable node (top) gets its children set, and those children become the new stack frontier. No separate "completed" mechanism needed.

### 3. Step Data (`VisualizationTypes.fs`)

Replace `LLStackFrame` (with `LLTree`/`LLMarker`) and `LLParsingStep` (with `completed`) with:

```fsharp
[<Struct>]
type LLStackLeaf<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      path: int list }

[<Struct>]
type LLParsingStep<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      stack: LLStackLeaf<'t, 'nt> list
      input: StepInput<'t> }
```

- `tree`: immutable snapshot of the root MutableTree at this step moment
- `stack`: immutable snapshots of each stack MutableTree node, with its path in the tree

For step recording (each iteration), snapshot root + each stack node:
```fsharp
let recordStep stack pos =
    let tree = root.ToImmutable()
    let stackLeaves = stack |> List.map (fun n -> { tree = n.ToImmutable(); path = n.GetPath() })
    steps <- { tree = tree; stack = stackLeaves; input = { tokens = terminals; position = pos } } :: steps
```

### 4. Combined Tree+Stack Rendering (`DerivationTreeDot.fs`)

`toDotWithLLStack` receives:
- The immutable `tree` (full derivation tree snapshot)
- The `stack` list (LLStackLeaf list identifying stack frontier)

Rendering algorithm:
1. Render the full tree recursively, assigning DOT IDs
2. Track node IDs by path (during tree rendering, record (path → nodeId) mappings)
3. After tree rendering, for consecutive stack leaves, add dashed edges (using path → nodeId lookup)
4. Add same-rank constraint for stack leaves

Since we render by path, we always get the correct node IDs even with duplicate leaf values.

### 5. Step Visualizer Update (`LLStepVisualizer.fs`)

Simplified: passes `step.tree` and `step.stack` directly to `DerivationTreeDot.toDotWithLLStack`.

No more `completed` list.

## Files Modified

| File | Change |
|------|--------|
| `src/FLPQ.Languages/DerivationTree.fs` | Add MutableTree class |
| `src/FLPQ.Languages/VisualizationTypes.fs` | Replace LLStackFrame/LLMarker with LLStackLeaf, simplify LLParsingStep |
| `src/FLPQ.Languages/LLParser.fs` | Rewrite to use MutableTree, remove LLMarker + completed |
| `src/FLPQ.Printers/DerivationTreeDot.fs` | Rewrite toDotWithLLStack for combined tree+stack |
| `src/FLPQ.Printers/LLStepVisualizer.fs` | Update for new step data |
| `tests/FLPQ.Languages.Tests/LLParserTests.fs` | Tree structure tests unchanged (same final output) |
| `tests/FLPQ.Printers.Tests/LLVisualizerTests.fs` | Update rendering expectations (remove completed subtree checks, update stack checks) |
| `docs/ll-parser.md` | Update algorithm description |
| `docs/derivation-tree.md` | Add MutableTree documentation |
| `docs/visualization-types.md` | Update type descriptions |
| `docs/derivation-tree-viz.md` | Update toDotWithLLStack description |

## Order of Implementation

1. Add MutableTree type to DerivationTree.fs
2. Update VisualizationTypes.fs (new LLStackLeaf, simplified LLParsingStep)
3. Rewrite LLParser.fs
4. Rewrite DerivationTreeDot.fs (toDotWithLLStack)
5. Update LLStepVisualizer.fs
6. Update tests
7. Update documentation
8. Format, build, test
