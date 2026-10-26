module FlpqdotLibraryTests

open System.IO
open System.Text.RegularExpressions
open Xunit
open FLPQ.Printers

/// Tests for the project-owned flpqdot graph-drawing library (the `dot layout`
/// key). At the S1 stage `dot layout` is an alias of the stock `layered
/// layout`; these tests pin the loading/wiring and the coordinate-extraction
/// harness that later subtasks (spline router, parity suite) build on.

let private tikzTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")

/// Probe template that installs a hook around the spline router's `run` in the
/// preamble (before `\usegdlibrary{flpqdot}`) and records layout metrics in
/// globals: `_G.flpqdot_curveto_total` (total cubic Bezier segments),
/// `_G.flpqdot_reciprocal_pairs`, and `_G.flpqdot_reciprocal_opposite`.
let private probeTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_flpqdot_probe_template.tex")

/// Compile a probe document (the spline-router hook lives in the probe
/// template's preamble) and return the lualatex log.
let private compileProbeLog (content: string) : string =
    let ok, log =
        ExternalTools.compileTexStringWithTemplateLog probeTemplatePath content

    if not ok then
        failwithf "probe lualatex failed:\n%s" log

    log

/// Return the total number of cubic Bezier segments emitted across all edge
/// paths after the layout.
let private countCurvetoSegments (content: string) : int =
    let log = compileProbeLog content

    log.Split('\n')
    |> Array.tryFind (fun l -> l.StartsWith("CURVETO_TOTAL="))
    |> function
        | Some l -> int (l.Substring("CURVETO_TOTAL=".Length))
        | None -> failwithf "CURVETO_TOTAL not found in log:\n%s" log

/// Return the reciprocal-pair stats: the number of reciprocal (u->v / v->u)
/// pairs, and how many of them bow to opposite sides of their chord.
let private reciprocalStats (content: string) : int * int =
    let log = compileProbeLog content

    let find (prefix: string) =
        log.Split('\n')
        |> Array.tryFind (fun l -> l.StartsWith(prefix))
        |> function
            | Some l -> int (l.Substring(prefix.Length))
            | None -> failwithf "%s not found in log:\n%s" prefix log

    (find "RECIPROCAL_PAIRS=", find "RECIPROCAL_OPPOSITE=")

/// Wrap a graph body in a `tikzpicture` with the given layout key and the
/// standard circle-node options. The graph drawing library names each TikZ node
/// by its label, so the labels double as the coordinate-extraction names.
let private tikzGraphWithLayout (layoutOpt: string) (body: string) : string =
    sprintf
        """\begin{tikzpicture}
  \graph [%s, nodes={draw, circle}, grow'=right, level sep=2cm, sibling sep=1.5cm] {
    %s
  };
\end{tikzpicture}"""
        layoutOpt
        body

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
let ``spline router emits Beziers and smooths multi-layer edges`` () =
    let total = countCurvetoSegments smoothProbeContent
    // Beziers are present at all (a pure polyline would yield 0).
    Assert.True(total > 0, sprintf "expected Beziers, got curveto_total=%d" total)
    // The multi-layer edge a->e is smoothed into several segments, so the
    // total must exceed the raw edge count (5).
    Assert.True(total > 5, sprintf "expected multi-layer smoothing, got curveto_total=%d" total)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``spline router routes a simple two-node edge`` () =
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
let ``spline router bows a reciprocal pair to opposite sides`` () =
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

    let pairs, opposite = reciprocalStats content
    // The graph has exactly one reciprocal pair (a->b and b->a).
    Assert.True(pairs >= 1, sprintf "expected a reciprocal pair, got %d" pairs)
    // Every reciprocal pair must bow to opposite sides (no overlap).
    Assert.Equal(pairs, opposite)

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
/// of cubic Bezier segments the spline router emitted for it.
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
