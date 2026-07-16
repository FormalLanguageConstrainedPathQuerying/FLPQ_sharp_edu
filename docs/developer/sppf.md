# SPPF Module

Shared Packed Parse Forest: a graph-structured representation of all possible derivation trees produced by GLL parsing.
Built once from the path index after GLL execution, then traversed to enumerate derivation trees.

Book reference: sec:CFPQ_GLL (06_GLL_Based.tex), paper DAMDID_GLL_CFPQ/sections/gll.tex.

## Overview

The SPPF is a directed graph where vertices carry parse information (`SppfNodeInfo`) and edges are labeled with structural relationships (`SppfEdgeLabel`). The SPPF is constructed top-down from the path index using memoization to share sub-graphs. Derivation trees are extracted lazily via iterative deepening to handle inherent cycles.

## Types

### `SppfNodeInfo<'t, 'nt>`

Located in `src/FLPQ.Languages/Sppf.fs`. Discriminated union with `[<RequireQualifiedAccess>]`:

```fsharp
type SppfNodeInfo<'t, 'nt when 't: comparison and 'nt: comparison> =
    | SppfTerminal of Terminal<'t> * leftPos: int * rightPos: int
    | SppfNonterminal of Nonterminal<'nt> * leftPos: int * rightPos: int * fromState: int * toState: int
    | SppfEpsilon of Nonterminal<'nt> option * pos: int
    | SppfIntermediate of state: int * pos: int * fromState: int * fromPos: int * toState: int * toPos: int
    | SppfRange of fromState: int * fromPos: int * toState: int * toPos: int
```

**Variant semantics:**

| Variant | Fields | Purpose |
|---------|--------|---------|
| `SppfTerminal` | terminal, leftPos, rightPos | Leaf node for a matched terminal spanning graph positions [leftPos, rightPos] |
| `SppfNonterminal` | nonterminal, leftPos, rightPos, fromState, toState | Node representing a nonterminal instance spanning a range. Non self-referencing: its SingleChild links to the callee's range node (see Design Decisions) |
| `SppfEpsilon` | optional nonterminal, pos | Epsilon (empty) derivation at position pos. Carries an optional nonterminal to track which PEpsilonNonterminal created it |
| `SppfIntermediate` | state, pos, fromState, fromPos, toState, toPos | Internal node for a concatenation split point in the RSM: the left half spans [fromState,fromPos)→[state,pos), right half spans [state,pos)→[toState,toPos) |
| `SppfRange` | fromState, fromPos, toState, toPos | Structural grouping node: collects all PackedAlternative children that span the same range. Created once per range and reused |

**Design rationale:** The SppfRange node acts as an indirection layer so that multiple derivation choices for the same range are packaged under a single shared node. This is the "packed" aspect of SPPF — one range node collects all alternatives (terminals, nonterminals, intermediates, epsilon-nonterminals) via PackedAlternative edges.

### `SppfEdgeLabel`

```fsharp
type SppfEdgeLabel =
    | SingleChild
    | LeftChild
    | RightChild
    | PackedAlternative
```

Directed edges in the SPPF graph. Between a specific (parent, child) pair there cannot be two edges of different types; therefore the edge matrix type is `Option<SppfEdgeLabel>` (not `NonEmptySet`).

| Label | Connects | Meaning |
|-------|----------|---------|
| `SingleChild` | Nonterminal → Range (or Epsilon) | The nonterminal derives the target sub-derivation |
| `LeftChild` | Intermediate → Range (or Epsilon) | Left sub-derivation of a concatenation split |
| `RightChild` | Intermediate → Range (or Epsilon) | Right sub-derivation of a concatenation split |
| `PackedAlternative` | Range → Terminal\|Nonterminal\|Intermediate\|Epsilon | One alternative derivation for this range |

### `SPPF<'t, 'nt>`

```fsharp
type SPPF<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Graph: Graph<SppfNodeInfo<'t, 'nt>, Option<SppfEdgeLabel>>
      RootIndices: int list }
```

- `Graph`: the SPPF as a vertex/edge-labeled graph. Vertex count = total SPPF nodes; edges represent structural relationships.
- `RootIndices`: indices of SppfRange nodes that correspond to the queried root ranges. These are the entry points for tree extraction.

## Functions

### `Sppf.buildSppfFromIndex`

```fsharp
buildSppfFromIndex
    : PathIndex<'t, 'nt>
   -> RangeKey list
   -> Map<Nonterminal<'nt>, int> option
   -> Map<Nonterminal<'nt>, Set<int>> option
   -> SPPF<'t, 'nt>
```

