namespace FLPQ.Printers

open System.Collections.Generic
open System.Text

/// Graphviz DOT rendering of the RPQ input graph, used by `RpqGraphViz`. Unlike the general
/// GSS renderer, this module is dedicated to RPQ and does not depend on the GSS stack.
/// Vertex fill precedence (highest first): highlighted (target) > frontier (current) > start.
/// Highlighted (current) edges render red bold, path edges light red; an edge in both sets
/// renders as highlighted.
module RpqGraphDot =

    /// The DOT attribute list for a vertex: label and shape always; style and fillcolor when
    /// a fill color applies.
    let private vertexAttrs (label: string) (fillColor: string option) : string list =
        match fillColor with
        | Some c ->
            [ sprintf "label=\"%s\"" label
              "shape=ellipse"
              "style=filled"
              sprintf "fillcolor=%s" c ]
        | None -> [ sprintf "label=\"%s\"" label; "shape=ellipse" ]

    /// Render the RPQ input graph as a Graphviz DOT digraph. `activeVertices`/`activeEdges`
    /// are the full graph (every vertex and edge); the highlight sets select fills and edge
    /// styles. Labels are escaped for DOT.
    let toDot
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

        sb.AppendLine("digraph GSS {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore
        sb.AppendLine("  compound=true;") |> ignore

        let renderedVertices = HashSet<int>()

        let renderVertex (vidx: int) =
            let label = vertexLabelPrinter vidx |> DerivationTreeDot.escapeLabel

            let isStart = Set.contains vidx startVertices
            let isFrontier = Set.contains vidx frontierVertices
            let isHighlighted = Set.contains vidx highlightedVertices

            let fillColor =
                if isHighlighted then Some "lightyellow"
                elif isFrontier then Some "lightblue"
                elif isStart then Some "green"
                else None

            let attrs = vertexAttrs label fillColor |> String.concat ", "

            sb.AppendLine(sprintf "  v%d [%s];" vidx attrs) |> ignore

        for vidx in activeVertices do
            renderedVertices.Add(vidx) |> ignore
            renderVertex vidx

        // Render vertices referenced by edges but absent from the active set.
        let edgeVertices =
            activeEdges
            |> Set.fold (fun acc (from, to_) -> Set.add from (Set.add to_ acc)) Set.empty

        for vidx in edgeVertices do
            if not (renderedVertices.Contains(vidx)) then
                renderedVertices.Add(vidx) |> ignore
                renderVertex vidx

        for fromIdx, toIdx in activeEdges do
            let label = edgeLabelPrinter (fromIdx, toIdx) |> DerivationTreeDot.escapeLabel

            let isHighlighted = Set.contains (fromIdx, toIdx) highlightedEdges
            let isPath = not isHighlighted && Set.contains (fromIdx, toIdx) pathEdges

            let attrs =
                if isHighlighted then
                    sprintf "label=\"%s\", color=red, penwidth=2.0" label
                elif isPath then
                    // Quoted: an unquoted # would start a DOT comment and break the line.
                    sprintf "label=\"%s\", color=\"#FF9999\"" label
                else
                    sprintf "label=\"%s\"" label

            sb.AppendLine(sprintf "  v%d -> v%d [%s];" fromIdx toIdx attrs) |> ignore

        sb.AppendLine("}") |> ignore
        sb.ToString()
