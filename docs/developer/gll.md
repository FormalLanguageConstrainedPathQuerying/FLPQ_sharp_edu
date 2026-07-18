# GLL for RSM

**Tags:** algorithm, parsing, gll, cfpq, graph, rsm, path-index, gss, sppf
**Kind:** algorithm
**Module:** GLL
**Source:** `src/FLPQ.Languages/GLL.fs`
**Depends on:** Matrix, Graph, RSM, PathIndex, Grammar, Automaton, GllTypes
**Used by:** FLPQ.Cli, TestHelpers
**Book reference:** Section sec:CFPQ_GLL (Chapter 6, `06_GLL_Based.tex`)

> **Abstract:** Implements Generalized LL (GLL) parsing for Recursive State Machines (RSM) as described in the book section sec:CFPQ_GLL. GLL builds a **path index** (K×K matrix) during execution using a descriptor-based worklist queue. The **SPPF** (Shared Packed Parse Forest) is immutable, built once from the index as a separate step. The input string is treated as a special case of a graph: each character is an edge between consecutive vertices.

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

Core GLL algorithm (listing `lst:gll_rsm_cfpq`):

1. **Initialization**: Flatten RSM state space (assign global indices), pre-allocate GSS with all |Q|×|V| vertices. For each start vertex, create descriptor at the extended RSM start block's start state.

2. **Main loop** (queue-based with handled set):
   - **Terminal transitions**: Match RSM terminal transitions with graph edges, add PTerminal and PIntermediate entries, extend matched range.
   - **Nonterminal transitions (calls)**: Push GSS edge with return address and current range, handle storedPops, create descriptor for called block's start state.
   - **Final state (return)**: Pop GSS, save recognized range, add PNonterminal and PIntermediate entries, create continuation descriptors.

3. **Acceptance**: Check if the path index contains a path from the extended RSM start state at start vertex to the final state at the end vertex.

## Type Definitions

### GSS Types
Located in `src/FLPQ.Languages/GllTypes.fs`.

#### GssVertexInfo (struct)
```fsharp
GssVertexInfo (struct): { state: int; vertex: int }
```

#### GssEdgeInfo (struct)
```fsharp
GssEdgeInfo (struct):
  { ReturnState: int
    PreCallState: int
    PreCallVertex: int
    MatchedRange: RangeDescriptor }
```

#### GSS
```fsharp
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

### SPPF and PathIndex Types

SPPF types (`SppfNodeInfo`, `SppfEdgeLabel`, `SPPF`) and PathIndex types (`RangeKey`, `RangeDescriptor`, `PathIndexEntry`, `PathIndex`) are defined in separate modules:
- [SPPF module](sppf.md) — `Sppf.fs`
- [PathIndex module](path-index.md) — `PathIndex.fs`

## Function Signatures

### GLL.buildPathIndex
```fsharp
buildPathIndex : RSM<'t,'nt> -> Graph<int, Option<'t>> -> Set<int> -> PathIndex<'t,'nt>
```
Core GLL algorithm that builds the path index during execution. Initializes from start vertices, runs the descriptor worklist, and populates the path index matrix with PTerminal, PNonterminal, PEpsilonNonterminal, and PIntermediate entries.

### GLL.extractDerivationTree
```fsharp
extractDerivationTree : PathIndex<'t,'nt> -> RsmStateInfo<'nt>[] -> Dictionary<'nt,int> -> Dictionary<'nt,Set<int>> -> int -> int -> int -> int -> DerivationTree<'t,'nt> option
```
Extracts a single derivation tree directly from the path index (bypassing SPPF). Depth-limited (100) to prevent infinite recursion through the index. Picks the first available derivation entry at each decomposition point.

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

| Decision | Rationale |
|----------|-----------|
| StoredPops in separate mutable array | Avoids the value-copy issue with immutable Maps returning copies of structs |
| Epsilon acceptance for nullable blocks | When start state is final, adds PNonterminal even without parent GSS edges |
| Grammar to RSM via EBNF | Tests use `RsmBuilder.buildRSMFromText` via EBNF text conversion |
| PathIndex linear indexing | Uses `idx(state, vertex) = state * vertexCount + vertex` to map (state, vertex) pairs to matrix indices |

## Book Reference

Section `sec:CFPQ_GLL` (Chapter 6, `06_GLL_Based.tex`) and paper `DAMDID_GLL_CFPQ/sections/gll.tex`.

## See Also

- [SPPF module](sppf.md) — SPPF types, construction, validation, tree enumeration
- [PathIndex module](path-index.md) — path index types and operations
- [RNGLR module](rnglr.md) — LR-based counterpart sharing SPPF and PathIndex
- [RSM module](rsm.md) — Recursive State Machine model
