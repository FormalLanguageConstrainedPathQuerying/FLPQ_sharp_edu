namespace FLPQ.Printers

open System.Text

/// TikZ graphdrawing rendering of the RPQ input graph, used by `RpqGraphViz`. Unlike the
/// general GSS renderer, this module is dedicated to RPQ and does not depend on the GSS
/// stack. Vertex fill precedence (highest first): highlighted (target) > frontier (current)
/// > start. Highlighted (current) edges render red bold, path edges light red; an edge in
/// both sets renders as highlighted. Reciprocal pairs are separated into distinct channels
/// by the flpqdot router. Non-loop edge labels are registered in the
/// `\flpqdotmeasureedgelabel` pre-pass and placed by the router (the stock quote is
/// dropped); loop edges keep their stock quote.
module RpqGraphTikz =

    /// Render the RPQ input graph as a TikZ `tikzpicture` with dot layout. Labels are
    /// taken as already-rendered TeX (the caller escapes/formats them); the RPQ callers pass
    /// `$v_i$` vertices and escaped edge labels. `activeVertices`/`activeEdges` are the full
    /// graph; the highlight sets select fills and edge styles. Every non-loop edge with a
    /// non-empty label is registered in the measurement pre-pass (its stock quote is
    /// dropped, the router places the label); loop edges keep their stock quote.
    let toTikz
        (vertexLabelPrinter: int -> string)
        (edgeLabelPrinter: int * int -> string)
        (activeVertices: Set<int>)
        (activeEdges: Set<int * int>)
        (highlightedVertices: Set<int>)
        (startVertices: Set<int>)
        (frontierVertices: Set<int>)
        (highlightedEdges: Set<int * int>)
        (pathEdges: Set<int * int>)
        : string =
        let sb = StringBuilder()

        let allVertices =
            let fromEdges =
                activeEdges
                |> Set.fold (fun acc (from, to_) -> Set.add from (Set.add to_ acc)) Set.empty

            Set.union activeVertices fromEdges

        // Non-loop edges with a non-empty label: registered in the measurement
        // pre-pass, their stock quote dropped (the router's edge-callback wrapper
        // places the label at a collision-free position).
        let labeledEdges =
            activeEdges
            |> Set.toList
            |> List.filter (fun (from, to_) -> from <> to_ && edgeLabelPrinter (from, to_) <> "")
            |> List.map (fun (from, to_) ->
                (FlpqdotTikz.edgeKey (sprintf "v%d" from) (sprintf "v%d" to_), edgeLabelPrinter (from, to_)))

        AutomatonTikz.tikzHeaderWithOptions
            (FlpqdotTikz.prePass labeledEdges)
            (AutomatonTikz.graphOptions "circle" AutomatonTikz.defaultGrowDirection)
            sb

        for vidx in allVertices do
            let label = vertexLabelPrinter vidx

            let isStart = Set.contains vidx startVertices
            let isFrontier = Set.contains vidx frontierVertices
            let isHighlighted = Set.contains vidx highlightedVertices

            let fill =
                if isHighlighted then Some "yellow!20"
                elif isFrontier then Some "lightblue!20"
                elif isStart then Some "green!30"
                else None

            let opts =
                match fill with
                | Some c -> sprintf "as={%s}, fill=%s" label c
                | None -> sprintf "as={%s}" label

            sb.AppendLine(sprintf "    v%d [%s];" vidx opts) |> ignore

        for fromIdx, toIdx in activeEdges do
            let label = edgeLabelPrinter (fromIdx, toIdx)

            // Highlighted edges win over path edges (an edge in both sets renders
            // as highlighted).
            let style =
                if Set.contains (fromIdx, toIdx) highlightedEdges then
                    "red, thick"
                elif Set.contains (fromIdx, toIdx) pathEdges then
                    "red!40"
                else
                    ""

            FlpqdotTikz.emitEdge sb (sprintf "v%d" fromIdx) (sprintf "v%d" toIdx) label style

        AutomatonTikz.tikzFooter sb
        sb.ToString()
