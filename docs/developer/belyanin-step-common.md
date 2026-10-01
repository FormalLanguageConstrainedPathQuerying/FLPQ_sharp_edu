# BelyaninStepCommon Module

**Tags:** visualization, rpq, belyanin, dot, tikz, matrix, step-visualization, shared
**Kind:** visualization
**Module:** BelyaninStepCommon
**Source:** `src/FLPQ.Printers/BelyaninStepCommon.fs`
**Depends on:** FLPQ.LinearAlgebra, FLPQ.Languages, FLPQ.RPQ, PathSemiringTeX, RpqGraphViz, AutomatonDot, AutomatonTikz
**Used by:** BelyaninSimplePathStepVisualizer, BelyaninReachabilityStepVisualizer, FLPQ.Cli (step writer)
**Book reference:** Chapter 11, Section 02_BFS.tex, Algorithm algo:RPQ_BFS_semiring

> **Abstract:** Shared record types and cell-agnostic rendering helpers for the two Belyanin
> RPQ step visualizers. Both the simple-path (path semiring) and the reachability (Boolean)
> visualizer return the same `BelyaninVisualizationStep` list, so the CLI step writer
> (`Helpers.writeBelyaninStepsVisualization`) and the `-s` summary section builder consume a
> single record shape regardless of semantics.

## Contents

- [Overview](#overview)
- [Type Definitions](#type-definitions)
- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The Belyanin step visualizers differ only in the matrix cell type and in whether path
information is available; their step layout (start boundary, per-label select/extend rows,
end boundary, DOT + TikZ figures) is identical. This module owns the parts that do not
depend on the cell type:

- the three visualization records returned by both `renderSteps` functions;
- label/state/vertex TeX printers and the adjustbox/formula wrappers;
- the DFA highlight figure helper;
- cell-generic frontier/edge predicates parameterized by an `isZero: 'a -> bool`
  predicate (`PathSemiring.isZero` for path matrices, `not` for Boolean matrices).

## Type Definitions

```fsharp
type BelyaninLabelVisual =
    { SelectFormula: string
      ExtendFormula: string
      AutomatonDot: string
      AutomatonTikz: string
      GraphDot: string
      GraphTikz: string }

type BelyaninBoundaryVisual =
    { Frontier: string
      Visited: string
      AutomatonDot: string
      AutomatonTikz: string
      GraphDot: string
      GraphTikz: string }

type BelyaninVisualizationStep =
    { Start: BelyaninBoundaryVisual
      Labels: BelyaninLabelVisual list
      End: BelyaninBoundaryVisual option }
```

A step has a start boundary, one label row per active label (in trace order), and — for
non-init steps — an end boundary. The init step has `End = None` and no label rows.

## Module Functions

```fsharp
val labelToTeX: ('t -> string) -> AutomatonLabel<'t> -> string
val stateLabel: int -> string   // q_i
val vertexLabel: int -> string  // v_i
val wrapAdjustbox: string -> string -> string
val wrapFormula: string -> string

val rowNonEmpty: ('a -> bool) -> Matrix<'a> -> int -> bool
val colNonEmpty: ('a -> bool) -> Matrix<'a> -> int -> bool
val frontierStates: ('a -> bool) -> Matrix<'a> -> Set<int>
val usedAutoEdges: ('a -> bool) -> Matrix<'a> -> Matrix<bool> -> Set<int * int>
val followedEdges: ('a -> bool) -> Matrix<bool> -> Matrix<'a> -> Set<int * int>

val dfaFigures:
    ('t -> string) -> DFA<'t, int> -> Set<int> -> Set<int> -> Set<int * int> -> string * string
```

- `rowNonEmpty isZero m i` / `colNonEmpty isZero m j` — whether a matrix row/column holds a
  cell that is not the additive zero.
- `frontierStates isZero m` — automaton states with a non-empty row (`{q : m[q, *] ≠ 0}`).
- `usedAutoEdges isZero m n` — DFA transitions `q → q'` with `n.[q, q']` and a non-empty
  frontier at `q`.
- `followedEdges isZero g select` — graph edges `u → v` with `g.[u, v]` and a non-empty
  selected cell at `u`.
- `dfaFigures` — the query DFA in DOT and TikZ with frontier states, target states, and
  transitions highlighted; both visualizers pick the format the runner requests.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Records live here, not in either visualizer | Both visualizers must return the identical record so the CLI writer and the `-s` summary keep one pipeline; defining it once avoids a copy |
| Frontier/edge predicates parameterized by `isZero` | The same set computations apply to path-set cells (`PathSemiring.isZero`) and Boolean cells (`not`); one implementation covers both without duplication |
| `dfaFigures` shared | The DFA highlight rendering is identical for both semantics; only the frontier/target/edge sets fed to it differ |
| Per-semantics formula and tile helpers stay in the visualizers | The matrix renderers differ (`PathSemiringTeX.matrixWithStateVertexLabelsBody` vs `boolMatrixToTeXBody`), so those small helpers are not shared |

## See Also

- [Belyanin simple-path step visualization](belyanin-step-viz.md) — the path semiring variant (shows paths)
- [Belyanin reachability step visualization](belyanin-reachability-step-viz.md) — the Boolean variant (no path edges)
- [Belyanin RPQ module](belyanin-rpq.md) — produces the two trace kinds
- [PathSemiringTeX module](path-semiring-tex.md) — matrix rendering for both cell types
