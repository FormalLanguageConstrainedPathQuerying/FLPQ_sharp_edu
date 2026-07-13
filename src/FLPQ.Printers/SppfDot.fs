namespace FLPQ.Printers

open FLPQ.Languages

/// Graphviz DOT visualization for SPPF (Shared Packed Parse Forest).
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex).
module SppfDot =

    let escapeLabel (s: string) = s.Replace("\"", "\\\"")

    /// Render an SPPF as a Graphviz DOT digraph.
    /// terminalPrinter converts a terminal value to a display string.
    /// nonterminalPrinter converts a nonterminal value to a display string.
    let toDot (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (sppf: SPPF<'t, 'nt>) : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph SPPF {") |> ignore
        sb.AppendLine("  rankdir=TB;") |> ignore

        let vertexCount = FLPQ.GraphAnalysis.Graph.vertexCount sppf.Graph
        let rootSet = Set.ofList sppf.RootIndices

        let edgeLabelStr (lbl: SppfEdgeLabel) : string =
            match lbl with
            | SppfEdgeLabel.SingleChild -> "1"
            | SppfEdgeLabel.LeftChild -> "L"
            | SppfEdgeLabel.RightChild -> "R"
            | SppfEdgeLabel.PackedAlternative -> "alt"

        for i in 0 .. vertexCount - 1 do
            let info = FLPQ.GraphAnalysis.Graph.getVertex i sppf.Graph

            let label, shape, extra =
                match info with
                | SppfNodeInfo.SppfTerminal(Terminal t, l, r) ->
                    sprintf "%s [%d,%d]" (terminalPrinter t) l r, "oval", ""
                | SppfNodeInfo.SppfNonterminal(Nonterminal nt, l, r) ->
                    sprintf "%s [%d,%d]" (nonterminalPrinter nt) l r, "oval", ""
                | SppfNodeInfo.SppfEpsilon(optNt, p) ->
                    match optNt with
                    | Some(Nonterminal nt) -> sprintf "%s^ε @%d" (nonterminalPrinter nt) p, "none", ""
                    | None -> sprintf "ε @%d" p, "none", ""
                | SppfNodeInfo.SppfRange(fs, fp, ts, tp) -> sprintf "[s%d,v%d]→[s%d,v%d]" fs fp ts tp, "rectangle", ""
                | SppfNodeInfo.SppfIntermediate(s, p, fs, fp, ts, tp) ->
                    sprintf "I(%d,%d) @[s%d,v%d]→[s%d,v%d]" s p fs fp ts tp, "diamond", ""

            let rootStyle =
                if Set.contains i rootSet then
                    ", style=filled, fillcolor=lightgreen"
                else
                    ""

            sb.AppendLine(sprintf "  n%d [label=\"%s\", shape=%s%s%s];" i (escapeLabel label) shape extra rootStyle)
            |> ignore

        for i in 0 .. vertexCount - 1 do
            for j in 0 .. vertexCount - 1 do
                match FLPQ.LinearAlgebra.Matrix.get sppf.Graph.Edges i j with
                | Some lbl ->
                    sb.AppendLine(sprintf "  n%d -> n%d [label=\"%s\"];" i j (edgeLabelStr lbl))
                    |> ignore
                | None -> ()

        sb.AppendLine("}") |> ignore
        sb.ToString()
