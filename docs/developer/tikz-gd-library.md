# flpqdot Graph-Drawing Library

**Tags:** visualization, tikz, graph, layout, graphdrawing
**Kind:** visualization
**Source:** `tikz/pgf/gd/flpqdot/library.lua`
**Depends on:** PGF/TikZ `graphdrawing` (stock `layered` library)
**Used by:** FLPQ.Printers (TikZ templates), FLPQ.Cli (summary generator)

> **Abstract:** A project-owned, additive graph-drawing library for PGF/TikZ that
> reuses the stock `layered` (Sugiyama) pipeline and swaps in improved phases to get
> GraphViz-`dot`-like output while staying fully inside TikZ. It is loaded via
> `\usegdlibrary{flpqdot}` and exposes the `dot layout` key. Pure Lua, MIT-licensed,
> no PGF file is modified; the layout algorithms are implemented from the published
> literature and GraphViz is used only as a behavioral reference.

## Contents

- [Overview](#overview)
- [The `dot layout` Key](#the-dot-layout-key)
- [Spline Edge Router](#spline-edge-router)
- [Dot-Parity Guarantees](#dot-parity-guarantees)
- [Loading Mechanism](#loading-mechanism)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The stock TikZ `layered` layout produces polygonal (straight-segment) edges and
sub-optimal edge tracing compared with GraphViz `dot`. The flpqdot library improves
this by reusing the modular Sugiyama pipeline — whose phases (cycle removal, node
ranking, crossing minimization, node positioning, layer edge routing) are each
independently swappable — and overriding the phases that need improvement.

The library is **additive**: it requires the stock `layered` library and layers its
own phase modules on top, then declares a single composite key (`dot layout`) that
selects the pipeline with the flpqdot overrides. No PGF source file is modified, so
the library is safe to upgrade alongside PGF and is upstream-ready.

## The `dot layout` Key

`dot layout` is a composite key declared with a `use` field. When set, it first
selects the stock `layered layout` algorithm and then applies the flpqdot phase
overrides listed in its `use` array. The one phase overridden so far is **layer
edge routing**, replaced by the flpqdot spline router (below). The remaining
phases (cycle removal, node ranking, crossing minimization, node positioning) keep
their stock defaults — in particular, node ranking already uses the
network-simplex method by default — so node coordinates are identical to the stock
`layered layout`; only the edge geometry changes.

## Spline Edge Router

The stock `layered` pipeline leaves the `layer edge routing` phase as an empty
stub, so edges are drawn as straight polylines between node/dummy centers. The
flpqdot spline router (`EdgeRoutingSpline.lua`) fills that phase: for each edge it
collects the point sequence `[tail center, bend points…, head center]` and fits a
smooth curve through it using the Catmull-Rom → cubic-Bézier conversion
(central-difference tangents `T_i = (P_{i+1} − P_{i−1})/2` with duplicated
endpoints; per-segment controls `C1 = P_i + T_i/3`, `C2 = P_{i+1} − T_{i+1}/3`).

The router writes the resulting `Path` (`moveto` + one `curveto` per segment) to
`edge.orig_m.path` and clears `edge.bend_points`, so the downstream
`compatibility_graph_to_digraph` step skips the edge (it would otherwise rebuild a
polyline from the bend points) and `cutEdges` trims the Bézier at the node borders
and places the arrowheads. An edge with no bend points (adjacent layers) yields a
single, near-straight Bézier; an edge spanning *k* layers yields *k* segments.

Node sizes are already TeX-measured by the stock pipeline before layout, so the
router works with final node dimensions in a single pass (no separate
measurement round trip).

**Reciprocal-pair separation.** A pair `u→v` / `v→u` would otherwise trace the same
chord and overlap. The router detects reciprocal pairs (by node name) and offsets
every Bézier control point of each edge to one side of its chord — a fraction of the
chord length, clamped to a maximum. Because the reverse edge's chord is negated, the
same perpendicular sign bows the two edges to opposite sides, reproducing the visual
intent of the old renderer-side `bend left=15` hack natively in the layout engine.
Non-reciprocal edges are untouched.

## Dot-Parity Guarantees

The `FlpqdotLibraryTests` parity suite verifies that `dot layout` is "dot-like"
relative to the GraphViz `dot` oracle (via
`ExternalTools.compileDotStringToNodePositions`). For a set of representative
graphs (chain, star, diamond, reciprocal pair, multi-component) it checks:

- **Layering parity** — the same partition of nodes into ranks (layers). The rank
  axis is `x` for both `dot` (`rankdir=LR`) and TikZ (`grow'=right`); same-rank nodes
  share an `x`-coordinate and are clustered with a 1 pt tolerance.
- **Ordering parity** — the same within-rank node order.
- **Edge smoothness** — every representative graph emits cubic Bézier segments
  (`curveto` count > 0).

The comparison is **structural** (partition + ordering), never absolute
coordinates, so it is robust to font-metric drift and to the two engines' different
spacing. Both axes may be globally reflected (the engines can lay out in opposite
directions), so the check aligns the rank sequence and the within-rank order up to a
single reflection per axis.

**Disconnected components.** `dot` stacks disconnected components in the same ranks,
whereas the layered layout places them side by side (yielding more ranks). This is a
deliberate difference, so the rank structure is compared **per connected component**;
within a component the two must agree.

## Loading Mechanism

`\usegdlibrary{flpqdot}` resolves the library through kpathsea's `lua` file lookup,
which searches the **current directory** for the bare module path
(`pgf/gd/flpqdot/library.lua`) but not arbitrary `TEXINPUTS` roots. The F# side
therefore stages the `tikz/` library contents into the compile working directory
before invoking lualatex (see [ExternalTools](external-tools.md)). The `tikz/` tree
is copied to the application and test output directories at build time, and
`ExternalTools` copies its contents into each compile's working directory at run
time. The CLI likewise stages the library into the summary output directory
(`Summary.buildSummary` calls `ExternalTools.stageFlpqdotLibrary`), so a generated
merged summary is self-contained and compiles in-place.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Additive library (no PGF files modified) | Safe to upgrade with PGF; upstream-ready; no fork maintenance |
| Pure Lua (no C bridge) | LuaTeX cannot load C modules (`package.loadlib` is a no-op); keeps the library portable |
| Reuse the stock Sugiyama pipeline, swap phases | Each phase is independently configurable; only the weak phases need replacing |
| Catmull-Rom → Bézier edge fit | Matches `dot`'s smooth edge tracing; a standard, well-documented curve through the ranked/dummy points, with border clipping left to `cutEdges` |
| Composite `dot layout` key | Single user-facing entry point; the phase selection lives in one place (the Lua library), not scattered across F# option strings |
| Stage `tikz/` into the working dir | kpathsea's `lua` lookup searches the cwd but not `TEXINPUTS` roots; staging is the reliable mechanism that does not clobber the default search path |
| Implement from published papers, `dot` as reference only | GraphViz is EPL-2.0 and the project is MIT; no GraphViz code is copied |

## See Also

- [ExternalTools](external-tools.md) — stages the library into the compile working directory
- [TeX conventions](guides/tex-conventions.md) — TikZ/TeX rendering conventions
- [Project architecture](../project/architecture.md) — the `tikz/` directory in the solution tree
