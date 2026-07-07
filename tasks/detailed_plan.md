# Detailed Plan: Task 137 - Implement GLL for RSM

## Goal
Implement GLL (Generalized LL) for RSM as described in the book section sec:CFPQ_GLL (06_GLL_Based.tex) and paper DAMDID_GLL_CFPQ/sections/gll.tex. GLL builds a path index during execution; SPPF is immutable, built once from the index as a separate step.

## Architecture

### New files in `src/FLPQ.Languages/`:
- `GllTypes.fs` — SPPF, PathIndex, and GSS types
- `Gll.fs` — buildPathIndex, buildSppfFromIndex, extractDerivationTree, stringToGraph

### New files in `tests/`:
- `tests/FLPQ.Languages.Tests/GllTests.fs` — all GLL tests

### Compilation order in .fsproj:
```
GllTypes.fs → Gll.fs
```
Both placed after LRParser.fs (last current file).

## Sub-tasks

### 1. Implement types in `GllTypes.fs`

#### 1.1 SPPF types
```
SppfNodeInfo<'t,'nt> = DU:
  | SppfTerminal of Terminal<'t> * leftPos: int * rightPos: int
  | SppfNonterminal of Nonterminal<'nt> * leftPos: int * rightPos: int
  | SppfEpsilon of pos: int
  | SppfIntermediate of state: int * pos: int
  | SppfRange of fromState: int * fromPos: int * toState: int * toPos: int

SppfEdgeLabel = DU:
  | SingleChild | LeftChild | RightChild | PackedAlternative

SPPF<'t,'nt> = { graph: Graph<SppfNodeInfo<'t,'nt>, Option<SppfEdgeLabel>>; rootIndices: int list }
```
Between a specific (parent,child) pair there cannot be two edges of different types → edge label is `Option<SppfEdgeLabel>` (not NonEmptySet). Packaging of alternatives: one RangeNode has multiple PackedAlternative edges to different child vertices.

#### 1.2 PathIndex types
```
[<Struct>] RangeKey = { fromState: int; fromVertex: int; toState: int; toVertex: int }

RangeDescriptor = DU: EmptyRange | NonEmptyRange of RangeKey

PathIndexEntry<'t,'nt> = DU:
  | PTerminal of Terminal<'t>
  | PNonterminal of Nonterminal<'nt>
  | PIntermediate of state: int * pos: int

PathIndex<'t,'nt> = { matrix: Matrix<Set<PathIndexEntry<'t,'nt>>>; stateCount: int; vertexCount: int }
```
Size K×K where K = |Q| * |V|. Mapping: idx(q,v) = q * vertexCount + v. Operations: add, get, indexOf.

#### 1.3 GSS types
```
[<Struct>] GssVertexInfo = { state: int; vertex: int; mutable storedPops: Set<RangeDescriptor> }

[<Struct>] GssEdgeInfo = { returnState: int; matchedRange: RangeDescriptor }

GSS<'t,'nt> = { graph: Graph<GssVertexInfo, Option<NonEmptySet<GssEdgeInfo>>> }
```
Vertices are all possible pairs (q,v): |Q|*|V| vertices, pre-allocated. Edges use NonEmptySet because multiple edges between same (source,target) pair are possible.

Module GSS functions:
- indexOf: int -> int -> int (maps state + vertex to linear index)
- addEdge: GSS -> int -> int -> GssEdgeInfo -> Set<RangeDescriptor> (adds edge, returns storedPops for immediate processing)
- pop: GSS -> int -> RangeDescriptor -> Set<int * GssEdgeInfo> (saves range to storedPops, returns all outgoing edges)

### 2. Implement GLL core in `Gll.fs`

#### 2.1 stringToGraph helper
`stringToGraph : string -> Graph<int, Option<'t>>` where 't is a character type.
Input: "aababb" → vertices 0..6 with edges i-[char]->i+1.

#### 2.2 buildPathIndex
Function signature:
```fsharp
buildPathIndex : RSM<'t,'nt> -> Graph<int, Option<'t>> -> Set<int> -> PathIndex<'t,'nt>
```

Input: RSM, input graph, set of start vertices.

Algorithm (Listing lst:gll_rsm_cfpq):
- Descriptor: struct { rsmState: int; vertex: int; gssIdx: int; range: RangeDescriptor }
- Initialization: for each start vertex vs, find start state q0' of start block (extended RSM),
  create GSS vertex at (q0', vs), create descriptor (q0', vs, gssIdx, EmptyRange), enqueue.
