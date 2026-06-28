# Detailed Plan: Epsilon in Symbol — Remove strings from first/follow

## Goal

Add `Epsilon` to `Symbol<'t,'nt>` so that epsilon is an explicit symbol, not an empty string `""`. This removes the `terminalToString` hack from FirstFollow, eliminates string-based representation of epsilon throughout the codebase, and follows classical language-theoretic conventions.

## Task breakdown

### 1. `Symbol<'t,'nt>` — add Epsilon case
**File:** `src/FLPQ.Languages/Grammar.fs`

Change from:
```fsharp
type Symbol<'t, 'nt> =
    | T of Terminal<'t>
    | N of Nonterminal<'nt>
```
To:
```fsharp
type Symbol<'t, 'nt> =
    | T of Terminal<'t>
    | N of Nonterminal<'nt>
    | Epsilon
```

### 2. Grammar BNF parsing
**File:** `src/FLPQ.Languages/Grammar.fs`

In `parseLine`: `"eps"` maps to `[Epsilon]`, not `[]`.

### 3. CNF pipeline — all four steps
**File:** `src/FLPQ.Languages/Grammar.fs`

- `computeNullable`: check `[Epsilon]` for explicit epsilon; `Epsilon` is trivially nullable in `List.forall`
- `eliminateEpsilon`: remove rules with `rhs = [Epsilon]`; when producing nullable variants, filter out nullable positions; empty-result variants are dropped (they'd be `[Epsilon]` after elimination, which we're removing)
- `eliminateUnit`: `match r.rhs with [N _] -> ...` unchanged (unit rules have one nonterminal; epsilon productions are `[Epsilon]`, not a unit)
- `binarize`: `match r.rhs with [ _ ] -> [r]` — but `[Epsilon]` has length 1, so this would be incorrectly treated as a valid production. Need to special-case: if rhs is `[Epsilon]`, keep as is (epsilon production). Only binarize RHS with 3+ symbols.
- `replaceTerminals`: when checking `r.rhs.Length > 1`, `[Epsilon]` has length 1 so it won't be affected. Fine.

### 4. DerivationTree — remove Epsilon DU case
**File:** `src/FLPQ.Languages/DerivationTree.fs`

Remove the `Epsilon` case. Use `Leaf(Epsilon)` instead.

```fsharp
type DerivationTree<'t, 'nt> =
    | Leaf of Terminal<'t>
    | Node of Nonterminal<'nt> * DerivationTree<'t, 'nt> list
```

`leaves` function: `Leaf(Epsilon)` contributes nothing, same as before.

### 5. FirstFollow — full rewrite with Symbol list
**File:** `src/FLPQ.Languages/FirstFollow.fs`

Epsilon is `[Epsilon]` (a single-element list). Epsilon lookahead is `[Epsilon]`.

New signatures:
```fsharp
val firstK: Grammar<'t, 'nt> -> int -> Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>
val followK: Grammar<'t, 'nt> -> int -> Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>
val firstKOfString: Map<Nonterminal<'nt>, Set<Symbol<'t,'nt> list>> -> int -> Symbol<'t,'nt> list -> Set<Symbol<'t,'nt> list>
```

Remove `terminalToString` parameter entirely.

Internal operations:
- `truncate(lst, k)`: take first k symbols from list
- `productTrunc(k, set1, set2)`: concatenation + truncation of two sets
- Concatenation: `lst1 @ lst2`

In `computeFirstK`: when initializing with direct productions:
- `T(Terminal t) :: _` → `truncate [T(Terminal t)] k`
- `[]` → `set [ [Epsilon] ]` — no, wait. Epsilon productions are `[Epsilon]`, not `[]`.

Hmm wait. In `computeFirstK`, the original code does:
```fsharp
match r.rhs with
| [] -> Some ""
| T(Terminal t) :: _ -> Some(prefix t k)
```

With the change:
```fsharp
match r.rhs with
| [Epsilon] -> Some [Epsilon]
| T(Terminal t) :: _ -> Some(truncate [T(Terminal t)] k)
```

