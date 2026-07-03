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

    /// Render LL parser stack frames and completed subtrees as a combined DOT graph.
    /// Completed subtrees are rendered as full trees on the left.
    /// Stack frames are rendered on the right with a dashed chain and same-rank constraint.
    /// LLMarker frames are rendered as gray boxes.
    let toDotWithLLStack
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (stack: LLStackFrame<'t, 'nt> list)
        (completed: DerivationTree<'t, 'nt> list)
        : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph StackTree {") |> ignore
        sb.AppendLine("  rankdir=TB;") |> ignore

        let mutable nodeId = 0

        let nextId () =
            nodeId <- nodeId + 1
            nodeId

        let rec renderTree (node: DerivationTree<'t, 'nt>) : int =
            let nid = nextId ()

            match node with
            | Leaf sym ->
                let label = escapeLabel (symbolVisualizer sym)
                sb.AppendLine(sprintf "  n%d [label=\"%s\", shape=box];" nid label) |> ignore

            | Node(nt, children) ->
                let label = escapeLabel (symbolVisualizer (N nt))
                sb.AppendLine(sprintf "  n%d [label=\"%s\"];" nid label) |> ignore

                for child in children do
                    let childId = renderTree child
                    sb.AppendLine(sprintf "  n%d -> n%d;" nid childId) |> ignore

            nid

        if not (List.isEmpty completed) then
            sb.AppendLine("  subgraph cluster_completed {") |> ignore
            sb.AppendLine("    label=\"Completed subtrees\";") |> ignore
            sb.AppendLine("    style=dashed;") |> ignore

            for tree in completed do
                renderTree tree |> ignore

            sb.AppendLine("  }") |> ignore

        let stackIds =
            stack
            |> List.map (fun frame ->
                match frame with
                | LLTree tree -> renderTree tree
                | LLMarker(nt, _) ->
                    let nid = nextId ()
                    let label = escapeLabel (symbolVisualizer (N nt))

                    sb.AppendLine(
                        sprintf "  n%d [label=\"%s\", shape=box, style=filled, fillcolor=lightgray];" nid label
                    )
                    |> ignore

                    nid)
            |> List.rev

        for i in 0 .. stackIds.Length - 2 do
            sb.AppendLine(sprintf "  n%d -> n%d [style=dashed, constraint=false];" stackIds.[i] stackIds.[i + 1])
            |> ignore

        if not (List.isEmpty stackIds) then
            let idList = stackIds |> List.map (sprintf "n%d") |> String.concat "; "

            sb.AppendLine(sprintf "  {rank=same; %s}" idList) |> ignore

        sb.AppendLine("}") |> ignore
        sb.ToString()

    /// Render LR parser stack frames as a combined stack-tree Graphviz dot graph.
    /// LRSymbol frames render as derivation tree nodes (subtrees fully expanded).
    /// LRState frames render as labeled "sN" boxes.
    /// All frames are connected via dashed edges and constrained to the same rank.
    let toDotWithLRStack (symbolVisualizer: Symbol<'t, 'nt> -> string) (stack: LRStackFrame<'t, 'nt> list) : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph StackTree {") |> ignore
        sb.AppendLine("  rankdir=TB;") |> ignore

        let mutable nodeId = 0

        let nextId () =
            nodeId <- nodeId + 1
            nodeId

        let rec renderTree (node: DerivationTree<'t, 'nt>) : int =
            let nid = nextId ()

            match node with
            | Leaf sym ->
                let label = escapeLabel (symbolVisualizer sym)
                sb.AppendLine(sprintf "  n%d [label=\"%s\", shape=box];" nid label) |> ignore

            | Node(nt, children) ->
                let label = escapeLabel (symbolVisualizer (N nt))
                sb.AppendLine(sprintf "  n%d [label=\"%s\"];" nid label) |> ignore

                for child in children do
                    let childId = renderTree child
                    sb.AppendLine(sprintf "  n%d -> n%d;" nid childId) |> ignore

            nid

        let mutable stackIds: int list = []

        for frame in stack do
            let nid =
                match frame with
                | LRSymbol st -> renderTree st
                | LRState s ->
                    let nid = nextId ()

                    sb.AppendLine(sprintf "  n%d [label=\"s%d\", shape=box, style=filled, fillcolor=lightgray];" nid s)
                    |> ignore

                    nid

            stackIds <- nid :: stackIds

        stackIds <- List.rev stackIds

        for i in 0 .. stackIds.Length - 2 do
            sb.AppendLine(sprintf "  n%d -> n%d [style=dashed, constraint=false];" stackIds.[i] stackIds.[i + 1])
            |> ignore

        if not (List.isEmpty stackIds) then
            let idList = stackIds |> List.map (sprintf "n%d") |> String.concat "; "

            sb.AppendLine(sprintf "  {rank=same; %s}" idList) |> ignore

        sb.AppendLine("}") |> ignore
        sb.ToString()
