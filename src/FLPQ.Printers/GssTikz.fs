namespace FLPQ.Printers

open System.Text
open FLPQ.LinearAlgebra

/// TikZ visualization for the Graph-Structured Stack (GSS).
/// Renders active vertices and edges using TikZ graphdrawing with dot layout.
/// Reuses AutomatonTikz primitives for consistent styling.
module GssTikz =

    /// Renders a GSS as a TikZ tikzpicture from vertex and edge sets.
    /// highlightedVertices get filled with yellow!20, highlightedEdges are red and bold
    /// (thick), pathEdges are light red (red!40); an edge in both sets renders as
    /// highlighted. The currentVertex (if specified) gets fill=lightblue!20.
    /// startVertices get filled with green!30 (graph sources, matching the automaton's
    /// initial-state fill), frontierVertices get fill=lightblue!20 (current frontier
    /// positions). Vertex fill precedence (highest first): current > stored-pop >
    /// highlighted > frontier > start. RPQ passes target as highlighted and current as
    /// frontier, so the RPQ graph renders target > current > start; GLL/RNGLR pass no
    /// start/frontier and therefore keep current > stored-pop > highlighted
    /// (current and frontier share the lightblue!20 fill).
    /// storedPopVertices get filled with orange!30 (stored pops handling triggered).
    /// When positionOf is Some, every vertex is constrained to the layer of its input position
    /// (one { [same layer] ... } collection per position). The graph then grows left (grow=left)
    /// so that input position 0 stays rightmost: pgf's same-layer cluster chaining reverses the
    /// orientation under the default grow direction.
    let toTikzFromSets
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
        (shape: string)
        (skipEscaping: bool)
        (positionOf: (int -> int) option)
        : string =
        let sb = StringBuilder()

        let growDirection =
            match positionOf with
            | Some _ -> AutomatonTikz.gssLayeredGrowDirection
            | None -> AutomatonTikz.defaultGrowDirection

        let edgeLabel (from: int) (to_: int) : string =
            let raw = edgeLabelPrinter (from, to_)

            if skipEscaping then raw else AutomatonTikz.escapeLatex raw

        // Non-loop edges with a non-empty label: registered in the measurement pre-pass,
        // their stock quote dropped (the router's edge-callback wrapper places the label).
        let labeledEdges =
            activeEdges
            |> Set.toList
            |> List.filter (fun (from, to_) -> from <> to_ && edgeLabel from to_ <> "")
            |> List.map (fun (from, to_) ->
                (FlpqdotTikz.edgeKey (sprintf "v%d" from) (sprintf "v%d" to_), edgeLabel from to_))

        AutomatonTikz.tikzHeaderWithOptions
            (FlpqdotTikz.prePass labeledEdges)
            (AutomatonTikz.graphOptions shape growDirection)
            sb

        let allVertices =
            let fromEdges =
                activeEdges
                |> Set.fold (fun acc (from, to_) -> Set.add from (Set.add to_ acc)) Set.empty

            Set.union activeVertices fromEdges

        for vidx in allVertices do
            let rawLabel = vertexLabelPrinter vidx

            let label =
                if skipEscaping then
                    rawLabel
                else
                    AutomatonTikz.escapeLatex rawLabel

            let isCurrent =
                match currentVertex with
                | Some cv -> cv = vidx
                | None -> false

            let isStart = Set.contains vidx startVertices

            let isFrontier = Set.contains vidx frontierVertices

            let isStoredPop = Set.contains vidx storedPopVertices

            let isHighlighted = Set.contains vidx highlightedVertices

            // Vertex fill precedence (highest first): current > stored-pop > highlighted >
            // frontier > start (current and frontier share the lightblue!20 fill).
            let fill =
                if isCurrent then Some "lightblue!20"
                elif isStoredPop then Some "orange!30"
                elif isHighlighted then Some "yellow!20"
                elif isFrontier then Some "lightblue!20"
                elif isStart then Some "green!30"
                else None

            let opts =
                match fill with
                | Some c -> sprintf "as={%s}, fill=%s" label c
                | None -> sprintf "as={%s}" label

            sb.AppendLine(sprintf "    v%d [%s];" vidx opts) |> ignore

        // Also render current vertex if not already in allVertices
        match currentVertex with
        | Some cv when not (Set.contains cv allVertices) ->
            let rawLabel = vertexLabelPrinter cv

            let label =
                if skipEscaping then
                    rawLabel
                else
                    AutomatonTikz.escapeLatex rawLabel

            sb.AppendLine(sprintf "    v%d [as={%s}, fill=lightblue!20];" cv label)
            |> ignore
        | _ -> ()

        for fromIdx, toIdx in activeEdges do
            let label = edgeLabel fromIdx toIdx

            // Highlighted edges win over path edges (an edge in both sets renders as
            // highlighted).
            let style =
                if Set.contains (fromIdx, toIdx) highlightedEdges then
                    "red, thick"
                elif Set.contains (fromIdx, toIdx) pathEdges then
                    "red!40"
                else
                    ""

            FlpqdotTikz.emitEdge sb (sprintf "v%d" fromIdx) (sprintf "v%d" toIdx) label style

        // Constrain all vertices at the same input position to the same layer. The grouping covers
        // every drawn vertex (allVertices plus the current vertex when it is not already among
        // them), mirroring GssDot's renderedVertices so no node is left outside its position layer.
        let groupedVertices =
            match currentVertex with
            | Some cv -> Set.add cv allVertices
            | None -> allVertices

        match positionOf with
        | Some posFn ->
            let byPosition = groupedVertices |> Set.toSeq |> Seq.groupBy posFn |> Map.ofSeq

            for position in Seq.sort byPosition.Keys do
                let idList = byPosition.[position] |> Seq.map (sprintf "v%d") |> String.concat ", "

                sb.AppendLine(sprintf "    { [same layer] %s };" idList) |> ignore
        | None -> ()

        AutomatonTikz.tikzFooter sb
        sb.ToString()
