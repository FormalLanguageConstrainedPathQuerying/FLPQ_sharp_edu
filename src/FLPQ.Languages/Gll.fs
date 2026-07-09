namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// Descriptor in the GLL worklist: current RSM state, input graph vertex,
/// current GSS node, and the range matched so far.
/// Book reference: sec:CFPQ_GLL, Listing lst:gll_rsm_cfpq.
[<Struct; CustomEquality; NoComparison>]
type Descriptor =
    { RsmState: int
      Vertex: int
      GssIdx: int
      MatchedRange: RangeDescriptor }

    override this.Equals(obj: obj) =
        match obj with
        | :? Descriptor as other ->
            this.RsmState = other.RsmState
            && this.Vertex = other.Vertex
            && this.GssIdx = other.GssIdx
            && this.MatchedRange = other.MatchedRange
        | _ -> false

    override this.GetHashCode() =
        hash (this.RsmState, this.Vertex, this.GssIdx, this.MatchedRange)

module GLL =

    /// Converts a string to a path graph: vertices 0..|chars| with edges i -[char]-> i+1.
    /// Each character becomes a separate vertex, edges carry the character as label.
    let stringToGraph (chars: 't list) : Graph<int, Option<'t>> =
        let n = chars.Length
        let vertices = [ 0..n ]

        let edges =
            Matrix.init (n + 1) (n + 1) None
            |> fun m ->
                for i in 0 .. n - 1 do
                    Matrix.set m i (i + 1) (Some chars.[i])

                m

        Graph.fromEdges vertices edges

    /// Extends a RangeDescriptor by updating its end to (newState, newVertex).
    /// If the range is EmptyRange, creates a new range from (fromState, fromVertex) to (newState, newVertex).
    let private extendRange
        (range: RangeDescriptor)
        (fromState: int)
        (fromVertex: int)
        (newState: int)
        (newVertex: int)
        : RangeDescriptor =
        match range with
        | RangeDescriptor.EmptyRange ->
            RangeDescriptor.NonEmptyRange
                { FromState = fromState
                  FromVertex = fromVertex
                  ToState = newState
                  ToVertex = newVertex }
        | RangeDescriptor.NonEmptyRange rk ->
            RangeDescriptor.NonEmptyRange
                { rk with
                    ToState = newState
                    ToVertex = newVertex }

    /// Builds the path index for the given RSM over the input graph, starting from the specified vertices.
    /// Book reference: sec:CFPQ_GLL, Listing lst:gll_rsm_cfpq.
    let buildPathIndex
        (rsm: RSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        (startVertices: Set<int>)
        : PathIndex<'t, 'nt> =
        let stateCount = RSM.stateCount rsm
        let vertexCount = Graph.vertexCount inputGraph
        let k = stateCount * vertexCount

        let pathIndex =
            { Matrix = Matrix.init k k Set.empty
              StateCount = stateCount
              VertexCount = vertexCount }

        let flat = RSM.flattenRsm rsm
        let stateInfo = flat.StateInfo
        let blockStart = flat.BlockStart
        let finalStates = flat.FinalStates
        let termTrans = flat.TermTrans
        let nontermTrans = flat.NontermTrans
        let graphEdges = GraphHelpers.collectGraphEdges inputGraph

        let gss = GSS.init stateCount vertexCount

        let queue = Queue<Descriptor>()
        let handled = HashSet<Descriptor>()

        // Helper: add entry at indices, avoiding redundant adds
        let addToIndex
            (fromState: int)
            (fromVertex: int)
            (toState: int)
            (toVertex: int)
            (entry: PathIndexEntry<'t, 'nt>)
            =
            let fromIdx = PathIndex.linearIndex pathIndex fromState fromVertex
            let toIdx = PathIndex.linearIndex pathIndex toState toVertex
            let current = Matrix.get pathIndex.Matrix fromIdx toIdx

            if not (Set.contains entry current) then
                Matrix.set pathIndex.Matrix fromIdx toIdx (Set.add entry current)

        // Helper: try to enqueue a descriptor
        let tryEnqueue (d: Descriptor) =
            if handled.Add(d) then
                queue.Enqueue(d)

        // Initialize: for each start vertex, create descriptor at start block's start state
        let startBlock = RSM.startBlock rsm

        let startGlobalState =
            match blockStart.TryGetValue(startBlock.Nonterminal) with
            | true, gs -> gs
            | false, _ -> failwithf "Start block %A not found" startBlock.Nonterminal

        for vs in startVertices do
            let gssIdx = GSS.linearIndex vertexCount startGlobalState vs

            let desc =
                { RsmState = startGlobalState
                  Vertex = vs
                  GssIdx = gssIdx
                  MatchedRange = RangeDescriptor.EmptyRange }

            tryEnqueue desc

        // Main loop
        while queue.Count > 0 do
            let desc = queue.Dequeue()
            let q0 = desc.RsmState
            let v0 = desc.Vertex
            let s0 = desc.GssIdx
            let MatchedRange = desc.MatchedRange

            // Case 1: Terminal transitions
            for (Terminal tVal, q1) in termTrans.[q0] do
                for (edgeTerm, v1) in graphEdges.[v0] do
                    if tVal = edgeTerm then
                        // Add PTerminal to index
                        addToIndex q0 v0 q1 v1 (PathIndexEntry.PTerminal(Terminal tVal))

                        // Add PIntermediate if we have a non-empty desc.MatchedRange
                        match desc.MatchedRange with
                        | RangeDescriptor.NonEmptyRange rk ->
                            addToIndex rk.FromState rk.FromVertex q1 v1 (PathIndexEntry.PIntermediate(q0, v0))
                        | RangeDescriptor.EmptyRange -> ()

                        // Create new descriptor with extended desc.MatchedRange
                        let newRange = extendRange desc.MatchedRange q0 v0 q1 v1

                        let newDesc =
                            { RsmState = q1
                              Vertex = v1
                              GssIdx = s0
                              MatchedRange = newRange }

                        tryEnqueue newDesc

            // Case 2: Nonterminal transitions (calls)
            for (nt, qRet) in nontermTrans.[q0] do
                // Find start state of the called nonterminal's block
                match blockStart.TryGetValue(nt) with
                | false, _ -> ()
                | true, qNStart ->
                    let gssTarget = GSS.linearIndex vertexCount qNStart v0

                    // Add GSS edge from target (callee's GSS node for N's start) to current (caller's GSS node)
                    let edgeInfo: GssEdgeInfo =
                        { ReturnState = qRet
                          MatchedRange = desc.MatchedRange }

                    let storedPops = GSS.addEdge gss gssTarget s0 edgeInfo

                    // Handle storedPops: these are ranges already recognized at gssTarget
                    for storedPop in storedPops do
                        match storedPop with
                        | RangeDescriptor.EmptyRange -> ()
                        | RangeDescriptor.NonEmptyRange popRange ->
                            // block N matched from (qNStart, v0) to (popRange.ToState, popRange.ToVertex)
                            let vFinal = popRange.ToVertex
                            let qNFinal = popRange.ToState

                            // Add PNonterminal entry
                            addToIndex qNStart v0 qNFinal vFinal (PathIndexEntry.PNonterminal nt)

                            // Add PIntermediate and create continuation
                            match desc.MatchedRange with
                            | RangeDescriptor.EmptyRange ->
                                addToIndex qNStart v0 qRet vFinal (PathIndexEntry.PIntermediate(qNStart, v0))

                                let newRange =
                                    RangeDescriptor.NonEmptyRange
                                        { FromState = qNStart
                                          FromVertex = v0
                                          ToState = qRet
                                          ToVertex = vFinal }

                                let contDesc =
                                    { RsmState = qRet
                                      Vertex = vFinal
                                      GssIdx = s0
                                      MatchedRange = newRange }

                                tryEnqueue contDesc
                            | RangeDescriptor.NonEmptyRange rk ->
                                addToIndex
                                    rk.FromState
                                    rk.FromVertex
                                    qRet
                                    vFinal
                                    (PathIndexEntry.PIntermediate(qNStart, v0))

                                let newRange =
                                    RangeDescriptor.NonEmptyRange
                                        { rk with
                                            ToState = qRet
                                            ToVertex = vFinal }

                                let contDesc =
                                    { RsmState = qRet
                                      Vertex = vFinal
                                      GssIdx = s0
                                      MatchedRange = newRange }

                                tryEnqueue contDesc

                    // Create descriptor for the start state of the called nonterminal
                    let callDesc =
                        { RsmState = qNStart
                          Vertex = v0
                          GssIdx = gssTarget
                          MatchedRange = RangeDescriptor.EmptyRange }

                    tryEnqueue callDesc

            // Case 3: Final state (return)
            if stateInfo.[q0].IsFinal then
                let recognizedRange =
                    match desc.MatchedRange with
                    | RangeDescriptor.EmptyRange ->
                        // Empty desc.MatchedRange means we entered this block at v0 and immediately finished (epsilon-like)
                        // Create a desc.MatchedRange from the block's start state at v0 to the final state at v0
                        let nt = stateInfo.[q0].BlockNonterminal

                        match blockStart.TryGetValue(nt) with
                        | true, qNStart ->
                            RangeDescriptor.NonEmptyRange
                                { FromState = qNStart
                                  FromVertex = v0
                                  ToState = q0
                                  ToVertex = v0 }
                        | false, _ -> desc.MatchedRange
                    | _ -> desc.MatchedRange

                let outgoingEdges = GSS.pop gss s0 recognizedRange

                match recognizedRange with
                | RangeDescriptor.EmptyRange -> ()
                | RangeDescriptor.NonEmptyRange recRange ->
                    let qNStart = recRange.FromState
                    let vStart = recRange.FromVertex
                    let qNFinal = recRange.ToState
                    let vFinal = recRange.ToVertex
                    let nt = stateInfo.[qNStart].BlockNonterminal

                    if List.isEmpty outgoingEdges then
                        addToIndex qNStart vStart qNFinal vFinal (PathIndexEntry.PNonterminal nt)
                    else
                        for (parentGssIdx, edgeInfo) in outgoingEdges do
                            let qRet = edgeInfo.ReturnState
                            let parentRange = edgeInfo.MatchedRange

                            addToIndex qNStart vStart qNFinal vFinal (PathIndexEntry.PNonterminal nt)

                            // Add PIntermediate and create continuation descriptor
                            match parentRange with
                            | RangeDescriptor.EmptyRange ->
                                addToIndex qNStart vStart qRet vFinal (PathIndexEntry.PIntermediate(qNStart, vStart))

                                let newRange =
                                    RangeDescriptor.NonEmptyRange
                                        { FromState = qNStart
                                          FromVertex = vStart
                                          ToState = qRet
                                          ToVertex = vFinal }

                                let contDesc =
                                    { RsmState = qRet
                                      Vertex = vFinal
                                      GssIdx = parentGssIdx
                                      MatchedRange = newRange }

                                tryEnqueue contDesc
                            | RangeDescriptor.NonEmptyRange rk ->
                                addToIndex
                                    rk.FromState
                                    rk.FromVertex
                                    qRet
                                    vFinal
                                    (PathIndexEntry.PIntermediate(qNStart, vStart))

                                let newRange =
                                    RangeDescriptor.NonEmptyRange
                                        { rk with
                                            ToState = qRet
                                            ToVertex = vFinal }

                                let contDesc =
                                    { RsmState = qRet
                                      Vertex = vFinal
                                      GssIdx = parentGssIdx
                                      MatchedRange = newRange }

                                tryEnqueue contDesc

        pathIndex

    /// Checks if the path index contains a path from (fromState, fromVertex) to some final state
    /// at the end of the input graph.
    let isAccepted
        (pathIndex: PathIndex<'t, 'nt>)
        (startGlobalState: int)
        (startVertex: int)
        (finalStates: Set<int>)
        (vertexCount: int)
        : bool =
        // Check if there's any entry from start to a final state at any vertex
        finalStates
        |> Set.exists (fun finalState ->
            let entries =
                PathIndex.get pathIndex startGlobalState startVertex finalState (vertexCount - 1)

            not (Set.isEmpty entries))

    /// Builds the SPPF from a path index starting from the given root ranges.
    /// Top-down traversal with memoization: range nodes are created once and reused for packed alternatives.
    /// Each range is processed exactly once to avoid infinite recursion.
    /// Book reference: sec:CFPQ_GLL.
    let buildSppfFromIndex (pathIndex: PathIndex<'t, 'nt>) (rootRanges: RangeKey list) : SPPF<'t, 'nt> =
        let mutable vertices: SppfNodeInfo<'t, 'nt> list = []
        let mutable edgeList: (int * Option<SppfEdgeLabel> * int) list = []

        let rangeNodeMap = Dictionary<RangeKey, int>()
        let processedRanges = System.Collections.Generic.HashSet<RangeKey>()
        let nodeMap = Dictionary<string, int>()

        let nodeKey (info: SppfNodeInfo<'t, 'nt>) : string =
            match info with
            | SppfNodeInfo.SppfTerminal(Terminal _, l, r) -> $"T({l},{r})"
            | SppfNodeInfo.SppfNonterminal(Nonterminal _, l, r) -> $"N({l},{r})"
            | SppfNodeInfo.SppfEpsilon p -> $"E({p})"
            | SppfNodeInfo.SppfIntermediate(s, p) -> $"I({s},{p})"
            | SppfNodeInfo.SppfRange(fs, fp, ts, tp) -> $"R({fs},{fp},{ts},{tp})"

        let getOrCreateNode (info: SppfNodeInfo<'t, 'nt>) : int =
            let key = nodeKey info

            match nodeMap.TryGetValue(key) with
            | true, idx -> idx
            | false, _ ->
                let idx = vertices.Length
                vertices <- vertices @ [ info ]
                nodeMap.[key] <- idx
                idx

        let getOrCreateRangeNode (fromState: int) (fromPos: int) (toState: int) (toPos: int) : int =
            let rk =
                { FromState = fromState
                  FromVertex = fromPos
                  ToState = toState
                  ToVertex = toPos }

            match rangeNodeMap.TryGetValue(rk) with
            | true, idx -> idx
            | false, _ ->
                let idx = vertices.Length
                let info = SppfNodeInfo.SppfRange(fromState, fromPos, toState, toPos)
                vertices <- vertices @ [ info ]
                rangeNodeMap.[rk] <- idx
                idx

        let addEdge (fromIdx: int) (label: SppfEdgeLabel) (toIdx: int) =
            let lbl = Some label

            if not (List.exists (fun (f, l, t) -> f = fromIdx && l = lbl && t = toIdx) edgeList) then
                edgeList <- (fromIdx, lbl, toIdx) :: edgeList

        let rec processRange (fromState: int) (fromPos: int) (toState: int) (toPos: int) : int =
            let rangeIdx = getOrCreateRangeNode fromState fromPos toState toPos

            let rk =
                { FromState = fromState
                  FromVertex = fromPos
                  ToState = toState
                  ToVertex = toPos }

            if processedRanges.Add(rk) then
                let entries = PathIndex.get pathIndex fromState fromPos toState toPos

                for entry in entries do
                    match entry with
                    | PathIndexEntry.PTerminal t ->
                        let termNode = getOrCreateNode (SppfNodeInfo.SppfTerminal(t, fromPos, toPos))

                        addEdge rangeIdx SppfEdgeLabel.PackedAlternative termNode

                    | PathIndexEntry.PNonterminal nt ->
                        let ntNode = getOrCreateNode (SppfNodeInfo.SppfNonterminal(nt, fromPos, toPos))
                        addEdge rangeIdx SppfEdgeLabel.PackedAlternative ntNode
                        addEdge ntNode SppfEdgeLabel.SingleChild rangeIdx

                    | PathIndexEntry.PIntermediate(state, pos) ->
                        let interNode = getOrCreateNode (SppfNodeInfo.SppfIntermediate(state, pos))
                        let leftChild = processRange fromState fromPos state pos
                        let rightChild = processRange state pos toState toPos

                        addEdge rangeIdx SppfEdgeLabel.PackedAlternative interNode
                        addEdge interNode SppfEdgeLabel.LeftChild leftChild
                        addEdge interNode SppfEdgeLabel.RightChild rightChild

            rangeIdx

        let rootIndices =
            rootRanges
            |> List.map (fun rk ->
                let idx = processRange rk.FromState rk.FromVertex rk.ToState rk.ToVertex
                idx)

        let n = vertices.Length
        let edgeMatrix = Matrix.init n n None

        let sortedEdges = List.rev edgeList

        for (fromIdx, label, toIdx) in sortedEdges do
            Matrix.set edgeMatrix fromIdx toIdx label

        { Graph = Graph.fromEdges vertices edgeMatrix
          RootIndices = rootIndices }

    /// Extracts a single derivation tree from a path index starting from (fromState, fromVertex) to (toState, toVertex).
    /// Works directly with the path index entries, bypassing the SPPF.
    /// For ambiguous grammars, picks the first available derivation.
    /// Book reference: sec:CFPQ_GLL.
    let extractDerivationTree
        (pathIndex: PathIndex<'t, 'nt>)
        (stateInfo: RsmStateInfo<'nt> array)
        (blockStart: System.Collections.Generic.Dictionary<Nonterminal<'nt>, int>)
        (fromState: int)
        (fromVertex: int)
        (toState: int)
        (toVertex: int)
        : DerivationTree<'t, 'nt> =
        let rec extract (fs: int) (fv: int) (ts: int) (tv: int) (depth: int) : DerivationTree<'t, 'nt> option =
            if depth > 100 then
                None
            else
                let entries = PathIndex.get pathIndex fs fv ts tv

                entries
                |> Set.toList
                |> List.tryPick (fun entry ->
                    match entry with
                    | PathIndexEntry.PTerminal(Terminal t) -> Some(Leaf(Symbol.T(Terminal t)))
                    | PathIndexEntry.PNonterminal nt ->
                        let qNStart =
                            match blockStart.TryGetValue(nt) with
                            | true, start -> start
                            | false, _ -> fs

                        let mutable foundTree = None

                        for i in 0 .. pathIndex.StateCount - 1 do
                            if stateInfo.[i].BlockNonterminal = nt && stateInfo.[i].IsFinal && foundTree.IsNone then
                                let innerEntries = PathIndex.get pathIndex qNStart fv i tv

                                if not (Set.isEmpty innerEntries) then
                                    match extract qNStart fv i tv (depth + 1) with
                                    | Some children -> foundTree <- Some(Node(nt, [ children ]))
                                    | None -> ()

                        match foundTree with
                        | Some _ -> foundTree
                        | None -> Some(Node(nt, []))
                    | PathIndexEntry.PIntermediate(state, pos) ->
                        let leftTree = extract fs fv state pos (depth + 1)
                        let rightTree = extract state pos ts tv (depth + 1)

                        match leftTree, rightTree with
                        | Some l, Some r ->
                            let nt = stateInfo.[fs].BlockNonterminal
                            Some(Node(nt, [ l; r ]))
                        | Some t, None -> Some t
                        | None, Some t -> Some t
                        | None, None -> None)

        match extract fromState fromVertex toState toVertex 0 with
        | Some tree -> tree
        | None -> Leaf Symbol.Epsilon

    /// Extracts a single derivation tree from an SPPF root.
    /// For ambiguous grammars, picks the first alternative at each packed node.
    /// Uses a visited set to break cycles in the Nonterminal-SingleChild-Range-PackedAlternative loop.
    /// Book reference: sec:CFPQ_GLL.
    let extractDerivationTreeFromSppf (sppf: SPPF<'t, 'nt>) (rootIdx: int) : DerivationTree<'t, 'nt> =
        let vertexCount = Graph.vertexCount sppf.Graph

        let allEdgesTo (nodeIdx: int) (predicate: Option<SppfEdgeLabel> -> bool) : int list =
            [ for toIdx in 0 .. vertexCount - 1 do
                  let edgeLabel = Matrix.get sppf.Graph.Edges nodeIdx toIdx

                  if predicate edgeLabel then
                      toIdx ]

        let rec edgeTo (nodeIdx: int) (predicate: Option<SppfEdgeLabel> -> bool) : int option =
            let mutable result = None

            for toIdx in 0 .. vertexCount - 1 do
                match result with
                | Some _ -> ()
                | None ->
                    let edgeLabel = Matrix.get sppf.Graph.Edges nodeIdx toIdx

                    if predicate edgeLabel then
                        result <- Some toIdx

            result

        let rec extractChildren
            (visited: System.Collections.Generic.HashSet<int>)
            (nodeIdx: int)
            : DerivationTree<'t, 'nt> list =
            if not (visited.Add(nodeIdx)) then
                []
            else
                let info = Graph.getVertex nodeIdx sppf.Graph

                match info with
                | SppfNodeInfo.SppfTerminal(t, _, _) -> [ Leaf(Symbol.T t) ]
                | SppfNodeInfo.SppfEpsilon _ -> [ Leaf Symbol.Epsilon ]
                | SppfNodeInfo.SppfNonterminal(nt, _, _) ->
                    match edgeTo nodeIdx (fun e -> e = Some SppfEdgeLabel.SingleChild) with
                    | Some rangeIdx ->
                        let alternatives =
                            allEdgesTo rangeIdx (fun e -> e = Some SppfEdgeLabel.PackedAlternative)

                        let child = alternatives |> List.tryFind (fun alt -> alt <> nodeIdx)

                        match child with
                        | Some childIdx ->
                            let children = extractChildren visited childIdx
                            [ Node(nt, children) ]
                        | None -> [ Node(nt, []) ]
                    | None -> [ Node(nt, []) ]
                | SppfNodeInfo.SppfIntermediate _ ->
                    let left =
                        match edgeTo nodeIdx (fun e -> e = Some SppfEdgeLabel.LeftChild) with
                        | Some idx -> extractChildren visited idx
                        | None -> []

                    let right =
                        match edgeTo nodeIdx (fun e -> e = Some SppfEdgeLabel.RightChild) with
                        | Some idx -> extractChildren visited idx
                        | None -> []

                    left @ right
                | SppfNodeInfo.SppfRange _ ->
                    match edgeTo nodeIdx (fun e -> e = Some SppfEdgeLabel.PackedAlternative) with
                    | Some childIdx -> extractChildren visited childIdx
                    | None -> [ Leaf Symbol.Epsilon ]

        extractChildren (System.Collections.Generic.HashSet<int>()) rootIdx
        |> List.tryHead
        |> Option.defaultValue (Leaf Symbol.Epsilon)
