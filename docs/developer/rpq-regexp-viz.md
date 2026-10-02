# RPQ Regexp Visualization

**Tags:** visualization, tex, rpq, regexp, automaton
**Kind:** visualization
**Module:** RegexpTeX
**Source:** `src/FLPQ.Printers/RegexpTeX.fs`
**Depends on:** Regexp (FLPQ.Languages), AutomatonTikz (escapeLatex)
**Used by:** Arroyuelo RPQ summary/steps (task 280), Belyanin RPQ summary/steps (task 281)
**Book reference:** Chapter 11, Section sec:RPQ_Arroyuelo (query regexp display)

> **Abstract:** TeX rendering of the RPQ query regexp as a math-mode formula. The query is shown next to its DFA in the summary and step figures; this module renders the regexp side. Each constructor maps to the book's notation: epsilon, empty, terminals, nonterminals, sequence (`\cdot`), alternative (`\mid`), star.

## Contents

- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Module Functions

```fsharp
val toTeX: ('t -> string) -> ('nt -> string) -> Regexp<'t, 'nt> -> string
```

`toTeX terminal nonterm r` renders a regexp as a math-mode formula. The `terminal` and
`nonterm` printers map each symbol to its display name; the single-character check in the
terminal rendering applies to the printed name.

| Constructor | Rendering | Example |
| --- | --- | --- |
| `REps` | `\varepsilon` | `\varepsilon` |
| `REmpty` | `\varnothing` | `\varnothing` |
| `RTerm(Terminal t)` | single letter as-is; longer names via `\text{...}` with LaTeX escaping | `a`, `\text{walk}` |
| `RNonterm(Nonterminal nt)` | `\text{...}` with LaTeX escaping | `\text{S}` |
| `RSeq(l, r)` | `l \cdot r` (parentheses around non-atomic children) | `(a \cdot b) \cdot c` |
| `RAlt(l, r)` | `l \mid r` (parentheses around non-atomic children) | `(a \cdot b) \mid c` |
| `RStar(r)` | `(r)^*` — the operand is always parenthesized | `(a \cdot b)^*` |

Parenthesization rule: a child of a binary operator is wrapped in parentheses only when it is itself a binary operator (`RSeq`/`RAlt`); `RStar` is self-parenthesized by its own rendering.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Sequence renders as `\cdot` | The rendered formula uses the mathematical concatenation dot. The EBNF query input still spells concatenation with `/` (e.g. `walk/(O \mid R)+/walk`); only the display differs |
| Single-letter terminals render bare, longer names via `\text{...}` | A single letter is a valid math-mode identifier; multi-character names would otherwise render as a product of variables. `\text{...}` keeps them as literal labels, escaped with `AutomatonTikz.escapeLatex` (one source of truth for LaTeX escaping) |
| Nonterminals render via `\text{...}` | In the RPQ query every EBNF identifier is an edge label (see RpqInput), so nonterminals and terminals are both literal labels, not grammar symbols |
| Math-mode output only | The formula is placed in the summary's math wrappers next to the DFA figure; document-level commands would break that context |

## Regexp Tree Figure

The Arroyuelo step figures also show the query regexp as a tree (AST nodes, parent→child edges,
the current node highlighted). The tree is rendered by the dedicated
[RegexpTreeDot](regexp-tree-dot.md) / [RegexpTreeTikz](regexp-tree-tikz.md) modules: vertical
layout (root at top), rectangle nodes, node labels = subexpressions rendered by
`RegexpTeX.toTeX`, and the current node filled light blue. Every AST node is drawn, including
leaves; only intermediate nodes become steps. The tree is an AST, not a graph-structured stack,
so RPQ does not depend on the GSS renderers (see [FLPQ.Printers hub](FLPQ.Printers.md)).

## Book Reference

Chapter 11, `03_Arroyuelo.tex`: Section sec:RPQ_Arroyuelo — the query regexp displayed alongside its DFA. The book's running example query `walk/(O \mid R)+/walk` is used in tests.

## See Also

- [RpqInput module](rpq-input.md) — parses the query regexp from an EBNF file
- [EBNF Parser module (Regexp type)](ebnf-parser.md) — the `Regexp` type and `toDfa`
- [PathSemiringTeX module](path-semiring-tex.md) — matrix rendering for the same figures
- [FLPQ.Printers hub](FLPQ.Printers.md)
