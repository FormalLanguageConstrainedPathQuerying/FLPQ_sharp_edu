# GrammarTeX Module

## Module Purpose

Renders a `Grammar<'t, 'nt>` as a TeX `align*` environment. Each production rule is displayed
on its own line with `\rightarrow` connecting the left-hand side to the right-hand side.
Symbols within the right-hand side are separated by `\ ` (TeX thin space in math mode).

The module depends on `SymbolTeX.toLaTeX` for rendering individual grammar symbols.

## Function Signatures

### `grammarToTeX`
```fsharp
val grammarToTeX: Grammar<'t, 'nt> -> string
```
Renders the grammar without production numbers. Rules are ordered: productions for
the start nonterminal first, then all other rules in their original order.

### `grammarToTeXWithNumbers`
```fsharp
val grammarToTeXWithNumbers: Grammar<'t, 'nt> -> string
```
Renders the grammar with 0-based production numbers in brackets (e.g., `[0]`, `[1]`).
Same rule ordering as `grammarToTeX`.

### `renderGrammar` (private)
```fsharp
val renderGrammar: showNumbers:bool -> Grammar<'t, 'nt> -> string
```
Internal implementation shared by both public functions.

## Output Format

```tex
\begin{align*}
S &\rightarrow a\ S\ b\ S \\
S &\rightarrow \varepsilon
\end{align*}
```

With numbers (`grammarToTeXWithNumbers`):

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
- Terminal/nonterminal content rendered via `SymbolTeX.toLaTeX`

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| `align*` environment | Standard LaTeX math alignment; each rule on its own line, aligned at `&` |
| Start nonterminal first | Consistent with the book's convention; makes the grammar entry point easy to find |
| `\ ` (thin space) between RHS symbols | Provides visual separation in rendered output without adding commas or extra notation |
| No production numbers by default | Clean output for inline grammar display; numbers available via separate function when needed for table references |
| Generation relies on `SymbolTeX.toLaTeX` | Centralizes symbol rendering in one module; avoids duplication of symbol-to-TeX logic |

## Tests

Golden (snapshot) tests are located in `tests/FLPQ.Printers.Tests/GrammarTeXGoldenTests.fs`.
Reference TeX files are stored in `tests/FLPQ.Printers.Tests/GoldenData/`.
Tests cover three grammars (BNF and CNF) with both `grammarToTeX` and `grammarToTeXWithNumbers`.
