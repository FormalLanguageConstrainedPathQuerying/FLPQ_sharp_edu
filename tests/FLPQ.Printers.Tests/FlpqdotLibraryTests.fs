module FlpqdotLibraryTests

open System.IO
open System.Text.RegularExpressions
open Xunit
open FLPQ.Printers
open FlpqdotProbeHelpers

/// Tests for the project-owned flpqdot graph-drawing library (the `dot layout`
/// key): loading/wiring and the coordinate-extraction harness, the channel
/// router (Bezier emission, reciprocal-channel separation, no-overshoot), the
/// label-aware collision sweep, exact TeX label measurement, collision-free
/// label placement, and the dot-parity suite against the Graphviz `dot` oracle.

let private tikzTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")

/// Return the total number of cubic Bezier segments emitted across all edge
/// paths after the layout.
let private countCurvetoSegments (content: string) : int =
    probeInt "CURVETO_TOTAL=" (compileProbeLog content)

/// Return the reciprocal-pair stats from a probe log: the number of reciprocal
/// (u->v / v->u) pairs, and how many of them occupy opposite channels.
let private reciprocalStats (log: string) : int * int =
    (probeInt "RECIPROCAL_PAIRS=" log, probeInt "RECIPROCAL_OPPOSITE=" log)

/// Return the minimum interior distance (pt) between any reciprocal pair's two
/// edges, as emitted by the probe's RECIPROCAL_MIN_DIST line (endpoints are
/// excluded, since a pair shares its endpoints).
let private reciprocalMinDist (log: string) : float = probeFloat "RECIPROCAL_MIN_DIST=" log

/// A measured label size in pt (width, height, depth).
type private LabelSize = { W: float; H: float; D: float }

/// The sampled path y-extent (PyMin/PyMax) and the on-path points' y-range
/// (OyMin/OyMax) for one edge; no-overshoot means the former stays within the
/// latter.
type private EdgeBox =
    { PyMin: float
      PyMax: float
      OyMin: float
      OyMax: float }

/// The router's stored label anchor (X, Y, the channel point after the S3
/// collision sweep) and reserved box (W = label width, Th = label height +
/// depth + path stroke).
type private LabelBox =
    { X: float
      Y: float
      W: float
      Th: float }

/// Parse the `EDGEBOX <key> pymin=.. pymax=.. oymin=.. oymax=..` lines (emitted
/// by the probe) from a lualatex log into a map of edge-key -> EdgeBox.
let private parseEdgeBoxes (log: string) : Map<string, EdgeBox> =
    log.Split('\n')
    |> Array.filter (fun l -> l.StartsWith("EDGEBOX "))
    |> Array.map (fun l ->
        let m =
            Regex.Match(l, @"^EDGEBOX (\S+) pymin=([-\d.]+) pymax=([-\d.]+) oymin=([-\d.]+) oymax=([-\d.]+)")

        if not m.Success then
            failwithf "unparseable edgebox line: %s" l

        (m.Groups.[1].Value,
         { PyMin = float m.Groups.[2].Value
           PyMax = float m.Groups.[3].Value
           OyMin = float m.Groups.[4].Value
           OyMax = float m.Groups.[5].Value }))
    |> Map.ofArray

/// Parse the `LABEL <tag> w=.. h=.. d=..` lines (emitted by the LabelMeasure
/// probe) from a lualatex log into a map of tag -> LabelSize (pt).
let private parseLabelMeasurements (log: string) : Map<string, LabelSize> =
    log.Split('\n')
    |> Array.filter (fun l -> l.StartsWith("LABEL "))
    |> Array.map (fun l ->
        let m = Regex.Match(l, @"^LABEL (\S+) w=([-\d.]+) h=([-\d.]+) d=([-\d.]+)")

        if not m.Success then
            failwithf "unparseable label line: %s" l

        (m.Groups.[1].Value,
         { W = float m.Groups.[2].Value
           H = float m.Groups.[3].Value
           D = float m.Groups.[4].Value }))
    |> Map.ofArray

