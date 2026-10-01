namespace FLPQ.Printers

open System.Text
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// TikZ visualization for the input graph (linear path of terminals).
/// Renders a sequence of positions connected by labeled edges,
/// with optional highlighting of the current input position.
module InputGraphTikz =

    /// Render the input graph as a TikZ tikzpicture.
    /// Vertices are labeled by position number. currentVertex gets fill=lightgreen!20.
    let toTikz
        (terminalPrinter: 't -> string)
        (inputGraph: Graph<int, Option<'t>>)
        (currentVertex: int option)
        : string =
        let sb = StringBuilder()
        let n = Graph.vertexCount inputGraph

        AutomatonTikz.tikzHeader "circle" sb

        for i in 0 .. n - 1 do
            let label = string i

            let isCurrent =
                match currentVertex with
                | Some cv -> cv = i
                | None -> false

            let opts =
                if isCurrent then
                    sprintf "as={%s}, fill=lightgreen!20" label
                else
                    sprintf "as={%s}" label

            sb.AppendLine(sprintf "    v%d [%s];" i opts) |> ignore

        for i in 0 .. n - 1 do
            for j in 0 .. n - 1 do
                match inputGraph.Edges.[i, j] with
                | Some tok ->
                    let edgeLabel = terminalPrinter tok |> AutomatonTikz.escapeLatex

                    // Input graphs are normally linear paths, but the renderer is generic over
                    // Graph<int, Option<'t>>; split a reciprocal pair so it never draws as one
                    // overlapping line (task 289).
                    let bendAttr =
                        AutomatonTikz.reciprocalBendAttr true (Option.isSome inputGraph.Edges.[j, i]) i j

                    sb.AppendLine(sprintf "    v%d ->[\"%s\"%s] v%d;" i edgeLabel bendAttr j)
                    |> ignore
                | None -> ()

        AutomatonTikz.tikzFooter sb
        sb.ToString()
