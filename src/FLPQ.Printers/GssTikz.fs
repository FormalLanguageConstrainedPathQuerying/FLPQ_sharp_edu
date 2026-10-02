namespace FLPQ.Printers

open System.Text
open FLPQ.LinearAlgebra

/// TikZ visualization for the Graph-Structured Stack (GSS).
/// Renders active vertices and edges using TikZ graphdrawing with layered layout.
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
    /// When bendReciprocalEdges is true, every edge whose reverse is also drawn gets
    /// `bend left=15`: a reciprocal pair (u->v and v->u) would otherwise be two straight
    /// lines on top of each other, while the bend turns it into two symmetric arcs
    /// (Graphviz DOT separates such pairs automatically, so this is a TikZ-only concern).
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
        (bendReciprocalEdges: bool)
        : string =
        let sb = StringBuilder()

        let growDirection =
            match positionOf with
            | Some _ -> AutomatonTikz.gssLayeredGrowDirection
            | None -> AutomatonTikz.defaultGrowDirection

        AutomatonTikz.tikzHeaderWithOptions (AutomatonTikz.layeredGraphOptions shape growDirection) sb

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
            let rawLabel = edgeLabelPrinter (fromIdx, toIdx)

            let label =
                if skipEscaping then
                    rawLabel
                else
                    AutomatonTikz.escapeLatex rawLabel

            let isHighlighted = Set.contains (fromIdx, toIdx) highlightedEdges
            let isPath = not isHighlighted && Set.contains (fromIdx, toIdx) pathEdges

            let loopAttr = if fromIdx = toIdx then ",loop above" else ""

            // A reciprocal pair (u->v and v->u) draws as two overlapping straight lines;
            // bending both edges of the pair separates them into symmetric arcs (shared rule,
            // task 289). Self-loops are never bent.
            let bendAttr =
                AutomatonTikz.reciprocalBendAttr
                    bendReciprocalEdges
                    (Set.contains (toIdx, fromIdx) activeEdges)
                    fromIdx
                    toIdx

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
