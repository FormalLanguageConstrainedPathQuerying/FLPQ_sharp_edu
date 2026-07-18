# GrammarTeX Module

**Tags:** visualization, tex, grammar, rendering, latex
**Kind:** visualization
**Module:** GrammarTeX
**Source:** `src/FLPQ.Printers/GrammarTeX.fs`
**Depends on:** Grammar, SymbolTeX
**Used by:** FLPQ.Cli (summary generation)

> **Abstract:** Renders a `Grammar<'t,'nt>` as a TeX `align*` environment. Each production rule is displayed on its own line with `\rightarrow`. Symbols within the RHS are separated by `\ ` (thin space). Supports numbered (`grammarToTeXWithNumbers`) and unnumbered (`grammarToTeX`) output. Depends on `SymbolTeX.toLaTeX` for individual symbol rendering.

## Contents

- [Overview](#overview)
- [Function Signatures](#function-signatures)
- [Output Format](#output-format)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

GrammarTeX converts a grammar into TeX for display in algorithm summary documents. The start nonterminal's rules appear first, followed by all other rules in original order.

## Function Signatures

### `grammarToTeX`
```fsharp
val grammarToTeX: Grammar<'t, 'nt> -> string
```
Renders the grammar without production numbers.

### `grammarToTeXWithNumbers`
```fsharp
val grammarToTeXWithNumbers: Grammar<'t, 'nt> -> string
```
Renders the grammar with 0-based production numbers in brackets (e.g., `[0]`, `[1]`).

## Output Format

```tex
\begin{align*}
S &\rightarrow a\ S\ b\ S \\
S &\rightarrow \varepsilon
\end{align*}
```

With numbers:
```tex
\begin{align*}
[0] S &\rightarrow a\ S\ b\ S \\
[1] S &\rightarrow \varepsilon
\end{align*}
```

- Environment: `\begin{align*} ... \end{align*}`
- Each rule: `lhs &\rightarrow rhs \\`
- RHS symbols joined with `\ ` (thin space)
- Epsilon renders as `\varepsilon`

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| `align*` environment | Standard LaTeX math alignment; each rule on its own line |
| Start nonterminal first | Consistent with the book's convention |
| `\ ` (thin space) between RHS symbols | Visual separation without adding extra notation |
| No production numbers by default | Clean output for inline display; numbers available when needed |
| Relies on `SymbolTeX.toLaTeX` | Centralizes symbol rendering in one module |

## See Also

- [SummaryTeX module](summary-tex.md) — uses GrammarTeX in merged documents
- [Grammar module](grammar.md) — grammar types
- [SymbolTeX](symbol-tex.md) — individual symbol rendering
