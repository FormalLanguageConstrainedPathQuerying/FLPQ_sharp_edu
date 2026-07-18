# DerivationTree Module

**Tags:** data-structure, derivation-tree, grammar, parsing
**Kind:** data-structure
**Module:** DerivationTree
**Source:** `src/FLPQ.Languages/DerivationTree.fs`
**Depends on:** Grammar
**Used by:** LLParser, LRParser, GLL, RNGLR
**Book reference:** _(standard parse tree representation)_

> **Abstract:** Shared derivation tree types used by LL, LR, GLL, and RNGLR parsers to represent parse results. Provides both immutable (`DerivationTree<'t,'nt>`) and mutable (`MutableTree<'t,'nt>`) representations. The immutable type is the final parse result; the mutable type supports in-place tree construction during LL parsing with parent pointers for efficient path computation. Leaf collection (`leaves`) reproduces the input token sequence.

## Contents

- [Data Structure](#data-structure)
- [Type Definitions](#type-definitions)
- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Data Structure

A derivation tree represents how a string is derived from a grammar's start symbol:
- **Leaf nodes** hold grammar symbols (terminal, epsilon) — these form the input string when concatenated.
- **Internal nodes** hold a nonterminal and a list of child subtrees — the children correspond to one RHS expansion.
- The tree is rooted at the start symbol; a successful parse produces exactly one root-to-leaf spanning of the entire input.

## Type Definitions

### `DerivationTree<'t, 'nt>`
```fsharp
type DerivationTree<'t, 'nt> =
    | Leaf of Symbol<'t, 'nt>
    | Node of Nonterminal<'nt> * DerivationTree<'t, 'nt> list
```
Immutable discriminated union representing a parse tree. `Leaf(sym)` holds any grammar symbol (terminal or epsilon); `Node(nt, children)` is a nonterminal with its expansion.

### `MutableTree<'t, 'nt>`
```fsharp
type MutableTree<'t, 'nt>(sym: Symbol<'t, 'nt>) =
    member val Symbol: Symbol<'t, 'nt> with get, set
    member val Children: MutableTree<'t, 'nt> list with get, set
    member val Parent: MutableTree<'t, 'nt> option with get, set

    member this.ToImmutable() : DerivationTree<'t, 'nt>
    member this.GetPath() : int list
```
Mutable class for in-place tree construction during LL parsing. `Children` are set when a nonterminal expands. `Parent` pointers enable O(log n) path computation for visualization. `ToImmutable()` converts to the immutable type; internal nodes become `Node`, everything else becomes `Leaf`.

## Module Functions

### `DerivationTree.leaves`
```fsharp
val leaves: DerivationTree<'t, 'nt> -> 't list
```
Collects all leaf terminal values in left-to-right traversal order. Epsilon leaves contribute nothing.

**Postconditions:** Concatenating the result reproduces the original input string (modulo epsilon leaves).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Separate module | Shared by LL, LR, GLL, and RNGLR parsers; avoids circular dependencies |
| Generic over `'t` and `'nt` | Works with arbitrary terminal/nonterminal representations |
| `Leaf` carries `Symbol` | Epsilon is a Symbol case; no separate DU case needed |
| `leaves` returns `'t list`, not strings | Caller controls joining format |
| `MutableTree` as class with mutable properties | Enables in-place tree construction: leaf nodes on stack get children added when expanded |
| Parent pointers in `MutableTree` | Enable O(log n) path computation for visualization |

## Book Reference

Derivation trees are the standard representation of parse results in context-free parsing. Both top-down (LL) and bottom-up (LR) parsers produce derivation trees as output.

## See Also

- [LL parser](ll-parser.md) — uses MutableTree for in-place tree construction
- [LR parser](lr-parser.md) — produces immutable DerivationTree
- [SPPF module](sppf.md) — packed forest; enumerateTrees yields DerivationTree
- [Grammar module](grammar.md) — Symbol, Nonterminal types
