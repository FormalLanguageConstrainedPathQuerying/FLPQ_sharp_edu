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
- [Graphviz/dot Analysis](#graphvizdot-analysis)
- [The `dot layout` Key](#the-dot-layout-key)
- [Channel-Based Label-Aware Router](#channel-based-label-aware-router)
- [Label Measurement](#label-measurement)
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

## Graphviz/dot Analysis

Task 30 analyzed how GraphViz `dot` achieves its edge quality and encoded the findings
into flpqdot. `dot` is a **behavioral reference only**: GraphViz is EPL-2.0 and this
project is MIT, so no GraphViz code is copied — the algorithms are implemented from the
published literature (below). For each of the three concerns — layout, label placement,
edge routing — the table states how `dot` does it and what flpqdot reuses or encodes.

| Concern | How `dot` does it | flpqdot |
| --- | --- | --- |
| **Layout (ranking / positioning)** | The Sugiyama hierarchical pipeline: cycle removal, node ranking (network simplex), crossing minimization (median/barycenter heuristics), coordinate assignment. | **Reused unchanged.** The stock `layered` pipeline is the same Sugiyama pipeline and its node ranking already defaults to network simplex, so node coordinates are identical to the stock `layered layout`; only the edge geometry differs. No work. |
| **Label placement** | Each label's rendered size is measured and space reserved for it; edge labels are placed off the edge path with a **white background** so the edge line does not show through the text. | **Encoded.** The `\flpqdotmeasureedgelabel` pre-pass measures each label as a real TeX box (exact size — the Lua router cannot see labels at all); the router's collision-free placement offsets the label perpendicular to its path's tangent, to the side that clears the most obstacles; the white background is added by `EdgeLabel.sty` (task 30, S4). |
| **Edge routing** | Edges are routed as splines through via-points (dummy nodes for edges spanning multiple ranks); the spline is **iteratively re-aimed so it clears the node boxes** along the path (the path is a sequence of node and inter-rank boxes, and the spline is adjusted until it passes through the corridors between them). | **Encoded.** The monotone (clamped-Hermite) Bézier fit gives the smooth, non-overshooting curve; reciprocal channel separation and the label collision sweep handle the two-direction and label-overlap cases; the node-clearance pass (task 30, S3) iteratively pushes the edge's interior points to clear node boxes, mirroring `dot`'s obstacle avoidance. |

The section above distinguishes what is **reused** (the stock layered pipeline) from
what is **encoded from `dot`** (label measurement, channel routing, node clearance,
white background). Parity with `dot` is structural, not absolute-coordinate — see
[Dot-Parity Guarantees](#dot-parity-guarantees).

**References.** The layout pipeline is the Sugiyama framework: Sugiyama, Tagawa & Toda,
*Methods for visual understanding of hierarchical systems*, IEEE Trans. Systems, Man,
Cybernetics SMC-11(2):109–125, 1981. `dot`'s four-pass algorithm — cycle removal,
ranking, crossing minimization, and spline edge routing ("the fourth pass makes splines
to draw edges") — is Gansner, Koutsofios, North & Vo, *A Technique for Drawing Directed
Graphs*, IEEE Trans. Software Engineering 19(3):214–230, 1993. The general-purpose edge
router is Eppstein, Gansner, Koutsofios & North, *Implementing a General-Purpose Edge
Router*, in Graph Drawing (GD'97), Springer LNCS. GraphViz is consulted as a behavioral
reference only; no code is copied.

## The `dot layout` Key

`dot layout` is a composite key declared with a `use` field. When set, it first
selects the stock `layered layout` algorithm and then applies the flpqdot phase
overrides listed in its `use` array. The one phase overridden so far is **layer
edge routing**, replaced by the flpqdot channel router (below). The remaining
phases (cycle removal, node ranking, crossing minimization, node positioning) keep
their stock defaults — in particular, node ranking already uses the
network-simplex method by default — so node coordinates are identical to the stock
`layered layout`; only the edge geometry changes.

## Channel-Based Label-Aware Router

The stock `layered` pipeline leaves the `layer edge routing` phase as an empty
stub, so edges are drawn as straight polylines between node/dummy centers. The
flpqdot router (`EdgeRoutingSpline.lua`; the module and `spline layer edge routing`
phase key retain their historical names) fills that phase: for each edge it collects
the point sequence `[tail center, bend points…, head center]` and fits a smooth,
**non-overshooting** curve through it.

**Monotone Bézier fit.** The per-segment control points come from a clamped-Hermite
(monotone) fit rather than the old Catmull-Rom spline. The tangent at a point is the
central difference `T_i = (P_{i+1} − P_{i−1})/2` (end point duplicated at the ends);
the raw controls are `C1 = P_i + T_i/3` and `C2 = P_{i+1} − T_{i+1}/3`, and each is
**clamped into its segment's bounding slab**. Because a cubic Bézier lies in the
convex hull of its four points, clamping the controls to the slab guarantees the curve
stays within the slab and cannot loop or overshoot at sharp turns (the failure mode of
the old Catmull-Rom fit).

The router writes the resulting `Path` (`moveto` + one `curveto` per segment) to
`edge.orig_m.path` and clears `edge.bend_points`, so the downstream
`compatibility_graph_to_digraph` step skips the edge (it would otherwise rebuild a
polyline from the bend points) and `cutEdges` trims the Bézier at the node borders
and places the arrowheads. An edge with no bend points (adjacent layers) yields a
single Bézier; an edge spanning *k* layers yields *k* segments.

Node sizes are already TeX-measured by the stock pipeline before layout, so the
router works with final node dimensions in a single pass (no separate
measurement round trip).

**Distinct-channel reciprocal separation.** A pair `u→v` / `v→u` would otherwise
trace the same chord and overlap. The router detects reciprocal pairs (by node name)
and displaces each edge's interior points (and, for a direct edge with no interior
points, its control points) **perpendicular to the chord** — the reverse edge's chord
is negated, so the same perpendicular sign sends it to the opposite side. The two
edges thus occupy **distinct channels** instead of sharing one chord. The displacement
`sep` is a fraction of the chord length, clamped to `[min, max]`, and **at least the
sum of the two edges' label half-heights** (read from the `_G.flpqdot_edge_label_sizes`
channel, [Label Measurement](#label-measurement); a default when absent), so the two
labels do not collide. The endpoints (tail/head centers) are never displaced — the edge
must still connect to the nodes, and `cutEdges` trims at the borders. Non-reciprocal
edges are routed with the monotone fit and no displacement.

**Label-aware collision sweep.** Reciprocal separation handles only `u→v`/`v→u` pairs;
two *non*-reciprocal edges can still land in the same channel with overlapping labels
(e.g. the two diagonals of a K₂,₂, whose labels both sit at the crossing). After the
reciprocal displacement, the router runs a collision sweep over every edge. Each edge's
**reserved box** is its measured label width by (label height + depth + a path-stroke
margin), centred on its **channel point** — the arc-length midpoint of the routed path,
where the label sits. Two edges are in the **same channel** when their reserved boxes
overlap in `x`; if they also overlap in `y`, the sweep pushes the later edge (lower
priority) away in `y` by the overlap plus a small gap. It repeats until no reserved
boxes overlap (bounded iterations). The displacement is applied to the edge's channel
points *before* the Bézier fit — the interior (bend) points for a multi-layer edge, the
control points for a direct edge — so the final path (and hence its label) reflects it.
The router stores each edge's final label anchor and reserved box on
`edge.orig_m` (`labelAnchor`, `labelBox`) for the downstream label placement and the
probe harness.

**Node clearance (task 30, S3).** The reciprocal separation and the sweep displace an
edge's interior (bend) points, and either can push a bend point into the bounding box
of a node the edge passes beside — the routed path would then cross that node. After
the sweep displacement and before the Bézier is emitted, the router runs a
node-clearance pass (`EdgeRoutingSpline.clearNodeObstacles`): each interior point that
falls inside a node's bounding box (the edge's own tail/head boxes excluded) is pushed
in `x` — the rank-perpendicular axis of the layout model (the rank axis is `y`) — to
the nearer side of the box plus a small margin, so the path detours *beside* the node
rather than through it (pushing along the rank axis would not clear a box for an edge
travelling along it). This mirrors `dot`'s iterative spline re-aiming to clear node
boxes. The pass iterates to a fixed point (bounded), leaving a point that cannot be
cleared (e.g. trapped between two boxes) as-is. The node boxes come from the
TeX-measured `node.tex` extents (relative to the node position): the node's own path
is not built until the compatibility step, after routing, so it is not available as an
obstacle source. The same boxes feed the label-placement collision check below. For a
multi-point edge the label anchor is recomputed from the cleared polyline so the label
tracks the detoured path.

**Collision-free label placement.** The sweep keeps labels from overlapping *each
other*, but a label sitting on its edge's path can still cover a node or cross another
edge. After the paths are final, the router places each label: it takes the arc-length
midpoint of the edge's *sampled* path (so a direct edge's Bézier bulge is honoured) and
its tangent, offsets the label **perpendicular to the tangent** by (label half-height +
a small gap), and evaluates the two candidate sides (either side of the path). It picks
the side whose label box intersects fewer obstacles — node bounding boxes and the other
edges' sampled paths — so the label sits off the path and clears nodes and edges. The
chosen position is stored in the `_G.flpqdot_label_placement` channel, keyed by the
edge's `tail->head`.

The position is consumed on the TeX side: `EdgeLabel.sty` (loaded by `library.lua`)
**wraps** the stock TikZ edge callback (`\pgfgdtikzedgecallback`) so that, after each
edge is drawn, the label is placed at the router's computed position (reading the label
text from the `_G.flpqdot_edge_labels` channel, populated by
`\flpqdotmeasureedgelabel`). The placed label is drawn with a **white background**
(`inner sep=1pt, fill=white`) so it masks any edge that runs under it — `dot`'s
readability fallback; the 1pt margin matches the router's reserved-box margin
(`PATH_STROKE_HALF`). Wrapping — rather than a one-time
`\pgfgdsetedgecallback` — is deliberate: the graphdrawing request callback reinstalls
`\pgfgdtikzedgecallback` at the start of every graph scope, so only a wrapper survives.
The placement is **backward-compatible**: a label is placed only when it is registered
in the channel (the renderer emitted a measurement and no default quote); otherwise the
wrapper draws the edge exactly as the stock callback does and lets the stock quote place
the label. Because the layered layout reverses back-edges during cycle removal, both the
size lookup (`LabelMeasure.channelSizeEither`) and the callback's placement/label lookup
tolerate a direction reversal (they try the key and its reverse).

## Label Measurement

The router needs each edge label's rendered size to reserve space for it and avoid
label/edge/node collisions. The stock pipeline measures **node** boxes in TeX (and
keeps the results for layout) but never measures **edge** labels — and, critically,
the graphdrawing Lua router **cannot see edge labels at all**: a label (`["S"]`), its
styling, and any edge-specific TikZ option live on the TeX side and are never exposed
to a routing phase. So the size must be measured in TeX and handed to the router
through a channel.

**Primary path — TeX measures, Lua routes.** The renderer emits a pre-pass of
`\flpqdotmeasureedgelabel{<key>}{<label>}` commands (defined in `LabelMeasure.sty`,
loaded by `library.lua`) **before** the `\graph`. Each command `\setbox`es the label
exactly as TeX renders it (any math/text/accent) and stores the size in the Lua global
`_G.flpqdot_edge_label_sizes[<key>] = { w, h, d }` (pt). The key is the syntactic edge
direction, `tail .. "->" .. head`, as written in the source. Because the measurement is
a real TeX box, it is exact for arbitrary complex labels (e.g. `$a+b$` measures ~21.8 pt,
not the sum of its letters). (The layered layout may reverse a back-edge during cycle
removal, so the model's direction can differ from this source key; the reader tolerates
that via `channelSizeEither`, below.)

The F# side owns the pre-pass emission in the shared
[FlpqdotTikz](flpqdot-tikz.md) helper: `RpqGraphTikz` emits it (task 30, S1a — the
Kronecker-CFPQ step-5 fix), and the remaining `dot layout` renderers adopt the same
helper (task 30, S1b). A registered non-loop edge drops its stock quote, so the
`EdgeLabel.sty` wrapper places the label; loop edges keep their quote.

**Fallback — Lua estimator.** `LabelMeasure.measure(content, isMath)` (in
`LabelMeasure.lua`) measures a label in pure Lua by summing per-glyph metrics in the
current font. It is exact for single-character labels and a safe (slight) over-estimate
for longer ones (kerning is not applied). It is used only when no measured size is
present for an edge.

**Reading the channel.** `LabelMeasure.channelSize(key)` returns the stored
`{ w, h, d }` for an edge key, or a fixed default single-character size when absent.
`LabelMeasure.channelSizeEither(key)` additionally tolerates a direction reversal — it
tries the key, then its reverse (`LabelMeasure.reverseKey`), then the default — because
the layered layout reverses back-edges during cycle removal, so the model's `tail->head`
can differ from the source direction the measurement pre-pass used. The router calls
`channelSizeEither` to obtain each label's size.

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
| Monotone (clamped-Hermite) Bézier edge fit | Matches `dot`'s smooth edge tracing without Catmull-Rom overshoot; controls clamped to each segment's bounding slab so the curve cannot loop at sharp turns, with border clipping left to `cutEdges` |
| Label-aware distinct-channel reciprocal separation | A `u→v`/`v→u` pair is displaced to opposite sides of its chord by at least the sum of the two label half-heights (from the measurement channel), so the edges and their labels do not overlap |
| Label-aware collision sweep | Any two non-reciprocal edges whose reserved label boxes (label width × (height + depth + path stroke), centred on the path midpoint) overlap in the same channel are pushed apart in `y` before the Bézier fit, so no two edge labels overlap regardless of graph shape |
| Node clearance (interior points pushed out of node boxes) | A reciprocal/sweep-displaced bend point can land inside a node's box, making the path cross the node; each interior point inside a box (the edge's own tail/head excluded) is pushed in `x` (the rank-perpendicular axis) to the nearer side plus a margin, iterating to a bounded fixed point, mirroring `dot`'s spline re-aiming; the boxes come from the TeX-measured `node.tex` (the node's own path is not built until after routing) |
| Collision-free label placement (wrapped edge callback) | Each label is offset perpendicular to its path's tangent, to the side whose box intersects fewer node boxes / other edges' paths, and placed by a TeX-side wrapper around the stock edge callback (a one-time `\pgfgdsetedgecallback` would be clobbered by the per-scope request callback); placement and size lookups tolerate the cycle-removal direction reversal |
| White background behind placed labels | A placed label is drawn with `inner sep=1pt, fill=white` so it masks any edge running under it (`dot`'s readability fallback); the 1pt margin matches the router's reserved-box margin; only placed (channel-registered) labels get it, stock-quote labels are untouched |
| Composite `dot layout` key | Single user-facing entry point; the phase selection lives in one place (the Lua library), not scattered across F# option strings |
| Stage `tikz/` into the working dir | kpathsea's `lua` lookup searches the cwd but not `TEXINPUTS` roots; staging is the reliable mechanism that does not clobber the default search path |
| Implement from published papers, `dot` as reference only | GraphViz is EPL-2.0 and the project is MIT; no GraphViz code is copied |

## See Also

- [FlpqdotTikz](flpqdot-tikz.md) — the F# shared helper that emits the measurement pre-pass
- [ExternalTools](external-tools.md) — stages the library into the compile working directory
- [TeX conventions](guides/tex-conventions.md) — TikZ/TeX rendering conventions
- [Project architecture](../project/architecture.md) — the `tikz/` directory in the solution tree
