namespace FLPQ.Printers

open System.Text
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// DOT visualization for the input graph (linear path of terminals).
/// Renders a sequence of positions connected by labeled edges,
/// with optional highlighting of the current input position.
module InputGraphDot =

    let toDot
        (terminalPrinter: 't -> string)
        (inputGraph: Graph<int, Option<'t>>)
        (currentVertex: int option)
        : string =
        let sb = StringBuilder()
        let n = Graph.vertexCount inputGraph

        sb.AppendLine("digraph InputGraph {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore

        for i in 0 .. n - 1 do
            let label = string i |> DerivationTreeDot.escapeLabel

            let attrs =
                match currentVertex with
                | Some cv when cv = i -> sprintf "label=\"%s\", shape=circle, style=filled, fillcolor=lightgreen" label
                | _ -> sprintf "label=\"%s\", shape=circle" label

            sb.AppendLine(sprintf "  v%d [%s];" i attrs) |> ignore

        for i in 0 .. n - 1 do
            for j in 0 .. n - 1 do
                match inputGraph.Edges.[i, j] with
                | Some tok ->
                    let edgeLabel = terminalPrinter tok |> DerivationTreeDot.escapeLabel
                    sb.AppendLine(sprintf "  v%d -> v%d [label=\"%s\"];" i j edgeLabel) |> ignore
                | None -> ()

        sb.AppendLine("}") |> ignore
        sb.ToString()