/// Parse the `MEASURED <key> w=.. h=.. d=..` lines (emitted by the S1b probe,
/// which reads the _G.flpqdot_edge_label_sizes channel populated by
/// \flpqdotmeasureedgelabel) from a lualatex log into a map of edge-key ->
/// LabelSize (pt).
let private parseMeasuredSizes (log: string) : Map<string, LabelSize> =
    log.Split('\n')
    |> Array.filter (fun l -> l.StartsWith("MEASURED "))
    |> Array.map (fun l ->
        let m = Regex.Match(l, @"^MEASURED (\S+) w=([-\d.]+) h=([-\d.]+) d=([-\d.]+)")

        if not m.Success then
            failwithf "unparseable measured line: %s" l

        (m.Groups.[1].Value,
         { W = float m.Groups.[2].Value
           H = float m.Groups.[3].Value
           D = float m.Groups.[4].Value }))
    |> Map.ofArray

/// Parse the `LABELBOX <key> x=.. y=.. w=.. th=..` lines (emitted by the probe)
/// from a lualatex log into a map of edge-key -> LabelBox.
let private parseLabelBoxes (log: string) : Map<string, LabelBox> =
    log.Split('\n')
    |> Array.filter (fun l -> l.StartsWith("LABELBOX "))
    |> Array.map (fun l ->
        let m =
            Regex.Match(l, @"^LABELBOX (\S+) x=([-\d.]+) y=([-\d.]+) w=([-\d.]+) th=([-\d.]+)")

        if not m.Success then
            failwithf "unparseable labelbox line: %s" l

        (m.Groups.[1].Value,
         { X = float m.Groups.[2].Value
           Y = float m.Groups.[3].Value
           W = float m.Groups.[4].Value
           Th = float m.Groups.[5].Value }))
    |> Map.ofArray

/// Return the number of overlapping reserved label-box pairs from a probe log
/// (0 = collision-free after the S3 collision sweep).
let private labelOverlapCount (log: string) : int = probeInt "LABEL_OVERLAP_COUNT=" log

/// The number of edges the router assigned a label position to (S4).
let private labelPlacedCount (log: string) : int = probeInt "LABEL_PLACED_COUNT=" log

/// The number of placed label boxes that intersect a node bounding box (S4;
/// 0 = every label clears all nodes).
let private labelNodeCollisions (log: string) : int = probeInt "LABEL_NODE_COLLISIONS=" log

/// The number of placed label boxes that intersect another edge's sampled path
/// (S4; 0 = no label crosses another edge).
let private labelEdgeCollisions (log: string) : int = probeInt "LABEL_EDGE_COLLISIONS=" log

/// A diamond graph (a at layer 0; b,c at layer 1; d at layer 2). `layoutOpt`
/// selects the layout key under test (`dot layout` or `layered layout`).
let private graphSnippet (layoutOpt: string) : string =
    tikzGraphWithLayout layoutOpt "a;\n    b;\n    c;\n    d;\n    a -> b;\n    a -> c;\n    b -> d;\n    c -> d;"

let private nodeNames = [ "a"; "b"; "c"; "d" ]

/// Inject `\typeout{COORD <name> x=.. y=..}` dumps for each named node before
/// `\end{tikzpicture}`, compile, and parse the coordinates out of the lualatex
/// log. Generalizes the same-layer coordinate harness in DerivationTreeTikzTests.
let private extractNodePositions (tikz: string) (names: string list) : Map<string, float * float> =
    let dumps =
        names
        |> List.map (fun n ->
            sprintf
                @"\makeatletter\pgfpointanchor{%s}{center}\typeout{COORD %s x=\the\pgf@x y=\the\pgf@y}\makeatother"
                n
                n)
        |> String.concat "\n"

    let content =
        tikz.Replace(@"\end{tikzpicture}", dumps + "\n" + @"\end{tikzpicture}")

    let ok, log = ExternalTools.compileTexStringWithTemplateLog tikzTemplatePath content

    if not ok then
        failwithf "coordinate-extraction lualatex failed:\n%s" log

    log.Split('\n')
    |> Array.filter (fun l -> l.StartsWith("COORD "))
    |> Array.map (fun l ->
        let m = Regex.Match(l, @"^COORD (\S+) x=([-\d.]+)pt\s*y=([-\d.]+)pt")

        if not m.Success then
            failwithf "unparseable coordinate line: %s" l

        (m.Groups.[1].Value, (float m.Groups.[2].Value, float m.Groups.[3].Value)))
    |> Map.ofArray

[<Fact>]
[<Trait("Category", "TeX")>]
let ``dot layout compiles with lualatex`` () =
    let tikz = graphSnippet "dot layout"
    Assert.Contains("dot layout", tikz)

    Assert.True(
        ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz,
        "dot layout did not compile (library not loaded or key unknown)"
    )

