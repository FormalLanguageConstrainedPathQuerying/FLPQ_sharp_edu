namespace FLPQ.Languages

/// Basic (Rekers-style) SPPF built directly from BNF productions with numbered rules.
/// Book reference: def:basicSPPF.
/// Simpler than RSM-based SPPF: only 2 structural node types (symbol + production),
/// matching the classical derivation tree structure.
module BasicSppf =

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
