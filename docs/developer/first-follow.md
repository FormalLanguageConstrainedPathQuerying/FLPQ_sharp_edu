# FirstFollow Module

First_k and follow_k set computations for context-free grammars. Used by LL and LR parsing table construction. Epsilon is represented explicitly as `[Epsilon]` (a singleton `Symbol` list), eliminating the need for string-based representations and `terminalToString` conversions.

## Function Signatures

### `FirstFollow.firstK`

```fsharp
val firstK: Grammar<'t, 'nt> -> int -> Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>
```

Computes the first_k set for every nonterminal in the grammar.
`first_k(A)` is the set of all terminal strings of length ≤ k that can begin strings derived from A.
The empty string ε is represented as `[Epsilon]`.

**Algorithm:** Fixed-point iteration over all productions. Initializes each nonterminal with the first symbols of its productions (truncated to k). Iteratively adds `first_k(α)` for each production A → α until no changes occur.

**Parameters:**
- `g`: the grammar.
- `k`: maximum lookahead length (lists longer than k are truncated).

### `FirstFollow.followK`

```fsharp
val followK: Grammar<'t, 'nt> -> int -> Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>
```

Computes the follow_k set for every nonterminal in the grammar.
`follow_k(A)` is the set of all terminal strings of length ≤ k that can appear immediately after A in some derivation from the start symbol.

**Algorithm:** Fixed-point iteration. Initializes the start symbol with `{[Epsilon]}` (ε). For each production A → αBβ:
- Add `first_k(β)` to `follow(B)`.
- If ε ∈ `first_k(β)`, also add `follow(A)` to `follow(B)`.

### `FirstFollow.firstKOfString`

```fsharp
val firstKOfString: Map<Nonterminal<'nt>, Set<Symbol<'t,'nt> list>> -> int -> Symbol<'t,'nt> list -> Set<Symbol<'t,'nt> list>
```

Computes first_k for an arbitrary string of grammar symbols (concatenation of first_k sets with truncation).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| `[Epsilon]` for ε | Explicit Symbol representation; no ambiguity with empty terminal strings. No `terminalToString` parameter needed. |
| Generic over terminal type | firstK/followK work with any grammar, not only string-based ones. Lookahead is `Symbol list`, naturally matching the grammar's symbol type. |
| Fixed-point iteration with mutable maps | Standard approach; clear termination condition (set sizes stop growing). |
| `concat` treats `[Epsilon]` as identity | `[Epsilon] @ s = s` and `s @ [Epsilon] = s` for concatenation operations. |
| Truncation returns `[Epsilon]` for k=0 | Consistent with classical definition: the 0-length prefix of any string is ε. |

## Book Relationship

First_k and follow_k are standard concepts from the book's coverage of LL and LR parsing. The fixed-point computation follows the textbook definition. The explicit Epsilon symbol makes the representation match the mathematical notation used in the book: ε as a distinct symbol, not conflated with terminal strings.