[<Fact>]
[<Trait("Category", "TeX")>]
let ``dot layout node positions match layered layout`` () =
    let dotCoords = extractNodePositions (graphSnippet "dot layout") nodeNames
    let layeredCoords = extractNodePositions (graphSnippet "layered layout") nodeNames

    Assert.Equal(nodeNames.Length, Map.count dotCoords)
    Assert.Equal(nodeNames.Length, Map.count layeredCoords)

    for n in nodeNames do
        let dx, dy = dotCoords.[n]
        let lx, ly = layeredCoords.[n]
        let close = abs (dx - lx) < 1e-3 && abs (dy - ly) < 1e-3
        Assert.True(close, sprintf "node %s differs: dot=(%f,%f) layered=(%f,%f)" n dx dy lx ly)

/// Probe content for the smoothness test: a 5-node chain plus a long edge
/// `a -> e` that spans four layers (three dummy nodes). The long edge is
/// smoothed into one Bezier segment per layer, so the total curveto count must
/// exceed the number of edges (5).
///
/// NOTE: the `\directlua` body builds the `\typeout` line by string
/// concatenation, not `string.format("...%d...")`. A literal `%` inside a
/// `\directlua{...}` argument is a TeX comment character (it comments out the
/// rest of the line and leaves the group unclosed), so `%` must be avoided.
let private smoothProbeContent =
    """\begin{tikzpicture}
  \graph [dot layout, nodes={draw, circle}, grow'=right, level sep=2cm, sibling sep=1.5cm] {
    a; b; c; d; e;
    a -> b; b -> c; c -> d; d -> e; a -> e;
  };
\end{tikzpicture}
\directlua{ tex.sprint("\\typeout{CURVETO_TOTAL=" .. tostring(_G.flpqdot_curveto_total) .. "}") }"""

[<Fact>]
[<Trait("Category", "TeX")>]
let ``channel router emits Beziers and smooths multi-layer edges`` () =
    let total = countCurvetoSegments smoothProbeContent
    // Beziers are present at all (a pure polyline would yield 0).
    Assert.True(total > 0, sprintf "expected Beziers, got curveto_total=%d" total)
    // The multi-layer edge a->e is smoothed into several segments, so the
    // total must exceed the raw edge count (5).
    Assert.True(total > 5, sprintf "expected multi-layer smoothing, got curveto_total=%d" total)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``channel router routes a simple two-node edge`` () =
    let content =
        """\begin{tikzpicture}
  \graph [dot layout, nodes={draw, circle}, grow'=right, level sep=2cm, sibling sep=1.5cm] {
    a; b;
    a -> b;
  };
\end{tikzpicture}
\directlua{ tex.sprint("\\typeout{CURVETO_TOTAL=" .. tostring(_G.flpqdot_curveto_total) .. "}") }"""

    let total = countCurvetoSegments content
    // A single adjacent edge is routed as one (degenerate) Bezier segment.
    Assert.True(total >= 1, sprintf "expected a routed edge, got curveto_total=%d" total)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``reciprocal pairs occupy distinct channels and do not overlap`` () =
    let content =
        """\begin{tikzpicture}
   \graph [dot layout, nodes={draw, circle}, grow'=right, level sep=2cm, sibling sep=1.5cm] {
     a; b;
     a -> b; b -> a;
   };
\end{tikzpicture}
\directlua{
  tex.sprint("\\typeout{RECIPROCAL_PAIRS=" .. tostring(_G.flpqdot_reciprocal_pairs) .. "}")
  tex.sprint("\\typeout{RECIPROCAL_OPPOSITE=" .. tostring(_G.flpqdot_reciprocal_opposite) .. "}")
}"""

    let log = compileProbeLog content
    let pairs, opposite = reciprocalStats log
    let minDist = reciprocalMinDist log
    // The graph has exactly one reciprocal pair (a->b and b->a).
    Assert.True(pairs >= 1, sprintf "expected a reciprocal pair, got %d" pairs)
    // Every reciprocal pair must occupy opposite channels (one edge each side).
    Assert.Equal(pairs, opposite)
    // The two edges' interior points are well separated (they do not coincide).
    Assert.True(minDist > 2.0, sprintf "reciprocal edges overlap: min_dist=%f" minDist)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``multi-layer edge does not overshoot its bounding slab`` () =
    // A 5-node chain plus a long edge a->e spanning four layers (three dummy
    // nodes). The monotone fit must keep the sampled path within the on-path
    // points' y-range (plus a small margin) -- no Catmull-Rom loops.
    let content =
        """\begin{tikzpicture}
   \graph [dot layout, nodes={draw, circle}, grow'=right, level sep=2cm, sibling sep=1.5cm] {
     a; b; c; d; e;
     a -> b; b -> c; c -> d; d -> e; a -> e;
   };
\end{tikzpicture}"""

    let log = compileProbeLog content
    let boxes = parseEdgeBoxes log
    Assert.True(Map.containsKey "a->e" boxes, "edge a->e not found in EDGEBOX lines")

    let box = boxes.["a->e"]
    let margin = 1.0
    Assert.True(box.PyMax <= box.OyMax + margin, sprintf "a->e overshoots above: pymax=%f oymax=%f" box.PyMax box.OyMax)
    Assert.True(box.PyMin >= box.OyMin - margin, sprintf "a->e overshoots below: pymin=%f oymin=%f" box.PyMin box.OyMin)

