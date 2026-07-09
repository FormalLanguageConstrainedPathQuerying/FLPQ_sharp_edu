# RSM to Grammar Module

## Overview

Converts a Recursive State Machine (RSM) to a BNF grammar. Implements the DFA-to-right-linear-grammar conversion from the book.

## Module Functions

```fsharp
val rsmToGrammar: RSM<'t, 'nt> -> Grammar<'t, 'nt>
```

### rsmToGrammar

Converts an RSM to an equivalent BNF grammar by converting each DFA block to a right-linear grammar fragment:

1. For each block's transition `(q, x, q')`:
   - If `x` is a terminal: add rule `Q_q -> x Q_{q'}`
   - If `x` is a block start state for nonterminal `N_j`: add rule `Q_q -> N_j Q_{q'}`
2. For each final state `q_f`: add rule `Q_{q_f} -> ε`
3. The nonterminal for block `B_{N_i}` is identified with the grammar nonterminal `Q_{q_S}` (where `q_S` is the block's start state)

## Design Decisions

- **Right-linear grammar fragments** — each DFA block produces a right-linear grammar where state names become nonterminals
- **Start state identification** — block's start state becomes the grammar nonterminal matching the block's nonterminal name
- **Auxiliary nonterminals** — other block states become auxiliary grammar nonterminals distinct from the block's identifying nonterminal

## Book References

- Chapter 5: DFA to right-linear grammar conversion
- Chapter 6: EBNF to BNF via RSM (Theorem `\ref{thm:ebnf_cfg}`)