In `firstOfSymbols`:
```fsharp
| [] -> set [ [Epsilon] ]  // empty list of symbols = epsilon
| T(Terminal t) :: rest -> ...
| Epsilon :: _ -> set [ [Epsilon] ]  // can't happen in practice but is trivially epsilon
| N nt :: rest -> ...
```

The `productTrunc` function replaces `concatTrunc`. When s1 is epsilon (empty list? No, `[Epsilon]`):
- In the original: `if s1 = "" then set2 |> Set.toSeq`  
- In the new: `if s1 = [Epsilon] then set2 |> Set.toSeq`

When s1 reaches length k, output s1 directly (truncated to k).

### 6. LLParser — lookahead as Symbol list
**File:** `src/FLPQ.Languages/LLParser.fs`

- Lookahead: `string` → `Symbol<string, string> list`
- Table key: `Nonterminal * string` → `Nonterminal * Symbol<string, string> list`
- `buildTable`: return `Map<Nonterminal<string> * Symbol<string, string> list, int>`
- `parse`: use Symbol-list lookahead instead of string
- `lookaheadStr` function → renamed, returns next k symbols
- Table entry selection uses symbol lists, not strings

The LL parser's `parse` function currently uses `lookaheadStr tokens pos k` where tokens is `string list`. This should become passing the actual token symbols.

Actually, the LL parser receives the tokenized input as `string list`. These need to become `Symbol<string, string> list`. The lookahead is the next k symbols from the input, concatenated.

Wait, the tokenizer returns `Symbol<string, string> list` via `tokenize`. The LL parser's `tokenize` currently returns `string list` (for lookahead computation). With the change, it should return `Symbol<string, string> list` and lookahead picks the next k symbols.

### 7. LRParser — lookahead as Symbol
**File:** `src/FLPQ.Languages/LRParser.fs`

- `LR1Item.Lookahead`: `Terminal<string>` → `Symbol<string, string>`
- LR table action keys: `(int * string)` → `(int * Symbol<string, string>)`
  - For end-of-input `$`, the action uses `""`; this becomes `Epsilon`
- `allTerminals` in table construction: collect all `T(Terminal t)` symbols from rules
- Reduce actions: for LR(0), reduce on all terminals + epsilon (end-of-input)
  - For SLR(1), reduce on follow set symbols
  - For CLR(1), reduce on the item's lookahead

The LR parser's `parse` function uses terminal tokens. `Epsilon` (end of input) is used as lookahead when we've consumed all input.

### 8. Cyk.fs and Valiant.fs
**Files:** `src/FLPQ.Languages/Cyk.fs`, `src/FLPQ.Languages/Valiant.fs`

These work with CNF internally. Need to update:
- Empty RHS checks: `r.rhs.IsEmpty` → `r.rhs = [Epsilon]` (for epsilon acceptance)
- Terminal production matching: unchanged (matches `[T(Terminal t)]`)
- Binary production matching: unchanged (matches `[N left; N right]`)

Wait, there may also be checks for `Rule<'t,'nt>` with `r.rhs = []`. In Cyk:
```fsharp
cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && r.rhs = [])
```
This checks if the start symbol has an epsilon production. With the change:
```fsharp
cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && r.rhs = [Epsilon])
```

### 9. Tokenizer
No changes. Epsilon never appears as input token.

### 10. Tests — massive updates
All test files need updating for the new Symbol.Epsilon, changed lookahead types, and changed first/follow return types.

### 11. Documentation
Update: grammar.md, first-follow.md, derivation-tree.md, ll-parser.md

## Implementation Order

1. Grammar.fs: Add Epsilon to Symbol, update CNF
2. DerivationTree.fs: Remove Epsilon case
3. FirstFollow.fs: Rewrite
4. LLParser.fs + LRParser.fs: Update lookahead
5. Cyk.fs + Valiant.fs: Minor fixes
6. Tests: Update all
7. Docs: Update all
