# Detailed Plan: Tasks 77--80

## Task 78: Refactor visualization pattern

**Goal**: Separate data collection (parseWithSteps producing F# data) from rendering (pure functions converting data to TeX/DOT strings).

**Current problem**: `LLStepVisualizer.visualizeSteps` and `LRStepVisualizer.visualizeSteps` both internally call the parser (`LLParser.parseWithSteps` / `LRParser.parseWithSteps`), then render. This means:
- You cannot call parser once and handle result and trace independently
- You cannot use the trace data for anything else without re-parsing

**Target pattern** (already used by CYK/Valiant):
```fsharp
let result, trace = Parser.parseWithSteps ...
let visualized = Visualizer.renderSteps trace   // or renderStep for each step
```

**Changes**:

### 1. VisualizationTypes.fs — Add type for stack-rendered visualization step

Add `StackTreeVisualizationStep` type that holds raw parse step data + rendering:
Actually, the clean approach: keep `VisualizationStep` as the rendered output, but add functions that render from raw step data.

Current:
- `LLStepVisualizer.visualizeSteps` takes parser args + calls parser + renders → `VisualizationStep list`

Target:
- `LLStepVisualizer.renderSteps` takes `LLParsingStep list` → `VisualizationStep list`
- `LRStepVisualizer.renderSteps` takes `LRParsingStep list` → `VisualizationStep list`

### 2. LLStepVisualizer.fs — Replace visualizeSteps with renderSteps

```fsharp
module LLStepVisualizer =
    let renderStep (symbolVisualizer: Symbol<'t,'nt> -> string) (step: LLParsingStep<'t,'nt>) : VisualizationStep =
        let stackTrees = step.stack |> List.map LLStackFrame.tree
        { treeAndStack = DerivationTreeDot.toDotWithStack symbolVisualizer step.tree stackTrees
          input = TeXRenderer.inputRow symbolVisualizer step.input.tokens step.input.position }

    let renderSteps symbolVisualizer steps =
        steps |> List.map (renderStep symbolVisualizer)
```

### 3. LRStepVisualizer.fs — Replace visualizeSteps with renderSteps

```fsharp
module LRStepVisualizer =
    let renderStep (symbolVisualizer: Symbol<'t,'nt> -> string) (step: LRParsingStep<'t,'nt>) : VisualizationStep =
        let stackTrees = step.stack |> List.choose (function LRSymbol tree -> Some tree | _ -> None)
        { treeAndStack = DerivationTreeDot.toDotWithStack symbolVisualizer step.tree stackTrees
          input = TeXRenderer.inputRow symbolVisualizer step.input.tokens step.input.position }

    let renderSteps symbolVisualizer steps =
        steps |> List.map (renderStep symbolVisualizer)
```

### 4. Program.fs (CLI) — Update to new pattern

```fsharp
// LL:
let _, steps = LLParser.parseWithSteps grammar table k tokens
let vizSteps = LLStepVisualizer.renderSteps string steps
writeStepsVisualization outputDir vizSteps

// LR:
let _, steps = LRParser.parseWithSteps aug table tokens
let vizSteps = LRStepVisualizer.renderSteps string steps
writeStepsVisualization outputDir vizSteps
```

### 5. Tests — Update to new pattern

All tests currently calling `LLStepVisualizer.visualizeSteps` / `LRStepVisualizer.visualizeSteps` must be updated to:
- Call parser first: `LLParser.parseWithSteps ...` / `LRParser.parseWithSteps ...`
- Then render: `LLStepVisualizer.renderSteps ...` / `LRStepVisualizer.renderSteps ...`

---

## Task 77: LR visualize all stack frames including state frames

**Goal**: Currently `LRStepVisualizer.renderStep` discards `LRState` frames. The DOT visualization should include ALL frames, showing state numbers as labeled nodes in the stack chain.

**Changes**:

### 1. DerivationTreeDot.fs — Add `toDotWithLRStack`

New function that accepts the full `LRStackFrame list` (not just filtered `DerivationTree list`):

```fsharp
let toDotWithLRStack
    (symbolVisualizer: Symbol<'t,'nt> -> string)
    (tree: DerivationTree<'t,'nt>)
    (stack: LRStackFrame<'t,'nt> list)
    : string =
```

For each frame:
- `LRSymbol tree` → render as current (tree node, shape=box for Leaf, oval for Node)
- `LRState n` → render as a special node labeled "sN" (e.g., "s0"), no shape=box

All frames form the dashed chain. All frames are included in `rank=same`.

### 2. LRStepVisualizer.fs — Use `toDotWithLRStack`

```fsharp
let renderStep symbolVisualizer (step: LRParsingStep<'t,'nt>) : VisualizationStep =
    { treeAndStack = DerivationTreeDot.toDotWithLRStack symbolVisualizer step.tree step.stack
      input = TeXRenderer.inputRow symbolVisualizer step.input.tokens step.input.position }
```

### 3. Tests — Verify state frames appear in DOT output

- Verify DOT output contains "s0", "s1" etc. labels for state frames
- Verify rank=same includes state frame nodes

---

## Task 79: Implement graph hierarchy

**Goal**: Create `Graph<'v,'e>` type where vertices are in a map and edges in `Matrix<'e>`. Automaton wraps Graph.

### 1. New file: `src/FLPQ.Languages/Graph.fs`

```fsharp
namespace FLPQ.Languages
open FLPQ.LinearAlgebra

type Graph<'v, 'e> =
    { vertexMap: Map<int, 'v>
      edges: Matrix<'e> }

module Graph =
    let vertexCount (g: Graph<'v,'e>) = g.vertexMap.Count
    let vertices (g: Graph<'v,'e>) = g.vertexMap |> Map.toList |> List.sortBy fst
    let tryGetVertex idx (g: Graph<'v,'e>) = Map.tryFind idx g.vertexMap
    let getVertex idx (g: Graph<'v,'e>) = Map.find idx g.vertexMap
    let edge (g: Graph<'v,'e>) (fromIdx: int) (toIdx: int) = g.edges.data.[fromIdx, toIdx]
    let mapVertices f (g: Graph<'v,'e>) = { g with vertexMap = g.vertexMap |> Map.map (fun _ v -> f v) }
    let mapEdges f (g: Graph<'v,'e>) = { g with edges = Matrix.map f g.edges }
    let fromEdges vertices edges : Graph<'v,'e> = ...
```

### 2. Refactor Automaton.fs

NFA and DFA wrap Graph:

```fsharp
type NFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<'t>>>
      epsTransitions: Set<int * int>
      startStates: Set<int>
      finalStates: Set<int> }

type DFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<'t>>>
      startState: int
      finalStates: Set<int> }
```

Keep backward-compatible accessors:
- `Nfa.stateCount a = Graph.vertexCount a.graph`
- `Dfa.stateCount a = Graph.vertexCount a.graph`
- `a.states` → `Graph.vertices a.graph |> List.map snd`
- `a.transitions` → `a.graph.edges`

This means updating all references to `.states` and `.transitions` throughout the codebase.

Wait — this is a big change. Let me think about whether backward compatibility is the right approach or whether we should just do a breaking change.

The task says: "Graph provides functions to operate with vertices and edges. Automaton are wrapper on graph: transitions and states is a graph, additional information about start and final states stored."

So the automaton wraps a graph. But to minimize changes across the codebase, I should provide accessors that give backward-compatible access to `.states` and `.transitions`.

Actually, let me check: `.states` and `.transitions` are record fields. If I change the record definition, those field names won't work anymore. I need to either:
1. Keep the old field names as computed properties via members
2. Or update all usages

Given the constraint of the task (clear code, book-aligned), I think the cleanest approach is:
- Change the record definition
- Add member accessors for backward compat
- Or just update all usages

Let me check how many places use `.states` and `.transitions` on NFA/DFA.

Actually, let me reconsider. The task says clearly what needs to happen, but modifying all automaton-related code might be too much scope. Let me do the minimum: create `Graph` type and use it as the internal representation of NFA/DFA, with accessor members preserving backward compatibility.

Actually, let me re-read: "Implement the following hierarchy. Graph is a generic structure. Edges are generic and stored in Matrix<'t>. Vertices are generic and stored in map. Graph provides functions to operate with vertices and edges. Automaton are wrapper on graph: transitions and states is a graph, additional information about start and final states stored."

I think the best approach:
1. Create Graph.fs with Graph type + module
2. Add backward-compatible member accessors to NFA/DFA for `.states` and `.transitions`
3. All existing code continues to work
4. New code can use Graph directly

Let me check what the NFA/DFA record fields are: `states`, `transitions`, `epsTransitions`, `startStates`, `finalStates` (NFA) and `states`, `transitions`, `startState`, `finalStates` (DFA).

These are used in many places. Let me use F# member properties to provide backward compat.

```fsharp
type NFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<'t>>>
      epsTransitions: Set<int * int>
      startStates: Set<int>
      finalStates: Set<int> }
    member this.states = this.graph |> Graph.vertices |> List.map snd
    member this.transitions = this.graph |> Graph.edges
```

Wait, F# record fields are accessed as properties, but you can also add members. However, `.states` would shadow the member if it's a record field. Since we're removing it as a record field, the member accessor should work.

Actually, in F# you can have a member with the same name as a former record field. The issue is that `{ states = ... }` in construction won't work anymore. We'll need to change record construction.

Let me check all the places that construct NFA/DFA records...

This is going to be a significant refactoring. Let me approach it systematically.

---

## Task 80: Filter graph edges via diagonal matrix multiplication

**Goal**: Select edges from/to specific vertices using matrix multiplication with diagonal matrices.

### 1. LinearAlgebra.fs — Add `diagonal` function

```fsharp
let diagonal (size: int) (indices: Set<int>) (one: 'a) (zero: 'a) : Matrix<'a> =
    Matrix.create size size (fun i j ->
        if i = j && Set.contains i indices then one else zero)
```

This creates an `n × n` diagonal matrix where positions (i,i) for i∈indices are `one`, rest are `zero`.

For Boolean matrices: `diagonal n indices true false`

### 2. Graph.fs — Add edge filtering functions

For the Boolean semiring `⟨{0,1}, ∨, ∧⟩`:

```fsharp
/// Filter to keep only outgoing edges from specified vertices.
/// Equivalent to diagonal(selectedVertices) × edges in Boolean semiring.
let filterOutgoing selectedVertices (g: Graph<'v, bool>) : Graph<'v, bool> =
    let n = vertexCount g
    let diag = Matrix.diagonal n selectedVertices true false
    let filtered = LinearAlgebra.mxm (&&) (||) false diag g.edges
    { g with edges = filtered }

/// Filter to keep only incoming edges to specified vertices.
/// Equivalent to edges × diagonal(selectedVertices) in Boolean semiring.
let filterIncoming selectedVertices (g: Graph<'v, bool>) : Graph<'v, bool> =
    let n = vertexCount g
    let diag = Matrix.diagonal n selectedVertices true false
    let filtered = LinearAlgebra.mxm (&&) (||) false g.edges diag
    { g with edges = filtered }
```

### 3. Tests

- Simple graph with 3 vertices, edges 0→1, 0→2, 1→2
- filterOutgoing {0}: 0→1, 0→2 remain, 1→2 is gone
- filterIncoming {2}: 0→2, 1→2 remain, 0→1 is gone
- Both filters with all vertices = identity
- filter with empty set = zero matrix
