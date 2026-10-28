-- flpqdot channel edge router.
--
-- A "layer edge routing" phase for the layered (Sugiyama) pipeline that draws
-- each edge as a smooth, non-overshooting Bezier path through its tail,
-- dummy-node (bend) positions, and head. Unlike the old Catmull-Rom spline, the
-- per-segment control points come from a monotone (clamped-Hermite) fit: the
-- tangent at a point is the central difference, and each control point is
-- clamped into its segment's bounding slab, so the curve (which lies in the
-- convex hull of its four points) cannot loop or overshoot at sharp turns.
--
-- The router is label-aware in three ways. First, reciprocal pairs (u->v / v->u)
-- are separated into distinct channels: each edge's interior points (and, for a
-- direct edge with no interior points, its control points) are displaced
-- perpendicular to the chord, the reverse edge to the opposite side (its chord
-- is negated, so the same perpendicular sign sends it the other way). The
-- displacement is at least the sum of the two edges' label half-heights, read
-- from the _G.flpqdot_edge_label_sizes channel (S1b; a default when absent), so
-- the two labels do not collide. Second, a collision sweep (S3) pushes apart any
-- two edges whose reserved label boxes overlap in the same channel: an edge's
-- reserved box is its measured label width by (label height + depth + path
-- stroke), centred on the edge's channel point (where its label sits, the
-- arc-length midpoint of the routed path). Edges in the same channel (their
-- reserved boxes overlap in x) whose boxes also overlap in y are pushed apart in
-- y until no reserved boxes overlap, and the displacement is applied to the
-- edge's channel points before the Bezier fit so the final path reflects it.
-- Third, collision-free label placement (S4): after the paths are final, each
-- label is offset perpendicular to its path's tangent (at the arc-length
-- midpoint of the sampled path) by the label half-height plus a gap, to the side
-- whose box intersects fewer node boxes and other edges' sampled paths. The
-- chosen position is stored in the _G.flpqdot_label_placement channel for the
-- TeX-side edge-callback wrapper (EdgeLabel.sty) to place the label. Size and
-- placement lookups tolerate a direction reversal (the layered layout reverses
-- back-edges during cycle removal).
--
-- The path is emitted center-to-center; the stock pipeline's cutEdges step then
-- trims it at the node borders (and places the arrowhead). After setting the
-- path the edge's bend_points are cleared so the stock polyline builder
-- (compatibility_graph_to_digraph) skips the edge and does not overwrite it.
--
-- License: MIT (see the repository LICENSE). The monotone cubic fit is the
-- standard clamped-Hermite construction; GraphViz is used only as a behavioral
-- reference, no GraphViz code is copied.

local Coordinate = require "pgf.gd.model.Coordinate"
local Path = require "pgf.gd.model.Path"
local LabelMeasure = require "pgf.gd.flpqdot.LabelMeasure"

local EdgeRoutingSpline = {}

-- Reciprocal-pair channel separation (S2). Each edge of a u->v / v->u pair is
-- displaced perpendicular to the chord by `sep` (the reverse edge to the
-- opposite side), so the two edges occupy distinct channels and their labels do
-- not collide. `sep` is a fraction of the chord length, clamped to [min, max],
-- and at least the sum of the two edges' label half-heights.
local RECIP_SEP_FRACTION = 0.15
local RECIP_SEP_MIN_PT = 4
local RECIP_SEP_MAX_PT = 20

-- Label-aware collision sweep (S3). An edge's reserved box is its label width
-- by (label height + depth + 2*PATH_STROKE_HALF), centred on its channel point.
-- Two reserved boxes that overlap in x (same channel) and in y are pushed apart
-- in y by the overlap plus LABEL_GAP. PATH_STROKE_HALF reserves half a path
-- stroke on each side of a label so a label does not sit on the path and collide
-- with a neighbouring edge.
local PATH_STROKE_HALF = 1
local LABEL_GAP = 2
local MAX_SWEEP_ITERS = 10