- Pre-allocate GSS with all |Q|*|V| vertices.
- Main loop: queue Q, Set<Descriptor> handled. While Q not empty:
  1. Terminal transitions: for each (q0, t, q1) in transition set and (v0, t, v1) in graph edges:
     - Create descriptor (q1, v1, s0, R^{p,u}_{q1,v1}) where (p,u) are from current range
     - Add PTerminal(t) to I[(q0,v0)][(q1,v1)]
     - Add PIntermediate(q0, v0) to I[(p,u)][(q1,v1)] if current range is non-empty
  2. Nonterminal transitions (calls): for each (q0, N, q1) in transitions:
     - GSS.addEdge with return address q1 and current range
     - Handle storedPops immediately: for each storedPop, combine with current range, add PIntermediate, create continuation descriptor
     - Create descriptor for the start state of block N
  3. Final state (return): if q0 is final in some block:
     - GSS.pop saves range, returns outgoing edges
     - For each edge (returnState, R^{q2,v2}_{q3,w0}):
       - Add PNonterminal(N) to I[(q3,w0)][(returnState,v0)]
       - Add PIntermediate(q3,w0) to I[(q2,v2)][(returnState,v0)]
       - Create descriptor (returnState, v0, parentGssIdx, R^{q2,v2}_{returnState,v0})

#### 2.3 buildSppfFromIndex
```fsharp
buildSppfFromIndex : PathIndex<'t,'nt> -> RangeKey list -> SPPF<'t,'nt>
```
Top-down traversal:
- For range R^{qi,vi}_{qj,vj} look at I[(qi,vi)][(qj,vj)]
- PTerminal(t) → SppfTerminal
- PNonterminal(N) → SppfNonterminal, recursively process range inside block N
- PIntermediate(q,v) → SppfIntermediate with LeftChild to R^{qi,vi}_{q,v} and RightChild to R^{q,v}_{qj,vj}
- Range nodes are reused: first visit creates SppfRange, subsequent visits add PackedAlternative edge

#### 2.4 extractDerivationTree
```fsharp
extractDerivationTree : SPPF<'t,'nt> -> int -> DerivationTree<'t,'nt>
```
Top-down traversal:
- SppfTerminal(t, l, r) → Leaf(Symbol.T(t))
- SppfEpsilon(pos) → Leaf(Symbol.Epsilon)
- SppfNonterminal(nt, l, r) → Node(nt, [child]) via SingleChild
- SppfIntermediate → recursively process LeftChild and RightChild, concatenate
- SppfRange → follow first PackedAlternative child

### 3. Tests in `GllTests.fs`

#### 3.1 Equivalence with CYK (property test)
For random grammars and random strings, GLL accepts/rejects same as CYK.
- Generate random RSM from grammar (RSM for grammar via EBNF parser or construct directly)
- For string input grammar: each production N -> rhs gets converted to a block
- Acceptance: for each start vertex, check I has entries for range from (q0', vs) to any final state (qf', vf)
- Include ambiguous and left-recursive grammars.

#### 3.2 Extracted tree yield
For accepted strings: `DerivationTree.leaves (extractDerivationTree sppf rootIdx) = input string chars`

#### 3.3 Comparison with classical LL
For unambiguous grammar without left recursion, nonterminal node count in GLL-SPPF tree equals classical LLParser tree (adjusted for extended RSM S' block).

### 4. Documentation

Create `docs/gll.md` describing:
- Types and design rationale
- Algorithm descriptions with book references
- Function signatures with pre/postconditions
- Design decisions

## Decisions

1. **SPPF types**: Types defined in `GllTypes.fs` to avoid circular dependencies. SPPF wraps Graph where vertices are SppfNodeInfo and edges are Option<SppfEdgeLabel>. Between (parent,child) pair there cannot be two edges of different types, justifying Option instead of NonEmptySet.

2. **PathIndex**: Uses `Matrix<Set<...>>` (set-semiring) consistent with CYK and Valiant. Linear index mapping: idx(q,v) = q * vertexCount + v. The path index stores entries of type PathIndexEntry that describe what was recognized in each range.

3. **GSS**: Wraps `Graph<GssVertexInfo, Option<NonEmptySet<GssEdgeInfo>>>`. `storedPops` is mutable in the vertex record (accessed via `Graph.vertexMap` which is immutable by keys but record values are mutable). Pre-allocated with all |Q|*|V| vertices.

4. **GLL algorithm**: Follows listing lst:gll_rsm_cfpq from the book (paper section "GLL-Based CFPQ Algorithm"). Uses queue-based worklist with handled set. Three cases: terminal, nonterminal (call), final state (return).

5. **SPPF construction**: Built as a separate step from path index. Top-down traversal with memoization: range nodes are reused on subsequent visits, adding packed alternative edges. Graph vertices and edges are accumulated and assembled via `Graph.fromEdges` at the end.

6. **Derivation tree extraction**: Extracts a single tree (first alternative) from the packed SPPF. For ambiguous grammars, picks first alternative per range.

7. **Grammar to RSM conversion**: For testing equivalence with CYK, we need to convert Grammar to RSM. Each production N → rhs becomes a block: a right-linear DFA recognizing the rhs. Alternatively, use the existing RsmBuilder (EBNF parser) path.
