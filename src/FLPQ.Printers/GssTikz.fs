namespace FLPQ.Printers

open System.Text
open FLPQ.LinearAlgebra

/// TikZ visualization for the Graph-Structured Stack (GSS).
/// Renders active vertices and edges using TikZ graphdrawing with layered layout.
/// Reuses AutomatonTikz primitives for consistent styling.
module GssTikz =

    /// Renders a GSS as a TikZ tikzpicture from vertex and edge sets.
    /// highlightedVertices get filled with yellow!20, highlightedEdges are red.
    /// The currentVertex (if specified) gets fill=lightblue!20.
    let toTikzFromSets
        (vertexLabelPrinter: int -> string)
        (edgeLabelPrinter: int * int -> string)
        (activeVertices: Set<int>)
        (activeEdges: Set<int * int>)
        (highlightedVertices: Set<int>)
        (highlightedEdges: Set<int * int>)
        (currentVertex: int option)
        : string =
        let sb = StringBuilder()

        AutomatonTikz.tikzHeader "circle" sb

        let allVertices =
            let fromEdges =
                activeEdges
                |> Set.fold (fun acc (from, to_) -> Set.add from (Set.add to_ acc)) Set.empty

            Set.union activeVertices fromEdges

        for vidx in allVertices do
            let label = vertexLabelPrinter vidx |> AutomatonTikz.escapeLatex

            let isCurrent =
                match currentVertex with
                | Some cv -> cv = vidx
                | None -> false

            let isHighlighted = Set.contains vidx highlightedVertices

            let opts =
                if isCurrent then
                    sprintf "as={%s}, fill=lightblue!20" label
                elif isHighlighted then
                    sprintf "as={%s}, fill=yellow!20" label
                else
                    sprintf "as={%s}" label

            sb.AppendLine(sprintf "    v%d [%s];" vidx opts) |> ignore

        // Also render current vertex if not already in allVertices
        match currentVertex with
        | Some cv when not (Set.contains cv allVertices) ->
            let label = vertexLabelPrinter cv |> AutomatonTikz.escapeLatex

            sb.AppendLine(sprintf "    v%d [as={%s}, fill=lightblue!20];" cv label)
            |> ignore
        | _ -> ()

        for fromIdx, toIdx in activeEdges do
            let label = edgeLabelPrinter (fromIdx, toIdx) |> AutomatonTikz.escapeLatex

            let isHighlighted = Set.contains (fromIdx, toIdx) highlightedEdges

            let loopAttr = if fromIdx = toIdx then ",loop above" else ""

            if isHighlighted then
                sb.AppendLine(sprintf "    v%d ->[\"%s\", red%s] v%d;" fromIdx label loopAttr toIdx)
                |> ignore
            elif label = "" then
                if fromIdx = toIdx then
                    sb.AppendLine(sprintf "    v%d ->[loop above] v%d;" fromIdx fromIdx) |> ignore
                else
                    sb.AppendLine(sprintf "    v%d -> v%d;" fromIdx toIdx) |> ignore
            else
                sb.AppendLine(sprintf "    v%d ->[\"%s\"%s] v%d;" fromIdx label loopAttr toIdx)
                |> ignore

        AutomatonTikz.tikzFooter sb
        sb.ToString()