-- Label placement (S4). Each label is offset perpendicular to the path tangent
-- by (label half-height + LABEL_OFFSET_GAP) to the side with fewer collisions
-- (against node boxes and the other edges' sampled paths), so the label sits off
-- the path and does not cover a node or cross another edge.
local LABEL_OFFSET_GAP = 1
local PATH_SAMPLE_STEPS = 20

local function clamp (v, lo, hi)
  if v < lo then return lo end
  if v > hi then return hi end
  return v
end

local function dist (a, b)
  return math.sqrt((a.x - b.x) ^ 2 + (a.y - b.y) ^ 2)
end

---
-- Tangent (a Coordinate vector) at pts[i] of the point sequence |pts|: the
-- central difference of the neighbours, halved, with the end point duplicated
-- at the sequence ends (the standard Catmull-Rom endpoint convention).
--
local function tangentAt (pts, i)
  local prev = pts[i-1] or pts[i]
  local next = pts[i+1] or pts[i]
  return (next - prev) * 0.5
end

---
-- Control points for the segment pts[i] -> pts[i+1] of a monotone cubic fit
-- through |pts|. The raw Hermite control points are p_i + t_i/3 and
-- p_{i+1} - t_{i+1}/3 (t_i the tangent at p_i); each is clamped into the
-- segment's bounding slab so the Bezier (which lies in the convex hull of its
-- four points) cannot overshoot the segment.
--
local function segmentControlPoints (pts, i)
  local p1 = pts[i]
  local p2 = pts[i+1]
  local t1 = tangentAt(pts, i)
  local t2 = tangentAt(pts, i+1)

  local function clampToSlab (c)
    return Coordinate.new(
      clamp(c.x, math.min(p1.x, p2.x), math.max(p1.x, p2.x)),
      clamp(c.y, math.min(p1.y, p2.y), math.max(p1.y, p2.y))
    )
  end

  local c1 = clampToSlab(p1 + t1 * (1/3))
  local c2 = clampToSlab(p2 - t2 * (1/3))
  return c1, c2
end

