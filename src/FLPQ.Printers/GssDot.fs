namespace FLPQ.Printers

open System.Collections.Generic
open System.Text
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// Graphviz DOT visualization for the Graph-Structured Stack (GSS).
/// Renders active vertices and edges with optional highlighting for newly added elements.
module GssDot =

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

    /// Renders a GSS as a Graphviz DOT digraph.
    /// Only active vertices (those with outgoing edges) are rendered.
    /// Highlighted vertices are filled with yellow!30, highlighted edges are red with penwidth=2.
    /// The current vertex (if specified) is filled with lightblue, overriding the highlighted color.
    let toDot
        (vertexLabelPrinter: int -> string)
        (edgeLabelPrinter: int * int -> string)
        (highlightedVertices: Set<int>)
        (highlightedEdges: Set<int * int>)
        (currentVertex: int option)
        (gss: GSS)
        : string =
        let sb = StringBuilder()

        sb.AppendLine("digraph GSS {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore
        sb.AppendLine("  compound=true;") |> ignore

        let n = gss.Graph.VertexMap.Count

        // Collect active vertices (those with outgoing edges) and all edges
        let activeVertices = HashSet<int>()
        let allEdges: ResizeArray<int * int> = ResizeArray()
        let allVertices = HashSet<int>()

        for fromIdx in 0 .. n - 1 do
            for toIdx in 0 .. n - 1 do
                match gss.Graph.Edges.[fromIdx, toIdx] with
                | Some _ ->
                    activeVertices.Add(fromIdx) |> ignore
                    allEdges.Add((fromIdx, toIdx))
                    allVertices.Add(fromIdx) |> ignore
                    allVertices.Add(toIdx) |> ignore
                | None -> ()

        // Vertex declarations
        for vidx in allVertices do
            let label = vertexLabelPrinter vidx |> DerivationTreeDot.escapeLabel

            let isCurrent =
                match currentVertex with
                | Some cv -> cv = vidx
                | None -> false

            let isHighlighted = Set.contains vidx highlightedVertices

            let fillColor =
                if isCurrent then Some "lightblue"
                elif isHighlighted then Some "lightyellow"
                else None

            let attrs = vertexAttrs label fillColor |> String.concat ", "

            sb.AppendLine(sprintf "  v%d [%s];" vidx attrs) |> ignore

        // Edge declarations
        for fromIdx, toIdx in allEdges do
            let label = edgeLabelPrinter (fromIdx, toIdx) |> DerivationTreeDot.escapeLabel

            let isHighlighted = Set.contains (fromIdx, toIdx) highlightedEdges

            let attrs =
                if isHighlighted then
                    sprintf "label=\"%s\", color=red, penwidth=2.0" label
                else
                    sprintf "label=\"%s\"" label

            sb.AppendLine(sprintf "  v%d -> v%d [%s];" fromIdx toIdx attrs) |> ignore

        sb.AppendLine("}") |> ignore
        sb.ToString()

    /// Renders GSS from vertex and edge sets directly (without full GSS struct).
    /// Used for step visualization where only active elements are known.
    /// highlightedEdges are red with penwidth=2.0, pathEdges are light red (#FF9999); an
    /// edge in both sets renders as highlighted.
    /// startVertices get filled green (graph sources, matching the automaton's
    /// initial-state fill), frontierVertices get filled lightblue (current frontier
    /// positions). Vertex fill precedence (highest first): current > stored-pop >
    /// highlighted > frontier > start. RPQ passes target as highlighted and current as
    /// frontier, so the RPQ graph renders target > current > start; GLL/RNGLR pass no
    /// start/frontier and therefore keep current > stored-pop > highlighted.
    /// storedPopVertices get filled with orange (stored pops handling triggered at these vertices).
    /// When positionOf is Some, every vertex is constrained to the rank of its input position
    /// (one {rank=same; ...} subgraph per position), so all nodes at the same input position share
    /// a layer while the left-to-right global layout (position 0 rightmost) is preserved.
    let toDotFromSets
        (vertexLabelPrinter: int -> string)
        (edgeLabelPrinter: int * int -> string)
        (activeVertices: Set<int>)
        (activeEdges: Set<int * int>)
        (highlightedVertices: Set<int>)
        (startVertices: Set<int>)
        (frontierVertices: Set<int>)
        (highlightedEdges: Set<int * int>)
        (pathEdges: Set<int * int>)
        (storedPopVertices: Set<int>)
        (currentVertex: int option)
        (positionOf: (int -> int) option)
        : string =
        let sb = StringBuilder()

        sb.AppendLine("digraph GSS {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore
        sb.AppendLine("  compound=true;") |> ignore

        // Vertex declarations
        let renderedVertices = HashSet<int>()

        let renderVertex (vidx: int) =
            let label = vertexLabelPrinter vidx |> DerivationTreeDot.escapeLabel

            let isCurrent =
                match currentVertex with
                | Some cv -> cv = vidx
                | None -> false

            let isStart = Set.contains vidx startVertices

            let isFrontier = Set.contains vidx frontierVertices

            let isStoredPop = Set.contains vidx storedPopVertices

            let isHighlighted = Set.contains vidx highlightedVertices

            // Vertex fill precedence (highest first): current > stored-pop > highlighted >
            // frontier > start (current and frontier share the lightblue fill).
            let fillColor =
                if isCurrent then Some "lightblue"
                elif isStoredPop then Some "orange"
                elif isHighlighted then Some "lightyellow"
                elif isFrontier then Some "lightblue"
                elif isStart then Some "green"
                else None

            let attrs = vertexAttrs label fillColor |> String.concat ", "

            sb.AppendLine(sprintf "  v%d [%s];" vidx attrs) |> ignore

        for vidx in activeVertices do
            renderedVertices.Add(vidx) |> ignore
            renderVertex vidx

        // Collect all vertices referenced by edges that are not in active set
        let edgeVertices =
            activeEdges
            |> Set.fold (fun acc (from, to_) -> Set.add from (Set.add to_ acc)) Set.empty

        for vidx in edgeVertices do
            if not (renderedVertices.Contains(vidx)) then
                renderedVertices.Add(vidx) |> ignore
                renderVertex vidx

        // Ensure current vertex is always rendered even if not in active set
        match currentVertex with
        | Some cv when not (renderedVertices.Contains(cv)) ->
            renderedVertices.Add(cv) |> ignore
            renderVertex cv
        | _ -> ()

        // Edge declarations
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

        // Constrain all vertices at the same input position to the same rank (layer).
        match positionOf with
        | Some posFn ->
            let byPosition = renderedVertices |> Seq.groupBy posFn |> Map.ofSeq

            for position in Seq.sort byPosition.Keys do
                let idList =
                    byPosition.[position]
                    |> Seq.sort
                    |> Array.ofSeq
                    |> Array.map (sprintf "v%d")
                    |> String.concat "; "

                sb.AppendLine(sprintf "  {rank=same; %s;}" idList) |> ignore
        | None -> ()

        sb.AppendLine("}") |> ignore
        sb.ToString()
