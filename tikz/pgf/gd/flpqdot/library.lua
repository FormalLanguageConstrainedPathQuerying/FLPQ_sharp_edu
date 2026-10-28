-- flpqdot: a project-owned, additive graph-drawing library for PGF/TikZ.
--
-- It reuses the stock `layered` (Sugiyama) pipeline and swaps in improved
-- phases (a smooth, label-aware channel edge router) to get GraphViz-`dot`-like
-- output while staying fully inside TikZ. No PGF file is modified; this library
-- is loaded additively via \usegdlibrary{flpqdot} and found through TEXINPUTS.
--
-- License: MIT (see the repository LICENSE). The layout algorithms are
-- implemented from the published literature (Sugiyama et al.; Gansner, Kindler
-- & Seibert 1993); GraphViz is used only as a behavioral reference, no GraphViz
-- code is copied.

-- Load the stock layered library (idempotent) so its keys and default
-- phases are available, then layer our phases on top.
require "pgf.gd.layered.library"

-- Load the TeX-side label measurement command (\flpqdotmeasureedgelabel),
-- which is the primary path for feeding edge-label sizes to the channel
-- router (the router itself cannot see labels; they live on the TeX side).
-- Injected as a TeX \RequirePackage so kpathsea resolves the .sty through
-- TEXINPUTS; the require cache above ensures this runs once per document.
tex.sprint("\\RequirePackage{pgf/gd/flpqdot/LabelMeasure}")

-- Load the TeX-side edge-label placement (wraps the stock TikZ edge callback
-- to place each label at the router's computed position, S4). Must load after
-- the TikZ graphdrawing front-end defines \pgfgdtikzedgecallback (it guards
-- against the front-end being absent).
tex.sprint("\\RequirePackage{pgf/gd/flpqdot/EdgeLabel}")

local declare = require("pgf.gd.interface.InterfaceToAlgorithms").declare

-- The flpqdot channel edge router, registered as a "layer edge routing" phase
-- option (an alternative to the stock "polyline layer edge routing"). The key
-- and module keep their historical "spline" names.
declare {
  key = "spline layer edge routing",
  algorithm = require "pgf.gd.flpqdot.EdgeRoutingSpline",
  phase = "layer edge routing",
  summary = [[
    Routes layered-layout edges as smooth, non-overshooting cubic Beziers
    (monotone/clamped-Hermite fit) through their tail, dummy-node, and head
    positions, with label-aware distinct-channel separation for reciprocal
    pairs, a label-aware collision sweep that pushes apart any two edges
    whose reserved label boxes overlap in the same channel, and
    collision-free label placement (each label offset off its path to the
    side that clears nodes and other edges), giving GraphViz-`dot`-like
    curved edges without overlapping labels.
  ]],
}

-- `dot layout` selects the layered (Sugiyama) pipeline with the flpqdot phase
-- overrides: the channel edge router replaces the stock polyline router. The
-- other phases (cycle removal, node ranking, crossing minimization, node
-- positioning) keep their stock defaults.
declare {
  key = "dot layout",
  use = {
    { key = "layered layout" },
    { key = "spline layer edge routing" },
  },
  summary = [[
    The |dot layout| is the flpqdot layered layout: the stock Sugiyama
    pipeline with flpqdot phase overrides (smooth, non-overshooting,
    label-aware channel edges).
  ]],
}
