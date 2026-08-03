# SPPF Parsing Table Types

**Tags:** parsing-table, sppf, data-structure, cyk, valiant, basic-sppf
**Kind:** data-structure
**Module:** ParsingTable
**Source:** `src/FLPQ.Languages/ParsingTable.fs`
**Depends on:** Matrix, Grammar
**Used by:** Cyk, Valiant

> **Abstract:** Defines enriched parsing table types for BasicSPPF construction. `SppfParsingEntry<'nt>` stores `(nonterminal, splitPoint, productionIndex)` tuples enabling SPPF reconstruction from CYK/Valiant parse tables. `SppfParsingTable<'nt>` wraps an `N×N` matrix of entry sets. Also contains `ParsingTable<'nt>` (set-based for acceptance only) and `LRAction<'a>` (LR/RNGLR table actions).

## Contents

- [Data Structure](#data-structure)
- [Type Definitions](#type-definitions)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Data Structure

### ParsingTable

The standard parsing table used by CYK and Valiant for boolean acceptance checking. Each cell `(i, j)` stores the set of nonterminals that derive the substring `w[i..j]`. The table size is `N×N` where `N` is the input length.

**Invariant:** Cell `(i, j)` is populated if and only if `i ≤ j` (upper-triangular). Cells with `i > j` are always empty.

### SppfParsingTable

An enriched parsing table for BasicSPPF reconstruction. Each cell `(i, j)` stores a set of `(nonterminal, splitPoint, productionIndex)` tuples where:
- `nonterminal` — the nonterminal deriving `w[i..j]`
- `splitPoint` — for terminal rules: position of the terminal character (equal to i); for binary rules: the index `k` where the left child spans `[i, k]` and the right child spans `[k+1, j]`
- `productionIndex` — 0-based index of the CNF grammar rule that produced this derivation

**Invariant:** For every entry at cell `(i, j)`:
- If rule at `productionIndex` is terminal `A → a`: `splitPoint = i`, and position `i` in input is `a`
- If rule at `productionIndex` is binary `A → B C`: cell `(i, splitPoint)` contains a nonterminal for `B`, and cell `(splitPoint+1, j)` contains a nonterminal for `C`

## Type Definitions

### SppfParsingEntry<'nt>

```fsharp
type SppfParsingEntry<'nt when 'nt: comparison> = Nonterminal<'nt> * int * int
```

Tuple of `(nonterminal, splitPoint, productionIndex)`. Represents a single derivation step that can be used to reconstruct the parse forest.

### SppfParsingTable<'nt>

```fsharp
type SppfParsingTable<'nt when 'nt: comparison> = Matrix<Set<SppfParsingEntry<'nt>>>
```

Matrix where cell `(i, j)` stores all possible ways to derive `w[i..j]` using the CNF grammar. Empty cells contain `Set.empty`.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Tuple over record for SppfParsingEntry | Minimizes allocations in hot loops; fields are self-evident from naming convention |
| Separate types for ParsingTable and SppfParsingTable | ParsingTable is Set<Nonterminal<'nt>> for acceptance-only use; SppfParsingTable adds splitPoint and productionIndex for SPPF construction |
| splitPoint semantics for terminal rules | Set to position i (terminal position) for uniform access pattern: child is always `Terminal(a, splitPoint, splitPoint+1)` |
| productionIndex stored directly | Enables O(1) lookup of the CNF rule during SPPF construction without searching the grammar |
| Type defined in ParsingTable.fs | Both ParsingTable and SppfParsingTable serve the same purpose (storing algorithm intermediate results); `LRAction` co-located as another parsing-table type |

## Book Reference

Def:basicSPPF in section sec:basicSPPF. The enriched table format is a book extension — classical CYK and Valiant use boolean/set-based tables for acceptance only; the enriched format adds traceability for parse forest construction.

## See Also

- [CYK algorithm](cyk.md) — produces SppfParsingTable
- [Valiant algorithm](valiant.md) — produces SppfParsingTable
- [Basic SPPF](sppf.md) — consumes SppfParsingTable for parse forest construction
- [Matrix module](matrix.md) — underlying matrix type
