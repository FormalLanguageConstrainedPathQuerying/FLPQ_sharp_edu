# EBNF Parser Module

## Overview

Parses EBNF grammar files (`.ebnf`) and constructs RSM (Recursive State Machine) via Brzozowski derivatives.

## Types

### EBNF Grammar AST

Regular expression nodes: `Epsilon`, `Terminal<'t>`, `Nonterminal<'nt>`, `Concatenate`, `Alternative`, `Star`, `Plus`, `Optional`.

### EBNF Grammar

```fsharp
type EbnfGrammar<'t, 'nt> = { rules: Map<'nt, Regexp<'t, 'nt>>; start: 'nt }
```

## Module Functions

```fsharp
val parseEbnfGrammar: string -> EbnfGrammar<string, string>
val ebnfToRsm: EbnfGrammar<'t, 'nt> -> RSM<'t, 'nt>
```

### parseEbnfGrammar

Parses an EBNF grammar from file content. Uses FParsec for parsing. Handles grouping: multiple rules for the same nonterminal are joined with `|`.

### ebnfToRsm

Converts an EBNF grammar to an RSM. For each nonterminal:
1. Builds a DFA from the regular expression using Brzozowski derivatives
2. Each DFA state is a derivative; start state is the original regex; final states are nullable derivatives
3. Transitions on nonterminals are relabeled to transitions on the corresponding block's start state

## Design Decisions

- **FParsec** for parsing EBNF syntax (alternation `|`, Kleene star `*`, plus `+`, optional `?`, grouping `(...)`)
- **Brzozowski derivatives** for DFA construction — generates deterministic automata directly, no need for NFA → DFA determinization
- **Two-stage parsing**: parse to AST, then group rules by nonterminal and build combined regex per nonterminal

## Book References

- Chapter 6: EBNF grammar, Brzozowski derivatives, RSM construction