Top-down SPPF construction from a path index. Each root range in `rootRanges` is processed via `processRange`, which recurses into sub-ranges.

**Parameters:**

| Parameter | Purpose |
|-----------|---------|
| `pathIndex` | Path index populated by GLL execution |
| `rootRanges` | Initial ranges to build SPPF nodes for (typically the accepted top-level ranges) |
| `blockStart` | Optional map from nonterminal to the start state of its RSM block. Required for expanding nonterminal calls into their callee bodies |
| `blockFinals` | Optional map from nonterminal to the set of final states of its RSM block. Used to determine which callee ranges to link for each nonterminal |

**Construction logic (per range):**

1. **Memoization** (`rangeNodeMap`, `rangeResultMap`): Each range is processed exactly once. The SppfRange node is created on first visit; subsequent visits (from different callers) reuse it and add new PackedAlternative edges.

2. **PTerminal** → Creates an `SppfTerminal` leaf node, adds a `PackedAlternative` edge from the range node.

3. **PNonterminal** → Creates an `SppfNonterminal` node (recording the call site coordinates: fromState/toState and fromPos/toPos). Adds a `PackedAlternative` edge from the range node to the nonterminal node. Then, if `blockStart`/`blockFinals` are provided, for each final state of the callee block, looks up callee ranges in the path index and links the nonterminal node to each non-empty callee range via `SingleChild` edges. Empty callee final entries (no path index entries) are silently filtered out.

4. **PEpsilonNonterminal** → Creates an `SppfEpsilon` node carrying the nonterminal, adds a `PackedAlternative` edge.

5. **PIntermediate** → Creates an `SppfIntermediate` node recording the split point and enclosing range boundaries. Adds a `PackedAlternative` edge. Then recursively processes the left half (from start to split point) and right half (split point to end), adding `LeftChild` and `RightChild` edges respectively. If a half has no path index entries, an `SppfEpsilon` (without nonterminal) is created and linked instead.

**Node deduplication:** All node types use dedicated `Dictionary` caches (`terminalNodeMap`, `nonterminalNodeMap`, etc.) to ensure structurally identical nodes are shared. SppfRange uses `rangeNodeMap` keyed by `RangeKey`.

### `Sppf.enumerateTrees`

```fsharp
enumerateTrees : SPPF<'t, 'nt> -> rootIdx: int -> seq<DerivationTree<'t, 'nt>>
```

Lazily enumerates derivation trees from the SPPF starting at the given root node index. Trees are produced in order of **increasing depth** using iterative deepening.

**Key properties:**

- **Lazy evaluation (`seq`):** Trees are generated on demand; the caller controls consumption.
- **No visited tracking:** The SPPF contains inherent cycles (see Design Decisions: SPPF cycles). These cycles are valid — they represent recursive derivations like S → S S nesting. A visited set would incorrectly exclude deeper valid trees.
- **Iterative deepening:** The outer loop increments `depth` from 1 up to a hard limit of 49, calling `childrenByDepth depth rootIdx` at each level and yielding the first tree found at that depth.
- **Transparent Range nodes:** `SppfRange` nodes at any depth simply forward to all their `PackedAlternative` children via `Seq.collect` — they do not consume a depth level. This ensures the depth accounting reflects semantic derivation depth, not SPPF structural depth.
- **Intermediate node depth distribution:** For `SppfIntermediate`, the total depth budget `depth-1` is split between left and right children: `max dL dR = depth - 1`. All valid (dL, dR) pairs are explored.
- **Seq.append for intermediate concatenation:** Left and right child tree sequences are concatenated with `Seq.append lc rc`, producing a flat sequence of children.
- **Terminal/Epsilon at depth=1:** Leaf nodes (terminal, epsilon) are only produced when `depth = 1`, implementing the base case of the depth recursion.
- **Depth limit:** The outer `while depth < 50` loop provides a safety bound against unbounded recursion in pathological cases.

### Validation Functions

All validation functions follow the same pattern: return `Result<unit, string list>` — `Ok()` if valid, `Error(errors)` listing all violations found.

#### `Sppf.validateRangeNodesHaveChildren`

```fsharp
validateRangeNodesHaveChildren : SPPF<'t, 'nt> -> Result<unit, string list>
```

Every `SppfRange` node must have at least one outgoing `PackedAlternative` edge. An empty range node indicates an internal inconsistency in SPPF construction.

#### `Sppf.validateIntermediateChildren`

```fsharp
validateIntermediateChildren : SPPF<'t, 'nt> -> Result<unit, string list>
```

Every `SppfIntermediate` node must have both a `LeftChild` and a `RightChild` outgoing edge. Missing either indicates incomplete range decomposition.

