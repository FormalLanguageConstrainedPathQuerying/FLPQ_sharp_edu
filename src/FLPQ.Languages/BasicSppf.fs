namespace FLPQ.Languages

/// Basic (Rekers-style) SPPF built directly from BNF productions with numbered rules.
/// Book reference: def:basicSPPF.
/// Simpler than RSM-based SPPF: only 2 structural node types (symbol + production),
/// matching the classical derivation tree structure.
module BasicSppf =

    open System.Collections.Generic
    open FLPQ.GraphAnalysis
    open FLPQ.LinearAlgebra

    /// Node information in a basic SPPF.
    /// Each variant represents a distinct parse forest element with position tracking.
    [<RequireQualifiedAccess>]
    type BasicSppfNodeInfo<'t, 'nt when 't: comparison and 'nt: comparison> =
        /// Terminal leaf node: matched terminal at span [leftPos, rightPos).
        | Terminal of Terminal<'t> * leftPos: int * rightPos: int
        /// Nonterminal internal node: one per unique (nonterminal, leftPos, rightPos).
        /// May have multiple production children representing packed alternatives.
        | Nonterminal of Nonterminal<'nt> * leftPos: int * rightPos: int
        /// Epsilon leaf node: empty derivation at position pos.
        | Epsilon of pos: int
        /// Production internal node: stores rule index and span [leftPos, rightPos).
        /// Parent is a single nonterminal node; children are RHS elements with split points.
        | Production of ruleIndex: int * leftPos: int * rightPos: int

    /// Edge labels in a basic SPPF.
    [<RequireQualifiedAccess>]
    type BasicSppfEdgeLabel =
        /// Nonterminal -> Production (derivation step).
        | Derives
        /// Production -> Child node, with position index in the RHS.
        | ChildOf of positionInRhs: int

    /// A basic Shared Packed Parse Forest.
    /// Wraps a Graph with node/edge types and a root vertex index.
    type BasicSPPF<'t, 'nt when 't: comparison and 'nt: comparison> =
        { Graph: Graph<BasicSppfNodeInfo<'t, 'nt>, Option<BasicSppfEdgeLabel>>
          RootIndex: int }

    /// Construct a basic SPPF from a vertex list and edge list.
    let fromEdges
        (vertices: BasicSppfNodeInfo<'t, 'nt> list)
        (edges: (int * BasicSppfEdgeLabel * int) list)
        (rootIdx: int)
        : BasicSPPF<'t, 'nt> =
        let n = vertices.Length
        let edgeMatrix = Matrix.init n n None

        for (fromIdx, label, toIdx) in edges do
            edgeMatrix.[fromIdx, toIdx] <- Some label

        { Graph = Graph.fromEdges vertices edgeMatrix
          RootIndex = rootIdx }

    /// Build a BasicSPPF from an enriched parsing table and CNF grammar.
    /// Each cell entry (nt, k, prodIdx) creates Nonterminal and Production nodes.
    /// Terminal rules produce Terminal children; binary rules link to child Nonterminals.
    /// Returns root Nonterminal(start, 0, n) for input of length n.
    let fromParsingTable (cnf: Grammar<'t, 'nt>) (table: SppfParsingTable<'nt>) : BasicSPPF<'t, 'nt> =
        let n = Matrix.rows table
        let nodeMap = Dictionary<BasicSppfNodeInfo<'t, 'nt>, int>()
        let vertices = ResizeArray<BasicSppfNodeInfo<'t, 'nt>>()
        let edges = ResizeArray<int * BasicSppfEdgeLabel * int>()

        let getOrCreate (info: BasicSppfNodeInfo<'t, 'nt>) : int =
            match nodeMap.TryGetValue(info) with
            | true, idx -> idx
            | false, _ ->
                let idx = vertices.Count
                nodeMap.[info] <- idx
                vertices.Add(info)
                idx

        let childNtInCell (row: int) (col: int) (targetNt: Nonterminal<'nt>) : Nonterminal<'nt> option =
            if row <= col && row >= 0 && col < n then
                table.[row, col]
                |> Set.filter (fun (nt, _, _) -> nt = targetNt)
                |> Set.toList
                |> List.tryHead
                |> Option.map (fun (nt, _, _) -> nt)
            else
                None

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                let entries = table.[i, j]

                for (nt, k, prodIdx) in entries do
                    let ntNode = getOrCreate (BasicSppfNodeInfo.Nonterminal(nt, i, j + 1))
                    let prodNode = getOrCreate (BasicSppfNodeInfo.Production(prodIdx, i, j + 1))
                    edges.Add(ntNode, BasicSppfEdgeLabel.Derives, prodNode)

                    let rule = cnf.Rules.[prodIdx]

                    match Rhs.toNonEpsilonList rule.Rhs with
                    | [ Symbol.T(Terminal t) ] ->
                        let termNode = getOrCreate (BasicSppfNodeInfo.Terminal(Terminal t, k, k + 1))
                        edges.Add(prodNode, BasicSppfEdgeLabel.ChildOf 0, termNode)
                    | [ Symbol.N leftNt; Symbol.N rightNt ] ->
                        match childNtInCell i k leftNt with
                        | Some _ ->
                            let leftNode = getOrCreate (BasicSppfNodeInfo.Nonterminal(leftNt, i, k + 1))

                            edges.Add(prodNode, BasicSppfEdgeLabel.ChildOf 0, leftNode)
                        | None -> ()

                        match childNtInCell (k + 1) j rightNt with
                        | Some _ ->
                            let rightNode = getOrCreate (BasicSppfNodeInfo.Nonterminal(rightNt, k + 1, j + 1))

                            edges.Add(prodNode, BasicSppfEdgeLabel.ChildOf 1, rightNode)
                        | None -> ()
                    | _ -> ()

        let rootIdx = getOrCreate (BasicSppfNodeInfo.Nonterminal(cnf.Start, 0, n))

        fromEdges (List.ofSeq vertices) (List.ofSeq edges) rootIdx

    /// Get child indices of a production node (via ChildOf edges).
    let private getChildIndices
        (graph: Graph<BasicSppfNodeInfo<'t, 'nt>, Option<BasicSppfEdgeLabel>>)
        (prodIdx: int)
        : int list =
        [ 0 .. Graph.vertexCount graph - 1 ]
        |> List.choose (fun j ->
            match graph.Edges.[prodIdx, j] with
            | Some(BasicSppfEdgeLabel.ChildOf _) -> Some j
            | _ -> None)

    /// Get production child indices of a nonterminal node (via Derives edges).
    let private getProductionIndices
        (graph: Graph<BasicSppfNodeInfo<'t, 'nt>, Option<BasicSppfEdgeLabel>>)
        (ntIdx: int)
        : int list =
        [ 0 .. Graph.vertexCount graph - 1 ]
        |> List.choose (fun j ->
            match graph.Edges.[ntIdx, j] with
            | Some BasicSppfEdgeLabel.Derives -> Some j
            | _ -> None)

    /// Extract a single derivation tree from a basic SPPF starting at the given vertex index.
    /// For Nonterminal nodes: follows first Derives edge to Production, extracts all children.
    /// For Production nodes: extracts and concatenates all ChildOf children.
    /// Returns a list of trees (children to be wrapped by parent Nonterminal).
    let rec private extractSingle
        (graph: Graph<BasicSppfNodeInfo<'t, 'nt>, Option<BasicSppfEdgeLabel>>)
        (nodeIdx: int)
        : DerivationTree<'t, 'nt> list =
        let info = Graph.getVertex nodeIdx graph

        match info with
        | BasicSppfNodeInfo.Terminal(Terminal t, _, _) -> [ Leaf(Symbol.T(Terminal t)) ]
        | BasicSppfNodeInfo.Epsilon _ -> [ Leaf Symbol.Epsilon ]
        | BasicSppfNodeInfo.Nonterminal(_, _, _) ->
            let prodIndices = getProductionIndices graph nodeIdx

            match prodIndices with
            | [] -> []
            | firstProd :: _ -> getChildIndices graph firstProd |> List.collect (extractSingle graph)
        | BasicSppfNodeInfo.Production(_, _, _) -> getChildIndices graph nodeIdx |> List.collect (extractSingle graph)

    /// Extract a single derivation tree from the root of a basic SPPF.
    let extractDerivationTree (sppf: BasicSPPF<'t, 'nt>) : DerivationTree<'t, 'nt> =
        let info = Graph.getVertex sppf.RootIndex sppf.Graph

        match info with
        | BasicSppfNodeInfo.Nonterminal(Nonterminal nt, _, _) ->
            let children = extractSingle sppf.Graph sppf.RootIndex

            if children.IsEmpty then
                Leaf(Symbol.N(Nonterminal nt))
            else
                Node(Nonterminal nt, children)
        | BasicSppfNodeInfo.Terminal(Terminal t, _, _) -> Leaf(Symbol.T(Terminal t))
        | BasicSppfNodeInfo.Epsilon _ -> Leaf Symbol.Epsilon
        | BasicSppfNodeInfo.Production(_, _, _) ->
            let children = extractSingle sppf.Graph sppf.RootIndex
            List.head children

    /// Enumerate all derivation trees from a basic SPPF.
    /// Returns a lazy sequence of all distinct trees.
    let enumerateTrees (sppf: BasicSPPF<'t, 'nt>) : seq<DerivationTree<'t, 'nt>> =
        let graph = sppf.Graph

        let rec extractAll (nodeIdx: int) : DerivationTree<'t, 'nt> list list =
            let info = Graph.getVertex nodeIdx graph

            match info with
            | BasicSppfNodeInfo.Terminal(Terminal t, _, _) -> [ [ Leaf(Symbol.T(Terminal t)) ] ]
            | BasicSppfNodeInfo.Epsilon _ -> [ [ Leaf Symbol.Epsilon ] ]
            | BasicSppfNodeInfo.Nonterminal(_, _, _) ->
                let prodIndices = getProductionIndices graph nodeIdx

                match prodIndices with
                | [] -> [ [] ]
                | _ ->
                    prodIndices
                    |> List.collect (fun prodIdx -> getChildIndices graph prodIdx |> combineChildren)
            | BasicSppfNodeInfo.Production(_, _, _) -> getChildIndices graph nodeIdx |> combineChildren

        and combineChildren (childIndices: int list) : DerivationTree<'t, 'nt> list list =
            match childIndices with
            | [] -> [ [] ]
            | [ single ] -> extractAll single
            | first :: rest ->
                let firstResults = extractAll first
                let restResults = rest |> List.map extractAll

                firstResults
                |> List.collect (fun firstTrees ->
                    combineLists restResults |> List.map (fun restTrees -> firstTrees @ restTrees))

        and combineLists (lists: DerivationTree<'t, 'nt> list list list) : DerivationTree<'t, 'nt> list list =
            match lists with
            | [] -> [ [] ]
            | [ l ] -> l
            | first :: rest -> first |> List.collect (fun a -> combineLists rest |> List.map (fun b -> a @ b))

        let rootInfo = Graph.getVertex sppf.RootIndex graph
        let trees = extractAll sppf.RootIndex

        match rootInfo with
        | BasicSppfNodeInfo.Nonterminal(Nonterminal nt, _, _) ->
            seq {
                for children in trees do
                    if children.IsEmpty then
                        Leaf(Symbol.N(Nonterminal nt))
                    else
                        Node(Nonterminal nt, children)
            }
        | _ ->
            seq {
                for children in trees ->
                    match children with
                    | [ t ] -> t
                    | [] -> Leaf Symbol.Epsilon
                    | _ -> List.head children
            }

    /// Count strongly connected components in the SPPF graph using Tarjan's algorithm.
    /// Treats the SPPF as a directed graph ignoring edge labels.
    let countScc (sppf: BasicSPPF<'t, 'nt>) : int =
        let n = Graph.vertexCount sppf.Graph
        let mutable sccIndex = 0
        let indices = Array.create n -1
        let lowlink = Array.create n -1
        let onStack = Array.create n false
        let stack = ResizeArray<int>()
        let mutable sccCount = 0

        let rec strongconnect (v: int) : unit =
            indices.[v] <- sccIndex
            lowlink.[v] <- sccIndex
            sccIndex <- sccIndex + 1
            stack.Add(v)
            onStack.[v] <- true

            for w in 0 .. n - 1 do
                if sppf.Graph.Edges.[v, w].IsSome then
                    if indices.[w] = -1 then
                        strongconnect w
                        lowlink.[v] <- min lowlink.[v] lowlink.[w]
                    elif onStack.[w] then
                        lowlink.[v] <- min lowlink.[v] indices.[w]

            if lowlink.[v] = indices.[v] then
                sccCount <- sccCount + 1

                let mutable w = -1

                while w <> v do
                    w <- stack.[stack.Count - 1]
                    stack.RemoveAt(stack.Count - 1)
                    onStack.[w] <- false

        for v in 0 .. n - 1 do
            if indices.[v] = -1 then
                strongconnect v

        sccCount
