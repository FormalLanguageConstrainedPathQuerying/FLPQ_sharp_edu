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
    let toDot
        (vertexLabelPrinter: int -> string)
        (edgeLabelPrinter: int * int -> string)
        (highlightedVertices: Set<int>)
        (highlightedEdges: Set<int * int>)
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

            let attrs =
                let isHighlighted = Set.contains vidx highlightedVertices

                let parts =
                    if isHighlighted then
                        [ sprintf "label=\"%s\"" label
                          "shape=ellipse"
                          "style=filled"
                          "fillcolor=lightyellow" ]
                    else
                        [ sprintf "label=\"%s\"" label; "shape=ellipse" ]

                String.concat ", " parts

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
        : string =
        let sb = StringBuilder()

        sb.AppendLine("digraph GSS {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore
        sb.AppendLine("  compound=true;") |> ignore

        // Vertex declarations
        for vidx in activeVertices do
            let label = vertexLabelPrinter vidx |> DerivationTreeDot.escapeLabel

            let isHighlighted = Set.contains vidx highlightedVertices

            let parts =
                if isHighlighted then
                    [ sprintf "label=\"%s\"" label
                      "shape=ellipse"
                      "style=filled"
                      "fillcolor=lightyellow" ]
                else
                    [ sprintf "label=\"%s\"" label; "shape=ellipse" ]

            let attrs = String.concat ", " parts

            sb.AppendLine(sprintf "  v%d [%s];" vidx attrs) |> ignore

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
