# Task 254: Render grammars with production numbers using alignat* environment

## Scope

`GrammarTeX.grammarToTeXWithNumbers` currently renders numbered grammars as an
`align*` environment with a `[idx]` (0-based) prefix:

```tex
\begin{align*}
[0] S &\rightarrow a\ S\ b\ S \\
\end{align*}
```

Task 254 changes this to an `alignat*{3}` environment with 1-based numbers in the
form `N) \ ` and a double `&&` between the LHS column and the `\rightarrow` column:

```tex
\begin{alignat*}{3}
1) \ & S &&\rightarrow a\ S\ b\ S \\
\end{alignat*}
```

The unnumbered `grammarToTeX` output is unchanged (stays `align*`).

## Reuse Analysis

- **Existing `SymbolTeX.nonterminalContent` / `SymbolTeX.toLaTeX`** — reused as-is for LHS and RHS rendering (no change)
- **Existing RHS joining with `\ `** (`String.concat "\\ "`) — reused as-is
- **Existing `renderGrammar` shared helper** — refactored to branch on `showNumbers` for the environment and line format; both paths share LHS/RHS content computation via a small extracted helper
- **Existing golden test infrastructure** (`GoldenHelpers.verifyGolden`) — reused; 6 numbered reference files regenerated

---

### S1: Rework `GrammarTeX` numbered rendering to `alignat*`

**Code:** `src/FLPQ.Printers/GrammarTeX.fs`
**Tests:** `tests/FLPQ.Printers.Tests/GrammarTeXGoldenTests.fs` (unchanged) — regenerate the 6 numbered golden reference files in `tests/FLPQ.Printers.Tests/GoldenData/`
**Docs:** `docs/developer/grammar-tex.md`

**Spec:**
- Extract LHS/RHS content computation into a private helper so both numbered and unnumbered paths share it (no duplication)
- Numbered output (`showNumbers = true`):
  - environment `\begin{alignat*}{3}` ... `\end{alignat*}`
  - line format: `sprintf "%d) \\ & %s &&\\rightarrow %s \\\\" (idx + 1) lhs rhs` — 1-based number, `) \ & ` before LHS, ` &&\rightarrow ` between LHS and RHS
  - number format: `N)` followed by `\ ` (thin space) then `& `
- Unnumbered output (`showNumbers = false`): unchanged `align*` with `&\rightarrow`
- Equivalence: plain golden files unchanged; numbered golden files regenerated with the new format

**Status:** [done] — committed as `033860a`

---

### S2: Update documentation

**Code:** none
**Tests:** none
**Docs:** `docs/developer/grammar-tex.md`

**Spec:**
- Update the abstract and "Output Format" section to describe `alignat*{3}` for numbered output and 1-based numbering
- Update the `grammarToTeXWithNumbers` signature description (1-based `N)` numbers, no longer `[idx]` brackets)
- Update the Design Decisions table rows referencing `align*` and 0-based numbering

**Status:** [done]
