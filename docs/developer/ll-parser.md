# LL Parser Module

**Tags:** algorithm, parsing, ll, recursive-descent, grammar, derivation-tree
**Kind:** algorithm
**Module:** LLParser
**Source:** `src/FLPQ.Languages/LLParser.fs`
**Depends on:** Grammar, FirstFollow, DerivationTree, VisualizationTypes
**Used by:** FLPQ.Cli
**Book reference:** LL(k) parsing chapter

> **Abstract:** Implements LL(k) parsing: table construction (via first_k/follow_k fixed-point computation) and table-driven recursive descent with derivation tree building. Uses mutable tree nodes on a unified stack for in-place tree construction, with immutable snapshots per step for visualization. Provides both `parse` (tree only) and `parseWithSteps` (tree + visualization steps).

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

### Table Construction

1. Compute `first_k` and `follow_k` for the grammar.
2. For each rule `A → α`:
   - If `α = [Epsilon]`, LA = `follow(A)`.
   - Otherwise, LA = `first_k(α)`. If ε ∈ `first_k(α)`, add `follow(A)` as well.
   - For each `w ∈ LA`, add entry `(A, w) → ruleIdx` to the table.
3. If a table entry already exists with a different rule index, throw an LL(k) conflict exception.

### Table-Driven Parsing

1. Tokenize the input: convert terminals to symbol list.
2. Create root `MutableTree(N g.start)` and a single-element stack `[root]`.
3. At each iteration:
   - **Record step**: snapshot the root tree to immutable, snapshot each stack node to `LLStackLeaf`.
   - **Top is Terminal**: if matches input symbol, pop and advance position.
   - **Top is Epsilon**: pop without consuming input.
   - **Top is Nonterminal**: compute lookahead, look up table, set node's `Children` to new `MutableTree` nodes for RHS symbols, push RHS nodes onto stack (first on top). If no table entry, fail.
4. When stack is empty and all input consumed, convert root to immutable via `ToImmutable()`.

**Preconditions:** `k ≥ 1`, grammar must be `Grammar<string, string>`.

## Type Definitions

### `LLStackLeaf<'t, 'nt>` (struct)
```fsharp
[<Struct>]
type LLStackLeaf<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      path: int list }
```
Identifies a leaf node currently on the stack by its immutable snapshot and its path from the tree root.

The `path` is a list of child indices from the root to the leaf (e.g., `[0; 1]` means root → child[0] → child[1]). This enables the DOT renderer to locate stack nodes in the full tree.

### Derivation Tree Types
The derivation tree type (`DerivationTree<'t,'nt>`) and the mutable tree type (`MutableTree<'t,'nt>`) are defined in the [DerivationTree module](derivation-tree.md).

## Function Signatures

### `LLParser.buildTable`
```fsharp
val buildTable: Grammar<'t, 'nt> -> k: int -> Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>
```
Constructs an LL(k) parsing table. Lookahead is a list of grammar symbols; end-of-input is `[Epsilon]`.

**Postconditions:**
- Returns a map from `(nonterminal, lookahead_string)` to rule index.
- All lookahead strings have length ≤ `k`.
- Throws on LL(k) conflict (two productions for the same nonterminal and lookahead).

### `LLParser.parseWithSteps`
```fsharp
val parseWithSteps: Grammar<'t, 'nt> -> table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int> -> k: int -> terminals: Terminal<'t> list -> Option<DerivationTree<'t, 'nt>> * LLParsingStep<'t, 'nt> list
```
Table-driven LL(k) recursive descent parser that builds a derivation tree and collects visualization steps.

### `LLParser.parse`
```fsharp
val parse: Grammar<'t, 'nt> -> table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int> -> k: int -> terminals: Terminal<'t> list -> Option<DerivationTree<'t, 'nt>>
```
Same as `parseWithSteps` but returns only the tree (no steps).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Table is `Map<(Nonterminal * Symbol list), int>` | Simple lookup; nonterminal + lookahead uniquely identifies a rule |
| Conflict detection throws exception | Immediate feedback during table construction |
| Lookahead from first_k and follow_k | Standard LL(k) construction from the book |
| End-of-input lookahead is `[Epsilon]` | Consistent with explicit Epsilon symbol in first/follow sets |
| Mutable tree nodes on stack | When a nonterminal leaf is expanded, existing mutable node gets children set in-place, children become new stack — no separate marker frames needed |
| Immutable snapshot per step | At each step, root tree is snapshotted to `DerivationTree` and each stack node snapshotted to `LLStackLeaf` with path |
| Parent pointers in `MutableTree` | Enable `GetPath()` for computing each stack node's path from root. Used by DOT renderer to locate stack leaves |
| Derivation tree in separate module | Shared by LL and LR parsers; avoids circular dependencies |

## Book Reference

The LL(k) parsing algorithm corresponds to the book's coverage of top-down parsing with lookahead. The table construction uses standard first_k and follow_k computations. The parser is a table-driven implementation of the predictive parsing algorithm.

## See Also

- [LR parser](lr-parser.md) — bottom-up counterpart
- [DerivationTree module](derivation-tree.md) — derivation tree types
- [First/Follow module](first-follow.md) — first_k/follow_k computation
- [Grammar module](grammar.md) — grammar types
