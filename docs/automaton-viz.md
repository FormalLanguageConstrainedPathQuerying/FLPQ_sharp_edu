# Automaton Visualization

## AutomatonDot Module

Module: `AutomatonDot` in `FLPQ.Printers`.

### Overview

Visualizes NFA and DFA as Graphviz DOT graphs.

### Types

Uses `NFA<'t,'s>` and `DFA<'t,'s>` from `Automaton.fs`. No new types.

### Functions

- `nfaToDot: (int -> 's -> string) -> NFA<'t,'s> -> string` — renders NFA to DOT with state labels, green start states, double-circle final states, dotted epsilon transitions
- `dfaToDot: (int -> 's -> string) -> DFA<'t,'s> -> string` — renders DFA to DOT

### Design decisions

- State visualizer callback allows parameterized label generation
- Epsilon transitions rendered as dotted edges with epsilon label

---

## AutomatonTikz Module

Module: `AutomatonTikz` in `FLPQ.Printers`.

### Overview

Visualizes NFA and DFA as Tikz graphs using the `graphdrawing` library with layered layout.
Generates `\begin{tikzpicture}...\end{tikzpicture}` blocks (no document preamble — intended for wrapping in a standalone template or inline inclusion).

### Functions

- `nfaToTikz: (labelPrinter: 't -> string) -> (stateVisualizer: int -> 's -> string) -> (shape: string) -> NFA<'t,'s> -> string`
- `dfaToTikz: (labelPrinter: 't -> string) -> (stateVisualizer: int -> 's -> string) -> (shape: string) -> DFA<'t,'s> -> string`

### Design decisions

- Default shape: `circle` (parametrizable — `rectangle` for LR automata)
- Layout: `layered layout, grow'=right, level sep=2cm, sibling sep=1.5cm` (left-to-right, enhanced spacing)
- Start states: `fill=green!30, label=above:Start`
- Final states: `double, double distance=1.5pt, fill=red!30`
- Loop edges (self-loops): `s%d ->["label",loop above] s%d` for both terminal and epsilon transitions
- Epsilon transitions: `dotted` edges with `\varepsilon` label
- Edge labels use the `quotes` library (`s%d ->["label"] s%d` syntax)
- State content rendered via `as={...}` key — the caller provides LaTeX-ready content (not escaped)
- Retains `escapeLatex` helper for edge labels only (LaTeX special characters)
- Arrow heads enlarged via `\tikzset{>={Latex[width=3mm,length=3mm]}}`

### Template

Compiled via `data/tex_tikz_template.tex`:
```latex
\documentclass{standalone}
\usepackage{amsmath}
\usepackage{tikz}
\usetikzlibrary{graphs, graphdrawing, quotes, babel, arrows.meta}
\usegdlibrary{layered}
\tikzset{>={Latex[width=3mm,length=3mm]}}
```

`babel` is required for proper handling of edge label quotes in the Tikz graph syntax.
When embedded in a merged summary, the tikzpicture is wrapped in `\resizebox{0.98\textwidth}{!}{...}` for page fitting.

---

## LRAutomatonTikz Module

Module: `LRAutomatonTikz` in `FLPQ.Printers`.

### Overview

Specialized Tikz renderer for LR automata. Uses rectangle shape and renders state content as aligned LR items with state numbers.

### Functions

- `lr0AutomatontoTikz: (aug: Grammar<'t,'nt>) -> (dfa: DFA<Symbol<'t,'nt>, Set<LR0Item<'t,'nt>>>) -> string`
- `lr1AutomatontoTikz: (aug: Grammar<'t,'nt>) -> (dfa: DFA<Symbol<'t,'nt>, Set<LR1Item<'t,'nt>>>) -> string`

### State content format

```
$\begin{aligned}
\text{State N}\\
A &\to \alpha \cdot \beta \\
...
\end{aligned}$
```

- State number in `\text{State N}` header
- LR items aligned by `&` before `\to`
- Dot rendered as `\cdot`
- LR(1) lookahead appended after comma
- Delegates to `AutomatonTikz.dfaToTikz` with `shape = "rectangle"`
