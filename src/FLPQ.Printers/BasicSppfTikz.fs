namespace FLPQ.Printers

open System.Text
open FLPQ.Languages
open FLPQ.Languages.BasicSppf

/// TikZ visualization for basic (Rekers-style) SPPF.
/// Book reference: def:basicSPPF.
module BasicSppfTikz =

    /// Render a basic SPPF as a TikZ tikzpicture using layered layout with top-down growth.
    let toTikz (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (sppf: BasicSPPF<'t, 'nt>) : string =
        let sb = StringBuilder()

        sb.AppendLine(@"\begin{tikzpicture}") |> ignore

        sb.AppendLine(@"  \graph [layered layout, nodes={draw}, grow'=down, level sep=1.5cm, sibling sep=1.0cm] {")
        |> ignore

        let vertexCount = FLPQ.GraphAnalysis.Graph.vertexCount sppf.Graph

        for i in 0 .. vertexCount - 1 do
            let info = FLPQ.GraphAnalysis.Graph.getVertex i sppf.Graph

            let label =
                match info with
                | BasicSppfNodeInfo.Terminal(Terminal t, l, r) -> sprintf "%s_{%d,%d}" (terminalPrinter t) l r
                | BasicSppfNodeInfo.Nonterminal(Nonterminal nt, l, r) ->
                    sprintf "%s [%d,%d]" (nonterminalPrinter nt) l r
                | BasicSppfNodeInfo.Epsilon p -> sprintf "\\varepsilon_{%d}" p
                | BasicSppfNodeInfo.Production(ruleIdx, k) -> sprintf "%d, %d" k ruleIdx

            let escapedLabel = AutomatonTikz.escapeLatex label

            let opts =
                if i = sppf.RootIndex then
                    sprintf "as={%s}, fill=green!30" escapedLabel
                else
                    sprintf "as={%s}" escapedLabel

            sb.AppendLine(sprintf "    n%d [%s];" i opts) |> ignore

        for i in 0 .. vertexCount - 1 do
            for j in 0 .. vertexCount - 1 do
                if sppf.Graph.Edges.[i, j] then
                    sb.AppendLine(sprintf "    n%d -> n%d;" i j) |> ignore

        AutomatonTikz.tikzFooter sb
        sb.ToString()
