# Step TeX

**Tags:** visualization, tex, rpq, step-visualization
**Kind:** visualization
**Module:** StepTeX
**Source:** `src/FLPQ.Printers/StepTeX.fs`
**Used by:** ArroyueloStepCommon, ArroyueloSimplePathStepVisualizer, ArroyueloReachabilityStepVisualizer, BelyaninStepCommon

> **Abstract:** Generic TeX helpers shared by the RPQ step visualizers. `wrapAdjustbox` wraps content in a single shrink-only adjustbox with caller-supplied options; `wrapFormula` wraps a one-line math expression in a top-aligned, width-limited adjustbox so side-by-side matrices fit the formula column.

## Contents

- [Overview](#overview)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The Arroyuelo and Belyanin step visualizers both compose several matrices into one-line formulas
and wrap figures/expressions in shrink-only adjustboxes. `StepTeX` owns the two generic helpers
so neither visualizer depends on the other's module.

## Function Signatures

### `wrapAdjustbox: string -> string -> string`

`wrapAdjustbox options content` emits `\begin{adjustbox}{options} content \end{adjustbox}`.

### `wrapFormula: string -> string`

`wrapFormula expression` wraps `expression` with `max width=\textwidth, valign=T` so the
expression shrinks to the current column and top-aligns with the figure beside it.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Generic helpers in a neutral module | Both Arroyuelo and Belyanin need the same adjustbox/formula wrapping; keeping them in `BelyaninStepCommon` would make Arroyuelo depend on Belyanin (task 291) |
| `valign=T` in `wrapFormula` | The formula sits beside a figure in a two-column step row; top alignment keeps the row compact |
| Shrink-only adjustbox | Over-wide matrix formulas shrink to the column width, while small formulas keep their natural size and font metrics |

## See Also

- [Arroyuelo step common](arroyuelo-step-common.md)
- [Belyanin step common](belyanin-step-common.md)
- [SummaryTeX module](summary-tex.md)
