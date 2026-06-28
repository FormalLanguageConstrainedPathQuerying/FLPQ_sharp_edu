namespace FLPQ.Languages

/// Graphviz dot visualization for derivation trees.
module DerivationTreeVisualizer =

    let private escapeLabel (s: string) = s.Replace("\"", "\\\"")

    /// Render a derivation tree as a Graphviz dot graph.
    /// symbolVisualizer converts a grammar symbol to a display string.
    let toDot (symbolVisualizer: Symbol<'t, 'nt> -> string) (tree: DerivationTree<'t, 'nt>) : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph DerivationTree {") |> ignore
        sb.AppendLine("  rankdir=TB;") |> ignore

        let mutable nodeId = 0

        let nextId () =
            nodeId <- nodeId + 1
            nodeId

        let rec traverse (node: DerivationTree<'t, 'nt>) : int =
            let nid = nextId ()

            match node with
            | Leaf sym ->
                let label = escapeLabel (symbolVisualizer sym)
                sb.AppendLine(sprintf "  n%d [label=\"%s\", shape=box];" nid label) |> ignore

            | Node(nt, children) ->
                let label = escapeLabel (symbolVisualizer (N nt))
                sb.AppendLine(sprintf "  n%d [label=\"%s\"];" nid label) |> ignore

                for child in children do
                    let childId = traverse child
                    sb.AppendLine(sprintf "  n%d -> n%d;" nid childId) |> ignore

            nid

        traverse tree |> ignore
        sb.AppendLine("}") |> ignore
        sb.ToString()
