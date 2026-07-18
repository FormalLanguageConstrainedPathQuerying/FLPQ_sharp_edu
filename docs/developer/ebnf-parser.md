# EBNF Parser Module

**Tags:** algorithm, ebnf, rsm, automaton, derivative, parsing, grammar, regular
**Kind:** algorithm
**Module:** EbnfParser
**Source:** `src/FLPQ.Languages/EbnfParser.fs`
**Depends on:** Grammar, RSM, Automaton
**Used by:** GLL, RNGLR
**Book reference:** Chapter 6

> **Abstract:** Parses EBNF grammar files (`.ebnf`) and constructs RSM (Recursive State Machine) via Brzozowski derivatives. Uses FParsec for parsing EBNF syntax. Converts each regular expression in the EBNF to a deterministic finite automaton using derivatives — generates DFA directly, no NFA-to-DFA determinization needed. Provides `parseEbnfGrammar` and `ebnfToRsm` functions.

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

### EBNF Parsing (FParsec-based)
1. Parse EBNF syntax: alternation `|`, Kleene star `*`, plus `+`, optional `?`, grouping `(...)`.
2. Two-stage parsing: parse to AST, then group rules by nonterminal and build combined regex per nonterminal.

### RSM Construction (Brzozowski Derivatives)
For each nonterminal:
1. Build a DFA from the regular expression using Brzozowski derivatives.
2. Each DFA state is a derivative of the regex; start state is the original regex; final states are nullable derivatives.
3. Transitions on nonterminals are relabeled to transitions on the corresponding block's start state.

## Type Definitions

### EBNF Grammar AST
Regular expression nodes: `Epsilon`, `Terminal<'t>`, `Nonterminal<'nt>`, `Concatenate`, `Alternative`, `Star`, `Plus`, `Optional`.

### EBNF Grammar
```fsharp
type EbnfGrammar<'t, 'nt> = { rules: Map<'nt, Regexp<'t, 'nt>>; start: 'nt }
```

## Function Signatures

```fsharp
val parseEbnfGrammar: string -> EbnfGrammar<string, string>
val ebnfToRsm: EbnfGrammar<'t, 'nt> -> RSM<'t, 'nt>
```

### parseEbnfGrammar
Parses an EBNF grammar from file content. Uses FParsec for parsing. Handles grouping: multiple rules for the same nonterminal are joined with `|`.

### ebnfToRsm
Converts an EBNF grammar to an RSM. Builds deterministic blocks via Brzozowski derivatives — start state is the original regex, final states are nullable derivatives.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| FParsec for parsing | Handles EBNF syntax robustly: alternation `|`, Kleene star `*`, plus `+`, optional `?`, grouping `(...)` |
| Brzozowski derivatives for DFA construction | Generates deterministic automata directly, no need for NFA → DFA determinization |
| Two-stage parsing | Parse to AST, then group rules by nonterminal and build combined regex per nonterminal |

## Book Reference

Chapter 6: EBNF grammar, Brzozowski derivatives, RSM construction.

## See Also

- [RSM module](rsm.md) — Recursive State Machine types
- [Automaton module](automaton.md) — DFA type used in blocks
- [RsmToGrammar module](rsm-to-grammar.md) — RSM to BNF conversion
