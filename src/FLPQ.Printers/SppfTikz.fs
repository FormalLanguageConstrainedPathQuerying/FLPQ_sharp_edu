namespace FLPQ.Printers

open System.Text
open FLPQ.Languages

/// TikZ visualization for SPPF (Shared Packed Parse Forest).
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex).
module SppfTikz =

    /// Render an SPPF as a TikZ tikzpicture using layered layout with top-down growth.
    let toTikz (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (sppf: SPPF<'t, 'nt>) : string =
        let sb = StringBuilder()

        sb.AppendLine(@"\begin{tikzpicture}") |> ignore

        sb.AppendLine(@"  \graph [layered layout, nodes={draw}, grow'=down, level sep=1.5cm, sibling sep=1.0cm] {")
        |> ignore

        let vertexCount = FLPQ.GraphAnalysis.Graph.vertexCount sppf.Graph
        let rootSet = Set.ofList sppf.RootIndices

        let edgeLabelStr (lbl: SppfEdgeLabel) : string =
            match lbl with
            | SppfEdgeLabel.SingleChild -> ""
            | SppfEdgeLabel.LeftChild -> "L"
            | SppfEdgeLabel.RightChild -> "R"
            | SppfEdgeLabel.PackedAlternative -> "alt"

        let getShape (info: SppfNodeInfo<'t, 'nt>) : string =
            match info with
            | SppfNodeInfo.SppfTerminal _ -> "circle"
            | SppfNodeInfo.SppfNonterminal _ -> "circle"
            | SppfNodeInfo.SppfEpsilon _ -> "none"
            | SppfNodeInfo.SppfRange _ -> "rectangle"
            | SppfNodeInfo.SppfIntermediate _ -> "diamond"

        let isRoot i = Set.contains i rootSet

        for i in 0 .. vertexCount - 1 do
            let info = FLPQ.GraphAnalysis.Graph.getVertex i sppf.Graph

            let label =
                match info with
                | SppfNodeInfo.SppfTerminal(Terminal t, l, r) -> sprintf "%s [%d,%d]" (terminalPrinter t) l r
                | SppfNodeInfo.SppfNonterminal(Nonterminal nt, l, r, _, _) ->
                    sprintf "%s [%d,%d]" (nonterminalPrinter nt) l r
                | SppfNodeInfo.SppfEpsilon(Nonterminal nt, p) -> sprintf "%s^\\varepsilon @%d" (nonterminalPrinter nt) p
                | SppfNodeInfo.SppfRange(fs, fp, ts, tp) -> sprintf "[s%d,v%d]\\to[s%d,v%d]" fs fp ts tp
                | SppfNodeInfo.SppfIntermediate(s, p, fs, fp, ts, tp) ->
                    sprintf "I(%d,%d) @[s%d,v%d]\\to[s%d,v%d]" s p fs fp ts tp

            let escapedLabel = AutomatonTikz.escapeLatex label
            let shape = getShape info

            let opts =
                if isRoot i then
                    sprintf "as={%s}, fill=green!30" escapedLabel
                else
                    sprintf "as={%s}" escapedLabel

            sb.AppendLine(sprintf "    n%d [%s];" i opts) |> ignore

        for i in 0 .. vertexCount - 1 do
            for j in 0 .. vertexCount - 1 do
                match sppf.Graph.Edges.[i, j] with
                | Some lbl ->
                    let edgeLabel = edgeLabelStr lbl

                    if edgeLabel = "" then
                        sb.AppendLine(sprintf "    n%d -> n%d;" i j) |> ignore
                    else
                        sb.AppendLine(sprintf "    n%d ->[\"%s\"] n%d;" i edgeLabel j) |> ignore
                | None -> ()

        AutomatonTikz.tikzFooter sb
        sb.ToString()
