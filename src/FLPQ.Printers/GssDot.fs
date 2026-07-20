namespace FLPQ.Printers

open System.Collections.Generic
open System.Text
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// Graphviz DOT visualization for the Graph-Structured Stack (GSS).
/// Renders active vertices and edges with optional highlighting for newly added elements.
module GssDot =

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

        for fromIdx in 0 .. n - 1 do
            for toIdx in 0 .. n - 1 do
                match Matrix.get gss.Graph.Edges fromIdx toIdx with
                | Some _ ->
                    activeVertices.Add(fromIdx) |> ignore
                    allEdges.Add((fromIdx, toIdx))
                | None -> ()

        // Vertex declarations
        for vidx in activeVertices do
            let label = vertexLabelPrinter vidx |> DerivationTreeDot.escapeLabel

            let isCurrent =
                match currentVertex with
                | Some cv -> cv = vidx
                | None -> false

            let isHighlighted = Set.contains vidx highlightedVertices

            let parts =
                if isCurrent then
                    [ sprintf "label=\"%s\"" label
                      "shape=ellipse"
                      "style=filled"
                      "fillcolor=lightblue" ]
                elif isHighlighted then
                    [ sprintf "label=\"%s\"" label
                      "shape=ellipse"
                      "style=filled"
                      "fillcolor=lightyellow" ]
                else
                    [ sprintf "label=\"%s\"" label; "shape=ellipse" ]

            let attrs = String.concat ", " parts

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
    let toDotFromSets
        (vertexLabelPrinter: int -> string)
        (edgeLabelPrinter: int * int -> string)
        (activeVertices: Set<int>)
        (activeEdges: Set<int * int>)
        (highlightedVertices: Set<int>)
        (highlightedEdges: Set<int * int>)
        (currentVertex: int option)
        : string =
        let sb = StringBuilder()

        sb.AppendLine("digraph GSS {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore
        sb.AppendLine("  compound=true;") |> ignore

        // Vertex declarations
        let renderedVertices = HashSet<int>()

        for vidx in activeVertices do
            renderedVertices.Add(vidx) |> ignore

            let label = vertexLabelPrinter vidx |> DerivationTreeDot.escapeLabel

            let isCurrent =
                match currentVertex with
                | Some cv -> cv = vidx
                | None -> false

            let isHighlighted = Set.contains vidx highlightedVertices

            let parts =
                if isCurrent then
                    [ sprintf "label=\"%s\"" label
                      "shape=ellipse"
                      "style=filled"
                      "fillcolor=lightblue" ]
                elif isHighlighted then
                    [ sprintf "label=\"%s\"" label
                      "shape=ellipse"
                      "style=filled"
                      "fillcolor=lightyellow" ]
                else
                    [ sprintf "label=\"%s\"" label; "shape=ellipse" ]

            let attrs = String.concat ", " parts

            sb.AppendLine(sprintf "  v%d [%s];" vidx attrs) |> ignore

        // Ensure current vertex is always rendered even if not in active set
        match currentVertex with
        | Some cv when not (renderedVertices.Contains(cv)) ->
            let label = vertexLabelPrinter cv |> DerivationTreeDot.escapeLabel

            let attrs =
                sprintf "label=\"%s\", shape=ellipse, style=filled, fillcolor=lightblue" label

            sb.AppendLine(sprintf "  v%d [%s];" cv attrs) |> ignore
        | _ -> ()

        // Edge declarations
        for fromIdx, toIdx in activeEdges do
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
