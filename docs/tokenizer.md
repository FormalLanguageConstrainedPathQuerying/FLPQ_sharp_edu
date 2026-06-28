# Tokenizer Module

Common tokenizer used by all parsing algorithms. Supports multi-character terminals via space-separated input.

## Function Signatures

### `Tokenizer.tokenizeStrings`

```fsharp
val tokenizeStrings: string -> string list
```

Splits an input string into a list of terminal strings using spaces as delimiters.
Empty or whitespace-only input returns an empty list.

**Example:** `tokenizeStrings "x + y"` returns `["x"; "+"; "y"]`.

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

## Book Relationship

Tokenization is a prerequisite for all parsing algorithms. The space-separated convention allows multi-character terminals while keeping the grammar definition format unchanged.
