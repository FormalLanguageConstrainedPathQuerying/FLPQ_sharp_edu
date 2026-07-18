# First/Follow Module

**Tags:** algorithm, grammar, ll, lr, fixed-point, parsing
**Kind:** algorithm
**Module:** FirstFollow
**Source:** `src/FLPQ.Languages/FirstFollow.fs`
**Depends on:** Grammar
**Used by:** LLParser, LRParser
**Book reference:** LL/LR parsing chapters

> **Abstract:** Computes first_k and follow_k sets for context-free grammars using fixed-point iteration. Used by LL and LR parsing table construction. Epsilon is represented explicitly as `[Epsilon]` (a singleton `Symbol` list), eliminating the need for string-based representations and `terminalToString` conversions. Generic over terminal and nonterminal types.

## Contents

- [Algorithm](#algorithm)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

### firstK — Fixed-point iteration

1. Initialize: for each nonterminal A, collect the first k symbols of each production's RHS.
2. Iteratively expand: for each production A → X₁ X₂ … Xₙ with nonterminals in the RHS, compute first_k(X₁ X₂ … Xₙ) by concatenating and truncating first_k of each symbol.
3. Repeat until no changes (set sizes stop growing).

### followK — Fixed-point iteration

1. Initialize: follow(start) = {[Epsilon]} (end-of-input).
2. For each production A → αBβ:
   - Add first_k(β) to follow(B).
   - If ε ∈ first_k(β), also add follow(A) to follow(B).
3. Repeat until no changes.

## Function Signatures

### `FirstFollow.firstK`
```fsharp
val firstK: Grammar<'t, 'nt> -> int -> Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>
```
Computes the first_k set for every nonterminal in the grammar. `first_k(A)` is the set of all terminal strings of length ≤ k that can begin strings derived from A. The empty string ε is represented as `[Epsilon]`.

**Parameters:**
- `g`: the grammar.
- `k`: maximum lookahead length (lists longer than k are truncated).

### `FirstFollow.followK`
```fsharp
val followK: Grammar<'t, 'nt> -> int -> Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>
```
Computes the follow_k set for every nonterminal in the grammar. `follow_k(A)` is the set of all terminal strings of length ≤ k that can appear immediately after A in some derivation from the start symbol.

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

## Book Reference

First_k and follow_k are standard concepts from the book's coverage of LL and LR parsing. The fixed-point computation follows the textbook definition. The explicit Epsilon symbol makes the representation match the mathematical notation used in the book: ε as a distinct symbol, not conflated with terminal strings.

## See Also

- [LL parser module](ll-parser.md) — uses firstK/followK for table construction
- [LR parser module](lr-parser.md) — uses followK for SLR(1) table construction
- [Grammar module](grammar.md) — grammar types
