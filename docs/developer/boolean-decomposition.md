# BooleanDecomposition Module

**Tags:** algorithm, boolean-decomposition, sparse-matrix, matrix, boolean
**Kind:** algorithm
**Module:** BooleanDecomposition
**Source:** `src/FLPQ.LinearAlgebra/BooleanDecomposition.fs`
**Depends on:** Matrix
**Used by:** BelyaninRPQ, ArroyueloRPQ
**Book reference:** Definition def:BoolDecomposition

> **Abstract:** Implements Boolean decomposition of set-valued matrices and optional-non-empty-set matrices into families of Boolean matrices (one per distinct element). The inverse operation reconstructs the original set-valued matrix from the decomposition. Used by Belyanin and Arroyuelo RPQ algorithms to convert multi-labeled transition matrices into per-label boolean matrices.

## Contents

- [Algorithm](#algorithm)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

**Decomposition:** For a matrix M over sets P(L), produce a family {M_l | l ∈ L} where:
```
M_l[i,j] = 1 if l ∈ M[i,j]
M_l[i,j] = 0 otherwise
```

**Recomposition:** The inverse operation — given {M_l}, reconstruct M:
```
M[i,j] = {l | M_l[i,j] = 1}
```

## Function Signatures

### `decompose`
```fsharp
val decompose: Matrix<Set<'a>> -> Map<'a, Matrix<bool>>
```
Decomposes a matrix of sets into a map from each distinct element to a Boolean matrix of the same dimensions. The Boolean matrix at key `e` has `true` at position `[i,j]` iff `e ∈ original[i,j]`.

**Postcondition**: For each key `e` in the result, `result[e].rows = original.rows` and `result[e].cols = original.cols`.

### `decomposeNonEmptySet`
```fsharp
val decomposeNonEmptySet: Matrix<Option<NonEmptySet<'a>>> -> Map<'a, Matrix<bool>>
```
Decomposes a matrix of option-of-non-empty-sets. None cells are treated as empty. Equivalent semantics to `decompose` but works directly on the `NonEmptySet` representation used by automaton transition matrices.

### `recompose`
```fsharp
val recompose: Map<'a, Matrix<bool>> -> Matrix<Set<'a>>
```
Reconstructs a set-valued matrix from a decomposition. Each cell `[i,j]` contains the set of all elements whose corresponding Boolean matrix has `true` at that position.

**Preconditions:**
- The map must be non-empty (throws `ArgumentException` otherwise)
- All Boolean matrices must have the same dimensions

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| `Map<'a, Matrix<bool>>` as representation | Natural key-value structure; O(log n) lookup for element access |
| `decompose` collects all distinct elements first | Ensures each distinct element gets exactly one Boolean matrix; avoids duplication |
| `recompose` requires non-empty map | Dimensions can't be inferred from an empty map |
| Type parameter `'a` requires `comparison` | Required by `Map` and `Set`; satisfied by all realistic element types |

## Book Reference

Definition `\label{def:BoolDecomposition}`:
> Boolean decomposition of an adjacency matrix M (built over P(L)) is the family of Boolean matrices {M_l | l ∈ L}, where each M_l is defined as M_l[i,j] = 1 if l ∈ M[i,j], 0 otherwise.

## See Also

- [Belyanin RPQ](belyanin-rpq.md) — uses BooleanDecomposition for per-label matrices
- [Arroyuelo RPQ](arroyuelo-rpq.md) — uses BooleanDecomposition for per-label matrices
- [Matrix module](matrix.md) — underlying matrix type
