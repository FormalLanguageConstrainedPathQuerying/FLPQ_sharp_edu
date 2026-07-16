# PathIndex Module

## Module Purpose

Implements the **path index** data structure used by the GLL algorithm for CFPQ on Recursive State Machines. The path index records which grammatical constructions have been recognized over which graph ranges during GLL parsing. It is a K×K matrix where K = |Q| × |V| (total RSM states × graph vertices). Each cell `(fromKey, toKey)` stores the set of recognized entries for the corresponding range.

**Book reference**: Section `sec:CFPQ_GLL` (Chapter 6, `06_GLL_Based.tex`).

## Type Definitions

### RangeKey

```fsharp
[<Struct>]
type RangeKey =
    { FromState: int
      FromVertex: int
      ToState: int
      ToVertex: int }
```

A range in the path index: from `(fromState, fromVertex)` to `(toState, toVertex)`. Both state and vertex are needed because the GLL state space is the product `Q × V`. The state component identifies a position in the RSM's control flow graph; the vertex component identifies a position in the input graph.

**Design rationale**:
- **Struct**: A value type to avoid heap allocation for the many range comparisons that occur during GLL execution. The `[<Struct>]` attribute is used (not `struct ... end` syntax) for F# 6.0+ implicit struct records.
- **Named fields**: `FromState`/`FromVertex`/`ToState`/`ToVertex` make the four-dimensional nature of the range explicit. Positional access would obscure which component is state vs. vertex.
- **Book correspondence**: This is the GLL descriptor range `(c_U, i)(c_V, j)` from listing `lst:gll_rsm_cfpq`.

### RangeDescriptor

```fsharp
[<RequireQualifiedAccess>]
type RangeDescriptor =
    | EmptyRange
    | NonEmptyRange of RangeKey
```

Describes a matched range (possibly empty) during GLL execution.

- `EmptyRange` — no input has been matched yet. Used for initial GSS descriptors.
- `NonEmptyRange of RangeKey` — a concrete matched range.

**Design rationale**:
- **`[<RequireQualifiedAccess>]`**: Prevents unqualified name collisions. `RangeDescriptor.EmptyRange` is unambiguous in context where `EmptyRange` could clash with other types.
- **Why a separate DU instead of `option<RangeKey>`**: A discriminated union carries semantic intent "empty range" vs. the sentinel `None`. In the GLL algorithm, the empty range has specific operational meaning (matching epsilon) and cannot be confused with absence of a range.

### PathIndexEntry

```fsharp
[<RequireQualifiedAccess>]
type PathIndexEntry<'t, 'nt when 't: comparison and 'nt: comparison> =
    | PTerminal of Terminal<'t>
    | PNonterminal of Nonterminal<'nt>
    | PEpsilonNonterminal of Nonterminal<'nt>
    | PIntermediate of state: int * pos: int
```

An entry stored in a path index cell — describes what was recognized in the corresponding range.

| Variant | Fields | Meaning |
|---------|--------|---------|
| `PTerminal` | `Terminal<'t>` | A terminal symbol was matched; the range covers one graph edge |
| `PNonterminal` | `Nonterminal<'nt>` | A nonterminal `A` was derived spanning the range `(i,p)→(j,q)`, where `i` is the call state and `j` is the return state of block A |
| `PEpsilonNonterminal` | `Nonterminal<'nt>` | A nonterminal `A` derives epsilon (`A ⇒* ε`), so it matches a zero-length range `(i,p)→(i,p)` with no input consumed |
| `PIntermediate` | `state: int, pos: int` | An intermediate GLL descriptor position; the range `(i,p)→(state,pos)` is partially matched and execution continues from `(state,pos)` |

**Design rationale**:
- **`PEpsilonNonterminal` as separate variant**: Epsilon derivations occupy the same-position cell `(i,p)→(i,p)` because no input is consumed. Without a dedicated variant, epsilon matches would collide with regular nonterminal entries at the same cell. The `PEpsilonNonterminal` variant distinguishes "A derives ε at this position" from "A spans a non-empty range that happens to start and end at the same position." The GLL algorithm checks `epsilonClosure` for nullable nonterminals and records `PEpsilonNonterminal` at same-state/same-vertex cells.
- **`PIntermediate` stores state and pos as named fields**: The `state` is the RSM state reached (global index), `pos` is the current vertex in the input graph. This represents a partial recognition in-progress.
- **All variants prefixed with `P`**: Distinguishes path index entries from SPPF node types and from the raw `Nonterminal`/`Terminal` wrappers, preventing name collisions when the module is opened.
- **Generic over `'t` and `'nt`**: Supports arbitrary terminal/nonterminal types. The `comparison` constraints are required because entries are stored in `Set<PathIndexEntry<'t, 'nt>>`.

