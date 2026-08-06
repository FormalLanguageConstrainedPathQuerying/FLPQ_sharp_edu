namespace FLPQ.Printers

open FLPQ.Languages
open FLPQ.Languages.BasicSppf

/// Graphviz DOT visualization for basic (Rekers-style) SPPF.
/// Book reference: def:basicSPPF.
module BasicSppfDot =

    let escapeLabel (s: string) = s.Replace("\"", "\\\"")

    /// Render a basic SPPF as a Graphviz DOT digraph.
    /// terminalPrinter converts a terminal value to a display string.
    /// nonterminalPrinter converts a nonterminal value to a display string.
    let toDot (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (sppf: BasicSPPF<'t, 'nt>) : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph BasicSPPF {") |> ignore
        sb.AppendLine("  rankdir=TB;") |> ignore

        let vertexCount = FLPQ.GraphAnalysis.Graph.vertexCount sppf.Graph

        for i in 0 .. vertexCount - 1 do
            let info = FLPQ.GraphAnalysis.Graph.getVertex i sppf.Graph

            let label, shape =
                match info with
                | BasicSppfNodeInfo.Terminal(Terminal t, l, r) -> sprintf "%s_{%d,%d}" (terminalPrinter t) l r, "circle"
                | BasicSppfNodeInfo.Nonterminal(Nonterminal nt, l, r) ->
                    sprintf "%s [%d,%d]" (nonterminalPrinter nt) l r, "rectangle"
                | BasicSppfNodeInfo.Epsilon p -> sprintf "\\varepsilon_{%d}" p, "circle"
                | BasicSppfNodeInfo.Production(ruleIdx, k) -> sprintf "%d, %d" k ruleIdx, "oval"

            let rootStyle =
                if i = sppf.RootIndex then
                    ", style=filled, fillcolor=lightgreen"
                else
                    ""

            sb.AppendLine(sprintf "  n%d [label=\"%s\", shape=%s%s];" i (escapeLabel label) shape rootStyle)
            |> ignore

        for i in 0 .. vertexCount - 1 do
            for j in 0 .. vertexCount - 1 do
                if sppf.Graph.Edges.[i, j] then
                    sb.AppendLine(sprintf "  n%d -> n%d;" i j) |> ignore

        sb.AppendLine("}") |> ignore
        sb.ToString()
