# EBNF Parser Module

**Tags:** algorithm, ebnf, rsm, automaton, derivative, parsing, grammar, regular
**Kind:** algorithm
**Module:** EbnfParser, Regexp, RsmBuilder
**Source:** `src/FLPQ.Languages/EbnfParser.fs`
**Depends on:** Grammar, RSM, Automaton
**Used by:** GLL, RNGLR, RpqInput
**Book reference:** Chapter 6

> **Abstract:** Parses EBNF grammar text into regular-expression ASTs and constructs RSMs (Recursive State Machines) via Brzozowski derivatives. Each rule's right-hand side becomes a `Regexp`; each nonterminal gets a deterministic block built directly from derivatives — no NFA-to-DFA determinization. Also provides `Regexp.toDfa` for building a DFA from a terminal-only regular expression, used by RPQ query processing.

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

### EBNF Parsing (hand-written tokenizer + recursive descent)

1. Tokenize line by line: identifiers, alternation `|`, Kleene star `*`, plus `+`, optional `?`, explicit concatenation `/`, grouping `(...)`, rule arrow `->`, and the keyword `eps`.
2. Recursive-descent parse per line with precedence: alternation < sequence < postfix (`*`, `+`, `?`). Sequence is implicit (juxtaposition) or explicit via `/`.
3. Identifiers starting with an uppercase letter are nonterminals, all others are terminals; `eps` is epsilon.
4. Two-stage usage: parse to a list of `(Nonterminal, Regexp)` rules, then group rules by nonterminal and build a combined regex per nonterminal.

### DFA Construction (Brzozowski Derivatives)

For a regular expression and an alphabet:

1. Each DFA state is a derivative of the regex; start state is the original regex; final states are nullable derivatives.
2. State discovery is a worklist over the alphabet; transitions are added for every (state, symbol) pair.
3. `mkAlt` flattens nested alternations and removes duplicates — required for termination: without flattening, repeated derivation of expressions like `(a*)(aa)*` nests `RAlt` one level deeper per step and the derivative closure is infinite.

## Type Definitions

### Regexp AST

```fsharp
type Regexp<'t, 'nt> =
    | REps
    | REmpty
    | RTerm of Terminal<'t>
    | RNonterm of Nonterminal<'nt>
    | RSeq of Regexp<'t, 'nt> * Regexp<'t, 'nt>
    | RAlt of Regexp<'t, 'nt> * Regexp<'t, 'nt>
    | RStar of Regexp<'t, 'nt>
```

`REmpty` is the empty language (derivative dead state); it is not producible by the EBNF parser but arises during derivation.

## Function Signatures

### EbnfParser

```fsharp
val parseEbnf: string -> (Nonterminal<string> * Regexp<string, string>) list
val parseEbnfFile: string -> (Nonterminal<string> * Regexp<string, string>) list
val groupRules: (Nonterminal<string> * Regexp<string, string>) list -> Map<Nonterminal<string>, Regexp<string, string>>
```

- `parseEbnf` — parses EBNF text into one `(nonterminal, regexp)` pair per rule line. Throws on malformed input.
- `parseEbnfFile` — reads a file and calls `parseEbnf`.
- `groupRules` — joins multiple rules for the same nonterminal with `RAlt`.

### Regexp

```fsharp
val nullable: Regexp<'t, 'nt> -> bool
val derive: Regexp<'t, 'nt> -> RsmSymbol<'t, 'nt> -> Regexp<'t, 'nt>
val symbols: Regexp<'t, 'nt> -> RsmSymbol<'t, 'nt> list
val toString: (Terminal<'t> -> string) -> (Nonterminal<'nt> -> string) -> Regexp<'t, 'nt> -> string
val buildDfaFromRegex: 'sym list -> (Regexp<'t, 'nt> -> 'sym -> Regexp<'t, 'nt>) -> Regexp<'t, 'nt> -> DFA<'sym, int>
val toDfa: Regexp<'t, 'nt> -> DFA<'t, int>
```

- `derive` — Brzozowski derivative of a regexp with respect to a grammar symbol (terminal or nonterminal).
- `buildDfaFromRegex` — worklist DFA construction over a given alphabet and derivation function.
- `toDfa` — builds a DFA from a regular expression over terminals: the alphabet is the set of terminals occurring in the regexp (derived via `symbols`), derivation is over `RsmSymbol.RTerm`. Terminal-free expressions (e.g. `eps`) yield a single-state DFA. Used by RPQ query processing to compile the query regexp into the DFA consumed by the RPQ algorithms.

### RsmBuilder

```fsharp
val buildRSMWithStart: Map<Nonterminal<string>, Regexp<string, string>> -> Nonterminal<string> -> RSM<string, string>
val buildRSM: Map<Nonterminal<string>, Regexp<string, string>> -> RSM<string, string>
val buildRSMFromText: string -> RSM<string, string>
val buildRSMFromFile: string -> RSM<string, string>
```

Build one deterministic DFA block per nonterminal (derivation over all symbols of the regexp) and wire blocks into a single RSM.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Hand-written tokenizer + recursive descent | EBNF is small and fixed; no parser-combinator dependency needed |
| Brzozowski derivatives for DFA construction | Generates deterministic automata directly, no need for NFA → DFA determinization |
| `mkAlt` flattens and deduplicates alternations | Termination of `buildDfaFromRegex`: keeps the syntactic derivative closure finite (see `tasks/fixes_for_book.md`) |
| Explicit `/` concatenation operator | Matches the book's regexp notation (e.g. `walk/(O \| R)+/walk`, Chapter 11); juxtaposition remains supported, so existing inputs are unaffected |
| `toDfa` alphabet = terminals of the regexp | Terminal-free regexps need no invented symbols; derivation over absent symbols is empty, so the result is identical to any fallback alphabet |

## Book Reference

Chapter 6: EBNF grammar, Brzozowski derivatives, RSM construction. Chapter 11 uses `Regexp.toDfa` for RPQ query compilation.

## See Also

- [RSM module](rsm.md) — Recursive State Machine types
- [Automaton module](automaton.md) — DFA type used in blocks
- [RsmToGrammar module](rsm-to-grammar.md) — RSM to BNF conversion
- [RpqInput module](rpq-input.md) — RPQ query regexp parsing built on `parseEbnf`
