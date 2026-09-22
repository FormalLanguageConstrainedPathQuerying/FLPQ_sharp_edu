# Automaton Visualization

**Tags:** visualization, automaton, dot, tikz, nfa, dfa, lr, rnglr, graphviz, graphdrawing
**Kind:** visualization
**Module:** AutomatonDot, AutomatonTikz, LRAutomatonTikz, RnglrAutomatonTikz
**Source:** `src/FLPQ.Printers/`
**Depends on:** Automaton, LR parser types, RnglrItem (FLPQ.Languages)
**Used by:** FLPQ.Cli (summary generation)

> **Abstract:** Visualizes NFA and DFA automata in two output formats: Graphviz DOT (`AutomatonDot`) and Tikz with layered layout (`AutomatonTikz`). Provides specialized Tikz renderers for LR automata (`LRAutomatonTikz`, grammar-based) and the RNGLR LR automaton (`RnglrAutomatonTikz`, RSM-item based) using rectangle shapes and aligned state content. All renderers accept parameterized label and state visualizer callbacks. Supports start state highlighting, final state double-borders, epsilon transitions (dotted), and loop edges.

## Contents

- [Supported Formats](#supported-formats)
- [AutomatonDot Module](#automatonDot-module)
- [AutomatonTikz Module](#automatonTikz-module)
- [LRAutomatonTikz Module](#lrautomatonTikz-module)
- [RnglrAutomatonTikz Module](#rnglrAutomatonTikz-module)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Supported Formats

| Format | Module | Features |
| --- | --- | --- |
| **DOT** | `AutomatonDot` | Standard automaton visualization via Graphviz |
| **Tikz** | `AutomatonTikz` | Layered layout, parametrizable shapes, enhanced styling |
| **Tikz (LR)** | `LRAutomatonTikz` | Rectangle states, aligned LR items with state numbers |
| **Tikz (RNGLR)** | `RnglrAutomatonTikz` | Rectangle states, aligned RSM items (`<nt> : <rsmState>`) with state numbers |

## AutomatonDot Module

### Functions

- `nfaToDot: (int -> 's -> string) -> NFA<'t,'s> -> string` — renders NFA with green start states, double-circle final states, dotted epsilon transitions
- `dfaToDot: (int -> 's -> string) -> DFA<'t,'s> -> string` — renders DFA to DOT

## AutomatonTikz Module

### Functions

- `nfaToTikz: (labelPrinter: 't -> string) -> (stateVisualizer: int -> 's -> string) -> (shape: string) -> NFA<'t,'s> -> string`
- `dfaToTikz: (labelPrinter: 't -> string) -> (stateVisualizer: int -> 's -> string) -> (shape: string) -> DFA<'t,'s> -> string`

### Visual Style

- Default shape: `circle` (parametrizable — `rectangle` for LR automata)
- Layout: `layered layout, <grow direction>, level sep=2cm, sibling sep=1.5cm`. The grow direction is parameterized by `AutomatonTikz.layeredGraphOptions shape growDirection`: the default is `defaultGrowDirection = "grow'=right"` (used by NFA/DFA/RSM/input-graph renderers); the GSS renderer selects `gssLayeredGrowDirection = "grow=left"` when it constrains input positions to layers, because pgf's same-layer cluster chaining reverses the orientation under the default grow direction.
- Start states: `fill=green!30, label=above:Start`
- Final states: `double, double distance=1.5pt, fill=red!30`
- Loop edges: `s%d ->["label",loop above] s%d`
- Epsilon transitions: `dotted` edges with `$\varepsilon$` label (math mode — a bare `\varepsilon` in a text-mode edge quote renders as an empty box, silently losing the label)
- Arrow heads: `Latex[width=3mm,length=3mm]`

### Template

Uses `data/tex_tikz_template.tex` with `standalone` class, `tikz`, graphdrawing libraries, and `babel` for edge label quotes. When embedded in a merged summary, tikzpicture is wrapped in `\resizebox{0.98\textwidth}{!}{...}`.

## LRAutomatonTikz Module

### Functions

- `lr0AutomatontoTikz` / `lr1AutomatontoTikz` — render LR automata with rectangle states

### State Content Format

```
$\begin{aligned}
\text{State N}\\
A &\to \alpha \cdot \beta \\
\end{aligned}$
```

- State number in `\text{State N}` header, LR items aligned by `&`, dot as `\cdot`, LR(1) lookahead appended after comma
- Delegates to `AutomatonTikz.dfaToTikz` with `shape = "rectangle"`

## RnglrAutomatonTikz Module

### Functions

- `renderRnglrItem: ('nt -> string) -> RnglrItem<'nt> -> string` — one item as `<nonterminal> : <rsmState>`
- `renderRnglrStateContent: ('nt -> string) -> int -> Set<RnglrItem<'nt>> -> string` — aligned state content
- `rnglrAutomatonToTikz: (Symbol<'t,'nt> -> string) -> ('nt -> string) -> DFA<Symbol<'t,'nt>, Set<RnglrItem<'nt>>> -> string`

### State Content Format

```
$\begin{aligned}
\text{State N}\\
S : 1 \\
A : 0 \\
\end{aligned}$
```

- The RNGLR LR automaton is a DFA over RSM symbols whose states hold sets of `RnglrItem` (a block nonterminal and an RSM state within its block) — see [RNGLR module](rnglr.md)
- Each item renders as `<nonterminal> : <rsmState>`; items appear in set (sorted) order
- Delegates to `AutomatonTikz.dfaToTikz` with `shape = "rectangle"`, reusing `LRAutomatonTikz.stateContentToTikzAs` for the math-mode aligned wrapper

## Design Decisions

| Decision | Rationale |
| --- | --- |
| State visualizer callback | Allows parameterized label generation per state index and label |
| Tikz as default for LR automata | Richer rendering with aligned items; DOT as fallback via `--use-dot` CLI flag |
| `babel` library for edge label quotes | Required for proper handling of quote syntax in Tikz graph edges |
| Enhanced arrow heads | `Latex[width=3mm,length=3mm]` for visibility |

## See Also

- [Automaton module](automaton.md) — NFA/DFA types
- [LR parser](lr-parser.md) — LR automata rendered by LRAutomatonTikz
- [RNGLR module](rnglr.md) — RNGLR LR automaton rendered by RnglrAutomatonTikz
- [Derivation tree visualization](derivation-tree-viz.md) — DOT for derivation trees
- [ExternalTools module](external-tools.md) — Graphviz/lualatex for compilation
