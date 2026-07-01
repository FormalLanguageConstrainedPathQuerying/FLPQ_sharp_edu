# LL Parser Module

Implementation of LL(k) parsing: table construction and table-driven recursive descent with derivation tree building.

## Type Definitions

The derivation tree type is defined in the [DerivationTree module](derivation-tree.md) and shared by LL and LR parsers.

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

### `LLParser.parse`

```fsharp
val parse: Grammar<string, string> -> table: Map<Nonterminal<string> * Symbol<string, string> list, int> -> k: int -> terminals: Terminal<'t> list -> Option<DerivationTree<string, string>>
```

Table-driven LL(k) recursive descent parser that builds a derivation tree using a unified stack.

**Algorithm:**
1. Tokenize the input using `Tokenizer.tokenize` (space-separated terminals).
2. Maintain a single unified stack of `LLStackFrame` (initially `[LLFrame(N(start), Node(start, []))]`).
3. While the stack is non-empty:
   - Pop the top frame.
   - If it's a terminal: match against the current input symbol (advance if matched, fail otherwise). Add the frame's tree (`Leaf`) to completed subtrees.
   - If it's `Epsilon`: add `Leaf(Epsilon)` to completed subtrees without consuming input.
   - If it's a nonterminal: compute the lookahead (next `k` input symbols), look up the table entry, and expand using the indicated rule: push RHS symbols as new `LLFrame(sym, Leaf(sym))`.
4. Build the final derivation tree: `Node(g.start, completedSubtrees)`.

**Preconditions:**
- The table must be constructed by `buildTable` for the same grammar and `k`.

**Postconditions:**
- `Some(tree)` if the input is accepted, `None` otherwise.
- On acceptance, `DerivationTree.leaves tree` joined by spaces equals the input string (modulo epsilon leaves).
- The root of the tree is `Node(g.start, children)`.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Table is `Map<(Nonterminal * Symbol list), int>` | Simple lookup; nonterminal + lookahead uniquely identifies a rule. Lookahead is a Symbol list, naturally matching first/follow results. |
| Conflict detection throws exception | Immediate feedback during table construction. LL(k) conflict means the grammar is not LL(k) for the given k. |
| Tokenizer uses space-separated terminals | Supports multi-character terminals; consistent with all other parsers. |
| Derivation tree in separate module | Shared by LL and LR parsers; avoids circular dependencies. |
| Lookahead from first_k and follow_k | Standard LL(k) construction from the book. |
| End-of-input lookahead is `[Epsilon]` | Consistent with explicit Epsilon symbol in first/follow sets. No string/symbol conversion needed. |
| Unified stack (`LLStackFrame`) | Single stack replaces the previous dual-stack (symbol + tree). Each `LLFrame(sym, tree)` carries both a symbol and its tree node. The stack represents the tree frontier; current leaves of the partial tree are placed in stack and used as symbols. |

## Book Relationship

The LL(k) parsing algorithm corresponds to the book's coverage of top-down parsing with lookahead. The table construction uses standard first_k and follow_k computations. The parser is a table-driven implementation of the predictive parsing algorithm.

## Grammar LL(k) Compatibility

| Grammar | LL(1) | Notes |
|---------|-------|-------|
| grammar1 (S → aSbS \| ε) | Yes | No conflicts, all entries distinct |
| grammar3 (S → aS \| a) | No | Left-factoring conflict: both productions start with "a" |
| grammar8 (E → T+E \| T) | No | Left-factoring conflict |
| grammar7 (E → E+T \| T) | No | Left-recursive; not LL(k) for any k |
