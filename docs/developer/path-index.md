# PathIndex Module

**Tags:** data-structure, path-index, matrix, gll, cfpq, rsm
**Kind:** data-structure
**Module:** PathIndex
**Source:** `src/FLPQ.Languages/PathIndex.fs`
**Depends on:** Matrix, Grammar
**Used by:** GLL, RNGLR, SPPF
**Book reference:** Section sec:CFPQ_GLL (Chapter 6, `06_GLL_Based.tex`)

> **Abstract:** Implements the **path index** — the central data structure built during GLL/RNGLR parsing for CFPQ on Recursive State Machines. A K×K matrix where K = |Q| × |V| (total RSM states × graph vertices). Each cell `(fromKey, toKey)` stores the set of recognized entries for the corresponding range: `PTerminal` (matched terminal), `PNonterminal` (derived nonterminal), `PEpsilonNonterminal` (epsilon derivation), `PIntermediate` (partial recognition). Includes invariant checking (callee-reachability, start nonterminal).

## Contents

- [Data Structure](#data-structure)
- [Type Definitions](#type-definitions)
- [Module Functions](#module-functions)
- [Invariant Checking](#invariant-checking)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Data Structure

The path index is a K×K square matrix `Matrix<Set<PathIndexEntry<'t, 'nt>>>` where K = |Q| × |V|. Every (state, vertex) pair can be both a source and a target of a range, hence the square shape.

**Linear index mapping:** `idx(state, vertex) = state × VertexCount + vertex` — row-major layout, vertex varies fastest. The inverse recovers state via `idx / VertexCount` and vertex via `idx % VertexCount`.

## Type Definitions

### `RangeKey` (struct)
```fsharp
[<Struct>]
type RangeKey =
    { FromState: int; FromVertex: int; ToState: int; ToVertex: int }
```
A range in the path index: from `(fromState, fromVertex)` to `(toState, toVertex)`. This is the GLL descriptor range `(c_U, i)(c_V, j)` from listing `lst:gll_rsm_cfpq`.

### `RangeDescriptor`
```fsharp
[<RequireQualifiedAccess>]
type RangeDescriptor =
    | EmptyRange
    | NonEmptyRange of RangeKey
```
A matched range (possibly empty). `EmptyRange` means no input matched yet. `NonEmptyRange` carries a concrete range.

### `PathIndexEntry<'t, 'nt>`
```fsharp
[<RequireQualifiedAccess>]
type PathIndexEntry<'t, 'nt when 't: comparison and 'nt: comparison> =
    | PTerminal of Terminal<'t>
    | PNonterminal of Nonterminal<'nt>
    | PEpsilonNonterminal of Nonterminal<'nt>
    | PIntermediate of state: int * pos: int
```

| Variant | Meaning |
|---------|---------|
| `PTerminal` | A terminal symbol was matched; range covers one graph edge |
| `PNonterminal` | A nonterminal A was derived spanning the range. Call site: `(i,p)→(j,q)` where i is call state, j is return state |
| `PEpsilonNonterminal` | A nonterminal A derives ε, matching a zero-length range `(i,p)→(i,p)` |
| `PIntermediate` | Partial recognition: range `(i,p)→(state,pos)` is partially matched |

### `PathIndex`
```fsharp
type PathIndex<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Matrix: Matrix<Set<PathIndexEntry<'t, 'nt>>>
      StateCount: int
      VertexCount: int }
```
K×K matrix where K = `StateCount × VertexCount`. `StateCount`/`VertexCount` are stored alongside the matrix for direct access during linear index computation.

## Module Functions

### Core Cell Operations
```fsharp
val linearIndex: pi: PathIndex<'t, 'nt> -> state: int -> vertex: int -> int
val add: pi: PathIndex<'t, 'nt> -> fromState: int -> fromVertex: int -> toState: int -> toVertex: int -> entry: PathIndexEntry<'t, 'nt> -> unit
val get: pi: PathIndex<'t, 'nt> -> fromState: int -> fromVertex: int -> toState: int -> toVertex: int -> Set<PathIndexEntry<'t, 'nt>>
```
`add` mutates the underlying matrix cell in-place via `Matrix.set`. Repeatedly adding the same entry is idempotent (set semantics).

### Filtering
```fsharp
val filterNonterminals: entries: Set<PathIndexEntry<'t, 'nt>> -> Set<PathIndexEntry<'t, 'nt>>
```
Filters to only `PNonterminal` or `PEpsilonNonterminal` entries.

## Invariant Checking

### `checkCalleeReachabilityInvariant`
```fsharp
val checkCalleeReachabilityInvariant:
    pi: PathIndex<'t, 'nt> -> blockStart: Map<Nonterminal<'nt>, int>
    -> blockFinals: Map<Nonterminal<'nt>, Set<int>> -> Result<unit, string list>
```
Verifies: for every cell `(i,p)→(j,q)` containing `PNonterminal(A)` or `PEpsilonNonterminal(A)`, at least one **callee cell** `(s_A,p)→(f_A,q)` is non-empty, where `s_A = blockStart[A]` and `f_A ∈ blockFinals[A]`.

Collects all violations into a single error list rather than failing on the first one.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Linear index: `state × VertexCount + vertex` | Row-major layout; vertex varies fastest |
| `PEpsilonNonterminal` as separate variant | Epsilon derivations at same-position cells must be distinguishable from regular nonterminals |
| K×K matrix (not sparser) | Every (state, vertex) pair is a potential range endpoint; worst-case every pair reachable from every other pair |
| `Set` as cell value type | Idempotent addition, deterministic iteration, structural equality |
| `RangeDescriptor` as DU, not `option<RangeKey>` | Carries semantic intent "empty matched range" vs. "no range" |
| `RangeKey` as struct record | Value semantics with named fields; avoids heap allocation |
| `checkCalleeReachabilityInvariant` collects all errors | Comprehensive diagnostics in a single pass |

## Book Reference

| Reference | Description |
|-----------|-------------|
| `sec:CFPQ_GLL` | Core GLL algorithm — path index is the central data structure |
| `lst:gll_rsm_cfpq` | Pseudocode listing for GLL algorithm that populates and queries the path index |
| Chapter 6, `06_GLL_Based.tex` | Full chapter on GLL-based CFPQ with Recursive State Machines |

## See Also

- [SPPF module](sppf.md) — SPPF built from path index
- [GLL](gll.md) — GLL algorithm that builds the path index
- [RNGLR](rnglr.md) — RNGLR algorithm that also builds from path index
- [Matrix module](matrix.md) — underlying K×K matrix
- [Grammar module](grammar.md) — Terminal/Nonterminal types used in entries
- [RSM module](rsm.md) — blockStart/blockFinals maps for invariant checking
