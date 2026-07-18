# SPPF Module

**Tags:** data-structure, sppf, derivation-tree, forest, parsing, gll, cfpq
**Kind:** data-structure
**Module:** Sppf
**Source:** `src/FLPQ.Languages/Sppf.fs`
**Depends on:** Graph, Grammar, PathIndex
**Used by:** GLL, RNGLR
**Book reference:** Section sec:CFPQ_GLL (Chapter 6, `06_GLL_Based.tex`)

> **Abstract:** Shared Packed Parse Forest — a graph-structured representation of all possible derivation trees produced by GLL/RNGLR parsing. Built once from the path index as an immutable structure, then traversed to enumerate derivation trees. Uses node deduplication (one node per range regardless of ambiguity), packed alternatives (one SppfRange collects all derivation choices), and iterative deepening for cycle-aware tree enumeration.

## Contents

- [Data Structure](#data-structure)
- [Type Definitions](#type-definitions)
- [Construction](#construction)
- [Tree Enumeration](#tree-enumeration)
- [Validation Functions](#validation-functions)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Data Structure

The SPPF is a directed graph where:
- **Vertices** carry parse information (`SppfNodeInfo`) — terminals, nonterminals, epsilons, intermediates, ranges
- **Edges** are labeled with structural relationships (`SppfEdgeLabel`) — SingleChild, LeftChild, RightChild, PackedAlternative

Key structural properties:
- **Sharing**: Each distinct range has exactly one `SppfRange` node. Multiple derivation alternatives for the same range share this node via multiple `PackedAlternative` edges.
- **Packing**: Nonterminal nodes are children of range nodes (not alternatives themselves). A nonterminal is a call-site marker; its child range lies in the callee block.
- **Immutability**: The SPPF is built once after parsing and is fully immutable — it can be traversed multiple times independently.

## Type Definitions

### `SppfNodeInfo<'t, 'nt>`
```fsharp
type SppfNodeInfo<'t, 'nt when 't: comparison and 'nt: comparison> =
    | SppfTerminal of Terminal<'t> * leftPos: int * rightPos: int
    | SppfNonterminal of Nonterminal<'nt> * leftPos: int * rightPos: int * fromState: int * toState: int
    | SppfEpsilon of Nonterminal<'nt> option * pos: int
    | SppfIntermediate of state: int * pos: int * fromState: int * fromPos: int * toState: int * toPos: int
    | SppfRange of fromState: int * fromPos: int * toState: int * toPos: int
```

| Variant | Purpose |
|---------|---------|
| `SppfTerminal` | Leaf node for a matched terminal spanning graph positions [leftPos, rightPos] |
| `SppfNonterminal` | Call-site marker — a nonterminal spanning a range. Links to callee's range via SingleChild |
| `SppfEpsilon` | Epsilon derivation at position pos. Carries optional nonterminal to track PEpsilonNonterminal origin |
| `SppfIntermediate` | Concatenation split point: left half + right half span the full range |
| `SppfRange` | Grouping node: collects all PackedAlternative children that span the same range |

### `SppfEdgeLabel`
```fsharp
type SppfEdgeLabel =
    | SingleChild
    | LeftChild
    | RightChild
    | PackedAlternative
```

| Label | Connects | Meaning |
|-------|----------|---------|
| `SingleChild` | Nonterminal → Range\|Epsilon | The nonterminal derives the target sub-derivation |
| `LeftChild` | Intermediate → Range\|Epsilon | Left half of a concatenation split |
| `RightChild` | Intermediate → Range\|Epsilon | Right half of a concatenation split |
| `PackedAlternative` | Range → Terminal\|Nonterminal\|Intermediate\|Epsilon | One alternative derivation for this range |

### `SPPF<'t, 'nt>`
```fsharp
type SPPF<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Graph: Graph<SppfNodeInfo<'t, 'nt>, Option<SppfEdgeLabel>>
      RootIndices: int list }
```
- `Graph`: the SPPF as a vertex/edge-labeled graph
- `RootIndices`: indices of SppfRange nodes for the queried root ranges

## Construction

### `Sppf.buildSppfFromIndex`
```fsharp
buildSppfFromIndex
    : PathIndex<'t, 'nt> -> RangeKey list -> Map<Nonterminal<'nt>, int> option
   -> Map<Nonterminal<'nt>, Set<int>> option -> SPPF<'t, 'nt>
```
Top-down SPPF construction from a path index. Each root range is processed via `processRange`, which recurses into sub-ranges.

**Construction logic per range:**
1. **Memoization**: Each range processed exactly once. `SppfRange` created on first visit; subsequent visits reuse it and add new `PackedAlternative` edges.
2. **PTerminal** → `SppfTerminal` leaf, `PackedAlternative` edge.
3. **PNonterminal** → `SppfNonterminal` node (call-site). Links via `SingleChild` to callee's range node (found through blockStart/blockFinals lookup).
4. **PEpsilonNonterminal** → `SppfEpsilon` node carrying the nonterminal, `PackedAlternative` edge.
5. **PIntermediate** → `SppfIntermediate` node at split point. Left and right halves recursively processed, linked via `LeftChild`/`RightChild`.

**Node deduplication:** All node types use dedicated `Dictionary` caches to ensure structurally identical nodes are shared.

## Tree Enumeration

### `Sppf.enumerateTrees`
```fsharp
enumerateTrees : SPPF<'t, 'nt> -> rootIdx: int -> seq<DerivationTree<'t, 'nt>>
```
Lazily enumerates derivation trees in order of increasing depth using iterative deepening.

**Key properties:**
- **Lazy evaluation (`seq`)**: Trees generated on demand.
- **Iterative deepening**: Outer loop increments `depth` from 1 up to 50, calling `childrenByDepth depth rootIdx` at each level.
- **Transparent Range nodes**: `SppfRange` forwards to all `PackedAlternative` children without consuming a depth level.
- **Intermediate depth distribution**: For `SppfIntermediate`, total depth budget is split between left and right children: `max dL dR = depth - 1`.

## Validation Functions

All return `Result<unit, string list>` — `Ok()` if valid, `Error(errors)` listing all violations.

```fsharp
val validateRangeNodesHaveChildren : SPPF<'t, 'nt> -> Result<unit, string list>
val validateIntermediateChildren : SPPF<'t, 'nt> -> Result<unit, string list>
val validateNonterminalChildren : SPPF<'t, 'nt> -> Result<unit, string list>
val validateRangePositions : SPPF<'t, 'nt> -> Result<unit, string list>
val validateIntermediateConnectedness : SPPF<'t, 'nt> -> Result<unit, string list>
```

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| SPPF as separate immutable structure | Decouples parsing from forest construction and tree extraction |
| SppfRange as grouping node | One node per range regardless of ambiguity — the defining property of SPPF |
| PNonterminal not self-referencing | Nonterminal links to callee's range via blockStart/blockFinals lookup, avoiding trivial self-cycles |
| Iterative deepening for cycle handling | SPPF cycles are productive (recursive derivations); visited-set would incorrectly reject deeper valid trees |
| Transparent Range nodes in `enumerateTrees` | Depth counts only semantic derivation levels, not structural grouping indirection |
| Node deduplication via Dictionary | Guarantees the "shared" property: identical derivations represented by a single node |
| `Option<SppfEdgeLabel>` for edge matrix | Between a specific (parent, child) pair there cannot be two edges of different types |

## Book Reference

The SPPF implements the shared packed parse forest described in section `sec:CFPQ_GLL` (Chapter 6, `06_GLL_Based.tex`). Built from the path index produced by GLL/RNGLR.

## See Also

- [PathIndex module](path-index.md) — the path index from which SPPF is built
- [GLL](gll.md) — GLL parsing, builds path index for SPPF construction
- [RNGLR](rnglr.md) — RNGLR parsing, also builds from path index
- [DerivationTree module](derivation-tree.md) — tree type yielded by `enumerateTrees`