/// A K2,2 graph: a,b in one layer and c,d in the next, with all four edges.
/// The two diagonal edges (a->d and b->c) cross, so their labels (placed at the
/// path midpoints) coincide in the same channel and their reserved boxes
/// overlap. The S3 collision sweep must push one label apart so no reserved
/// boxes overlap. (Without the sweep the two diagonal labels coincide and the
/// overlap count is 1.)
let private crossingGraphContent =
    """\begin{tikzpicture}
  \graph [dot layout, nodes={draw, circle}, grow'=right, level sep=2cm, sibling sep=1.5cm] {
    a; b; c; d;
    a ->["a"] c;
    a ->["S"] d;
    b ->["b"] c;
    b ->["a"] d;
  };
\end{tikzpicture}"""

[<Fact>]
[<Trait("Category", "TeX")>]
let ``collision sweep separates overlapping labels in the same channel`` () =
    let log = compileProbeLog crossingGraphContent
    let boxes = parseLabelBoxes log

    // All four edges are routed and carry a reserved label box.
    Assert.Equal(4, Map.count boxes)

    // The crossing pair (a->d and b->c) share a channel (their anchors are
    // close in x, the rank axis being vertical here).
    let ad = boxes.["a->d"]
    let bc = boxes.["b->c"]

    Assert.True(
        abs (ad.X - bc.X) < 10.0,
        sprintf "crossing labels not in the same channel: a->d x=%f b->c x=%f" ad.X bc.X
    )

    // The sweep separated the two labels in y (they no longer coincide).
    Assert.True(abs (ad.Y - bc.Y) > 5.0, sprintf "crossing labels not separated in y: a->d y=%f b->c y=%f" ad.Y bc.Y)

    // No reserved label boxes overlap after the sweep.
    Assert.Equal(0, labelOverlapCount log)

/// Probe content for the S4 label-placement test: the Kronecker-CFPQ step_1
/// graph (the task's repro), with a `\flpqdotmeasureedgelabel` pre-pass for
/// every edge and no default quotes (the wrapper places the labels). It
/// exercises both a back-edge (`v2 -> v0`, reversed by the layered layout's
/// cycle removal) and a reciprocal pair (`v2 <-> v3`).
let private labelPlacementProbeContent =
    """\flpqdotmeasureedgelabel{v0->v1}{$a$}
\flpqdotmeasureedgelabel{v1->v2}{$a$}
\flpqdotmeasureedgelabel{v1->v3}{$S$}
\flpqdotmeasureedgelabel{v2->v0}{$a$}
\flpqdotmeasureedgelabel{v2->v3}{$b$}
\flpqdotmeasureedgelabel{v3->v2}{$b$}
\begin{tikzpicture}
  \graph [dot layout, nodes={draw, circle}, grow'=right, level sep=2cm, sibling sep=1.5cm] {
    v0; v1; v2; v3;
    v0 -> v1;
    v1 -> v2;
    v1 -> v3;
    v2 -> v0;
    v2 -> v3;
    v3 -> v2;
  };
\end{tikzpicture}"""

[<Fact>]
[<Trait("Category", "TeX")>]
let ``label placement is collision-free (no node or edge overlap)`` () =
    let log = compileProbeLog labelPlacementProbeContent

    // Every edge is assigned a label position (the graph has 6 edges).
    Assert.Equal(6, labelPlacedCount log)

    // No placed label box intersects a node bounding box.
    Assert.Equal(0, labelNodeCollisions log)

    // No placed label box intersects another edge's sampled path.
    Assert.Equal(0, labelEdgeCollisions log)

