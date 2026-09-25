# GllStepVisualizer Module

**Tags:** visualization, gll, cfg, dot, tikz, path-index, step-visualization
**Kind:** visualization
**Module:** GllStepVisualizer
**Source:** `src/FLPQ.Printers/GllStepVisualizer.fs`
**Depends on:** GssDot, GssTikz, RsmDot, RsmTikz, PathIndexTeX, InputGraphDot
**Used by:** FLPQ.Cli (GllRunner)
**Book reference:** Section sec:CFPQ_GLL (Chapter 6, `06_GLL_Based.tex`)

> **Abstract:** Renders each step of GLL parsing (one descriptor-queue worklist step, plus the
> initial state) into a set of TeX/DOT/TikZ artifacts: the descriptors queue and table, the newly
> added descriptors, the graph-structured stack (GSS), the path index matrix, the input tokens and
> input graph, and the extended RSM with the current state highlighted. Both DOT and TikZ are
> produced per step; the runner writes only one format (TikZ by default, DOT with `--use-dot`).

## Contents

- [Data Structure](#data-structure)
- [Module Functions](#module-functions)
- [Step Artifacts](#step-artifacts)
- [Step Templates](#step-templates)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Data Structure

```fsharp
type GllVisualizationStep =
    { Queue: string
      DescriptorsTable: string
      NewDescriptors: string
      GssDot: string
      GssTikz: string
      PathIndex: string
      Input: string
      InputTikz: string
      RsmDot: string
      RsmTikz: string }
```

One record per parsing step (`GLL.GLLParsingStep`), plus one for the initial state.

## Module Functions

```fsharp
val renderSteps: ('t -> string) -> ('nt -> string) -> ExtendedRSM<'t, 'nt> -> GLLParsingStep<'t, 'nt> list -> PathIndex<'t, 'nt> -> int -> Graph<int, Option<'t>> -> GllVisualizationStep list
```

`renderSteps terminalPrinter nonterminalPrinter ersm steps pathIndex vertexCount inputGraph`
renders every step. `renderStep` renders a single step with its highlights; `renderInit` renders the
initial state (the initial descriptor present but not highlighted, no GSS/RSM/input highlights).
The printers label terminals and nonterminals in the descriptors, GSS edge labels, and RSM.

## Step Artifacts

- **Descriptors** — the queue (`Queue`), the to-handle/handled table (`DescriptorsTable`), and the
  newly added descriptors with new/changed coloring (`NewDescriptors`), each as TeX. A descriptor is
  rendered as `(rsmState, vertex, gssIdx, matchedRange)`.
- **GSS** — the graph-structured stack with active/new vertices and edges highlighted and the
  current GSS index marked; DOT (`GssDot`) and TikZ (`GssTikz`) via `GssDot.toDotFromSets` /
  `GssTikz.toTikzFromSets`.
- **Path index** — the K×K path index matrix with the cells changed during the step highlighted
  (`PathIndex`, via `PathIndexTeX`).
- **Input** — the input tokens with the current position underlined (`Input`) and the input graph
  with the current vertex highlighted (`InputTikz`, via `InputGraphDot` / TikZ).
- **Extended RSM** — the extended RSM with the step's current state highlighted, DOT (`RsmDot`) and
  TikZ (`RsmTikz`).

## Step Templates

`data/GLL_step_template.tex` (DOT: `__STEP_GSS_PDF__`, `__STEP_INPUT_PDF__`, `__STEP_RSM_PDF__`)
and `data/GLL_step_tikz_template.tex` (TikZ: `__STEP_GSS_TIKZ__`, `__STEP_INPUT_TIKZ__`,
`__STEP_RSM_TIKZ__`), both with `__DESCRIPTORS_TABLE__`, `__NEW_DESCRIPTORS__`, and `__PATH_INDEX__`
slots. At summary time the section builder wraps the TikZ figures in `SummaryTeX.wrapTikzAdjustbox`
so each figure fits its column — see [SummaryTeX module](summary-tex.md).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| GSS reuses the shared GSS renderers (`GssDot.toDotFromSets` / `GssTikz.toTikzFromSets`) | One implementation of "layered graph with highlighted/current vertex" for all step figures; GLL passes no position-layering (`positionOf = None`) |
| Both DOT and TikZ produced per step, runner picks one | Matches the existing runner convention (TikZ default, DOT via `--use-dot`) without rendering twice |
| `renderStep` / `renderInit` share the same parameter list | The initial state is a step with no highlights; a single signature avoids a divergent variant |

## Book Reference

Section `sec:CFPQ_GLL` (Chapter 6, `06_GLL_Based.tex`) — GLL parsing over RSMs with a descriptor
worklist queue and a path index; each worklist step is one visualization snapshot.

## See Also

- [GLL algorithm module](gll.md) — produces the `GLLParsingStep` snapshots and path index
- [PathIndex module](path-index.md) — path index matrix and its TeX rendering
- [RSM visualization](rsm-viz.md) — extended RSM DOT/TikZ rendering
- [FLPQ.Printers hub](FLPQ.Printers.md)
