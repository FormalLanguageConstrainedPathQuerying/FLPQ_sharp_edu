# CfpqSemiring Module

**Tags:** cfpq, context-free, semiring, path-index, all-paths, utility
**Kind:** algorithm
**Module:** CfpqSemiring
**Source:** `src/FLPQ.CFPQ/CfpqSemiring.fs`
**Depends on:** none (pure F#)
**Used by:** AzimovCfpq
**Book reference:** Chapter 12, Section sec:CFPQ_MatrixBased; LAGraph PR #8 (singlePath), PR #9 (allPaths)

> **Abstract:** The semiring abstraction that parameterizes Azimov's matrix-based CFPQ algorithm. Matrix cells are `'elem option` (None = no path); a `Semiring<'elem>` supplies the cell-level `Add`, the middle-vertex `Mult k`, and the direct-path `InitPath i j`. Three instances: Boolean reachability, singlePath (one derivation per cell, LAGraph PR #8), and allPaths (all split vertices per cell, LAGraph PR #9).

## Contents

- [Purpose](#purpose)
- [Type and Function Signatures](#type-and-function-signatures)
- [Semiring Instances](#semiring-instances)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Purpose

Azimov's algorithm (book listing `alg:graphParse`) is the same matrix fixed point for every CFPQ question that differs only in what a cell means. This module isolates that difference: the core in [AzimovCfpq](azimov-cfpq.md) runs one generic fixed point, and each semiring instance below answers a different question about the same graph and grammar.

## Type and Function Signatures

```fsharp
type Semiring<'elem> =
    { Add: 'elem -> 'elem -> 'elem
      Mult: int -> 'elem -> 'elem -> 'elem
      InitPath: int -> int -> 'elem }

val addCell: Semiring<'elem> -> 'elem option -> 'elem option -> 'elem option
val multCell: Semiring<'elem> -> int -> 'elem option -> 'elem option -> 'elem option
```

- `Add x y` — combines two candidate values for the same cell (the semiring's `+`).
- `Mult k x y` — joins a left value and a right value through middle vertex `k` (the semiring's `*`, with the middle vertex as parameter because path-based elements record it).
- `InitPath i j` — the value of a direct (height-1) path from `i` to `j`.
- `addCell` / `multCell` — lift the operations to `option` cells: None is the additive identity, and multiplication with a missing side yields no path.

## Semiring Instances

### ReachabilitySemiring : Semiring<bool>

The plain CFPQ reachability question. `Add = (||)`, `Mult _ x y = x && y`, `InitPath = true`. The answer is the start nonterminal's matrix itself ([AzimovCfpq.answer](azimov-cfpq.md)).

### SinglePathSemiring : Semiring<PathIndex>

One representative derivation per cell (LAGraph PR #8). A `PathIndex` is `{ Middle: int; Height: int }`: the middle vertex of a two-part derivation and the height of the derivation tree (a direct derivation has height 1).

- `Add` keeps the minimum height; ties go to the smaller middle vertex.
- `Mult k x y = { Middle = k; Height = max x.Height y.Height + 1 }`.
- `InitPath i j = { Middle = i; Height = 1 }`.

The fixed point alone stores only indices; [AzimovCfpq.extractSinglePath](azimov-cfpq.md) walks the indices back through the grammar to recover one concrete edge sequence.

### AllPathsSemiring : Semiring<AllPathsElem>

Every derivation's split structure per cell (LAGraph PR #9): the matrix contains information about all paths, not all walks or trails. An `AllPathsElem` is:

- `Base` — a direct derivation exists (one terminal edge, or epsilon on the diagonal);

- `Splits of NonEmptyList<int>` — sorted, deduplicated middle vertices (never empty: every constructor of the case produces at least one vertex); if the list contains `allPathsSentinel` (= `Int32.MaxValue`, never a real vertex index), a direct derivation exists as well.

- `Add` is the ordered union; `Base` absorbs into `Splits` as the sentinel.

- `Mult k _ _ = Splits [k]` (a singleton) — only the middle vertex is recorded, so the cell stays small no matter how many derivations share it.

- `InitPath _ _ = Base`.

[AzimovCfpq.enumerateAllPaths](azimov-cfpq.md) walks the split lists and yields exactly the simple paths (no repeated vertex), because a non-simple concatenation is filtered out and the visited set bounds the recursion.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| One `Semiring<'elem>` record, three instances | The user directive: one common computational core parameterized by the semiring (LAGraph_CFPQ_core style); the fixed point in AzimovCfpq is written once |
| Cells are `'elem option`, not a bottom element | `Matrix` has no per-cell bottom; `None` is the additive identity and keeps `addCell`/`multCell` total (LAGraph's bottom elements, e.g. `PathIndex{0,0}`, stay conceptual — no path is represented by `None`) |
| `Mult` takes the middle vertex `k` as a parameter | Path-based elements (singlePath, allPaths) must record where the two subpaths join; Boolean reachability ignores it |
| allPaths stores split vertices, not paths | Storing every path in a cell would blow up exponentially; storing the sorted split list keeps each cell O(V) and defers enumeration to `enumerateAllPaths`, which filters to simple paths |
| `allPathsSentinel = Int32.MaxValue` | A direct derivation must be representable inside `Splits` so that `Add` is a plain union; the sentinel can never collide with a vertex index of any realistic graph |

## See Also

- [AzimovCfpq module](azimov-cfpq.md) — the generic fixed point and path extraction/enumeration
- [CfpqInput module](cfpq-input.md) — the `CfpqGraph` type the semirings operate over
- [FLPQ.CFPQ hub](FLPQ.CFPQ.md) — project overview
- [PathSemiring module](path-semiring.md) — the analogous semiring for RPQ (Belyanin/Kronecker)