/// Probe content that measures a set of known label snippets via the flpqdot
/// LabelMeasure module and dumps each as `LABEL <tag> w=.. h=.. d=..` (pt). The
/// leading `x` keeps the standalone document non-empty; the `\directlua` runs in
/// the body where the default font is current.
let private labelMeasureProbeContent =
    """x
\directlua{
  local LM = require "pgf.gd.flpqdot.LabelMeasure"
  local function dump(tag, content, isMath)
    local r = LM.measure(content, isMath)
    tex.sprint("\\typeout{LABEL " .. tag .. " w=" .. tostring(r.w) .. " h=" .. tostring(r.h) .. " d=" .. tostring(r.d) .. "}")
  end
  dump("a", "a", false)
  dump("b", "b", false)
  dump("S", "S", false)
  dump("ab", "ab", false)
  dump("Smath", "$S$", true)
}"""

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LabelMeasure measures text and math labels`` () =
    let m = parseLabelMeasurements (compileProbeLog labelMeasureProbeContent)

    for tag in [ "a"; "b"; "S"; "ab"; "Smath" ] do
        Assert.True(Map.containsKey tag m, sprintf "missing label measurement for %s" tag)

    let ma = m.["a"]
    let mb = m.["b"]
    let mS = m.["S"]
    let mAb = m.["ab"]
    let mSm = m.["Smath"]

    // A lowercase letter has a positive, sane size at the default ~10pt font.
    Assert.True(ma.W > 3.0 && ma.W < 8.0, sprintf "a width out of range: %f" ma.W)
    Assert.True(ma.H > 3.0 && ma.H < 8.0, sprintf "a height out of range: %f" ma.H)
    Assert.True(ma.D >= 0.0, sprintf "negative depth for a: %f" ma.D)
    // Two characters are wider than one.
    Assert.True(mAb.W > ma.W, sprintf "ab (%f) not wider than a (%f)" mAb.W ma.W)
    // The measurement is additive (no kerning applied): ab ~= a + b.
    Assert.True(abs (mAb.W - (ma.W + mb.W)) < 0.05, sprintf "ab (%f) != a+b (%f)" mAb.W (ma.W + mb.W))
    // A capital and an ascender are taller than a lowercase x-height.
    Assert.True(mS.H > ma.H, sprintf "S height (%f) not > a height (%f)" mS.H ma.H)
    Assert.True(mb.H > ma.H, sprintf "b height (%f) not > a height (%f)" mb.H ma.H)
    // A math label $S$ measures the same as its inner text S.
    Assert.True(
        abs (mSm.W - mS.W) < 1e-6 && abs (mSm.H - mS.H) < 1e-6,
        sprintf "$S$ (%f,%f) != S (%f,%f)" mSm.W mSm.H mS.W mS.H
    )

/// Probe content for the TeX-side measurement command (S1b). It emits
/// \flpqdotmeasureedgelabel for three edge keys (a single math symbol, a
/// multi-symbol math expression, and another single symbol), then reads the
/// _G.flpqdot_edge_label_sizes channel back out as `MEASURED <key> w=.. h=..
/// d=..` lines and dumps the `channelSize` fallback for an absent key.
let private measureCommandProbeContent =
    """\flpqdotmeasureedgelabel{v0->v2}{$S$}
\flpqdotmeasureedgelabel{v0->v1}{$a+b$}
\flpqdotmeasureedgelabel{v1->v0}{$b$}
\directlua{
  local LM = require "pgf.gd.flpqdot.LabelMeasure"
  local t = _G.flpqdot_edge_label_sizes
  for k, v in pairs(t) do
    tex.sprint("\\typeout{MEASURED " .. k .. " w=" .. tostring(v.w) .. " h=" .. tostring(v.h) .. " d=" .. tostring(v.d) .. "}")
  end
  local fb = LM.channelSize("nope->nope")
  tex.sprint("\\typeout{FALLBACK w=" .. tostring(fb.w) .. " h=" .. tostring(fb.h) .. " d=" .. tostring(fb.d) .. "}")
}"""

