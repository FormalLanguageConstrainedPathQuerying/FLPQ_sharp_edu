-- flpqdot spline edge router.
--
-- A "layer edge routing" phase for the layered (Sugiyama) pipeline that draws
-- each edge as a smooth Catmull-Rom spline (converted to cubic Beziers) through
-- its tail, dummy-node (bend) positions, and head -- the same point sequence the
-- stock polyline router uses, but smoothed. This is what gives the layout its
-- GraphViz-`dot`-like curved edges.
--
-- The path is emitted center-to-center; the stock pipeline's cutEdges step then
-- trims it at the node borders (and places the arrowhead), so border clipping is
-- handled for free. After setting the spline path the edge's bend_points are
-- cleared so the stock polyline builder (compatibility_graph_to_digraph) skips
-- the edge and does not overwrite the spline.
--
-- License: MIT (see the repository LICENSE). The spline is the standard
-- Catmull-Rom -> cubic Bezier conversion (Catmull & Rom 1974); GraphViz is used
-- only as a behavioral reference, no GraphViz code is copied.

local Coordinate = require "pgf.gd.model.Coordinate"
local Path = require "pgf.gd.model.Path"

local EdgeRoutingSpline = {}

-- Reciprocal-pair separation: a pair u->v / v->u would otherwise trace the same
-- chord and overlap. Each edge's Bezier control points are offset to one side of
-- the chord; the reverse edge's chord is negated, so the same perpendicular sign
-- bows it to the other side. The offset is a fraction of the chord length,
-- clamped to a maximum (pt).
local RECIPROCAL_BOW_FRACTION = 0.15
local RECIPROCAL_BOW_MAX_PT = 20

---
-- Cubic Bezier control points for the segment pts[i] -> pts[i+1] of a
-- Catmull-Rom spline through the point sequence |pts| (an array of
-- Coordinates). The tangent at a point is the central difference of its
-- neighbours; at the sequence ends the end point is duplicated (one-sided
-- difference), the standard Catmull-Rom endpoint convention.
--
local function segmentControlPoints (pts, i)
  local p0 = pts[i-1] or pts[i]
  local p1 = pts[i]
  local p2 = pts[i+1]
  local p3 = pts[i+2] or pts[i+1]

  local c1 = p1 + (p2 - p0) * (1/6)
  local c2 = p2 - (p3 - p1) * (1/6)
  return c1, c2
end

---
-- Route every edge of the graph as a smooth spline. Runs after dummy nodes are
-- removed (so edge.bend_points hold the intermediate positions) in the layered
-- pipeline.
--
function EdgeRoutingSpline:run()
  -- Directed arcs present in the graph, keyed by "tail\1head" node name, used to
  -- detect reciprocal pairs (u->v with a reverse v->u).
  local hasArc = {}
  for _, e in ipairs(self.graph.edges) do
    if not e:isLoop() then
      hasArc[e:getTail().name .. "\1" .. e:getHead().name] = true
    end
  end

  for _, e in ipairs(self.graph.edges) do
    if e.orig_m and not e:isLoop() then
      local tail = e:getTail()
      local head = e:getHead()

      -- Point sequence: tail center, bend (dummy) positions, head center.
      local pts = { Coordinate.new(tail.pos.x, tail.pos.y) }
      for _, bp in ipairs(e.bend_points) do
        pts[#pts + 1] = Coordinate.new(bp.x, bp.y)
      end
      pts[#pts + 1] = Coordinate.new(head.pos.x, head.pos.y)

      if #pts >= 2 then
        -- Reciprocal bow: offset every control point to one side of the chord so a
        -- reciprocal pair bows to opposite sides (zero for non-reciprocal edges).
        local offX, offY = 0, 0
        if hasArc[head.name .. "\1" .. tail.name] then
          local dx = head.pos.x - tail.pos.x
          local dy = head.pos.y - tail.pos.y
          local len = math.sqrt(dx * dx + dy * dy)
          if len > 0 then
            local k = math.min(RECIPROCAL_BOW_FRACTION * len, RECIPROCAL_BOW_MAX_PT)
            offX = -dy / len * k
            offY = dx / len * k
          end
        end

        local p = Path.new()
        p:appendMoveto(pts[1])
        for i = 1, #pts - 1 do
          local c1, c2 = segmentControlPoints(pts, i)
          p:appendCurveto(
            Coordinate.new(c1.x + offX, c1.y + offY),
            Coordinate.new(c2.x + offX, c2.y + offY),
            pts[i + 1]
          )
        end
        e.orig_m.path = p
        -- Clear so the stock polyline builder skips this edge.
        e.bend_points = {}
      end
    end
  end
end

-- done
return EdgeRoutingSpline