---
-- Arc-length midpoint of the polyline through |pts|: a good estimate of where
-- the edge label will sit (the label is placed at the path midpoint). For a
-- two-point (direct) edge this is the chord midpoint.
--
local function polylineArcMid (pts)
  local total = 0
  for i = 1, #pts - 1 do
    total = total + dist(pts[i], pts[i+1])
  end
  local half = total / 2
  local acc = 0
  for i = 1, #pts - 1 do
    local seg = dist(pts[i], pts[i+1])
    if acc + seg >= half then
      local f = seg > 0 and (half - acc) / seg or 0
      return Coordinate.new(
        pts[i].x + f * (pts[i+1].x - pts[i].x),
        pts[i].y + f * (pts[i+1].y - pts[i].y)
      )
    end
    acc = acc + seg
  end
  return pts[#pts]
end

---
-- Arc-length midpoint of the polyline through |pts| together with the unit
-- tangent (the direction of the segment containing the midpoint). Used by the
-- label placement (S4) to offset a label perpendicular to the path.
--
local function polylineMidAndTangent (pts)
  local total = 0
  for i = 1, #pts - 1 do
    total = total + dist(pts[i], pts[i+1])
  end
  local half = total / 2
  local acc = 0
  for i = 1, #pts - 1 do
    local seg = dist(pts[i], pts[i+1])
    if acc + seg >= half then
      local f = seg > 0 and (half - acc) / seg or 0
      local mid = Coordinate.new(
        pts[i].x + f * (pts[i+1].x - pts[i].x),
        pts[i].y + f * (pts[i+1].y - pts[i].y)
      )
      local dx = pts[i+1].x - pts[i].x
      local dy = pts[i+1].y - pts[i].y
      local len = math.sqrt(dx * dx + dy * dy)
      return mid, (len > 0 and dx / len or 0), (len > 0 and dy / len or 1)
    end
    acc = acc + seg
  end
  return pts[#pts], 0, 1
end

---
-- Sample a Path (a sequence of moveto/curveto operands) into a list of
-- {x, y} points, for the label-placement collision check (S4).
--
local function samplePath (path)
  local pts = {}
  local cnt = 0
  local cur = nil
  local i = 1
  while path[i] ~= nil do
    local op = path[i]
    if op == "moveto" then
      cur = path[i+1]
      cnt = cnt + 1
      pts[cnt] = { x = cur.x, y = cur.y }
      i = i + 2
    elseif op == "curveto" then
      local c1, c2, to = path[i+1], path[i+2], path[i+3]
      local p0 = cur
      for k = 0, PATH_SAMPLE_STEPS do
        local t = k / PATH_SAMPLE_STEPS
        local mt = 1 - t
        cnt = cnt + 1
        pts[cnt] = {
          x = mt*mt*mt*p0.x + 3*mt*mt*t*c1.x + 3*mt*t*t*c2.x + t*t*t*to.x,
          y = mt*mt*mt*p0.y + 3*mt*mt*t*c1.y + 3*mt*t*t*c2.y + t*t*t*to.y,
        }
      end
      cur = to
      i = i + 4
    else
      i = i + 1
    end
  end
  return pts
end

---
-- Count how many obstacles (node bounding boxes and the other edges' sampled
-- paths) a label box centred at (cx, cy) with size (w, th) intersects. The edge
-- at index |selfIdx| in |otherPaths| is the label's own edge and is skipped.
--
local function countLabelCollisions (cx, cy, w, th, nodeBoxes, otherPaths, selfIdx)
  local x0, x1 = cx - w / 2, cx + w / 2
  local y0, y1 = cy - th / 2, cy + th / 2
  local count = 0
  for _, nb in ipairs(nodeBoxes) do
    if x0 < nb.max_x and x1 > nb.min_x and y0 < nb.max_y and y1 > nb.min_y then
      count = count + 1
    end
  end
  for idx, path in ipairs(otherPaths) do
    if idx ~= selfIdx then
      for _, p in ipairs(path) do
        if p.x >= x0 and p.x <= x1 and p.y >= y0 and p.y <= y1 then
          count = count + 1
          break
        end
      end
    end
  end
  return count
end

---
-- Route every edge of the graph as a smooth, non-overshooting Bezier. Runs
-- after dummy nodes are removed (so edge.bend_points hold the intermediate
-- positions) in the layered pipeline.
--
function EdgeRoutingSpline:run()
  -- Directed arcs present in the graph, keyed by "tail\1head" node name, used
  -- to detect reciprocal pairs (u->v with a reverse v->u).
  local hasArc = {}
  for _, e in ipairs(self.graph.edges) do
    if not e:isLoop() then
      hasArc[e:getTail().name .. "\1" .. e:getHead().name] = true
    end
  end

  -- Phase 1: build each edge's point sequence (tail, bends, head) and apply the
  -- reciprocal channel separation (S2), storing the displaced points and, for a
  -- direct edge, the control-point displacement.
  local edgeData = {}
  for _, e in ipairs(self.graph.edges) do
    if e.orig_m and not e:isLoop() then
      local tail = e:getTail()
      local head = e:getHead()

      local pts = { Coordinate.new(tail.pos.x, tail.pos.y) }
      for _, bp in ipairs(e.bend_points) do
        pts[#pts + 1] = Coordinate.new(bp.x, bp.y)
      end
      pts[#pts + 1] = Coordinate.new(head.pos.x, head.pos.y)

      local offX, offY = 0, 0
      local displaceControls = false
      if #pts >= 2 and hasArc[head.name .. "\1" .. tail.name] then
        local dx = head.pos.x - tail.pos.x
        local dy = head.pos.y - tail.pos.y
        local len = math.sqrt(dx * dx + dy * dy)
        if len > 0 then
          local s1 = LabelMeasure.channelSize(tail.name .. "->" .. head.name)
          local s2 = LabelMeasure.channelSize(head.name .. "->" .. tail.name)
          local hh1 = (s1.h + s1.d) / 2
          local hh2 = (s2.h + s2.d) / 2
          local sep = math.max(
            clamp(RECIP_SEP_FRACTION * len, RECIP_SEP_MIN_PT, RECIP_SEP_MAX_PT),
            hh1 + hh2
          )
          offX = -dy / len * sep
          offY = dx / len * sep
          -- Displace the interior (bend) points; a direct edge has none, so
          -- displace its control points instead.
          if #pts > 2 then
            for i = 2, #pts - 1 do
              pts[i] = Coordinate.new(pts[i].x + offX, pts[i].y + offY)
            end
          else
            displaceControls = true
          end
        end
      end

      edgeData[#edgeData + 1] = {
        e = e,
        pts = pts,
        offX = offX,
        offY = offY,
        displaceControls = displaceControls,
        key = tail.name .. "->" .. head.name,
      }
    end
  end

  -- Phase 2: label-aware collision sweep (S3). Compute each edge's channel
  -- point (where its label sits) and reserved box, then push apart edges in the
  -- same channel (reserved boxes overlapping in x) whose boxes also overlap in
  -- y, until no reserved boxes overlap (bounded iterations).
  for _, d in ipairs(edgeData) do
    local cp = polylineArcMid(d.pts)
    -- For a direct edge the S2 displacement is on the control points, so the
    -- label (at the chord midpoint) is displaced by the same offset.
    if #d.pts == 2 then
      cp = Coordinate.new(cp.x + d.offX, cp.y + d.offY)
    end
    d.chanX = cp.x
    d.chanY = cp.y
    local sz = LabelMeasure.channelSizeEither(d.key)
    d.resW = sz.w
    d.resH = (sz.h + sz.d) + 2 * PATH_STROKE_HALF
    d.dy = 0
  end

  for _ = 1, MAX_SWEEP_ITERS do
    local moved = false
    for a = 1, #edgeData do
      for b = a + 1, #edgeData do
        local da, db = edgeData[a], edgeData[b]
        -- Same channel: the reserved boxes overlap in x.
        if math.abs(da.chanX - db.chanX) < (da.resW + db.resW) / 2 then
          local ya = da.chanY + da.dy
          local yb = db.chanY + db.dy
          local reqSep = (da.resH + db.resH) / 2
          local ysep = yb - ya
          local absSep = math.abs(ysep)
          if absSep < reqSep then
            -- Push the later edge (lower priority) away from the earlier one by
            -- the overlap plus a gap, so the reserved boxes clear each other.
            local push = (reqSep - absSep) + LABEL_GAP
            if ysep >= 0 then
              db.dy = db.dy + push
            else
              db.dy = db.dy - push
            end
            moved = true
          end
        end
      end
    end
    if not moved then break end
  end

  -- Phase 3: apply the sweep displacement and emit the Bezier path.
  for _, d in ipairs(edgeData) do
    local pts = d.pts
    local dy = d.dy
    if #pts >= 2 then
      -- Apply the sweep displacement: shift the interior (bend) points for a
      -- multi-layer edge, or the control points for a direct edge, so the
      -- routed path (and hence its label) reflects the sweep.
      if #pts >= 3 and dy ~= 0 then
        for i = 2, #pts - 1 do
          pts[i] = Coordinate.new(pts[i].x, pts[i].y + dy)
        end
      end

      local p = Path.new()
      p:appendMoveto(pts[1])
      for i = 1, #pts - 1 do
        local c1, c2 = segmentControlPoints(pts, i)
        if d.displaceControls then
          c1 = Coordinate.new(c1.x + d.offX, c1.y + d.offY)
          c2 = Coordinate.new(c2.x + d.offX, c2.y + d.offY)
        end
        if #pts == 2 and dy ~= 0 then
          c1 = Coordinate.new(c1.x, c1.y + dy)
          c2 = Coordinate.new(c2.x, c2.y + dy)
        end
        p:appendCurveto(c1, c2, pts[i + 1])
      end
      d.e.orig_m.path = p
      -- Clear so the stock polyline builder skips this edge.
      d.e.bend_points = {}
      -- Store the final label anchor (channel point after the sweep) and the
      -- reserved box for the downstream label placement (S4) and the probe
      -- harness.
      d.e.orig_m.labelAnchor = { x = d.chanX, y = d.chanY + dy }
      d.e.orig_m.labelBox = { w = d.resW, th = d.resH }
    end
  end

  -- Phase 4: label placement (S4). Offset each label perpendicular to the path
  -- tangent, to the side with fewer collisions (against node boxes and the
  -- other edges' sampled paths), so the label sits off the path and does not
  -- cover a node or cross another edge. The position is stored in the
  -- _G.flpqdot_label_placement channel for the edge callback to place the label.
  local nodeBoxes = {}
  for _, n in ipairs(self.graph.nodes) do
    if n.path then
      local min_x, min_y, max_x, max_y = n.path:boundingBox()
      nodeBoxes[#nodeBoxes + 1] = { min_x = min_x, min_y = min_y, max_x = max_x, max_y = max_y }
    end
  end
  local otherPaths = {}
  for idx, d in ipairs(edgeData) do
    otherPaths[idx] = d.e.orig_m.path and samplePath(d.e.orig_m.path) or {}
  end
  _G.flpqdot_label_placement = _G.flpqdot_label_placement or {}
  for idx, d in ipairs(edgeData) do
    local samples = otherPaths[idx]
    if #samples >= 2 then
      -- Use the sampled (final) path, not the bend polyline, so a direct
      -- edge's Bezier bulge (S2/S3 control-point displacement) is reflected in
      -- the label anchor and tangent.
      local mid, tx, ty = polylineMidAndTangent(samples)
      local sz = LabelMeasure.channelSizeEither(d.key)
      local offset = (sz.h + sz.d) / 2 + LABEL_OFFSET_GAP
      -- The two candidate positions, offset perpendicular to the tangent.
      local nx, ny = -ty, tx
      local c1x, c1y = mid.x + nx * offset, mid.y + ny * offset
      local c2x, c2y = mid.x - nx * offset, mid.y - ny * offset
      local col1 = countLabelCollisions(c1x, c1y, d.resW, d.resH, nodeBoxes, otherPaths, idx)
      local col2 = countLabelCollisions(c2x, c2y, d.resW, d.resH, nodeBoxes, otherPaths, idx)
      local px, py
      if col1 <= col2 then
        px, py = c1x, c1y
      else
        px, py = c2x, c2y
      end
      _G.flpqdot_label_placement[d.key] = { x = px, y = py }
    end
  end
end

-- done
return EdgeRoutingSpline
