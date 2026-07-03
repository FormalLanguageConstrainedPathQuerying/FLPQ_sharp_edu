# LL Parser Module

Implementation of LL(k) parsing: table construction and table-driven recursive descent with derivation tree building.

## Type Definitions

The derivation tree type is defined in the [DerivationTree module](derivation-tree.md) and shared by LL and LR parsers.

## LL Stack Frame Types

The `LLStackFrame` type (`VisualizationTypes.fs`) has two variants:

```fsharp
type LLStackFrame<'t, 'nt> =
    | LLTree of DerivationTree<'t, 'nt>
    | LLMarker of Nonterminal<'nt> * int
```

- `LLTree` carries a tree node (a frontier symbol – terminal, epsilon, or nonterminal leaf).
- `LLMarker(nt, n)` marks the boundary of a nonterminal expansion with `n` expected children. When the marker reaches the top of the stack, `n` trees are popped from `completed` and combined into `Node(nt, children)`.

## Function Signatures

### `LLParser.buildTable`

```fsharp
val buildTable: Grammar<string, string> -> k: int -> Map<Nonterminal<string> * Symbol<string, string> list, int>
```

Constructs an LL(k) parsing table for a given grammar and lookahead length `k`.
Lookahead is a list of grammar symbols; end-of-input is `[Epsilon]`.

**Algorithm:**
1. Compute `first_k` and `follow_k` for the grammar using `FirstFollow.firstK` and `FirstFollow.followK`.
2. For each rule `A → α`:
   - Compute the lookahead set LA(A → α):
     - If `α = [Epsilon]`, LA = `follow(A)`.
     - Otherwise, LA = `first_k(α)`. If ε ∈ `first_k(α)`, add `follow(A)` as well.
   - For each `w ∈ LA`, add entry `(A, w) → ruleIdx` to the table.
3. If a table entry already exists with a different rule index, throw an exception indicating an LL(k) conflict.

**Preconditions:**
- Grammar must be a `Grammar<string, string>` (terminals and nonterminals are strings).
- `k ≥ 1`.

**Postconditions:**
- Returns a map from `(nonterminal, lookahead_string)` to rule index.
- All lookahead strings have length ≤ `k`.
- Throws on LL(k) conflict (two productions for the same nonterminal and lookahead).

### `LLParser.parseWithSteps`

```fsharp
val parseWithSteps: Grammar<'t, 'nt> -> table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int> -> k: int -> terminals: Terminal<'t> list -> Option<DerivationTree<'t, 'nt>> * LLParsingStep<'t, 'nt> list
```

Table-driven LL(k) recursive descent parser that builds a properly nested derivation tree using a unified stack with markers. Also collects visualization steps.

**Algorithm:**
1. Tokenize the input using `Tokenizer.tokenize` (space-separated terminals).
2. Maintain a unified stack of `LLStackFrame` (initially `[LLTree(Leaf(N start))]`).
3. Also maintain a `completed` list of fully-built subtree roots.
4. While the stack is non-empty:
   - **Terminal** (`LLTree(Leaf(T(t)))`): match against current input symbol. If matched, advance input position and add `Leaf(T(Terminal t))` to `completed`. If not matched, fail.
   - **Epsilon** (`LLTree(Leaf(Epsilon))`): add `Leaf(Epsilon)` to `completed` without consuming input.
   - **Nonterminal** (`LLTree(Leaf(N nt))` or `LLTree(Node(nt, _))`): compute lookahead, look up the table, and expand: push RHS symbols as `LLTree(Leaf sym)`, followed by `LLMarker(nt, childrenCount)`.
   - **Marker** (`LLMarker(nt, n)`): pop `n` trees from the end of `completed`, reverse, build `Node(nt, children)`, add to `completed`.
5. When stack is empty and all input consumed, the last item in `completed` is the result tree.

**Preconditions:**
- The table must be constructed by `buildTable` for the same grammar and `k`.

**Postconditions:**
- `Some(tree)` if the input is accepted, `None` otherwise.
- On acceptance, `DerivationTree.leaves tree` joined by spaces equals the input string (modulo epsilon leaves).
- The tree is properly nested: intermediate nonterminals are preserved as `Node` in the tree, not flattened.
- The root of the tree is `Node(g.start, children)`.
- Each `LLParsingStep` contains the current stack, the current `completed` list (roots of fully-built subtrees), and the input state.

### `LLParser.parse`

```fsharp
val parse: Grammar<string, string> -> table: Map<Nonterminal<string> * Symbol<string, string> list, int> -> k: int -> terminals: Terminal<'t> list -> Option<DerivationTree<string, string>>
```

Same as `parseWithSteps` but returns only the tree (no steps).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Table is `Map<(Nonterminal * Symbol list), int>` | Simple lookup; nonterminal + lookahead uniquely identifies a rule. Lookahead is a Symbol list, naturally matching first/follow results. |
| Conflict detection throws exception | Immediate feedback during table construction. LL(k) conflict means the grammar is not LL(k) for the given k. |
| Tokenizer uses space-separated terminals | Supports multi-character terminals; consistent with all other parsers. |
| Derivation tree in separate module | Shared by LL and LR parsers; avoids circular dependencies. |
| Lookahead from first_k and follow_k | Standard LL(k) construction from the book. |
| End-of-input lookahead is `[Epsilon]` | Consistent with explicit Epsilon symbol in first/follow sets. No string/symbol conversion needed. |
| Unified stack with markers (`LLStackFrame`) | `LLTree` frames carry tree nodes for the current frontier. `LLMarker` frames track nonterminal boundaries, enabling properly nested tree construction. When a marker reaches the top of the stack, completed children are combined into a `Node`. This eliminates the flat-tree problem of the previous approach. |
| `completed` list in step data | Each step captures the roots of fully-built subtrees. The DOT visualizer renders both the stack frontier and the completed subtrees, giving a complete picture of the parse progress. |

## Book Relationship

The LL(k) parsing algorithm corresponds to the book's coverage of top-down parsing with lookahead. The table construction uses standard first_k and follow_k computations. The parser is a table-driven implementation of the predictive parsing algorithm.

## Grammar LL(k) Compatibility

| Grammar | LL(1) | Notes |
|---------|-------|-------|
| grammar1 (S → aSbS \| ε) | Yes | No conflicts, all entries distinct |
| grammar3 (S → aS \| a) | No | Left-factoring conflict: both productions start with "a" |
| grammar8 (E → T+E \| T) | No | Left-factoring conflict |
| grammar7 (E → E+T \| T) | No | Left-recursive; not LL(k) for any k |
