namespace FLPQ.Printers

open System.Text
open FLPQ.Languages

/// TikZ visualization for derivation trees using graphdrawing with layered layout.
/// Mirrors DerivationTreeDot: the full partial derivation tree is rendered and the stack
/// frontier is constrained to a single layer via the native `{ [same layer] ... }` collection
/// (the graphdrawing equivalent of DOT's `{rank=same; ...}`). Dashed chain edges between
/// frontier nodes are drawn but do not constrain the layout: within a same-layer cluster they
/// become loops that the layered algorithm removes before ranking and restores for drawing.
module DerivationTreeTikz =

    let private tikzHeader (sb: StringBuilder) : unit =
        sb.AppendLine(@"\begin{tikzpicture}") |> ignore

        sb.AppendLine(@"  \graph [layered layout, nodes={draw, shape=ellipse}, level sep=2cm, sibling sep=1.5cm] {")
        |> ignore

    /// Render LL parser derivation tree with stack chain overlay as TikZ.
    /// The full partial derivation tree is rendered, and the stack frontier leaves are marked
    /// with a dashed chain and a same-layer constraint. Node IDs follow the same pre-order
    /// numbering as DerivationTreeDot.toDotWithLLStack.
    let toTikzWithLLStack
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (tree: DerivationTree<'t, 'nt>)
        (stack: LLStackLeaf<'t, 'nt> list)
        : string =
        let sb = StringBuilder()

        tikzHeader sb

        let mutable nodeId = 0

        let nextId () =
            nodeId <- nodeId + 1
            nodeId

        let pathToId = System.Collections.Generic.Dictionary<int list, int>()

        let rec renderTree (node: DerivationTree<'t, 'nt>) (path: int list) : int =
            let nid = nextId ()
            pathToId.[path] <- nid

            match node with
            | Leaf sym ->
                let label = symbolVisualizer sym
                sb.AppendLine(sprintf "    n%d [as={$%s$}, rectangle];" nid label) |> ignore

            | Node(nt, children) ->
                let label = symbolVisualizer (Symbol.N nt)
                sb.AppendLine(sprintf "    n%d [as={$%s$}];" nid label) |> ignore

                for i, child in List.indexed children do
                    let childId = renderTree child (path @ [ i ])
                    sb.AppendLine(sprintf "    n%d -> n%d;" nid childId) |> ignore

            nid

        renderTree tree [] |> ignore

        let stackIds =
            stack
            |> List.choose (fun leaf ->
                match pathToId.TryGetValue(leaf.Path) with
                | true, id -> Some id
                | false, _ -> None)
            |> List.rev

        for i in 0 .. stackIds.Length - 2 do
            sb.AppendLine(sprintf "    n%d ->[dashed] n%d;" stackIds.[i] stackIds.[i + 1])
            |> ignore

        if not (List.isEmpty stackIds) then
            let idList = stackIds |> List.map (sprintf "n%d") |> String.concat ", "
            sb.AppendLine(sprintf "    { [same layer] %s };" idList) |> ignore

        AutomatonTikz.tikzFooter sb
        sb.ToString()

    /// Render LR parser stack frames as a combined stack-tree TikZ picture.
    /// LRSymbol frames render as derivation tree nodes (subtrees fully expanded).
    /// LRState frames render as gray "sN" boxes. All frames are constrained to the same layer.
    /// The dashed chain is emitted in reversed stack order (bottom to top): graphdrawing's
    /// crossing minimization places the frames left-to-right in stack order only with this
    /// edge direction (verified against the DOT layout baseline).
    let toTikzWithLRStack (symbolVisualizer: Symbol<'t, 'nt> -> string) (stack: LRStackFrame<'t, 'nt> list) : string =
        let sb = StringBuilder()

        tikzHeader sb

        let mutable nodeId = 0

        let nextId () =
            nodeId <- nodeId + 1
            nodeId

        let rec renderTree (node: DerivationTree<'t, 'nt>) : int =
            let nid = nextId ()

            match node with
            | Leaf sym ->
                let label = symbolVisualizer sym
                sb.AppendLine(sprintf "    n%d [as={$%s$}, rectangle];" nid label) |> ignore

            | Node(nt, children) ->
                let label = symbolVisualizer (Symbol.N nt)
                sb.AppendLine(sprintf "    n%d [as={$%s$}];" nid label) |> ignore

                for child in children do
                    let childId = renderTree child
                    sb.AppendLine(sprintf "    n%d -> n%d;" nid childId) |> ignore

            nid

        let mutable stackIds: int list = []

        for frame in stack do
            let nid =
                match frame with
                | LRSymbol st -> renderTree st
                | LRState s ->
                    let nid = nextId ()

                    sb.AppendLine(sprintf "    n%d [as={s%d}, rectangle, fill=lightgray];" nid s)
                    |> ignore

                    nid

            stackIds <- nid :: stackIds

        stackIds <- List.rev stackIds

        let reversed = List.rev stackIds

        for i in 0 .. reversed.Length - 2 do
            sb.AppendLine(sprintf "    n%d ->[dashed] n%d;" reversed.[i] reversed.[i + 1])
            |> ignore

        if not (List.isEmpty stackIds) then
            let idList = stackIds |> List.map (sprintf "n%d") |> String.concat ", "
            sb.AppendLine(sprintf "    { [same layer] %s };" idList) |> ignore

        AutomatonTikz.tikzFooter sb
        sb.ToString()
