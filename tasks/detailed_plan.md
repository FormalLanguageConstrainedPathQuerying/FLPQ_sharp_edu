# Detailed Plan: Task 104 — Tikz-based Visualization for Automata

## Problem

Automata are currently visualized only via Graphviz dot (`AutomatonDot.fs`). We need Tikz-based visualization for better integration with lualatex documents and to support rendering that dot cannot easily produce (e.g., LR items in aligned environments).

## Goal

Create `AutomatonTikz.fs` module with `nfaToTikz` and `dfaToTikz` functions that generate Tikz `\begin{tikzpicture}...\end{tikzpicture}` blocks using the `graphdrawing` library with layered layout.

## Design Decisions

### Decision 1: Return tikzpicture block only (not full document)

The module returns just the `\begin{tikzpicture}...\end{tikzpicture}` content. A template wraps it with the necessary preamble for standalone compilation. This allows the Tikz code to be inline in larger documents (e.g., summary).

### Decision 2: Interface follows AutomatonDot pattern

Same parameter pattern:
```fsharp
nfaToTikz (labelPrinter: 't -> string) (stateVisualizer: int -> 's -> string) (nfa: NFA<'t, 's>) : string
dfaToTikz (labelPrinter: 't -> string) (stateVisualizer: int -> 's -> string) (dfa: DFA<'t, 's>) : string
```

Additional optional parameter for node shape (default: `circle`).

### Decision 3: Node rendering

- Node identifiers: `s0`, `s1`, ... (safe for Tikz)
- Node content: via `as={...}` key with stateVisualizer output (escaped for LaTeX)
- Start states: `fill=green!30, label=above:Start`
- Final states: `double, double distance=1.5pt, fill=red!30`
- Default nodes: `draw, circle` (shape parametrizable)

### Decision 4: Edge rendering

- Terminal transitions: `s{i} ->["label"] s{j}` (with label from labelPrinter)
- Multiple labels on same edge: comma-separated
- Epsilon transitions: `s{i} ->[dotted, "ε"] s{j}`

### Decision 5: LaTeX escaping

The `as` key content needs escaping: `_` → `\_`, `{` → `\{`, `}` → `\}`, `$` → `\$`, `%` → `\%`, `#` → `\#`, `&` → `\&`, `\` → `\\`

### Decision 6: Shape parametrizability

Add a `?shape: string` parameter defaulting to `"circle"`. For basic automata: circle. For LR automata (task 105): rectangle.

## Implementation Checklist

### 1. `src/FLPQ.Printers/AutomatonTikz.fs` — New module
- [ ] Latex escape helper: `escapeLatex`
- [ ] `nodeOptions`: build node attribute string
- [ ] `transitionEdges`: build transition edges with labels
- [ ] `epsEdges`: build epsilon edges (dotted)
- [ ] `nfaToTikz`: render NFA as tikzpicture
- [ ] `dfaToTikz`: render DFA as tikzpicture
- [ ] Default shape `circle`; make shape parametrizable

### 2. `src/FLPQ.Printers/FLPQ.Printers.fsproj` — Add compilation entry
- [ ] Add `AutomatonTikz.fs` after `AutomatonDot.fs`

### 3. `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs` — Add Tikz tests
- [ ] Add Tikz compilation template (`data/tex_tikz_template.tex`)
- [ ] Test: simple automaton Tikz compiles with lualatex
- [ ] Test: automaton with epsilon Tikz compiles
- [ ] Test: DFA from LR(0) automaton Tikz compiles
- [ ] Test: multiple start/final states Tikz compiles

### 4. `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` — Add template content
- [ ] Add `tex_tikz_template.tex` to Content items

### 5. Final Checks
- [ ] Format: `dotnet fantomas . --check`
- [ ] Build: `dotnet build FLPQ.slnx -c Release`
- [ ] Tests: `dotnet test`
