# DerivationTree Module

Shared derivation tree types used by both LL and LR parsers to represent parse results.

## Type Definitions

### `DerivationTree<'t, 'nt>`

```fsharp
type DerivationTree<'t, 'nt> =
    | Leaf of Symbol<'t, 'nt>
    | Node of Nonterminal<'nt> * DerivationTree<'t, 'nt> list
```

Immutable discriminated union representing a parse tree.

- `Leaf(sym)`: a leaf node containing a grammar symbol `sym`. A terminal leaf is `Leaf(T(Terminal t))`; an epsilon leaf is `Leaf(Epsilon)`.
- `Node(nt, children)`: an internal node labeled with nonterminal `nt` and a list of child subtrees.

A successful parse produces a tree rooted at the start symbol. The leaves collected left-to-right reproduce the input token sequence.

### `MutableTree<'t, 'nt>`

```fsharp
type MutableTree<'t, 'nt>(sym: Symbol<'t, 'nt>) =
    member val Symbol: Symbol<'t, 'nt> with get, set
    member val Children: MutableTree<'t, 'nt> list with get, set
    member val Parent: MutableTree<'t, 'nt> option with get, set

    member this.ToImmutable() : DerivationTree<'t, 'nt>
    member this.GetPath() : int list
```

Mutable class for in-place derivation tree construction during LL parsing.

- `Symbol`: the grammar symbol at this node (terminal, epsilon, or nonterminal).
- `Children`: mutable list of child nodes. Initially `[]`; set when a nonterminal is expanded.
- `Parent`: mutable reference to the parent node. Used for computing the path from root to leaf (needed for visualization).
- `ToImmutable()`: converts the mutable tree to an immutable `DerivationTree`. Internal nodes (nonterminals with non-empty children) become `Node`; everything else becomes `Leaf`.
- `GetPath()`: returns the list of child indices from the root to this node (e.g., `[0; 1]` means root → child[0] → child[1]).

**Design rationale:** Mutable nodes allow leaf nodes stored in the stack to be updated in-place when a nonterminal is expanded. Children are added to the popped node, and the children become the new stack frontier. Parent pointers enable efficient path computation for visualization, avoiding structural searches.

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
| `MutableTree` as class with mutable properties | Enables in-place tree construction during LL parsing: leaf nodes on the stack get children added when expanded, and those children become the new stack. Final tree is converted to immutable `DerivationTree` via `ToImmutable()`. |
| Parent pointers in `MutableTree` | Enable O(log n) path computation for visualization. The path identifies each stack node's exact position in the tree, used by the DOT renderer to locate stack leaves in the full tree. |

## Book Relationship

Derivation trees are the standard representation of parse results in context-free parsing. Both top-down (LL) and bottom-up (LR) parsers produce derivation trees as output.
