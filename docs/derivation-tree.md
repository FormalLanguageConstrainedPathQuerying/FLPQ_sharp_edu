# DerivationTree Module

Shared derivation tree type used by both LL and LR parsers to represent parse results.

## Type Definitions

### `DerivationTree<'t, 'nt>`

```fsharp
type DerivationTree<'t, 'nt> =
    | Leaf of Symbol<'t, 'nt>
    | Node of Nonterminal<'nt> * DerivationTree<'t, 'nt> list
```

Discriminated union representing a parse tree.

- `Leaf(sym)`: a leaf node containing a grammar symbol `sym`. A terminal leaf is `Leaf(T(Terminal t))`; an epsilon leaf is `Leaf(Epsilon)`.
- `Node(nt, children)`: an internal node labeled with nonterminal `nt` and a list of child subtrees.

A successful parse produces a tree rooted at the start symbol. The leaves collected left-to-right reproduce the input token sequence.

## Function Signatures

### `DerivationTree.leaves`

```fsharp
val leaves: DerivationTree<'t, 'nt> -> 't list
```

Collects all leaf terminal values from a derivation tree in left-to-right traversal order.
Epsilon leaves contribute nothing to the output.

**Postconditions:** Concatenating the result (with the tokenizer's separator — a space) reproduces the original input string (modulo epsilon leaves).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Separate module | Shared by LL and LR parsers; avoids circular dependencies. |
| Generic over `'t` and `'nt` | Not tied to string types; works with arbitrary terminal/nonterminal representations. |
| `Leaf` carries `Symbol` | Epsilon is a Symbol case; no need for a separate `Epsilon` DU case in DerivationTree. Single point of truth for epsilon representation. |
| `leaves` returns `'t list`, not strings | Caller controls joining format (space-separated for tokenizer consistency). |

## Book Relationship

Derivation trees are the standard representation of parse results in context-free parsing. Both top-down (LL) and bottom-up (LR) parsers produce derivation trees as output.