### PathIndex

```fsharp
type PathIndex<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Matrix: Matrix<Set<PathIndexEntry<'t, 'nt>>>
      StateCount: int
      VertexCount: int }
```

The path index is a K×K matrix where **K = `StateCount` × `VertexCount`**. Cell `(fromIdx, toIdx)` stores the set of recognized entries for the range `(fromState, fromVertex)→(toState, toVertex)`.

**Design rationale**:
- **K×K square matrix**: Every (state, vertex) pair can be both a source and a target of a range. The matrix is square because any cell can be the start or end of a derivation.
- **Set-based cells**: A cell can contain multiple entries (e.g., both `PNonterminal(A)` and `PIntermediate(k, p)`). The `Set` type ensures determinism — duplicate additions are idempotent.
- **Explicit `StateCount`/`VertexCount`**: Stored alongside the matrix so that the linear index computation `idx(state, vertex) = state * VertexCount + vertex` has direct access to `VertexCount` without division. This is needed by `linearIndex`, `add`, `get`, and the invariant checker.

## Module Functions

All functions live in the `PathIndex` module.

### Core Cell Operations

#### `linearIndex`

```fsharp
val linearIndex: pi: PathIndex<'t, 'nt> -> state: int -> vertex: int -> int
```

Maps a `(state, vertex)` pair to a linear index in the path index matrix.

**Formula**: `idx(state, vertex) = state × pi.VertexCount + vertex`

**Preconditions**:
- `state ∈ [0, pi.StateCount)`
- `vertex ∈ [0, pi.VertexCount)`

**Postcondition**:
- Result `∈ [0, K)` where `K = pi.StateCount × pi.VertexCount`

#### `add`

```fsharp
val add: pi: PathIndex<'t, 'nt> -> fromState: int -> fromVertex: int -> toState: int -> toVertex: int -> entry: PathIndexEntry<'t, 'nt> -> unit
```

Adds an entry to the path index at range `(fromState, fromVertex) → (toState, toVertex)`. The function computes linear indices for both endpoints, retrieves the current set from the matrix cell, adds the entry via `Set.add`, and stores the updated set.

**Behavior**: Mutates the underlying matrix cell in-place via `Matrix.set`. Repeatedly adding the same entry is idempotent (set semantics).

**Preconditions**:
- All state values `∈ [0, pi.StateCount)`
- All vertex values `∈ [0, pi.VertexCount)`

#### `get`

```fsharp
val get: pi: PathIndex<'t, 'nt> -> fromState: int -> fromVertex: int -> toState: int -> toVertex: int -> Set<PathIndexEntry<'t, 'nt>>
```

Gets the set of entries stored at range `(fromState, fromVertex) → (toState, toVertex)`. Returns the empty set if no entries have been added to that cell.

**Preconditions**:
- All state values `∈ [0, pi.StateCount)`
- All vertex values `∈ [0, pi.VertexCount)`

### Filtering

#### `filterNonterminals`

```fsharp
val filterNonterminals: entries: Set<PathIndexEntry<'t, 'nt>> -> Set<PathIndexEntry<'t, 'nt>>
```

Filters a set of path index entries to only those of type `PNonterminal` or `PEpsilonNonterminal`. Used internally by `checkCalleeReachabilityInvariant` to extract nonterminal entries from each cell.

### Invariant Checking

#### `checkCalleeReachabilityInvariant`

```fsharp
val checkCalleeReachabilityInvariant:
    pi: PathIndex<'t, 'nt>
    -> blockStart: Map<Nonterminal<'nt>, int>
    -> blockFinals: Map<Nonterminal<'nt>, Set<int>>
    -> Result<unit, string list>
```

