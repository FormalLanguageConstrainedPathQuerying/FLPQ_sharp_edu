# Tokenizer Module

**Tags:** utility, tokenizer, grammar, parsing
**Kind:** utility
**Module:** Tokenizer
**Source:** `src/FLPQ.Languages/Tokenizer.fs`
**Depends on:** Grammar
**Used by:** Cyk, Valiant, LLParser, LRParser, all CLI runners

> **Abstract:** Common tokenizer used by all parsing algorithms. Supports multi-character terminals via space-separated input. Provides three output formats: raw string list (`tokenizeStrings`), grammar symbol list (`tokenize`), and Terminal list (`tokenizeTerminals`). Single module ensures consistent tokenization across CYK, Valiant, LL, and LR parsers.

## Contents

- [Purpose](#purpose)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Purpose

Tokenization is a prerequisite for all parsing algorithms. The space-separated convention allows multi-character terminals while keeping the grammar definition format unchanged. Different parsers need different token formats — this module provides all three from a single implementation.

## Function Signatures

### `Tokenizer.tokenizeStrings`
```fsharp
val tokenizeStrings: string -> string list
```
Splits an input string into a list of terminal strings using spaces as delimiters. Empty or whitespace-only input returns an empty list.

### `Tokenizer.tokenize`
```fsharp
val tokenize: string -> Symbol<string, string> list
```
Same as `tokenizeStrings`, but wraps each terminal in `T(Terminal ...)` producing a list of grammar symbols.

### `Tokenizer.tokenizeTerminals`
```fsharp
val tokenizeTerminals: string -> Terminal<string> list
```
Same as `tokenizeStrings`, but wraps each terminal in `Terminal(...)`.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Space as delimiter | Supports multi-character terminals (e.g., `"while"`). Single-character terminals work when space-separated. |
| Single module for all parsers | Ensures consistent tokenization across CYK, Valiant, LL, and LR parsers. |
| Three output formats | Different parsers need different token formats: grammar symbols (CYK), raw strings (LL lookahead), or Terminal values (LR). |

## See Also

- [Grammar module](grammar.md) — Terminal, Nonterminal, Symbol types
- [CYK algorithm](cyk.md) — uses tokenized input
- [LL parser](ll-parser.md) — uses tokenized input
- [LR parser](lr-parser.md) — uses tokenized input
