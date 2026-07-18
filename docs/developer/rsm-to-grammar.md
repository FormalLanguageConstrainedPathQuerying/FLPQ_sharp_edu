# RSM to Grammar Module

**Tags:** utility, rsm, grammar, ebnf, conversion
**Kind:** utility
**Module:** RsmToGrammar
**Source:** `src/FLPQ.Languages/RsmToGrammar.fs`
**Depends on:** RSM, Grammar
**Used by:** GLLTests, RnglrTests

> **Abstract:** Converts a Recursive State Machine (RSM) to an equivalent BNF grammar. Implements the DFA-to-right-linear-grammar conversion: each DFA block produces a right-linear grammar fragment where state names become nonterminals, terminal transitions become rules with terminal followed by the target state nonterminal, and final states produce epsilon rules. Used in tests for comparing RSM-based parsers (GLL/RNGLR) against grammar-based parsers (CYK).

## Contents

- [Purpose](#purpose)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Purpose

Provides the bridge between RSM-based QFPQ algorithms (GLL, RNGLR) and grammar-based algorithms (CYK). By converting an RSM to a grammar, tests can verify that all parsers agree on acceptance/rejection for the same input.

## Function Signatures

### `rsmToGrammar: RSM<'t, 'nt> -> Grammar<'t, 'nt>`
Converts an RSM to an equivalent BNF grammar:

1. For each block's transition `(q, x, q')`:
   - If `x` is a terminal: add rule `Q_q -> x Q_{q'}`
   - If `x` is a block start state for nonterminal `N_j`: add rule `Q_q -> N_j Q_{q'}`
2. For each final state `q_f`: add rule `Q_{q_f} -> ε`
3. The nonterminal for block `B_{N_i}` is identified with `Q_{q_S}` (start state)

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Right-linear grammar fragments | Each DFA block produces a right-linear grammar where state names become nonterminals |
| Start state identification | Block's start state becomes the grammar nonterminal matching the block's nonterminal name |
| Auxiliary nonterminals | Other block states become auxiliary grammar nonterminals distinct from the block's identifying nonterminal |

## Book Reference

Chapter 5: DFA to right-linear grammar conversion. Chapter 6: EBNF to BNF via RSM (Theorem `\ref{thm:ebnf_cfg}`).

## See Also

- [RSM module](rsm.md) — RSM types
- [Grammar module](grammar.md) — grammar types
- [EBNF Parser](ebnf-parser.md) — RSM construction from EBNF
- [GLL](gll.md) — uses RSM directly
- [RNGLR](rnglr.md) — uses RSM directly
