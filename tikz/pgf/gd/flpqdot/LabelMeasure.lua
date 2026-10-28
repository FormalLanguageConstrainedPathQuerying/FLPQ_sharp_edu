-- flpqdot label measurement.
--
-- Measures the TeX-rendered bounding box (width, height, depth, in pt) of an
-- edge label so the channel router can reserve space for it and avoid
-- label/edge/node collisions. This is the "first pass" that the stock layered
-- pipeline does for node boxes (internally, in TeX) but never does for edge
-- labels; the router needs it to route label-aware.
--
-- Each character is measured by creating a glyph node in the current font and
-- reading the width/height/depth that TeX computes for it (in scaled points).
-- The string width is the sum of the glyph advances; the height/depth is the
-- maximum across glyphs. This is exact for the single-character labels the
-- renderers emit (e.g. $S$, $a$, $b$) and a safe approximation for longer
-- labels: inter-glyph kerning is not applied, which for the fonts used here is
-- a small (usually negative) adjustment, so the width is a slight over-estimate
-- -- safe for reserving space. Math content ($...$) is measured by its inner
-- text content.
--
-- License: MIT (see the repository LICENSE).

local LabelMeasure = {}

-- LuaTeX node dimensions are in scaled points (1/65536 pt).
local SP_PER_PT = 65536

---
-- Measure a plain-text string in the current font. Returns (width, height,
-- depth) in pt. A byte with no glyph contributes the font's .notdef metrics
-- (no error).
--
local function measureText (str)
  local f = font.current()
  local totalW = 0
  local maxH = 0
  local maxD = 0

  for i = 1, #str do
    local g = node.new("glyph")
    g.font = f
    g.char = str:byte(i)
    totalW = totalW + g.width
    if g.height > maxH then maxH = g.height end
    if g.depth > maxD then maxD = g.depth end
  end

  return totalW / SP_PER_PT, maxH / SP_PER_PT, maxD / SP_PER_PT
end

---
-- Measure a label. `content` is the label text; `isMath` indicates it is a
-- math-mode label delimited by a single pair of `$`. Returns a table
-- `{ w, h, d }` in pt.
--
function LabelMeasure.measure (content, isMath)
  local s = content
  if isMath then
    -- Strip a single leading and trailing $, then measure the inner content.
    s = s:gsub("^%$", ""):gsub("%$$", "")
  end
  local w, h, d = measureText(s)
  return { w = w, h = h, d = d }
end

-- Default single-character label size (pt), used when no measured size is
-- available for an edge (the renderer did not emit a measurement command).
local DEFAULT_SIZE = { w = 6.0, h = 7.0, d = 0.2 }

---
-- Read the measured size for an edge key from the channel
-- (_G.flpqdot_edge_label_sizes, populated by \flpqdotmeasureedgelabel),
-- falling back to the default single-character size when absent. Returns a
-- table `{ w, h, d }` in pt.
--
function LabelMeasure.channelSize (key)
  local t = _G.flpqdot_edge_label_sizes
  if t and t[key] then
    return t[key]
  end
  return DEFAULT_SIZE
end

---
-- Reverse an edge key ("tail->head" -> "head->tail"). Returns nil if the key
-- is not in the expected form.
--
function LabelMeasure.reverseKey (key)
  local a, b = key:match("^(.*)%->(.*)$")
  if a and b then
    return b .. "->" .. a
  end
  return nil
end

---
-- Read the measured size for an edge, tolerating a direction reversal: the
-- layered layout reverses back-edges during cycle removal, so the model's
-- tail->head can differ from the source edge's direction used by the
-- measurement pre-pass. Tries the key, then its reverse, then the default.
--
function LabelMeasure.channelSizeEither (key)
  local t = _G.flpqdot_edge_label_sizes
  if t then
    if t[key] then
      return t[key]
    end
    local rev = LabelMeasure.reverseKey(key)
    if rev and t[rev] then
      return t[rev]
    end
  end
  return DEFAULT_SIZE
end

-- done
return LabelMeasure
