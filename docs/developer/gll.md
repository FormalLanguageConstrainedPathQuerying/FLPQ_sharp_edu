# GLL for RSM

Implements Generalized LL (GLL) parsing for Recursive State Machines (RSM) as described in the book section sec:CFPQ_GLL (06_GLL_Based.tex) and paper DAMDID_GLL_CFPQ/sections/gll.tex.

## Overview

GLL builds a **path index** (matrix) during execution. The **SPPF** (Shared Packed Parse Forest) is immutable, built once from the index as a separate step. The input string is treated as a special case of a graph: each character is an edge between consecutive vertices.

## Types

### GllTypes.fs

Located in `src/FLPQ.Languages/GllTypes.fs`. Defines GSS types and the `Descriptor` type for the GLL worklist queue.

#### GSS Types

```fsharp
GssVertexInfo (struct): { state: int; vertex: int }

GssEdgeInfo (struct):
  { ReturnState: int
    PreCallState: int
    PreCallVertex: int
    MatchedRange: RangeDescriptor }

GSS =
  { Graph: Graph<GssVertexInfo, Option<NonEmptySet<GssEdgeInfo>>>
    StoredPops: Set<RangeDescriptor> array }
```

StoredPops is stored in a separate mutable array (not in a mutable struct field) because `Graph.vertexMap` (an immutable Map) returns value copies for structs, preventing in-place mutation.

Vertices are all possible (state, vertex) pairs: |Q|×|V| vertices, pre-allocated. Edges use NonEmptySet because multiple edges between the same pair are possible.

#### Descriptor

```fsharp
Descriptor (struct):
  { RsmState: int
    Vertex: int
    GssIdx: int
    MatchedRange: RangeDescriptor }
```

The worklist queue element. Implements custom equality and hashing for handled-set deduplication.

#### SPPF and PathIndex Types

SPPF types (`SppfNodeInfo`, `SppfEdgeLabel`, `SPPF`) and PathIndex types (`RangeKey`, `RangeDescriptor`, `PathIndexEntry`, `PathIndex`) are defined in separate files:

- [SPPF module](sppf.md) — `Sppf.fs`
- [PathIndex module](path-index.md) — `PathIndex.fs`

## Functions

### GLL.buildPathIndex

```fsharp
buildPathIndex : RSM<'t,'nt> -> Graph<int, Option<'t>> -> Set<int> -> PathIndex<'t,'nt>
```

Core GLL algorithm (Listing lst:gll_rsm_cfpq):

1. **Initialization**: Flattens RSM state space (assigns global indices), pre-allocates GSS with all |Q|×|V| vertices. For each start vertex, creates descriptor at the extended RSM start block's start state.

2. **Main loop** (queue-based with handled set):
   - **Terminal transitions**: Match RSM terminal transitions with graph edges, add PTerminal and PIntermediate entries, extend matched range.
   - **Nonterminal transitions (calls)**: Push GSS edge with return address and current range, handle storedPops, create descriptor for called block's start state.
   - **Final state (return)**: Pop GSS, save recognized range, add PNonterminal and PIntermediate entries, create continuation descriptors.

### GLL.extractDerivationTree

```fsharp
extractDerivationTree : PathIndex<'t,'nt> -> RsmStateInfo<'nt>[] -> Dictionary<'nt,int> -> Dictionary<'nt,Set<int>> -> int -> int -> int -> int -> DerivationTree<'t,'nt> option
```

Extracts a single derivation tree directly from the path index (bypassing SPPF). Depth-limited (100) to prevent infinite recursion through the index. Picks the first available derivation entry at each decomposition point: prefers PIntermediate over PTerminal over PEpsilonNonterminal over PNonterminal.

### GLL.stringToGraph

```fsharp
stringToGraph : 't list -> Graph<int, Option<'t>>
```

Converts a list of terminals to a linear path graph: vertices 0..|list|, each edge i→i+1 carries the corresponding terminal.

### GLL.isAccepted

```fsharp
isAccepted : PathIndex<'t,'nt> -> int -> int -> Set<int> -> int -> bool
```

Checks if the path index contains a path from (startGlobalState, startVertex) to any of the given final states at the end of the input graph.

## Design Decisions

1. **StoredPops storage**: storedPops is in a separate mutable array rather than a mutable struct field, avoiding the value-copy issue with immutable Maps.

2. **Epsilon acceptance**: For RSM blocks where the start state is final (regex nullable), the GLL adds PNonterminal to the path index even without parent GSS edges, ensuring epsilon acceptance is correctly recorded.

3. **Grammar to RSM conversion**: Tests use `RsmBuilder.buildRSMFromText` via EBNF text conversion. Only alphabetic terminals (a-z) are supported due to EBNF tokenizer limitations.

4. **PathIndex linear indexing**: Uses `idx(state, vertex) = state * vertexCount + vertex` to map (state, vertex) pairs to matrix row/column indices. See [PathIndex module](path-index.md).

## See Also

- [SPPF module](sppf.md) — SPPF types, construction, validation, tree enumeration
- [PathIndex module](path-index.md) — path index types and operations
- [RNGLR module](rnglr.md) — LR-based counterpart sharing SPPF and PathIndex
