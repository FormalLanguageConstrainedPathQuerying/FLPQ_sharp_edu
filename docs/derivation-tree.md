# DerivationTree Module

Shared derivation tree type used by both LL and LR parsers to represent parse results.

## Type Definitions

### `DerivationTree<'t, 'nt>`

```fsharp
type DerivationTree<'t, 'nt> =
    | Leaf of Terminal<'t>
    | Epsilon
    | Node of Nonterminal<'nt> * DerivationTree<'t, 'nt> list
```

Discriminated union representing a parse tree.

- `Leaf(Terminal t)`: a leaf node containing a terminal symbol `t`.
- `Epsilon`: an epsilon leaf, representing an empty production (ε).
- `Node(nt, children)`: an internal node labeled with nonterminal `nt` and a list of child subtrees.

A successful parse produces a tree rooted at the start symbol. The leaves collected left-to-right reproduce the input token sequence.

## Function Signatures

### `DerivationTree.leaves`

```fsharp
val leaves: DerivationTree<'t, 'nt> -> 't list
```

Collects all leaf terminal values from a derivation tree in left-to-right traversal order.
Epsilon nodes contribute nothing to the output.

**Postconditions:** Concatenating the result (with the tokenizer's separator — a space) reproduces the original input string (modulo epsilon leaves).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Separate module | Shared by LL and LR parsers; avoids circular dependencies. |
| Generic over `'t` and `'nt` | Not tied to string types; works with arbitrary terminal/nonterminal representations. |
| `Epsilon` as explicit case | Some productions derive ε; tree must record this fact. |
| `leaves` returns `'t list`, not strings | Caller controls joining format (space-separated for tokenizer consistency). |

## Book Relationship

Derivation trees are the standard representation of parse results in context-free parsing. Both top-down (LL) and bottom-up (LR) parsers produce derivation trees as output.
