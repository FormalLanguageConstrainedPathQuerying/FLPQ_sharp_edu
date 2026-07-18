# Automaton Visualization

**Tags:** visualization, automaton, dot, tikz, nfa, dfa, lr, graphviz, graphdrawing
**Kind:** visualization
**Module:** AutomatonDot, AutomatonTikz, LRAutomatonTikz
**Source:** `src/FLPQ.Printers/`
**Depends on:** Automaton, LR parser types
**Used by:** FLPQ.Cli (summary generation)

> **Abstract:** Visualizes NFA and DFA automata in two output formats: Graphviz DOT (`AutomatonDot`) and Tikz with layered layout (`AutomatonTikz`). Provides a specialized Tikz renderer for LR automata (`LRAutomatonTikz`) using rectangle shapes and aligned LR-item state content. All renderers accept parameterized label and state visualizer callbacks. Supports start state highlighting, final state double-borders, epsilon transitions (dotted), and loop edges.

## Contents

- [Supported Formats](#supported-formats)
- [AutomatonDot Module](#automatonDot-module)
- [AutomatonTikz Module](#automatonTikz-module)
- [LRAutomatonTikz Module](#lrautomatonTikz-module)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Supported Formats

| Format | Module | Features |
|--------|--------|----------|
| **DOT** | `AutomatonDot` | Standard automaton visualization via Graphviz |
| **Tikz** | `AutomatonTikz` | Layered layout, parametrizable shapes, enhanced styling |
| **Tikz (LR)** | `LRAutomatonTikz` | Rectangle states, aligned LR items with state numbers |

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
- Layout: `layered layout, grow'=right, level sep=2cm, sibling sep=1.5cm`
- Start states: `fill=green!30, label=above:Start`
- Final states: `double, double distance=1.5pt, fill=red!30`
- Loop edges: `s%d ->["label",loop above] s%d`
- Epsilon transitions: `dotted` edges with `\varepsilon` label
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

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| State visualizer callback | Allows parameterized label generation per state index and label |
| Tikz as default for LR automata | Richer rendering with aligned items; DOT as fallback via `--use-dot` CLI flag |
| `babel` library for edge label quotes | Required for proper handling of quote syntax in Tikz graph edges |
| Enhanced arrow heads | `Latex[width=3mm,length=3mm]` for visibility |

## See Also

- [Automaton module](automaton.md) — NFA/DFA types
- [LR parser](lr-parser.md) — LR automata rendered by LRAutomatonTikz
- [Derivation tree visualization](derivation-tree-viz.md) — DOT for derivation trees
- [ExternalTools module](external-tools.md) — Graphviz/lualatex for compilation