Checks the **callee-reachability invariant**: for every cell `(i,p)→(j,q)` that contains `PNonterminal(A)` or `PEpsilonNonterminal(A)`, at least one **callee cell** `(s_A,p)→(f_A,q)` must be non-empty, where:
- `s_A = blockStart[A]` — the global start state of block A
- `f_A ∈ blockFinals[A]` — any final state of block A

**Parameters**:
| Parameter | Description |
|-----------|-------------|
| `pi` | The path index to check |
| `blockStart` | Map from nonterminal to its block's start state (global index) |
| `blockFinals` | Map from nonterminal to the set of its block's final states |

**Returns**:
- `Ok ()` if the invariant holds for all cells.
- `Error errors` where `errors` is a list of diagnostic messages, one per violation. Each message includes the cell coordinates, the nonterminal name, and the expected callee cell range that was empty.

**Algorithm**:
1. Iterate over all K×K cells of the path index matrix.
2. For each cell, extract nonterminal entries via `filterNonterminals`.
3. For each nonterminal `A` found:
   - Look up `s_A` from `blockStart` and `F_A` from `blockFinals`.
   - Check whether at least one `f_A ∈ F_A` has a non-empty cell `(s_A, p)→(f_A, q)`.
   - If no such cell exists, record an error message.

**Design rationale**:
- **Replaces the old `checkNonterminalInvariant`**: The previous invariant only checked that nonterminal entries exist. The callee-reachability invariant is stronger: it verifies that when a nonterminal is claimed to span a range, the underlying block machinery actually supports that claim.
- **Accumulates all errors**: Instead of failing on the first violation, the function collects all violations and returns them as a list. This enables comprehensive diagnostics in a single pass.
- **Uses `Map.TryGetValue`**: Gracefully handles nonterminals not present in `blockStart`/`blockFinals` by reporting an error rather than throwing.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Linear index: `state × VertexCount + vertex` | Row-major layout; vertex varies fastest. The inverse mapping recovers state via `idx / VertexCount` and vertex via `idx % VertexCount`, used in the invariant checker |
| `PEpsilonNonterminal` as separate variant | Epsilon derivations at same-position cells `(i,p)→(i,p)` must be distinguishable from regular nonterminal entries that span a non-empty range. Without the separation, `A ⇒* ε` and `A ⇒+ w` for `|w| ≥ 1` could collide at the same cell |
| K×K matrix (not sparser) | Every (state, vertex) pair is a potential range endpoint. The worst case during GLL is that every pair is reachable from every other pair. A full K×K avoids resizing during execution |
| `Set` as cell value type | Idempotent addition, deterministic iteration order, structural equality |
| `RangeDescriptor` as DU, not `option<RangeKey>` | Carries semantic intent "empty matched range" vs. "no range"; the empty range is a first-class concept in GLL |
| `RangeKey` as struct record | Value semantics with named fields; avoids heap allocation for the high-volume range comparisons in GLL |
| `checkCalleeReachabilityInvariant` collects all errors | Comprehensive diagnostics in a single pass; the caller can report all violations at once rather than fixing and re-running |

## Book References

| Reference | Description |
|-----------|-------------|
| `sec:CFPQ_GLL` | Core GLL algorithm for CFPQ with RSMs. The path index is the central data structure built during GLL execution |
| `lst:gll_rsm_cfpq` | Pseudocode listing for the GLL algorithm that populates and queries the path index |
| Chapter 6, `06_GLL_Based.tex` | Full chapter on GLL-based CFPQ with Recursive State Machines |

## Relationship to Other Modules

- **`GLL`** (`src/FLPQ.Languages/GLL.fs`): Builds and queries the path index during GLL parsing. The `buildPathIndex` function populates the path index; `buildSppfFromIndex` reads from it to construct the SPPF.
- **`Matrix`** (`src/FLPQ.LinearAlgebra/Matrix.fs`): The underlying K×K matrix is a `Matrix<Set<PathIndexEntry<'t, 'nt>>>` using the generic matrix type.
- **`Grammar`** (`src/FLPQ.Languages/Grammar.fs`): Provides `Terminal<'t>` and `Nonterminal<'nt>` types used in `PathIndexEntry` variants.
- **`RSM`** (`src/FLPQ.Languages/RSM.fs`): The `blockStart` and `blockFinals` maps passed to `checkCalleeReachabilityInvariant` come from RSM block metadata.
