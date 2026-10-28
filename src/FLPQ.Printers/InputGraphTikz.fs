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

        let edgeLabel (i: int) (j: int) : string =
            match inputGraph.Edges.[i, j] with
            | Some tok -> terminalPrinter tok |> AutomatonTikz.escapeLatex
            | None -> ""

        // Non-loop edges with a non-empty label: registered in the measurement pre-pass,
        // their stock quote dropped (the router's edge-callback wrapper places the label).
        let labeledEdges =
            [ for i in 0 .. n - 1 do
                  for j in 0 .. n - 1 do
                      let label = edgeLabel i j

                      if i <> j && label <> "" then
                          [ (FlpqdotTikz.edgeKey (sprintf "v%d" i) (sprintf "v%d" j), label) ]
                      else
                          [] ]
            |> List.collect id

        AutomatonTikz.tikzHeader (FlpqdotTikz.prePass labeledEdges) "circle" sb

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
                | Some _ -> FlpqdotTikz.emitEdge sb (sprintf "v%d" i) (sprintf "v%d" j) (edgeLabel i j) ""
                | None -> ()

        AutomatonTikz.tikzFooter sb
        sb.ToString()