#### `Sppf.validateNonterminalChildren`

```fsharp
validateNonterminalChildren : SPPF<'t, 'nt> -> Result<unit, string list>
```

Every `SppfNonterminal` node must have at least one `SingleChild` edge (≥1 children). Each child target must be either a `SppfRange` or `SppfEpsilon` node — other node types as children indicate a construction error.

#### `Sppf.validateRangePositions`

```fsharp
validateRangePositions : SPPF<'t, 'nt> -> Result<unit, string list>
```

Every `SppfRange` node must satisfy `fromPos ≤ toPos`. Violations indicate inverted range boundaries.

#### `Sppf.validateIntermediateConnectedness`

```fsharp
validateIntermediateConnectedness : SPPF<'t, 'nt> -> Result<unit, string list>
```

Every `SppfIntermediate` node's left child range must start at the intermediate's `fromState`/`fromPos` and end at the intermediate's `state`/`pos`. The right child range must start at the intermediate's `state`/`pos` and end at `toState`/`toPos`. Epsilon children are accepted without coordinate checks.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| SPPF as a separate immutable structure | Decouples parsing (GLL builds path index) from forest construction and tree extraction. The SPPF is built once and can be traversed multiple times independently. |
| SppfRange as grouping node | Multiple derivation alternatives for the same range share a single Range node with multiple PackedAlternative edges. This sharing is the defining property of SPPF: one node per range regardless of ambiguity count. |
| PNonterminal not self-referencing | The `SppfNonterminal` node does not link back to the range that contains it. Instead it links via `SingleChild` to the callee's range node (found through blockStart/blockFinals lookup). This avoids trivial self-cycles Range→PackedAlternative→Nonterminal→SingleChild→Range that would produce degenerate derivation trees of unbounded depth with no additional structure. The existing cycles are productive: Nonterminal→SingleChild→CalleeRange→...→PackedAlternative→Nonterminal passes through actual derivation material in the callee. |
| Empty callee finals filtered | When `processRange` encounters a PNonterminal, it looks up callee ranges for each final state of the nonterminal's block. If the path index has no entries for a (finalState, fromPos, toPos) triple, that callee is skipped — no SppfRange and no SingleChild edge is created. This prevents dangling edges to empty ranges. |
| SPPF cycles via iterative deepening | The SPPF inherently contains cycles: Nonterminal → SingleChild → Range → PackedAlternative → (potentially same) Nonterminal. A visited set would incorrectly reject valid deeper trees (e.g., S ⇒ S S). Iterative deepening with a hard depth limit (50) allows cycle-aware enumeration: each depth increase explores one more unwinding of the cycle, producing progressively deeper valid trees. |
| Transparent Range nodes in `enumerateTrees` | Range nodes at any depth forward directly to their alternatives without consuming a depth increment. This ensures depth counts only semantic derivation levels, not the SPPF's structural grouping indirection. |
| `Seq.append` for intermediate concatenation | Produces the correct flat child sequence without building intermediate list allocations. Matches the semantics of concatenation in a derivation: children of the left and right halves form a single sequence. |
| Node deduplication via Dictionary | Terminal, nonterminal, epsilon, and intermediate nodes are cached by structural key in mutable `Dictionary` instances. Range nodes are cached by `RangeKey`. This guarantees the "shared" property of SPPF: identical derivations are represented by a single node. |
| Edge deduplication | Before adding an edge, the edge list is checked to avoid duplicates between the same (parent, child) pair with the same label. |
| `Option<SppfEdgeLabel>` for edge matrix | Between a specific (parent, child) pair there cannot be two edges of different types, so `Option` suffices (contrast with GSS edges where `NonEmptySet` is needed for parallel edges). |

## Book Relationship

The SPPF implements the shared packed parse forest described in section sec:CFPQ_GLL (06_GLL_Based.tex). It is built from the path index produced by the GLL algorithm for RSM-based CFPQ.

- **Construction:** The `buildSppfFromIndex` function implements the forest construction step described in the GLL paper (DAMDID_GLL_CFPQ). The top-down approach with memoization mirrors the `getNodeP`/`getNodeT` operations in the original GLL SPPF construction algorithm.
- **Tree extraction:** `enumerateTrees` implements extraction of derivation trees from the SPPF graph. The iterative deepening approach handles the inherent cycles that arise from recursive grammar productions.
- **Validation:** The validation functions ensure structural invariants of the constructed SPPF: completeness (every range has children, every intermediate has both children), correctness (nonterminal children are ranges or epsilons, intermediate children have matching coordinates), and sanity (range positions are non-decreasing).
