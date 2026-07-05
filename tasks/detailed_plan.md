# Detailed Plan: Task 131 — Refactoring (BinaryPair, RsmDfa, RsmSymbol)

## Goal
1. Replace `Nonterminal<'nt> * Nonterminal<'nt>` tuples in Valiant with named struct `BinaryPair<'nt>`
2. Define type alias `RsmDfa<'t,'nt>` and use consistently in RSM modules
3. Make `RsmSymbol` consistent with `Symbol` by removing `[<RequireQualifiedAccess>]`

## Steps

### 1. Define `BinaryPair<'nt>` struct in Grammar.fs
- Add `[<Struct>] type BinaryPair<'nt> = { left: Nonterminal<'nt>; right: Nonterminal<'nt> }` near Nonterminal definition

### 2. Update Valiant.fs
- Replace all `Nonterminal<'nt> * Nonterminal<'nt>` tuple usages with `BinaryPair<'nt>`
- Pattern matches: `(left, right)` → `{ left = left; right = right }`
- Construction: `(nt1, nt2)` → `{ left = nt1; right = nt2 }`
- Dictionary keys and list types

### 3. Define `RsmDfa<'t,'nt>` type alias in RSM.fs
- Add `type RsmDfa<'t,'nt when 't: comparison and 'nt: comparison> = DFA<RsmSymbol<'t,'nt>, int>`

### 4. Update RsmBlock and RsmBuilder to use RsmDfa
- `RsmBlock.dfa: DFA<RsmSymbol<'t,'nt>, int>` → `RsmBlock.dfa: RsmDfa<'t,'nt>`
- `EbnfParser.fs` — update DFA<RsmSymbol<...>, int> references to RsmDfa
- Any other files using DFA<RsmSymbol<...>, int>

### 5. Remove `[<RequireQualifiedAccess>]` from RsmSymbol
- In RSM.fs: remove the attribute
- Update all pattern matches that use qualified `RsmSymbol.RTerm`/`RsmSymbol.RNonterm` → unqualified `RTerm`/`RNonterm`

### 6. Build, format, test

## Files to modify
- `src/FLPQ.Languages/Grammar.fs` — add BinaryPair struct
- `src/FLPQ.Languages/Valiant.fs` — replace tuples with BinaryPair
- `src/FLPQ.Languages/RSM.fs` — RsmDfa alias, RsmSymbol [<RQA>] removal
- `src/FLPQ.Languages/EbnfParser.fs` — use RsmDfa, update RsmSymbol patterns
- Any other files referencing RsmSymbol qualified or DFA<RsmSymbol<...>, int>
