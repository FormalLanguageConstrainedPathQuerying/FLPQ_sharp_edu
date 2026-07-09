# LL Parser Module

Implementation of LL(k) parsing: table construction and table-driven recursive descent with derivation tree building.

## Type Definitions

The derivation tree type (`DerivationTree<'t,'nt>`) and the mutable tree type (`MutableTree<'t,'nt>`) are defined in the [DerivationTree module](derivation-tree.md).

## LL Stack Leaf Type

The `LLStackLeaf` type (`VisualizationTypes.fs`) identifies a leaf node currently on the stack by its immutable snapshot and its path from the tree root:

```fsharp
[<Struct>]
type LLStackLeaf<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      path: int list }
```

The `path` is a list of child indices from the root to the leaf (e.g., `[0; 1]` means root → child[0] → child[1]). This enables the DOT renderer to locate stack nodes in the full tree.

## Function Signatures

### `LLParser.buildTable`

```fsharp
val buildTable: Grammar<'t, 'nt> -> k: int -> Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>
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

Table-driven LL(k) recursive descent parser that builds a derivation tree using mutable tree nodes on a unified stack. Also collects visualization steps.

**Algorithm:**
1. Tokenize the input: convert terminals to symbol list.
2. Create root `MutableTree(N g.start)` and a single-element stack `[root]`.
3. At each iteration:
   - **Record step**: snapshot the root tree to immutable, snapshot each stack node to `LLStackLeaf`.
   - **Top is Terminal**: if matches input symbol, pop and advance position.
   - **Top is Epsilon**: pop without consuming input.
   - **Top is Nonterminal**: compute lookahead, look up table, set the node's `Children` to new `MutableTree` nodes for RHS symbols (with parent pointers set), push RHS nodes onto stack (first on top). If no table entry, fail.
4. When stack is empty and all input consumed, convert root to immutable via `ToImmutable()`.

**Preconditions:**
- The table must be constructed by `buildTable` for the same grammar and `k`.

**Postconditions:**
- `Some(tree)` if the input is accepted, `None` otherwise.
- On acceptance, `DerivationTree.leaves tree` joined by spaces equals the input string (modulo epsilon leaves).
- The root of the tree is a `Node(g.start, children)`.
- Each `LLParsingStep` contains the full immutable tree snapshot, the stack leaf list, and the input state.

### `LLParser.parse`

```fsharp
val parse: Grammar<'t, 'nt> -> table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int> -> k: int -> terminals: Terminal<'t> list -> Option<DerivationTree<'t, 'nt>>
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
| Mutable tree nodes on stack | When a nonterminal leaf is expanded, the existing mutable node gets its children set in-place, and the children become the new stack. No separate marker frames or completed list needed. The tree is built top-down by mutating node children. |
| Immutable snapshot per step | At each step, the root tree is snapshotted to `DerivationTree` and each stack node is snapshotted to `LLStackLeaf` with its path. This captures the exact state at that moment, preserving correctness even though nodes are mutated later. |
| Parent pointers in `MutableTree` | Enable `GetPath()` for computing each stack node's path from root. Used by the DOT renderer to locate stack leaves in the full tree for dashed-edge connections. |

## Book Relationship

The LL(k) parsing algorithm corresponds to the book's coverage of top-down parsing with lookahead. The table construction uses standard first_k and follow_k computations. The parser is a table-driven implementation of the predictive parsing algorithm.

## Grammar LL(k) Compatibility

| Grammar | LL(1) | Notes |
|---------|-------|-------|
| grammar1 (S → aSbS \| ε) | Yes | No conflicts, all entries distinct |
| grammar3 (S → aS \| a) | No | Left-factoring conflict: both productions start with "a" |
| grammar8 (E → T+E \| T) | No | Left-factoring conflict |
| grammar7 (E → E+T \| T) | No | Left-recursive; not LL(k) for any k |
