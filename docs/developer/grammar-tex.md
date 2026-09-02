# GrammarTeX Module

**Tags:** visualization, tex, grammar, rendering, latex
**Kind:** visualization
**Module:** GrammarTeX
**Source:** `src/FLPQ.Printers/GrammarTeX.fs`
**Depends on:** Grammar, SymbolTeX
**Used by:** FLPQ.Cli (summary generation)

> **Abstract:** Renders a `Grammar<'t,'nt>` as a TeX environment. Each production rule is displayed on its own line with `\rightarrow`. Symbols within the RHS are separated by `\ ` (thin space). Unnumbered output (`grammarToTeX`) uses `align*`; numbered output (`grammarToTeXWithNumbers`) uses `alignat*{3}` with 1-based numbers and a double `&&` between the LHS and arrow columns. Depends on `SymbolTeX.toLaTeX` for individual symbol rendering.

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
Renders the grammar with 1-based production numbers in the form `N)`.

## Output Format

```tex
\begin{align*}
S &\rightarrow a\ S\ b\ S \\
S &\rightarrow \varepsilon
\end{align*}
```

With numbers:
```tex
\begin{alignat*}{3}
1) \ & S &&\rightarrow a\ S\ b\ S \\
2) \ & S &&\rightarrow \varepsilon
\end{alignat*}
```

- Unnumbered environment: `\begin{align*} ... \end{align*}`
- Numbered environment: `\begin{alignat*}{3} ... \end{alignat*}`
- Unnumbered rule: `lhs &\rightarrow rhs \\`
- Numbered rule: `N) \ & lhs &&\rightarrow rhs \\` (thin space `\ ` after the number, double `&&` between the LHS and arrow columns)
- RHS symbols joined with `\ ` (thin space)
- Epsilon renders as `\varepsilon`

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| `align*` environment (unnumbered) | Standard LaTeX math alignment; each rule on its own line |
| `alignat*{3}` environment (numbered) | Three-column alignment (number, LHS, RHS) with precise spacing control |
| 1-based `N)` numbering with `\ ` thin space | Matches the book's grammar rendering convention |
| Double `&&` between LHS and arrow | Separates the LHS column from the RHS column in `alignat*` |
| Start nonterminal first | Consistent with the book's convention; ordering and 1-based numbers come from `Grammar.numberedRules`, the single source of truth shared with CYK/Valiant table cells and Basic SPPF |
| `\ ` (thin space) between RHS symbols | Visual separation without adding extra notation |
| No production numbers by default | Clean output for inline display; numbers available when needed |
| Relies on `SymbolTeX.toLaTeX` | Centralizes symbol rendering in one module |

## See Also

- [SummaryTeX module](summary-tex.md) — uses GrammarTeX in merged documents
- [Grammar module](grammar.md) — grammar types
- [SymbolTeX](symbol-tex.md) — individual symbol rendering
