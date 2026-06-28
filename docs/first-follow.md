# FirstFollow Module

First_k and follow_k set computations for context-free grammars. Used by LL and LR parsing table construction.

## Function Signatures

### `FirstFollow.firstK`

```fsharp
val firstK: ('t -> string) -> Grammar<'t, 'nt> -> int -> Map<Nonterminal<'nt>, Set<string>>
```

Computes the first_k set for every nonterminal in the grammar.
`first_k(A)` is the set of all terminal strings of length ≤ k that can begin strings derived from A.
The empty string ε is represented as `""`.

**Algorithm:** Fixed-point iteration over all productions. Initializes each nonterminal with the first terminals of its productions (truncated to k). Iteratively adds `first_k(α)` for each production A → α until no changes occur.

**Parameters:**
- `terminalToString`: converts a terminal value to its string representation for lookahead computation.
- `k`: maximum lookahead length (strings longer than k are truncated).

### `FirstFollow.followK`

```fsharp
val followK: ('t -> string) -> Grammar<'t, 'nt> -> int -> Map<Nonterminal<'nt>, Set<string>>
```

Computes the follow_k set for every nonterminal in the grammar.
`follow_k(A)` is the set of all terminal strings of length ≤ k that can appear immediately after A in some derivation from the start symbol.

**Algorithm:** Fixed-point iteration. Initializes the start symbol with `{""}` (ε). For each production A → αBβ:
- Add `first_k(β)` to `follow(B)`.
- If ε ∈ `first_k(β)`, also add `follow(A)` to `follow(B)`.

### `FirstFollow.firstKOfString`

```fsharp
val firstKOfString: ('t -> string) -> Map<Nonterminal<'nt>, Set<string>> -> int -> Symbol<'t, 'nt> list -> Set<string>
```

Computes first_k for an arbitrary string of grammar symbols (concatenation of first_k sets with truncation).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Generic over terminal type | firstK/followK can be used with any grammar, not only string-based ones. The `terminalToString` function adapts arbitrary terminals to string lookahead. |
| Fixed-point iteration with mutable maps | Standard approach; clear termination condition (set sizes stop growing). |
| `""` represents ε | Consistent with string-based lookahead representation across parsers. |
| `productTrunc` for concatenation | Concatenates two sets of k-length strings with truncation to at most k characters. |

## Book Relationship

First_k and follow_k are standard concepts from the book's coverage of LL and LR parsing. The fixed-point computation follows the textbook definition.
