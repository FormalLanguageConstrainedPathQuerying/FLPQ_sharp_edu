# RSM Module

**Tags:** data-structure, rsm, automaton, dfa, grammar, ebnf
**Kind:** data-structure
**Module:** RSM
**Source:** `src/FLPQ.Languages/RSM.fs`
**Depends on:** Automaton, Grammar
**Used by:** GLL, RNGLR, EbnfParser
**Book reference:** Chapter 6, Section 03_RecursiveAutomata.tex, Definition def:rsm

> **Abstract:** Implements the Recursive State Machine (RSM) type as defined in the book. An RSM is a collection of deterministic finite automata (blocks), one per nonterminal, where transitions are labeled by either terminals (consume input) or nonterminals (recursive call to another block). Reuses the existing `DFA<'t,'s>` type for blocks. Provides `ExtendedRSM` wrapper for grammar augmentation (fresh start S').

## Contents

- [Data Structure](#data-structure)
- [Type Definitions](#type-definitions)
- [Module Functions](#module-functions)
- [ExtendedRSM](#extendedrsm)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Data Structure

An RSM `⟨N, Σ, B, B_S, Q, Q_S⟩` from the book:
- **N**: nonterminals — one block per nonterminal
- **Σ**: terminals — labels on transitions
- **B**: blocks — each a DFA over `RsmSymbol<'t,'nt>` = Σ ∪ Q_S
- **B_S**: start block — entry point nonterminal
- **Q**: states across blocks (integer indices)
- **Q_S**: start states (indices)

The key design feature: all transitions for all blocks are stored in a common `Matrix`, so state indices are globally unique — no per-block renumbering needed.

## Type Definitions

### `RsmSymbol<'t, 'nt>`
```fsharp
[<RequireQualifiedAccess>]
type RsmSymbol<'t, 'nt when 't: comparison and 'nt: comparison> =
    | RTerm of Terminal<'t>
    | RNonterm of Nonterminal<'nt>
```
A transition label in an RSM block. Either a terminal (consuming input) or a nonterminal (recursive call).

### `RsmBlock<'t, 'nt>`
```fsharp
type RsmBlock<'t, 'nt when 't: comparison and 'nt: comparison> =
    { nonterminal: Nonterminal<'nt>
      dfa: DFA<RsmSymbol<'t, 'nt>, int> }
```
A single block — a DFA for one nonterminal. States are simple integer indices.

### `RSM<'t, 'nt>`
```fsharp
type RSM<'t, 'nt when 't: comparison and 'nt: comparison> =
    { blocks: RsmBlock<'t, 'nt> list
      startBlock: Nonterminal<'nt> }
```
The full RSM tuple. `blocks` contains all blocks (one per nonterminal). `startBlock` identifies the entry point.

## Module Functions

```fsharp
val blocks: RSM<'t, 'nt> -> RsmBlock<'t, 'nt> list
val blockOf: Nonterminal<'nt> -> RSM<'t, 'nt> -> RsmBlock<'t, 'nt> option
val startBlock: RSM<'t, 'nt> -> RsmBlock<'t, 'nt>
val nonterminals: RSM<'t, 'nt> -> Nonterminal<'nt> list
val terminals: RSM<'t, 'nt> -> Terminal<'t> list
val startStates: RSM<'t, 'nt> -> Set<int>
val stateCount: RSM<'t, 'nt> -> int
```

## ExtendedRSM

### `ExtendedRSM<'t, 'nt>`
```fsharp
type ExtendedRSM<'t, 'nt when 't: comparison and 'nt: comparison> =
    { originalRsm: RSM<'t, 'nt>
      freshStart: Nonterminal<'nt>
      extendedRsm: RSM<'t, 'nt> }
```
An RSM augmented with fresh start `S'`. The extended RSM has `S'` as its start block with a single transition `0 --RNonterm(originalStart)--> 1`. Preserves the relationship between original and augmented RSMs.

### Module helpers
```fsharp
val create: Nonterminal<'nt> -> RSM<'t, 'nt> -> ExtendedRSM<'t, 'nt>
val originalRsm: ExtendedRSM<'t, 'nt> -> RSM<'t, 'nt>
val freshStart: ExtendedRSM<'t, 'nt> -> Nonterminal<'nt>
val extRsm: ExtendedRSM<'t, 'nt> -> RSM<'t, 'nt>
val originalStartBlock: ExtendedRSM<'t, 'nt> -> RsmBlock<'t, 'nt>
val originalStartNonterminal: ExtendedRSM<'t, 'nt> -> Nonterminal<'nt>
val flattenExtRsm: ExtendedRSM<'t, 'nt> -> FlattenedRsm<'t, 'nt>
val stateCount: ExtendedRSM<'t, 'nt> -> int
val extBlocks: ExtendedRSM<'t, 'nt> -> RsmBlock<'t, 'nt> list
```

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Reuse existing `DFA<'t, 's>` type for blocks | Avoids duplicating automaton infrastructure |
| `RsmSymbol` as discriminated union | Cleanly represents Σ ∪ Q_S alphabet |
| Simple `int` states | Block states are simple indices; no need for named states |
| `startBlock` stored as `Nonterminal<'nt>` | Sufficient to identify entry block; found via `blockOf` |
| Generic over terminal and nonterminal types | Enables reuse with different symbol types |
| `ExtendedRSM` as wrapper type | Preserves original-extended relationship; avoids ad-hoc positional access |

## Book Reference

Chapter 6, `03_RecursiveAutomata.tex`: Definition `def:rsm` — RSM is a tuple `⟨N, Σ, B, B_S, Q, Q_S⟩`. Extended RSM (with S' start) is described in `06_GLL_Based.tex` (section `sec:CFPQ_GLL`) and `sec:CFPQ_RNGLR`.

## See Also

- [Automaton module](automaton.md) — underlying DFA type
- [Grammar module](grammar.md) — grammar types (Terminal, Nonterminal, Symbol)
- [EBNF Parser](ebnf-parser.md) — RSM construction from EBNF
- [GLL](gll.md) — uses ExtendedRSM for parsing
- [RNGLR](rnglr.md) — uses ExtendedRSM for parsing
