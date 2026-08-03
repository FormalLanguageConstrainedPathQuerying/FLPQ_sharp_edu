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

        let edgeLabelStr (lbl: BasicSppfEdgeLabel) : string =
            match lbl with
            | BasicSppfEdgeLabel.Derives -> "derives"
            | BasicSppfEdgeLabel.ChildOf pos -> sprintf "%d" pos

        for i in 0 .. vertexCount - 1 do
            let info = FLPQ.GraphAnalysis.Graph.getVertex i sppf.Graph

            let label, shape =
                match info with
                | BasicSppfNodeInfo.Terminal(Terminal t, l, r) -> sprintf "%s_{%d,%d}" (terminalPrinter t) l r, "circle"
                | BasicSppfNodeInfo.Nonterminal(Nonterminal nt, l, r) ->
                    sprintf "%s_{%d,%d}" (nonterminalPrinter nt) l r, "rectangle"
                | BasicSppfNodeInfo.Epsilon p -> sprintf "\\varepsilon_{%d}" p, "circle"
                | BasicSppfNodeInfo.Production(ruleIdx, l, r) -> sprintf "%d [%d,%d]" ruleIdx l r, "oval"

            let rootStyle =
                if i = sppf.RootIndex then
                    ", style=filled, fillcolor=lightgreen"
                else
                    ""

            sb.AppendLine(sprintf "  n%d [label=\"%s\", shape=%s%s];" i (escapeLabel label) shape rootStyle)
            |> ignore

        for i in 0 .. vertexCount - 1 do
            for j in 0 .. vertexCount - 1 do
                match sppf.Graph.Edges.[i, j] with
                | Some lbl ->
                    sb.AppendLine(sprintf "  n%d -> n%d [label=\"%s\"];" i j (edgeLabelStr lbl))
                    |> ignore
                | None -> ()

        sb.AppendLine("}") |> ignore
        sb.ToString()
