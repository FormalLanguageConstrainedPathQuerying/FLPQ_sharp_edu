namespace FLPQ.Printers

open System.Text

/// TikZ graphdrawing rendering of the RPQ input graph, used by `RpqGraphViz`. Unlike the
/// general GSS renderer, this module is dedicated to RPQ and does not depend on the GSS
/// stack. Vertex fill precedence (highest first): highlighted (target) > frontier (current)
/// > start. Highlighted (current) edges render red bold, path edges light red; an edge in
/// both sets renders as highlighted. Reciprocal pairs are bent apart.
module RpqGraphTikz =

    /// Render the RPQ input graph as a TikZ `tikzpicture` with layered layout. Labels are
    /// taken as already-rendered TeX (the caller escapes/formats them); the RPQ callers pass
    /// `$v_i$` vertices and escaped edge labels. `activeVertices`/`activeEdges` are the full
    /// graph; the highlight sets select fills and edge styles.
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

        AutomatonTikz.tikzHeaderWithOptions
            (AutomatonTikz.layeredGraphOptions "circle" AutomatonTikz.defaultGrowDirection)
            sb

        let allVertices =
            let fromEdges =
                activeEdges
                |> Set.fold (fun acc (from, to_) -> Set.add from (Set.add to_ acc)) Set.empty

            Set.union activeVertices fromEdges

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

            let isHighlighted = Set.contains (fromIdx, toIdx) highlightedEdges
            let isPath = not isHighlighted && Set.contains (fromIdx, toIdx) pathEdges

            let loopAttr = if fromIdx = toIdx then ",loop above" else ""

            // A reciprocal pair (u->v and v->u) would otherwise draw as two overlapping
            // straight lines; bend both edges into symmetric arcs. Self-loops are never bent.
            let bendAttr =
                AutomatonTikz.reciprocalBendAttr true (Set.contains (toIdx, fromIdx) activeEdges) fromIdx toIdx

            if isHighlighted then
                sb.AppendLine(sprintf "    v%d ->[\"%s\", red, thick%s%s] v%d;" fromIdx label bendAttr loopAttr toIdx)
                |> ignore
            elif isPath then
                sb.AppendLine(sprintf "    v%d ->[\"%s\", red!40%s%s] v%d;" fromIdx label bendAttr loopAttr toIdx)
                |> ignore
            elif label = "" then
                if fromIdx = toIdx then
                    sb.AppendLine(sprintf "    v%d ->[loop above] v%d;" fromIdx fromIdx) |> ignore
                elif bendAttr <> "" then
                    sb.AppendLine(sprintf "    v%d ->[%s] v%d;" fromIdx (bendAttr.TrimStart(',', ' ')) toIdx)
                    |> ignore
                else
                    sb.AppendLine(sprintf "    v%d -> v%d;" fromIdx toIdx) |> ignore
            else
                sb.AppendLine(sprintf "    v%d ->[\"%s\"%s%s] v%d;" fromIdx label bendAttr loopAttr toIdx)
                |> ignore

        AutomatonTikz.tikzFooter sb
        sb.ToString()
