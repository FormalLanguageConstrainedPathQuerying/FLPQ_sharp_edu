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
        /// Production internal node: stores rule index and split point.
        /// Parent is a single nonterminal node; children are RHS elements.
        | Production of ruleIndex: int * splitPoint: int

    /// A basic Shared Packed Parse Forest.
    /// Wraps a Graph with node types and a root vertex index.
    /// Edges are unlabeled: NT→Production and Production→Child are implicit.
    type BasicSPPF<'t, 'nt when 't: comparison and 'nt: comparison> =
        { Graph: Graph<BasicSppfNodeInfo<'t, 'nt>, bool>
          RootIndex: int }

    /// Construct a basic SPPF from a vertex list and edge list.
    let fromEdges
        (vertices: BasicSppfNodeInfo<'t, 'nt> list)
        (edges: (int * int) list)
        (rootIdx: int)
        : BasicSPPF<'t, 'nt> =
        let n = vertices.Length
        let edgeMatrix = Matrix.init n n false

        for (fromIdx, toIdx) in edges do
            edgeMatrix.[fromIdx, toIdx] <- true

        { Graph = Graph.fromEdges vertices edgeMatrix
          RootIndex = rootIdx }

    /// Build a BasicSPPF from an enriched parsing table and CNF grammar.
    /// Only builds the SPPF for the start nonterminal in cell (0, n-1).
    /// Each cell entry (nt, k, prodIdx) creates Nonterminal and Production nodes.
    /// Terminal rules produce Terminal children; binary rules link to child Nonterminals.
    /// Returns root Nonterminal(start, 0, n) for input of length n.
    let fromParsingTable (cnf: Grammar<'t, 'nt>) (table: SppfParsingTable<'nt>) : BasicSPPF<'t, 'nt> =
        let n = Matrix.rows table

        if n = 0 then
            fromEdges [] [] 0
        else
            let startEntries =
                table.[0, n - 1] |> Set.filter (fun entry -> entry.Nt = cnf.Start)

            if Set.isEmpty startEntries then
                fromEdges [] [] 0
            else
                let nodeMap = Dictionary<BasicSppfNodeInfo<'t, 'nt>, int>()
                let vertices = ResizeArray<BasicSppfNodeInfo<'t, 'nt>>()
                let edges = ResizeArray<int * int>()
                let ruleMap = Grammar.productionNumberMap cnf

                let getOrCreate (info: BasicSppfNodeInfo<'t, 'nt>) : int =
                    match nodeMap.TryGetValue(info) with
                    | true, idx -> idx
                    | false, _ ->
                        let idx = vertices.Count
                        nodeMap.[info] <- idx
                        vertices.Add(info)
                        idx

                let processed = HashSet<(int * int) * Nonterminal<'nt>>()

                let rec processNonterminalCell (i: int) (j: int) (targetNt: Nonterminal<'nt>) : unit =
                    let key = ((i, j), targetNt)

                    if processed.Contains(key) then
                        ()
                    else
                        processed.Add(key) |> ignore

                        let entries = table.[i, j] |> Set.filter (fun entry -> entry.Nt = targetNt)

                        for entry in entries do
                            let ntNode = getOrCreate (BasicSppfNodeInfo.Nonterminal(entry.Nt, i, j + 1))

                            let rule = Map.find entry.ProdIdx ruleMap

                            let splitPoint =
                                match Rhs.toNonEpsilonList rule.Rhs with
                                | [ Symbol.N _; Symbol.N _ ] -> entry.SplitPoint + 1
                                | _ -> entry.SplitPoint

                            let prodNode = vertices.Count
                            vertices.Add(BasicSppfNodeInfo.Production(entry.ProdIdx, splitPoint))

                            edges.Add(ntNode, prodNode)

                            match Rhs.toNonEpsilonList rule.Rhs with
                            | [ Symbol.T(Terminal t) ] ->
                                let termNode =
                                    getOrCreate (
                                        BasicSppfNodeInfo.Terminal(Terminal t, entry.SplitPoint, entry.SplitPoint + 1)
                                    )

                                edges.Add(prodNode, termNode)
                            | [ Symbol.N leftNt; Symbol.N rightNt ] ->
                                if table.[i, entry.SplitPoint] |> Set.exists (fun e -> e.Nt = leftNt) then
                                    processNonterminalCell i entry.SplitPoint leftNt

                                    let leftNode =
                                        getOrCreate (BasicSppfNodeInfo.Nonterminal(leftNt, i, entry.SplitPoint + 1))

                                    edges.Add(prodNode, leftNode)

                                if table.[entry.SplitPoint + 1, j] |> Set.exists (fun e -> e.Nt = rightNt) then
                                    processNonterminalCell (entry.SplitPoint + 1) j rightNt

                                    let rightNode =
                                        getOrCreate (
                                            BasicSppfNodeInfo.Nonterminal(rightNt, entry.SplitPoint + 1, j + 1)
                                        )

                                    edges.Add(prodNode, rightNode)
                            | _ -> ()

                let mutable rootIdx = -1

                for entry in startEntries do
                    let ntNode = getOrCreate (BasicSppfNodeInfo.Nonterminal(entry.Nt, 0, n))
                    rootIdx <- ntNode
                    processNonterminalCell 0 (n - 1) cnf.Start

                if rootIdx < 0 then
                    fromEdges [] [] 0
                else
                    fromEdges (List.ofSeq vertices) (List.ofSeq edges) rootIdx

    let private getChildIndices (graph: Graph<BasicSppfNodeInfo<'t, 'nt>, bool>) (prodIdx: int) : int list =
        [ 0 .. Graph.vertexCount graph - 1 ]
        |> List.choose (fun j -> if graph.Edges.[prodIdx, j] then Some j else None)

    let private getProductionIndices (graph: Graph<BasicSppfNodeInfo<'t, 'nt>, bool>) (ntIdx: int) : int list =
        [ 0 .. Graph.vertexCount graph - 1 ]
        |> List.choose (fun j -> if graph.Edges.[ntIdx, j] then Some j else None)

    let private extractSingle
        (graph: Graph<BasicSppfNodeInfo<'t, 'nt>, bool>)
        (nodeIdx: int)
        : DerivationTree<'t, 'nt> list =
        let visited = System.Collections.Generic.HashSet<int>()

        let rec extractLoop (idx: int) : DerivationTree<'t, 'nt> list =
            if not (visited.Add(idx)) then
                []
            else
                let info = Graph.getVertex idx graph

                match info with
                | BasicSppfNodeInfo.Terminal(Terminal t, _, _) -> [ Leaf(Symbol.T(Terminal t)) ]
                | BasicSppfNodeInfo.Epsilon _ -> [ Leaf Symbol.Epsilon ]
                | BasicSppfNodeInfo.Nonterminal _ ->
                    let prodIndices = getProductionIndices graph idx

                    match prodIndices with
                    | [] -> []
                    | firstProd :: _ -> getChildIndices graph firstProd |> List.collect extractLoop
                | BasicSppfNodeInfo.Production _ -> getChildIndices graph idx |> List.collect extractLoop

        extractLoop nodeIdx

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
        | BasicSppfNodeInfo.Production _ ->
            let children = extractSingle sppf.Graph sppf.RootIndex
            List.head children

    let enumerateTrees (sppf: BasicSPPF<'t, 'nt>) : seq<DerivationTree<'t, 'nt>> =
        let graph = sppf.Graph

        let rec extractAll (visited: HashSet<int>) (nodeIdx: int) : DerivationTree<'t, 'nt> list list =
            if not (visited.Add(nodeIdx)) then
                [ [] ]
            else
                let info = Graph.getVertex nodeIdx graph

                match info with
                | BasicSppfNodeInfo.Terminal(Terminal t, _, _) -> [ [ Leaf(Symbol.T(Terminal t)) ] ]
                | BasicSppfNodeInfo.Epsilon _ -> [ [ Leaf Symbol.Epsilon ] ]
                | BasicSppfNodeInfo.Nonterminal _ ->
                    let prodIndices = getProductionIndices graph nodeIdx

                    match prodIndices with
                    | [] -> [ [] ]
                    | _ ->
                        prodIndices
                        |> List.collect (fun prodIdx ->
                            let branchVisited = HashSet<int>(visited)
                            getChildIndices graph prodIdx |> combineChildren branchVisited)
                | BasicSppfNodeInfo.Production _ -> getChildIndices graph nodeIdx |> combineChildren visited

        and combineChildren (visited: HashSet<int>) (childIndices: int list) : DerivationTree<'t, 'nt> list list =
            match childIndices with
            | [] -> [ [] ]
            | [ single ] -> extractAll visited single
            | first :: rest ->
                let firstResults = extractAll visited first
                let restResults = rest |> List.map (extractAll visited)

                firstResults
                |> List.collect (fun firstTrees ->
                    combineLists restResults |> List.map (fun restTrees -> firstTrees @ restTrees))

        and combineLists (lists: DerivationTree<'t, 'nt> list list list) : DerivationTree<'t, 'nt> list list =
            match lists with
            | [] -> [ [] ]
            | [ l ] -> l
            | first :: rest -> first |> List.collect (fun a -> combineLists rest |> List.map (fun b -> a @ b))

        let rootInfo = Graph.getVertex sppf.RootIndex graph
        let trees = extractAll (HashSet<int>()) sppf.RootIndex

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

    let validateProductionChildren (sppf: BasicSPPF<'t, 'nt>) (cnf: Grammar<'t, 'nt>) : Result<unit, string list> =
        let n = Graph.vertexCount sppf.Graph
        let errors = ResizeArray<string>()
        let ruleMap = Grammar.productionNumberMap cnf

        for v in 0 .. n - 1 do
            match Graph.getVertex v sppf.Graph with
            | BasicSppfNodeInfo.Production(ruleIndex, splitPoint) ->
                let childCount =
                    [ 0 .. n - 1 ] |> List.filter (fun w -> sppf.Graph.Edges.[v, w]) |> List.length

                if childCount < 1 || childCount > 2 then
                    errors.Add(
                        $"Production node v={v} ruleIdx={ruleIndex} split={splitPoint}: "
                        + $"has {childCount} children, expected 1 or 2"
                    )
                else
                    match Map.tryFind ruleIndex ruleMap with
                    | None ->
                        errors.Add(
                            $"Production node v={v}: ruleIndex={ruleIndex} not found (valid numbers are 1..{List.length cnf.Rules})"
                        )
                    | Some rule ->
                        let rhsLen = Rhs.toNonEpsilonList rule.Rhs |> List.length

                        if childCount <> rhsLen then
                            errors.Add(
                                $"Production node v={v} ruleIdx={ruleIndex} ({rule.Lhs} -> {rule.Rhs}): "
                                + $"has {childCount} children, RHS length is {rhsLen}"
                            )
            | _ -> ()

        if errors.Count = 0 then Ok() else Error(List.ofSeq errors)

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
                if sppf.Graph.Edges.[v, w] then
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

    let countNonTrivialScc (sppf: BasicSPPF<'t, 'nt>) : int =
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
            let stackPos = stack.Count
            stack.Add(v)
            onStack.[v] <- true

            for w in 0 .. n - 1 do
                if sppf.Graph.Edges.[v, w] then
                    if indices.[w] = -1 then
                        strongconnect w
                        lowlink.[v] <- min lowlink.[v] lowlink.[w]
                    elif onStack.[w] then
                        lowlink.[v] <- min lowlink.[v] indices.[w]

            if lowlink.[v] = indices.[v] then
                let componentSize = stack.Count - stackPos

                if componentSize > 1 then
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

    let traverseAndCompare (a: BasicSPPF<'t, 'nt>) (b: BasicSPPF<'t, 'nt>) : bool =
        let n = Graph.vertexCount a.Graph

        if n <> Graph.vertexCount b.Graph then
            false
        else
            let visited = HashSet<int * int>()

            let rec go (va: int) (vb: int) : bool =
                if visited.Contains(va, vb) then
                    true
                else
                    visited.Add(va, vb) |> ignore

                    let infoA = Graph.getVertex va a.Graph
                    let infoB = Graph.getVertex vb b.Graph

                    if infoA <> infoB then
                        false
                    else
                        let childrenA =
                            [ for w in 0 .. n - 1 do
                                  if a.Graph.Edges.[va, w] then
                                      yield w ]

                        let childrenB =
                            [ for w in 0 .. n - 1 do
                                  if b.Graph.Edges.[vb, w] then
                                      yield w ]

                        if childrenA.Length <> childrenB.Length then
                            false
                        else
                            let sortedA = childrenA |> List.sortBy (fun w -> Graph.getVertex w a.Graph)
                            let sortedB = childrenB |> List.sortBy (fun w -> Graph.getVertex w b.Graph)

                            (sortedA, sortedB)
                            ||> List.forall2 (fun ca cb ->
                                if Graph.getVertex ca a.Graph <> Graph.getVertex cb b.Graph then
                                    false
                                else
                                    go ca cb)

            go a.RootIndex b.RootIndex

    let private leftPos (info: BasicSppfNodeInfo<'t, 'nt>) : int =
        match info with
        | BasicSppfNodeInfo.Terminal(_, l, _) -> l
        | BasicSppfNodeInfo.Nonterminal(_, l, _) -> l
        | BasicSppfNodeInfo.Epsilon p -> p
        | BasicSppfNodeInfo.Production _ -> failwith "Production in leftPos"

    let private rightPos (info: BasicSppfNodeInfo<'t, 'nt>) : int =
        match info with
        | BasicSppfNodeInfo.Terminal(_, _, r) -> r
        | BasicSppfNodeInfo.Nonterminal(_, _, r) -> r
        | BasicSppfNodeInfo.Epsilon p -> p
        | BasicSppfNodeInfo.Production _ -> failwith "Production in rightPos"

    let validateProductionSplitConsistency (sppf: BasicSPPF<'t, 'nt>) : Result<unit, string list> =
        let n = Graph.vertexCount sppf.Graph
        let errors = ResizeArray<string>()

        for v in 0 .. n - 1 do
            match Graph.getVertex v sppf.Graph with
            | BasicSppfNodeInfo.Production(_, splitPoint) ->

                let children =
                    [ for w in 0 .. n - 1 do
                          if sppf.Graph.Edges.[v, w] then
                              yield w ]

                match children with
                | [ c1; c2 ] ->
                    let info1 = Graph.getVertex c1 sppf.Graph
                    let info2 = Graph.getVertex c2 sppf.Graph

                    let leftChild, rightChild =
                        if leftPos info1 <= leftPos info2 then
                            (info1, info2)
                        else
                            (info2, info1)

                    if rightPos leftChild <> splitPoint then
                        errors.Add(
                            $"Production v={v} split={splitPoint}: left child {leftChild} rightPos={rightPos leftChild}"
                        )

                    if leftPos rightChild <> splitPoint then
                        errors.Add(
                            $"Production v={v} split={splitPoint}: right child {rightChild} leftPos={leftPos rightChild}"
                        )
                | _ -> ()
            | _ -> ()

        if errors.Count = 0 then Ok() else Error(List.ofSeq errors)

    let validateSingleRoot
        (sppf: BasicSPPF<'t, 'nt>)
        (startNt: Nonterminal<'nt>)
        (inputLen: int)
        : Result<unit, string list> =
        let n = Graph.vertexCount sppf.Graph
        let incomingCounts = Array.create n 0

        for i in 0 .. n - 1 do
            for j in 0 .. n - 1 do
                if sppf.Graph.Edges.[i, j] then
                    incomingCounts.[j] <- incomingCounts.[j] + 1

        let noIncoming =
            [ for v in 0 .. n - 1 do
                  if incomingCounts.[v] = 0 then
                      yield v ]

        match noIncoming with
        | [ rootIdx ] ->
            match Graph.getVertex rootIdx sppf.Graph with
            | BasicSppfNodeInfo.Nonterminal(nt, 0, rightPosVal) when nt = startNt && rightPosVal = inputLen -> Ok()
            | info ->
                Error
                    [ $"Single root vertex {rootIdx} has info {info}, expected Nonterminal({startNt}, 0, {inputLen})" ]
        | roots -> Error [ $"Expected 1 vertex without incoming edges, found {List.length roots}: {roots}" ]
