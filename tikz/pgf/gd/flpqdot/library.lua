-- flpqdot: a project-owned, additive graph-drawing library for PGF/TikZ.
--
-- It reuses the stock `layered` (Sugiyama) pipeline and swaps in improved
-- phases (a smooth spline edge router) to get GraphViz-`dot`-like output
-- while staying fully inside TikZ. No PGF file is modified; this library is
-- loaded additively via \usegdlibrary{flpqdot} and found through TEXINPUTS.
--
-- License: MIT (see the repository LICENSE). The layout algorithms are
-- implemented from the published literature (Sugiyama et al.; Gansner,
-- Kindler & Seibert 1993; Catmull & Rom 1974); GraphViz is used only as a
-- behavioral reference, no GraphViz code is copied.

-- Load the stock layered library (idempotent) so its keys and default
-- phases are available, then layer our phases on top.
require "pgf.gd.layered.library"

local declare = require("pgf.gd.interface.InterfaceToAlgorithms").declare

-- The flpqdot spline edge router, registered as a "layer edge routing" phase
-- option (an alternative to the stock "polyline layer edge routing").
declare {
  key = "spline layer edge routing",
  algorithm = require "pgf.gd.flpqdot.EdgeRoutingSpline",
  phase = "layer edge routing",
  summary = [[
    Routes layered-layout edges as smooth Catmull-Rom splines (cubic Beziers)
    through their tail, dummy-node, and head positions, giving GraphViz-`dot`-
    like curved edges.
  ]],
}

-- `dot layout` selects the layered (Sugiyama) pipeline with the flpqdot phase
-- overrides: the spline edge router replaces the stock polyline router. The
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
    pipeline with flpqdot phase overrides (smooth spline edges).
  ]],
}
