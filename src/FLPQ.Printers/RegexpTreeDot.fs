namespace FLPQ.Printers

open System.Text

/// Graphviz DOT rendering of the RPQ regexp AST tree, used by the Arroyuelo step visualizers.
/// The tree is vertical (root at top, `rankdir=TB`); nodes are rectangles and the current
/// (handled) node is filled light blue. Dedicated to RPQ — it does not depend on the GSS
/// renderers. Labels are taken as already-rendered (the caller passes the subexpression).
module RegexpTreeDot =

    /// Render the regexp tree as a Graphviz DOT digraph. `nodes` are the AST node indices
    /// (post-order), `edges` the parent -> child pairs, `current` the highlighted node.
    let toDot (label: int -> string) (nodes: Set<int>) (edges: Set<int * int>) (current: int) : string =
        let sb = StringBuilder()

        sb.AppendLine("digraph RegexpTree {") |> ignore
        sb.AppendLine("  rankdir=TB;") |> ignore
        sb.AppendLine("  compound=true;") |> ignore

        for vidx in nodes do
            let text = label vidx |> DerivationTreeDot.escapeLabel

            let attrs =
                if vidx = current then
                    sprintf "label=\"%s\", shape=rectangle, style=filled, fillcolor=lightblue" text
                else
                    sprintf "label=\"%s\", shape=rectangle" text

            sb.AppendLine(sprintf "  v%d [%s];" vidx attrs) |> ignore

        for fromIdx, toIdx in edges do
            sb.AppendLine(sprintf "  v%d -> v%d;" fromIdx toIdx) |> ignore

        sb.AppendLine("}") |> ignore
        sb.ToString()