[<Fact>]
[<Trait("Category", "TeX")>]
let ``flpqdotmeasureedgelabel measures complex TeX labels into the channel`` () =
    let log = compileProbeLog measureCommandProbeContent
    let m = parseMeasuredSizes log

    // All three measured keys are present in the channel.
    for key in [ "v0->v2"; "v0->v1"; "v1->v0" ] do
        Assert.True(Map.containsKey key m, sprintf "missing measured size for %s" key)

    let mS = m.["v0->v2"] // $S$
    let mAb = m.["v0->v1"] // $a+b$
    let mB = m.["v1->v0"] // $b$

    // Single math symbols have a sane size at the default ~10pt font.
    Assert.True(mS.W > 4.0 && mS.W < 10.0, sprintf "$S$ width out of range: %f" mS.W)
    Assert.True(mB.W > 4.0 && mB.W < 10.0, sprintf "$b$ width out of range: %f" mB.W)
    // A multi-symbol expression is clearly wider than a single symbol.
    Assert.True(mAb.W > 2.0 * mS.W, sprintf "$a+b$ (%f) not clearly wider than $S$ (%f)" mAb.W mS.W)

    // The channel holds exactly the three measured keys (no leakage).
    Assert.Equal(3, Map.count m)

    // channelSize falls back to the fixed default for an absent key.
    let fbLine = log.Split('\n') |> Array.tryFind (fun l -> l.StartsWith("FALLBACK "))

    Assert.True(Option.isSome fbLine, "FALLBACK line not found in log")

    let fbm =
        Regex.Match(fbLine.Value, @"^FALLBACK w=([-\d.]+) h=([-\d.]+) d=([-\d.]+)")

    Assert.True(fbm.Success, sprintf "unparseable fallback line: %s" fbLine.Value)
    Assert.Equal(6.0, float fbm.Groups.[1].Value)
    Assert.Equal(7.0, float fbm.Groups.[2].Value)
    Assert.Equal(0.2, float fbm.Groups.[3].Value)

// ---------------------------------------------------------------------------
// S4: dot-parity suite
// ---------------------------------------------------------------------------
//
// The parity suite checks that `dot layout` is "dot-like" relative to the
// Graphviz `dot` oracle: the same partition of nodes into ranks (layers) and
// the same within-rank ordering, up to a global reflection along each axis.
// The comparison is structural (partition + ordering), never absolute
// coordinates, so it is robust to font-metric drift and to the two engines'
// different spacing.

