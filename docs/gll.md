# GLL for RSM

Implements Generalized LL (GLL) parsing for Recursive State Machines (RSM) as described in the book section sec:CFPQ_GLL (06_GLL_Based.tex) and paper DAMDID_GLL_CFPQ/sections/gll.tex.

## Overview

GLL builds a **path index** (matrix) during execution. The **SPPF** (Shared Packed Parse Forest) is immutable, built once from the index as a separate step. The input string is treated as a special case of a graph: each character is an edge between consecutive vertices.

## Types

### GllTypes.fs

Located in `src/FLPQ.Languages/GllTypes.fs`. Defines three type groups:

#### SPPF Types

```fsharp
SppfNodeInfo<'t,'nt> = DU:
  SppfTerminal(Terminal<'t>, leftPos: int, rightPos: int)
  SppfNonterminal(Nonterminal<'nt>, leftPos: int, rightPos: int)
  SppfEpsilon(pos: int)
  SppfIntermediate(state: int, pos: int)
  SppfRange(fromState: int, fromPos: int, toState: int, toPos: int)

SppfEdgeLabel = DU: SingleChild | LeftChild | RightChild | PackedAlternative

SPPF<'t,'nt> = { graph: Graph<SppfNodeInfo<'t,'nt>, Option<SppfEdgeLabel>>; rootIndices: int list }
```

Between a (parent, child) pair, there cannot be two edges of different types → edge label is `Option<SppfEdgeLabel>` (not `NonEmptySet`). Packing of alternatives: one RangeNode has multiple PackedAlternative edges to different child vertices. SPPF roots are range nodes corresponding to accepted ranges.

#### PathIndex Types

```fsharp
RangeKey (struct): { fromState: int; fromVertex: int; toState: int; toVertex: int }

RangeDescriptor = DU: EmptyRange | NonEmptyRange of RangeKey

PathIndexEntry<'t,'nt> = DU:
  PTerminal(Terminal<'t>)
  PNonterminal(Nonterminal<'nt>)
  PIntermediate(state: int, pos: int)

PathIndex<'t,'nt> = { matrix: Matrix<Set<PathIndexEntry<'t,'nt>>>; stateCount: int; vertexCount: int }
```

Size K×K where K = |Q| * |V|. Linear index: idx(state, vertex) = state * vertexCount + vertex.

#### GSS Types

```fsharp
GssVertexInfo (struct): { state: int; vertex: int }

GssEdgeInfo (struct): { returnState: int; matchedRange: RangeDescriptor }

GSS = { graph: Graph<GssVertexInfo, Option<NonEmptySet<GssEdgeInfo>>>; storedPops: Set<RangeDescriptor>[] }
```

StoredPops is stored in a separate mutable array (not in the struct's mutable field) because Graph's immutable Map returns value copies for structs, preventing in-place mutation.

Vertices are all possible (state, vertex) pairs: |Q|*|V| vertices, pre-allocated. Edges use NonEmptySet because multiple edges between the same pair are possible.

## Functions

### GLL.buildPathIndex

```fsharp
buildPathIndex : RSM<'t,'nt> -> Graph<int, Option<'t>> -> Set<int> -> PathIndex<'t,'nt>
```

Core GLL algorithm (Listing lst:gll_rsm_cfpq):

1. **Initialization**: Flattens RSM state space (assigns global indices), pre-allocates GSS with all |Q|*|V| vertices. For each start vertex, creates descriptor at the extended RSM start block's start state.

2. **Main loop** (queue-based with handled set):
   - **Terminal transitions**: Match RSM terminal transitions with graph edges, add PTerminal and PIntermediate entries, extend matched range.
   - **Nonterminal transitions (calls)**: Push GSS edge with return address and current range, handle storedPops, create descriptor for called block's start state.
   - **Final state (return)**: Pop GSS, save recognized range, add PNonterminal and PIntermediate entries, create continuation descriptors.

### GLL.buildSppfFromIndex

```fsharp
buildSppfFromIndex : PathIndex<'t,'nt> -> RangeKey list -> SPPF<'t,'nt>
```

Top-down SPPF construction from path index:
- Range nodes are memoized: first visit creates SppfRange, subsequent visits add PackedAlternative edges.
- PTerminal → SppfTerminal leaf node
- PNonterminal → SppfNonterminal with SingleChild back to the same range
- PIntermediate → SppfIntermediate with LeftChild/RightChild to sub-ranges

### GLL.extractDerivationTree

```fsharp
extractDerivationTree : PathIndex<'t,'nt> -> RsmStateInfo<'nt>[] -> Dictionary<'nt,int> -> int -> int -> int -> int -> DerivationTree<'t,'nt>
```

Extracts a single derivation tree directly from the path index (bypassing SPPF). Depth-limited (100) to prevent infinite recursion. Picks the first available derivation entry at each decomposition point.

### GLL.extractDerivationTreeFromSppf

```fsharp
extractDerivationTreeFromSppf : SPPF<'t,'nt> -> int -> DerivationTree<'t,'nt>
```

Alternative tree extraction from an already-built SPPF graph. Uses a visited set to break Nonterminal → SingleChild → Range → PackedAlternative → Nonterminal cycles.

### GLL.stringToGraph

```fsharp
stringToGraph : 't list -> Graph<int, Option<'t>>
```

Converts a list of terminals to a linear path graph: vertices 0..|list|, each edge i→i+1 carries the corresponding terminal.

## Design Decisions

1. **StoredPops storage**: storedPops is in a separate mutable array (`GSS.storedPops: Set<RangeDescriptor>[]`) rather than a mutable struct field. This avoids the issue where `Graph.vertexMap` (an immutable Map) returns value copies for structs.

2. **PathIndex linear indexing**: Uses `idx(state, vertex) = state * vertexCount + vertex` to map (state, vertex) pairs to matrix row/column indices. The matrix is K×K where K = |Q| × |V|.

3. **Epsilon acceptance**: For RSM blocks where the start state is final (regex nullable), the GLL adds PNonterminal to the path index even without parent GSS edges, ensuring epsilon acceptance is correctly recorded.

4. **SPPF cycles**: The SPPF inherently contains cycles (Nonterminal → SingleChild → Range → PackedAlternative → Nonterminal). Tree extraction handles this via depth limits and visited sets.

5. **Grammar to RSM conversion**: Tests use `RsmBuilder.buildRSMFromText` via EBNF text conversion. Only alphabetic terminals (a-z) are supported due to EBNF tokenizer limitations.
