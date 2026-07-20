namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

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

    /// Collects the set of currently active GSS vertices and edges.
    /// A vertex is active if it has any outgoing edges.
    let private collectActiveGss (gss: GSS) : Set<int> * Set<int * int> =
        let n = gss.Graph.VertexMap.Count
        let mutable vertices = Set.empty<int>
        let mutable edges = Set.empty<int * int>

        for fromIdx in 0 .. n - 1 do
            for toIdx in 0 .. n - 1 do
                match Matrix.get gss.Graph.Edges fromIdx toIdx with
                | Some _ ->
                    vertices <- Set.add fromIdx vertices
                    edges <- Set.add (fromIdx, toIdx) edges
                | None -> ()

        vertices, edges

    /// Core GLL algorithm shared by buildPathIndex and buildPathIndexWithSteps.
    /// The onStep callback is called at each step collection point with the current state.
    let private buildPathIndexCore
        (ersm: ExtendedRSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        (onStep:
            Queue<Descriptor>
                -> Set<int>
                -> Set<int * int>
                -> Matrix<Set<PathIndexEntry<'t, 'nt>>>
                -> Set<int * int>
                -> int
                -> int option
                -> Descriptor option
                -> Set<Descriptor>
                -> unit)
        : PathIndex<'t, 'nt> =
        let rsm = ersm.ExtendedRsm
        let stateCount = RSM.stateCount rsm
        let vertexCount = Graph.vertexCount inputGraph
        let k = stateCount * vertexCount

        let pathIndex =
            { Matrix = Matrix.init k k Set.empty
              StateCount = stateCount
              VertexCount = vertexCount }

        let stateInfo = rsm.StateInfo
        let blockStart = rsm.BlockStart
        let termTrans = RSM.termTransitions rsm
        let nontermTrans = RSM.nontermTransitions rsm
        let graphEdges = GraphHelpers.collectGraphEdges inputGraph

        let gss = GSS.init stateCount vertexCount

        let queue = Queue<Descriptor>()
        let handled = HashSet<Descriptor>()

        let handledNonEmpty = HashSet<int * int * int>()

        let mutable changedCells = Set.empty<int * int>

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
                changedCells <- Set.add (fromIdx, toIdx) changedCells

        let tryEnqueue (d: Descriptor) =
            match d.MatchedRange with
            | RangeDescriptor.NonEmptyRange _ ->
                if not (handledNonEmpty.Add(d.RsmState, d.Vertex, d.GssIdx)) then
                    ()
                elif handled.Add(d) then
                    queue.Enqueue(d)
            | RangeDescriptor.EmptyRange ->
                if handled.Add(d) then
                    queue.Enqueue(d)

        // Initialize: start from vertex 0 at the original start block's start state
        let startBlock = RSM.startBlock rsm

        let startGlobalState =
            match blockStart.TryGetValue(startBlock.Nonterminal) with
            | true, gs -> gs
            | false, _ -> failwithf "Start block %A not found" startBlock.Nonterminal

        let gssIdx = GSS.linearIndex vertexCount startGlobalState 0

        let desc =
            { RsmState = startGlobalState
              Vertex = 0
              GssIdx = gssIdx
              MatchedRange = RangeDescriptor.EmptyRange }

        tryEnqueue desc

        // Collect initial state step
        let activeVerts, activeEdges = collectActiveGss gss
        onStep queue activeVerts activeEdges pathIndex.Matrix changedCells 0 None None Set.empty

        // Main loop
        let mutable handledSnapshot = Set.empty<Descriptor>

        while queue.Count > 0 do
            let desc = queue.Dequeue()
            let q0 = desc.RsmState
            let v0 = desc.Vertex
            let s0 = desc.GssIdx

            // Case 1: Terminal transitions
            for (Terminal tVal, q1) in termTrans.[q0] do
                for (edgeTerm, v1) in graphEdges.[v0] do
                    if tVal = edgeTerm then
                        addToIndex q0 v0 q1 v1 (PathIndexEntry.PTerminal(Terminal tVal))

                        match desc.MatchedRange with
                        | RangeDescriptor.NonEmptyRange rk ->
                            addToIndex rk.FromState rk.FromVertex q1 v1 (PathIndexEntry.PIntermediate(q0, v0))
                        | RangeDescriptor.EmptyRange -> ()

                        let newRange = extendRange desc.MatchedRange q0 v0 q1 v1

                        let newDesc =
                            { RsmState = q1
                              Vertex = v1
                              GssIdx = s0
                              MatchedRange = newRange }

                        tryEnqueue newDesc

            // Case 2: Nonterminal transitions (calls)
            for (nt, qRet) in nontermTrans.[q0] do
                match blockStart.TryGetValue(nt) with
                | false, _ -> ()
                | true, qNStart ->
                    let gssTarget = GSS.linearIndex vertexCount qNStart v0

                    let edgeInfo: GssEdgeInfo =
                        { ReturnState = qRet
                          PreCallState = q0
                          PreCallVertex = v0
                          MatchedRange = desc.MatchedRange }

                    let storedPops = GSS.addEdge gss gssTarget s0 edgeInfo

                    for storedPop in storedPops do
                        match storedPop with
                        | RangeDescriptor.EmptyRange -> ()
                        | RangeDescriptor.NonEmptyRange popRange ->
                            let vFinal = popRange.ToVertex
                            let qNFinal = popRange.ToState

                            if v0 = vFinal then
                                addToIndex qNStart v0 qNFinal vFinal (PathIndexEntry.PEpsilonNonterminal nt)
                                addToIndex q0 v0 qRet vFinal (PathIndexEntry.PEpsilonNonterminal nt)
                            else
                                addToIndex q0 v0 qRet vFinal (PathIndexEntry.PNonterminal nt)

                            match desc.MatchedRange with
                            | RangeDescriptor.EmptyRange ->
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
                                addToIndex rk.FromState rk.FromVertex qRet vFinal (PathIndexEntry.PIntermediate(q0, v0))

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

                    let callDesc =
                        { RsmState = qNStart
                          Vertex = v0
                          GssIdx = gssTarget
                          MatchedRange = RangeDescriptor.EmptyRange }

                    tryEnqueue callDesc

            // Case 3: Final state (return)
            if stateInfo.[q0].IsFinal then
                let myNt = stateInfo.[q0].BlockNonterminal

                let recognizedRange =
                    match desc.MatchedRange with
                    | RangeDescriptor.EmptyRange ->
                        match blockStart.TryGetValue(myNt) with
                        | true, qNStart ->
                            RangeDescriptor.NonEmptyRange
                                { FromState = qNStart
                                  FromVertex = v0
                                  ToState = q0
                                  ToVertex = v0 }
                        | false, _ -> desc.MatchedRange
                    | RangeDescriptor.NonEmptyRange rk ->
                        match blockStart.TryGetValue(myNt) with
                        | true, qNStart ->
                            RangeDescriptor.NonEmptyRange
                                { rk with
                                    FromState = qNStart
                                    ToState = q0
                                    ToVertex = v0 }
                        | false, _ -> desc.MatchedRange

                let outgoingEdges = GSS.pop gss s0 recognizedRange

                match recognizedRange with
                | RangeDescriptor.EmptyRange -> ()
                | RangeDescriptor.NonEmptyRange recRange ->
                    let qNStart = recRange.FromState
                    let vStart = recRange.FromVertex
                    let qNFinal = recRange.ToState
                    let vFinal = recRange.ToVertex

                    if vStart = vFinal then
                        addToIndex qNStart vStart qNFinal vFinal (PathIndexEntry.PEpsilonNonterminal myNt)

                    for (parentGssIdx, edgeInfo) in outgoingEdges do
                        let qRet = edgeInfo.ReturnState
                        let parentRange = edgeInfo.MatchedRange

                        if vStart = vFinal then
                            addToIndex
                                edgeInfo.PreCallState
                                edgeInfo.PreCallVertex
                                qRet
                                vFinal
                                (PathIndexEntry.PEpsilonNonterminal myNt)
                        else
                            addToIndex
                                edgeInfo.PreCallState
                                edgeInfo.PreCallVertex
                                qRet
                                vFinal
                                (PathIndexEntry.PNonterminal myNt)

                        match parentRange with
                        | RangeDescriptor.EmptyRange ->
                            let callerBlockNt = stateInfo.[qRet].BlockNonterminal

                            let callerBlockStart =
                                match blockStart.TryGetValue(callerBlockNt) with
                                | true, cs -> cs
                                | false, _ -> qNStart

                            let newRange =
                                RangeDescriptor.NonEmptyRange
                                    { FromState = callerBlockStart
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
                                (PathIndexEntry.PIntermediate(edgeInfo.PreCallState, edgeInfo.PreCallVertex))

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

            // Collect step after descriptor processing
            let activeVerts, activeEdges = collectActiveGss gss
            let currentHandled = handled |> Set.ofSeq
            onStep queue activeVerts activeEdges pathIndex.Matrix changedCells v0 (Some s0) (Some desc) currentHandled
            handledSnapshot <- currentHandled
            changedCells <- Set.empty<int * int>

        pathIndex

    /// Builds the path index for the given extended RSM over the input graph.
    /// Uses the extended RSM internally, starting from the S' block which has one
    /// nonterminal transition to the original start — processed as a regular call.
    /// Book reference: sec:CFPQ_GLL, Listing lst:gll_rsm_cfpq.
    let buildPathIndex
        (_freshStart: Nonterminal<'nt>)
        (ersm: ExtendedRSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : PathIndex<'t, 'nt> =
        buildPathIndexCore ersm inputGraph (fun _ _ _ _ _ _ _ _ _ -> ())

    /// Builds the path index and collects step-by-step snapshots of the GLL execution.
    /// Each step captures: descriptors queue, active GSS state, path index snapshot, changed cells, and input position.
    /// Steps are collected at initial state (before main loop) and after each descriptor is processed.
    let buildPathIndexWithSteps
        (freshStart: Nonterminal<'nt>)
        (ersm: ExtendedRSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : PathIndex<'t, 'nt> * GLLParsingStep<'t, 'nt> list =
        let steps = ResizeArray<GLLParsingStep<'t, 'nt>>()
        let mutable prevVertices = Set.empty<int>
        let mutable prevEdges = Set.empty<int * int>
        let mutable prevHandled = Set.empty<Descriptor>

        let onStep
            (q: Queue<Descriptor>)
            (activeVerts: Set<int>)
            (activeEdges: Set<int * int>)
            (piMatrix: Matrix<Set<PathIndexEntry<'t, 'nt>>>)
            (changedCells: Set<int * int>)
            (inputPos: int)
            (currentGssIdx: int option)
            (currentDescriptor: Descriptor option)
            (handledSnapshot: Set<Descriptor>)
            =
            let newVertices = Set.difference activeVerts prevVertices
            let newEdges = Set.difference activeEdges prevEdges
            let newDescriptors = Set.difference handledSnapshot prevHandled

            steps.Add(
                { Queue = q |> Seq.toList
                  ActiveGssVertices = activeVerts
                  ActiveGssEdges = activeEdges
                  NewGssVertices = newVertices
                  NewGssEdges = newEdges
                  PathIndexMatrix = Matrix.copy piMatrix
                  ChangedCells = changedCells
                  InputPosition = inputPos
                  CurrentGssIdx = currentGssIdx
                  CurrentDescriptor = currentDescriptor
                  HandledDescriptors = handledSnapshot
                  NewDescriptors = newDescriptors }
            )

            prevVertices <- activeVerts
            prevEdges <- activeEdges
            prevHandled <- handledSnapshot

        buildPathIndexCore ersm inputGraph onStep
        |> fun pi -> pi, steps.ToArray() |> List.ofArray
