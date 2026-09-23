# RpqInput Module

**Tags:** regular, rpq, parsing, ebnf, utility
**Kind:** utility
**Module:** RpqInput
**Source:** `src/FLPQ.RPQ/RpqInput.fs`
**Depends on:** EbnfParser (Regexp)
**Used by:** ArroyueloRunner, BelyaninRunner
**Book reference:** Chapter 11, Section sec:RPQ_Arroyuelo

> **Abstract:** Parses RPQ query regular expressions from EBNF text or files. An RPQ query is a regular expression over graph edge labels: the first rule's right-hand side of the EBNF input is taken as the query, and every identifier in it is an edge label — nonterminal references are mapped to terminals.

## Contents

- [Purpose](#purpose)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Purpose

The RPQ algorithms (Arroyuelo, Belyanin) take a regular expression over edge labels and a graph. This module turns an EBNF file into the query regexp:

1. Parse the EBNF text with `EbnfParser.parseEbnf`.
2. Take the first rule's right-hand side as the query.
3. Map every nonterminal reference to a terminal (`mapNontermsToTerms`), because in an RPQ query every symbol is an edge label.

This makes the book's example query `S -> walk / (O | R)+ / walk` work directly: `walk`, `O`, and `R` all become edge labels.

## Function Signatures

```fsharp
val mapNontermsToTerms: Regexp<string, string> -> Regexp<string, string>
val parseRegexpText: string -> Regexp<string, string>
val parseRegexpFile: string -> Regexp<string, string>
```

- `mapNontermsToTerms` — recursively rewrites `RNonterm(Nonterminal s)` to `RTerm(Terminal s)`; all other constructors are preserved.
- `parseRegexpText` — parses EBNF text and returns the first rule's right-hand side with nonterminals mapped to terminals. Throws when the text contains no rules.
- `parseRegexpFile` — reads a file and calls `parseRegexpText`.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| First rule's RHS is the query | EBNF files may contain helper rules; the query is conventionally the first (start) rule, matching how `RsmBuilder.buildRSMFromText` picks the start nonterminal |
| Nonterminals mapped to terminals | In an RPQ query every identifier names an edge label; the EBNF uppercase/lowercase convention does not apply. Mapping keeps a single `Regexp<string, string>` type end-to-end |
| Built on `EbnfParser.parseEbnf` | Reuses the existing tokenizer and recursive-descent parser (including `/` concatenation) instead of a second regexp parser |

## See Also

- [EbnfParser module](ebnf-parser.md) — EBNF parsing and `Regexp.toDfa`
- [Arroyuelo RPQ](arroyuelo-rpq.md) — algorithm consuming the query regexp
- [Belyanin RPQ](belyanin-rpq.md) — algorithm consuming the compiled DFA
