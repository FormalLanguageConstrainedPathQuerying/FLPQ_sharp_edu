namespace FLPQ.Printers

open FLPQ.Languages

/// Graphviz dot visualization for derivation trees.
module DerivationTreeDot =

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

    /// Render a derivation tree with an overlay stack chain as a Graphviz dot graph.
    /// stackTrees are the tree nodes from stack frames, in top-to-bottom order.
    /// Tree nodes that match stack trees are connected via a dashed chain and
    /// constrained to the same rank via rank=same.
    let toDotWithStack
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (tree: DerivationTree<'t, 'nt>)
        (stackTrees: DerivationTree<'t, 'nt> list)
        : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph StackTree {") |> ignore
        sb.AppendLine("  rankdir=TB;") |> ignore

        let mutable nodeId = 0

        let nextId () =
            nodeId <- nodeId + 1
            nodeId

        let mutable nodeIds: (int * DerivationTree<'t, 'nt>) list = []

        let rec traverse (node: DerivationTree<'t, 'nt>) : int =
            let nid = nextId ()
            nodeIds <- (nid, node) :: nodeIds

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

        let mutable orderedStackIds = []

        for st in stackTrees do
            match nodeIds |> List.tryFind (fun (_, n) -> n = st) with
            | Some(id, _) -> orderedStackIds <- id :: orderedStackIds
            | None ->
                let nid = nextId ()

                match st with
                | Leaf sym ->
                    let label = escapeLabel (symbolVisualizer sym)
                    sb.AppendLine(sprintf "  n%d [label=\"%s\", shape=box];" nid label) |> ignore
                | Node(nt, _) ->
                    let label = escapeLabel (symbolVisualizer (N nt))
                    sb.AppendLine(sprintf "  n%d [label=\"%s\"];" nid label) |> ignore

                orderedStackIds <- nid :: orderedStackIds

        orderedStackIds <- List.rev orderedStackIds

        for i in 0 .. orderedStackIds.Length - 2 do
            sb.AppendLine(
                sprintf "  n%d -> n%d [style=dashed, constraint=false];" orderedStackIds.[i] orderedStackIds.[i + 1]
            )
            |> ignore

        if not (List.isEmpty orderedStackIds) then
            let idList = orderedStackIds |> List.map (sprintf "n%d") |> String.concat "; "

            sb.AppendLine(sprintf "  {rank=same; %s}" idList) |> ignore

        sb.AppendLine("}") |> ignore
        sb.ToString()
