# Detailed Plan: Task 107 — Improve Tikz-based Automata Visualization

## Problem

The Tikz-based automata visualization introduced in tasks 104-106 has several issues:
1. Edge labels in the Tikz `graph` syntax via `s0 ->["label"] s1` may not compile correctly without the `babel` library.
2. Loop edges (i → i) render as curved edges but should use `loop above` for clarity.
3. Templates are missing `babel` and `arrows.meta` libraries.
4. Arrow heads are too small.
5. When embedding tikzpicture into the merged summary, it should be wrapped in `\resizebox`.
6. Node spacing needs to be increased for better readability.

## Changes

### 1. `data/tex_tikz_template.tex` — Update template preamble

- Add `babel` and `arrows.meta` to `\usetikzlibrary`
- Add `\tikzset{>={Latex[width=3mm,length=3mm]}}` for larger arrow heads
- Keep standalone documentclass (no resizebox)

### 2. `data/tex_summary_template.tex` — Update template preamble

- Add `babel` and `arrows.meta` to `\usetikzlibrary`
- Add `\tikzset{>={Latex[width=3mm,length=3mm]}}` for larger arrow heads

### 3. `src/FLPQ.Printers/AutomatonTikz.fs` — Improve Tikz generation

- **Loop edges**: In `transitionEdges`, when `i == j`, generate `s{i} ->["label",loop above] s{i};`
- **Loop edges for epsilon**: In `epsEdges`, when `i == j`, generate `s{i} ->[dotted, "\\varepsilon",loop above] s{i};`
- **Spacing**: In `tikzHeader`, add `level sep=2cm, sibling sep=1.5cm` to `\graph` options

### 4. `src/FLPQ.Printers/SummaryTeX.fs` — Wrap Tikz in resizebox

- Add `wrapTikz` function that wraps Tikz content in `\resizebox{0.98\textwidth}{!}{...}`
- Use this when embedding tikz in the summary header section

### 5. `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs` — Fix tests

- Update test expectations to include loop edges check
- Add test for loop edges rendering with `loop above`
- Verify all tikz compilation tests still pass with updated template

### 6. Documentation

- Update `docs/automaton-viz.md` with new tikz options
- Update `tasks/knowledge_base.md` if needed

## Files Modified

| File | Change |
|------|--------|
| `data/tex_tikz_template.tex` | Add babel, arrows.meta, tikzset |
| `data/tex_summary_template.tex` | Add babel, arrows.meta, tikzset |
| `src/FLPQ.Printers/AutomatonTikz.fs` | Loop edges, spacing |
| `src/FLPQ.Printers/SummaryTeX.fs` | Resizebox wrapping |
| `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs` | Test updates |
| `docs/automaton-viz.md` | Documentation updates |

## Order of Implementation

1. Update templates
2. Update AutomatonTikz.fs
3. Update SummaryTeX.fs
4. Update tests
5. Update documentation
6. Format, build, test
