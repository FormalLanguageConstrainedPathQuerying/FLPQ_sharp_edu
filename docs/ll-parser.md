# LL Parser Module

Implementation of LL(k) parsing: table construction and table-driven recursive descent with derivation tree building.

## Type Definitions

### DerivationTree

```fsharp
type DerivationTree<'t, 'nt> =
    | Leaf of Terminal<'t>
    | Epsilon
    | Node of Nonterminal<'nt> * DerivationTree<'t, 'nt> list
```

Represents a parse tree produced by top-down parsing.

- `Leaf(Terminal t)`: a leaf node containing a terminal symbol `t`.
- `Epsilon`: an epsilon leaf, representing an empty production.
- `Node(nt, children)`: an internal node labeled with nonterminal `nt` and a list of child subtrees.

Defined in `DerivationTree.fs` (shared by both LL and LR parsers).

## Function Signatures

### `LLParser.buildTable`

```fsharp
val buildTable: Grammar<string, string> -> k: int -> Map<Nonterminal<string> * string, int>
```

Constructs an LL(k) parsing table for a given grammar and lookahead length `k`.

**Algorithm:**
1. Compute `first_k` and `follow_k` for the grammar using `FirstFollow.firstK` and `FirstFollow.followK`.
2. For each rule `A → α`:
   - Compute the lookahead set LA(A → α):
     - If `α = ε`, LA = `follow(A)`.
     - Otherwise, LA = `first_k(α)`. If `ε ∈ first_k(α)`, add `follow(A)` as well.
   - For each `w ∈ LA`, add entry `(A, w) → ruleIdx` to the table.
3. If a table entry already exists with a different rule index, throw an exception indicating an LL(k) conflict.

**Preconditions:**
- Grammar must be a `Grammar<string, string>` (terminals and nonterminals are strings).
- `k ≥ 1`.

**Postconditions:**
- Returns a map from `(nonterminal, lookahead_string)` to rule index.
- All lookahead strings have length ≤ `k`.
- Throws on LL(k) conflict (two productions for the same nonterminal and lookahead).

### `LLParser.parse`

```fsharp
val parse: Grammar<string, string> -> table: Map<Nonterminal<string> * string, int> -> k: int -> input: string -> Option<DerivationTree<string, string>>
```

Table-driven LL(k) recursive descent parser that builds a derivation tree.

**Algorithm:**
1. Tokenize the input using `Tokenizer.tokenizeStrings` (space-separated terminals).
2. Maintain a stack of grammar symbols (initially `[N(start)]`).
3. While the stack is non-empty:
   - Pop the top symbol.
   - If it's a terminal, match against the current input token (advance if matched, fail otherwise).
   - If it's a nonterminal, compute the lookahead string (next `k` tokens without spaces), look up the table entry `(nt, la)`, and expand using the indicated rule.
   - Build a derivation tree: push `Leaf` nodes for matched terminals, `Epsilon` for epsilon productions, and `Node` for nonterminal expansions (children populated after complete expansion).

**Preconditions:**
- The table must be constructed by `buildTable` for the same grammar and `k`.

**Postconditions:**
- `Some(tree)` if the input is accepted, `None` otherwise.
- On acceptance, `DerivationTree.leaves tree` joined by spaces equals the input string (modulo epsilon leaves).
- The root of the tree is `Node(g.start, children)`.

### `DerivationTree.leaves`

```fsharp
val leaves: DerivationTree<'t, 'nt> -> 't list
```

Collects all leaf terminal values from a derivation tree in left-to-right order. Epsilon nodes contribute nothing.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Table is `Map<(Nonterminal * string), int>` | Simple lookup; nonterminal + lookahead uniquely identifies a rule. |
| Conflict detection throws exception | Immediate feedback during table construction. LL(k) conflict means the grammar is not LL(k) for the given k. |
| Tokenizer uses space-separated terminals | Supports multi-character terminals; consistent with all other parsers. |
| Derivation tree in separate module | Shared by LL and LR parsers; avoids circular dependencies. |
| Lookahead from first_k and follow_k | Standard LL(k) construction from the book. |

## Book Relationship

The LL(k) parsing algorithm corresponds to the book's coverage of top-down parsing with lookahead. The table construction uses standard first_k and follow_k computations. The parser is a table-driven implementation of the predictive parsing algorithm.

## Grammar LL(k) Compatibility

| Grammar | LL(1) | Notes |
|---------|-------|-------|
| grammar1 (S → aSbS \| ε) | Yes | No conflicts, all entries distinct |
| grammar3 (S → aS \| a) | No | Left-factoring conflict: both productions start with "a" |
| grammar8 (E → T+E \| T) | No | Left-factoring conflict |
| grammar7 (E → E+T \| T) | No | Left-recursive; not LL(k) for any k |