/// Group a list into consecutive runs where `pred` holds between neighbours.
let private groupByConsecutive (pred: 'a * 'a -> bool) (xs: 'a list) : 'a list list =
    match xs with
    | [] -> []
    | x0 :: rest ->
        let rec go (acc: 'a list) (xs: 'a list) (groups: 'a list list) =
            match xs with
            | [] -> List.rev acc :: groups
            | x :: xs' ->
                if pred (List.head acc, x) then
                    go (x :: acc) xs' groups
                else
                    go [ x ] xs' (List.rev acc :: groups)

        go [ x0 ] rest [] |> List.rev

/// Cluster nodes into ranks by their x-coordinate (the rank axis for both
/// `dot` with `rankdir=LR` and TikZ `grow'=right`). Same-rank nodes share an
/// x-coordinate, so a small `tol` groups them. Returns ranks sorted by
/// ascending x; each rank is the node names sorted by ascending y.
let private ranksByX (positions: Map<string, float * float>) (tol: float) : (string list) list =
    positions
    |> Map.toList
    |> List.sortBy (fun (_, (x, _)) -> x)
    |> groupByConsecutive (fun ((_, (x, _)), (_, (x2, _))) -> abs (x - x2) <= tol)
    |> List.map (fun rank -> rank |> List.map fst |> List.sortBy (fun n -> snd (positions.[n])))

/// A representative graph for the parity suite: its TikZ source (using `dot
/// layout`), the equivalent Graphviz `dot` source (`rankdir=LR` to match the
/// TikZ grow direction), the node names (labels double as names), and the
/// directed edges (used to split the graph into connected component).
type private GraphSpec =
    { Name: string
      Tikz: string
      Dot: string
      Nodes: string list
      Edges: (string * string) list }

let private tikzGraph (body: string) : string = tikzGraphWithLayout "dot layout" body

let private graphSpecs: GraphSpec list =
    [ { Name = "chain"
        Tikz = tikzGraph "a; b; c; d;\n    a -> b; b -> c; c -> d;"
        Dot = "digraph { rankdir=LR; a -> b; b -> c; c -> d; }"
        Nodes = [ "a"; "b"; "c"; "d" ]
        Edges = [ ("a", "b"); ("b", "c"); ("c", "d") ] }
      { Name = "star"
        Tikz = tikzGraph "c; l1; l2; l3;\n    c -> l1; c -> l2; c -> l3;"
        Dot = "digraph { rankdir=LR; c -> l1; c -> l2; c -> l3; }"
        Nodes = [ "c"; "l1"; "l2"; "l3" ]
        Edges = [ ("c", "l1"); ("c", "l2"); ("c", "l3") ] }
      { Name = "diamond"
        Tikz = tikzGraph "a; b; c; d;\n    a -> b; a -> c; b -> d; c -> d;"
        Dot = "digraph { rankdir=LR; a -> b; a -> c; b -> d; c -> d; }"
        Nodes = [ "a"; "b"; "c"; "d" ]
        Edges = [ ("a", "b"); ("a", "c"); ("b", "d"); ("c", "d") ] }
      { Name = "reciprocal"
        Tikz = tikzGraph "a; b;\n    a -> b; b -> a;"
        Dot = "digraph { rankdir=LR; a -> b; b -> a; }"
        Nodes = [ "a"; "b" ]
        Edges = [ ("a", "b"); ("b", "a") ] }
      { Name = "multi-component"
        Tikz = tikzGraph "a; b; c; d;\n    a -> b; c -> d;"
        Dot = "digraph { rankdir=LR; a -> b; c -> d; }"
        Nodes = [ "a"; "b"; "c"; "d" ]
        Edges = [ ("a", "b"); ("c", "d") ] } ]

/// Undirected connected components of a graph, as a list of node-name lists.
let private connectedComponents (nodes: string list) (edges: (string * string) list) : (string list) list =
    let adj =
        edges
        |> List.collect (fun (u, v) -> [ (u, v); (v, u) ])
        |> List.groupBy fst
        |> List.map (fun (u, pairs) -> (u, pairs |> List.map snd))
        |> Map.ofList

    let visited = ref Set.empty<string>

    let rec visit (start: string) : string list =
        let rec dfs (stack: string list) (acc: string list) : string list =
            match stack with
            | [] -> acc
            | cur :: rest ->
                let neighbors =
                    (Map.tryFind cur adj |> Option.defaultValue [])
                    |> List.filter (fun nb -> not (Set.contains nb visited.Value))

                for nb in neighbors do
                    visited.Value <- visited.Value.Add nb

                dfs (neighbors @ rest) (cur :: acc)

        visited.Value <- visited.Value.Add start
        dfs [ start ] []

    let components = ResizeArray<string list>()

    for n in nodes do
        if not (Set.contains n visited.Value) then
            components.Add(visit n)

    List.ofSeq components

/// Compare the rank (layer) structure of the `dot` oracle and the TikZ `dot
/// layout` for one graph. Returns a list of human-readable mismatches (empty =
/// parity holds). Structural only: same partition of nodes into ranks (up to a
/// global reversal of the rank sequence) and, within each rank, the same node
/// order (up to a single global y-reflection).
let private rankParityMismatches
    (dotPos: Map<string, float * float>)
    (tikzPos: Map<string, float * float>)
    (tol: float)
    : string list =
    let dotRanks = ranksByX dotPos tol
    let tikzRanks = ranksByX tikzPos tol
    let mismatches = ResizeArray<string>()

    if dotRanks.Length <> tikzRanks.Length then
        mismatches.Add(sprintf "rank count: dot=%d tikz=%d" dotRanks.Length tikzRanks.Length)
    else
        let setEq (a: string list) (b: string list) = Set.ofList a = Set.ofList b

        // Align the tikz ranks to the dot ranks: same direction, or reversed
        // along the rank axis (the two engines may lay out in opposite
        // directions).
        let sameDir =
            dotRanks |> List.indexed |> List.forall (fun (i, dr) -> setEq dr tikzRanks.[i])

        let alignedTikz = if sameDir then tikzRanks else tikzRanks |> List.rev

        let badSets =
            dotRanks
            |> List.indexed
            |> List.filter (fun (i, dr) -> not (setEq dr alignedTikz.[i]))

        for i, dr in badSets do
            mismatches.Add(
                sprintf
                    "rank %d node-set: dot=[%s] tikz=[%s]"
                    i
                    (String.concat "," dr)
                    (String.concat "," alignedTikz.[i])
            )

        // Within-rank ordering, up to a single global y-reflection. The
        // reflection is fixed by the first rank holding more than one node.
        if badSets.IsEmpty then
            let firstMulti =
                dotRanks
                |> List.indexed
                |> List.tryPick (fun (i, dr) -> if dr.Length >= 2 then Some(i, dr) else None)

            match firstMulti with
            | Some(i, _) ->
                let dotNames = dotRanks.[i]
                let tikzNames = alignedTikz.[i]
                let sameOrder = dotNames = tikzNames
                let revOrder = dotNames = (tikzNames |> List.rev)

                if not (sameOrder || revOrder) then
                    mismatches.Add(
                        sprintf
                            "rank %d order: dot=[%s] tikz=[%s]"
                            i
                            (String.concat "," dotNames)
                            (String.concat "," tikzNames)
                    )
                else
                    let yReflect = not sameOrder

                    for j, dr2 in dotRanks |> List.indexed do
                        if dr2.Length >= 2 && j <> i then
                            let dn = dr2
                            let tn = alignedTikz.[j]
                            let ok = if yReflect then dn = (tn |> List.rev) else dn = tn

                            if not ok then
                                mismatches.Add(
                                    sprintf
                                        "rank %d order (reflect=%b): dot=[%s] tikz=[%s]"
                                        j
                                        yReflect
                                        (String.concat "," dn)
                                        (String.concat "," tn)
                                )
            | None -> ()

    List.ofSeq mismatches

/// Append the curveto-total dump to a TikZ graph and return the total number
/// of cubic Bezier segments the channel router emitted for it.
let private graphCurvetoTotal (tikz: string) : int =
    let dump =
        """\directlua{ tex.sprint("\\typeout{CURVETO_TOTAL=" .. tostring(_G.flpqdot_curveto_total) .. "}") }"""

    countCurvetoSegments (tikz + "\n" + dump)

[<Fact>]
let ``rank parity holds for matching layering and ordering`` () =
    // Same ranks (a | b,c | d) and same within-rank order (c above b), with
    // different absolute spacing — parity must hold.
    let dotPos =
        Map.ofList
            [ ("a", (0.0, 0.0))
              ("b", (50.0, 10.0))
              ("c", (50.0, -10.0))
              ("d", (100.0, 0.0)) ]

    let tikzPos =
        Map.ofList
            [ ("a", (0.0, 0.0))
              ("b", (75.0, 15.0))
              ("c", (75.0, -15.0))
              ("d", (150.0, 0.0)) ]

    let mismatches = rankParityMismatches dotPos tikzPos 1.0
    Assert.True(mismatches.IsEmpty, String.concat "\n" mismatches)

[<Fact>]
let ``rank parity detects a layering mismatch`` () =
    // dot puts a,b in one rank and c in the next; tikz splits all three.
    let dotPos =
        Map.ofList [ ("a", (0.0, 0.0)); ("b", (0.0, 10.0)); ("c", (50.0, 0.0)) ]

    let tikzPos =
        Map.ofList [ ("a", (0.0, 0.0)); ("b", (50.0, 0.0)); ("c", (100.0, 0.0)) ]

    let mismatches = rankParityMismatches dotPos tikzPos 1.0
    Assert.False(mismatches.IsEmpty)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``dot layout is dot-like: layering and ordering match the dot oracle`` () =
    let tol = 1.0

    // The rank structure is compared per connected component: `dot` stacks
    // disconnected components in the same ranks, whereas the layered layout
    // places them side by side. Within a component the two must agree.
    let allMismatches =
        graphSpecs
        |> List.collect (fun spec ->
            let tikzPos = extractNodePositions spec.Tikz spec.Nodes
            let dotPos = ExternalTools.compileDotStringToNodePositions spec.Dot

            connectedComponents spec.Nodes spec.Edges
            |> List.collect (fun comp ->
                let compSet = Set.ofList comp
                let dotSub = dotPos |> Map.filter (fun n _ -> compSet.Contains n)
                let tikzSub = tikzPos |> Map.filter (fun n _ -> compSet.Contains n)

                rankParityMismatches dotSub tikzSub tol
                |> List.map (fun m -> sprintf "%s [%s]: %s" spec.Name (String.concat "," comp) m)))

    Assert.True(allMismatches.IsEmpty, String.concat "\n" allMismatches)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``dot layout emits Beziers for every representative graph`` () =
    let problems =
        graphSpecs
        |> List.filter (fun spec -> graphCurvetoTotal spec.Tikz <= 0)
        |> List.map (fun spec -> spec.Name)

    Assert.True(problems.IsEmpty, sprintf "graphs without Beziers: %s" (String.concat ", " problems))
