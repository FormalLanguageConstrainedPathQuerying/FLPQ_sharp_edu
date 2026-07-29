namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

module Rnglr =

    let private invertRsmTransitions (block: RsmBlock<'t, 'nt>) : Map<int * RsmSymbol<'t, 'nt>, int list> =
        let n = Dfa.stateCount block.Dfa
        let mutable result = Map.empty

        for fromIdx in 0 .. n - 1 do
            for toIdx in 0 .. n - 1 do
                match block.Dfa.Transitions.[fromIdx, toIdx] with
                | Some labels ->
                    for label in NonEmptySet.toSeq labels do
                        match label with
                        | AutomatonLabel.ATerm(sym) ->
                            let key = (toIdx, sym)
                            let prev = Map.tryFind key result |> Option.defaultValue []
                            result <- Map.add key (fromIdx :: prev) result
                        | _ -> ()
                | None -> ()

        result

    [<Struct>]
    type private InvBlockData<'t, 'nt when 't: comparison and 'nt: comparison> =
        { Nonterminal: Nonterminal<'nt>
          Start: int
          Finals: Set<int>
          InvTrans: Map<int * RsmSymbol<'t, 'nt>, int list>
          GlobalOffset: int }

    let private buildPathIndexCore
        (ersm: ExtendedRSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        (onStep:
            RnglrDescriptor list[]
                -> Set<int>
                -> Set<int * int>
                -> Map<int * int, NonEmptySet<Symbol<'t, 'nt>>>
                -> Matrix<Set<PathIndexEntry<'t, 'nt>>>
                -> Set<int * int>
                -> int
                -> int option
                -> RnglrDescriptor option
                -> Set<RnglrDescriptor>
                -> Set<RnglrDescriptor>
                -> unit)
        : PathIndex<'t, 'nt> =
        let extRsm = ersm.ExtendedRsm
        let lrTable = RnglrLR.buildLR0Table extRsm
        let rsmStateCount = RSM.stateCount extRsm
        let lrStateCount = Dfa.stateCount lrTable.Automaton
        let vertexCount = Graph.vertexCount inputGraph
        let rsmK = rsmStateCount * vertexCount
        let lrK = lrStateCount * vertexCount

        let pathIndex =
            { Matrix = Matrix.init rsmK rsmK Set.empty
              StateCount = rsmStateCount
              VertexCount = vertexCount }

        let gss = RnglrGSS.init lrStateCount vertexCount
        let graphEdges = GraphHelpers.collectGraphEdges inputGraph

        let blocks = RSM.blocks extRsm
        let blockMap = blocks |> List.map (fun b -> (b.Nonterminal, b)) |> Map.ofList

        let blockGlobalOffset =
            let mutable offsetMap = Map.empty
            let mutable offset = 0

            for block in blocks do
                offsetMap <- Map.add block.Nonterminal offset offsetMap
                offset <- offset + Dfa.stateCount block.Dfa

            offsetMap

        let invBlockData =
            blocks
            |> List.map (fun b ->
                let invTrans = invertRsmTransitions b

                let invData =
                    { Nonterminal = b.Nonterminal
                      Start = b.Dfa.StartState
                      Finals = b.Dfa.FinalStates
                      InvTrans = invTrans
                      GlobalOffset = blockGlobalOffset.[b.Nonterminal] }

                (b.Nonterminal, invData))
            |> Map.ofList

        let linearIdx (state: int) (vertex: int) = state * vertexCount + vertex

        let changedCells = ref Set.empty<int * int>

        let addToIndex
            (fromState: int)
            (fromVertex: int)
            (toState: int)
            (toVertex: int)
            (entry: PathIndexEntry<'t, 'nt>)
            =
            PathIndex.addWithTracking pathIndex fromState fromVertex toState toVertex entry changedCells

        let getReduceNtWithStates (lrState: int) : (Nonterminal<'nt> * int) list =
            let items = lrTable.Automaton.States.[lrState]

            items
            |> Set.toList
            |> List.choose (fun item ->
                match Map.tryFind item.BlockNonterminal blockMap with
                | Some block ->
                    if Set.contains item.RsmState block.Dfa.FinalStates then
                        let globalState = blockGlobalOffset.[item.BlockNonterminal] + item.RsmState
                        Some(item.BlockNonterminal, globalState)
                    else
                        None
                | None -> None)

        let toRsmSym (sym: Symbol<'t, 'nt>) : RsmSymbol<'t, 'nt> option =
            match sym with
            | Symbol.T(Terminal t) -> Some(RsmSymbol.RTerm(Terminal t))
            | Symbol.N nt -> Some(RsmSymbol.RNonterm nt)
            | Symbol.Epsilon -> None

        let productBfs
            (invData: InvBlockData<'t, 'nt>)
            (starts: (int * int * int * int) list)
            : (int * int * int * int) list =
            let visited = HashSet<int * int>()
            let startSet = starts |> List.map (fun (g, i, _, _) -> (g, i)) |> set
            let queue = Queue<int * int * int * int>()
            let mutable predecessors = []

            for (gssIdx, invState, endState, endVertex) in starts do
                let p = (gssIdx, invState)

                if visited.Add(p) then
                    queue.Enqueue(gssIdx, invState, endState, endVertex)

            while queue.Count > 0 do
                let (currGss, currInv, endState, endVertex) = queue.Dequeue()
                let currVx = Graph.getVertex currGss gss.GssGraph
                let vCurr = currVx.InputVertex
                let globalCurrInv = invData.GlobalOffset + currInv
                let globalEnd = invData.GlobalOffset + endState

                for (nextGss, sym) in RnglrGSS.outgoingEdges gss currGss do
                    match toRsmSym sym with
                    | Some rSym ->
                        match Map.tryFind (currInv, rSym) invData.InvTrans with
                        | Some nextInvList ->
                            let nextVx = Graph.getVertex nextGss gss.GssGraph
                            let vNext = nextVx.InputVertex

                            for nextInv in nextInvList do
                                let globalNextInv = invData.GlobalOffset + nextInv

                                let addedEntry =
                                    match rSym with
                                    | RsmSymbol.RTerm(Terminal t) ->
                                        addToIndex
                                            globalNextInv
                                            vNext
                                            globalCurrInv
                                            vCurr
                                            (PathIndexEntry.PTerminal(Terminal t))

                                        true
                                    | RsmSymbol.RNonterm nt ->
                                        addToIndex
                                            globalNextInv
                                            vNext
                                            globalCurrInv
                                            vCurr
                                            (PathIndexEntry.PNonterminal nt)

                                        true

                                let isStart = nextInv = invData.Start

                                if addedEntry && (globalCurrInv, vCurr) <> (globalEnd, endVertex) then
                                    addToIndex
                                        globalNextInv
                                        vNext
                                        globalEnd
                                        endVertex
                                        (PathIndexEntry.PIntermediate(globalCurrInv, vCurr))

                                if isStart then
                                    predecessors <- (nextGss, nextVx.LrState, vNext, currInv) :: predecessors

                                let np = (nextGss, nextInv)

                                if visited.Add(np) then
                                    if not (Set.contains np startSet) then
                                        let cur = RnglrGSS.getStoredStates gss nextGss

                                        RnglrGSS.setStoredStates
                                            gss
                                            nextGss
                                            (Set.add (invData.Nonterminal, nextInv, endState, endVertex) cur)

                                    queue.Enqueue(nextGss, nextInv, endState, endVertex)
                        | None -> ()
                    | None -> ()

            predecessors

        let findPredecessors (gssIdx: int) (nt: Nonterminal<'nt>) : (int * int * int * int) list =
            match Map.tryFind nt invBlockData with
            | Some invData ->
                let vx = Graph.getVertex gssIdx gss.GssGraph

                let starts =
                    invData.Finals
                    |> Set.toList
                    |> List.map (fun finalState -> (gssIdx, finalState, finalState, vx.InputVertex))

                let preds = productBfs invData starts

                if invData.Finals |> Set.contains invData.Start && preds.IsEmpty then
                    [ (gssIdx, vx.LrState, vx.InputVertex, invData.GlobalOffset + invData.Start) ]
                else
                    preds
            | None -> []

        let pending = Array.init vertexCount (fun _ -> Queue<RnglrDescriptor>())

        let processedGotos: Set<Nonterminal<'nt> * int> array = Array.create lrK Set.empty

        let rec processReduction
            (reduceNt: Nonterminal<'nt>)
            (finalRsmState: int)
            (lrStatePre: int)
            (gssIdxPre: int)
            (vPre: int)
            (vEnd: int)
            (depth: int)
            : unit =
            match Map.tryFind (lrStatePre, reduceNt) lrTable.Goto with
            | Some gotoTarget ->
                let gotoGssIdx = linearIdx gotoTarget vEnd
                let dedupKey = (reduceNt, gssIdxPre)
                let existing = processedGotos.[gotoGssIdx]

                let isNew = not (Set.contains dedupKey existing)

                if isNew then
                    processedGotos.[gotoGssIdx] <- Set.add dedupKey existing
                    RnglrGSS.addEdge gss gotoGssIdx gssIdxPre (Symbol.N reduceNt) |> ignore

                match invBlockData.TryGetValue(reduceNt) with
                | true, invData ->
                    let globalStart = invData.GlobalOffset + invData.Start

                    if vPre = vEnd && finalRsmState = globalStart then
                        addToIndex globalStart vPre finalRsmState vEnd (PathIndexEntry.PEpsilonNonterminal reduceNt)
                | false, _ -> ()

                if isNew then
                    processNode gotoTarget vEnd (depth + 1)
            | None -> ()

        and processNode (lrState: int) (v: int) (depth: int) : unit =
            if depth > 1000 then
                failwith "Reduction cascade depth exceeded"

            for (reduceNt, finalRsmState) in getReduceNtWithStates lrState do
                let gssIdx = linearIdx lrState v
                let predecessors = findPredecessors gssIdx reduceNt

                for (gssIdxPre, lrStatePre, vPre, _finalRsmState) in predecessors do
                    processReduction reduceNt finalRsmState lrStatePre gssIdxPre vPre v depth

            if v < vertexCount - 1 then
                for (tVal, vNext) in graphEdges.[v] do
                    let shiftKey = (lrState, Symbol.T(Terminal tVal))

                    match Map.tryFind shiftKey lrTable.Action with
                    | Some(LRAction.Shift targetLrState) ->
                        let gssIdx = linearIdx lrState v
                        let targetGssIdx = linearIdx targetLrState vNext

                        let consumedStates =
                            RnglrGSS.addEdge gss targetGssIdx gssIdx (Symbol.T(Terminal tVal))

                        for (storedNt, storedInv, storedEndState, storedEndVertex) in consumedStates do
                            match Map.tryFind storedNt invBlockData with
                            | Some invData ->
                                let extPredecessors =
                                    productBfs invData [ (targetGssIdx, storedInv, storedEndState, storedEndVertex) ]

                                for (gssIdxPre, lrStatePre, vPre, _finalRsmState) in extPredecessors do
                                    processReduction storedNt storedInv lrStatePre gssIdxPre vPre vNext depth
                            | None -> ()

                        pending.[vNext].Enqueue
                            { LrState = targetLrState
                              Vertex = vNext }
                    | _ -> ()

        let collectActiveGss () : Set<int> * Set<int * int> =
            GraphHelpers.collectActiveGss gss.GssGraph.Edges

        let collectEdgeSymbols (activeVerts: Set<int>) : Map<int * int, NonEmptySet<Symbol<'t, 'nt>>> =
            let mutable symbols = Map.empty

            for fromIdx in activeVerts do
                for (toIdx, sym) in RnglrGSS.outgoingEdges gss fromIdx do
                    let key = (fromIdx, toIdx)

                    symbols <-
                        match Map.tryFind key symbols with
                        | Some existing -> Map.add key (NonEmptySet.add sym existing) symbols
                        | None -> Map.add key (NonEmptySet.singleton sym) symbols

            symbols

        let pendingSnapshot () =
            Array.init vertexCount (fun v -> pending.[v] |> List.ofSeq)

        let mutable handledAccum = Set.empty<RnglrDescriptor>

        let stepChanged = changedCells.Value
        changedCells.Value <- Set.empty<int * int>

        onStep
            (pendingSnapshot ())
            Set.empty
            Set.empty
            Map.empty
            (Matrix.copy pathIndex.Matrix)
            stepChanged
            -1
            None
            None
            handledAccum
            Set.empty

        pending.[0].Enqueue { LrState = 0; Vertex = 0 }

        for v in 0 .. vertexCount - 1 do
            let processed = HashSet<RnglrDescriptor>()

            while pending.[v].Count > 0 do
                let desc = pending.[v].Dequeue()

                if processed.Add(desc) then
                    let handledBefore = handledAccum
                    handledAccum <- Set.add desc handledAccum

                    processNode desc.LrState v 0

                    let activeVerts, activeEdges = collectActiveGss ()

                    let stepChanged = changedCells.Value
                    changedCells.Value <- Set.empty<int * int>

                    let attemptedThisStep = handledAccum - handledBefore

                    let edgeSymbols = collectEdgeSymbols activeVerts

                    onStep
                        (pendingSnapshot ())
                        activeVerts
                        activeEdges
                        edgeSymbols
                        (Matrix.copy pathIndex.Matrix)
                        stepChanged
                        v
                        (Some desc.LrState)
                        (Some desc)
                        handledAccum
                        attemptedThisStep

        pathIndex

    let buildPathIndex
        (_freshStart: Nonterminal<'nt>)
        (ersm: ExtendedRSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : PathIndex<'t, 'nt> =
        buildPathIndexCore ersm inputGraph (fun _ _ _ _ _ _ _ _ _ _ _ -> ())

    let buildPathIndexWithSteps
        (freshStart: Nonterminal<'nt>)
        (ersm: ExtendedRSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : PathIndex<'t, 'nt> * RnglrParsingStep<'t, 'nt> list =
        let steps = ResizeArray<RnglrParsingStep<'t, 'nt>>()
        let mutable prevVertices = Set.empty<int>
        let mutable prevEdges = Set.empty<int * int>
        let mutable prevAttempted = Set.empty<RnglrDescriptor>

        let onStep
            pendingQueues
            activeVerts
            activeEdges
            edgeSymbols
            piMatrix
            changedCells
            inputVertex
            currentLrState
            currentDescriptor
            handledAccum
            attemptedThisStep
            =
            let newVertices = Set.difference activeVerts prevVertices
            let newEdges = Set.difference activeEdges prevEdges
            let newDescriptors = Set.difference attemptedThisStep prevAttempted

            prevVertices <- activeVerts
            prevEdges <- activeEdges
            prevAttempted <- Set.union prevAttempted attemptedThisStep

            steps.Add(
                { PendingQueues = pendingQueues
                  ActiveGssVertices = activeVerts
                  ActiveGssEdges = activeEdges
                  ActiveGssEdgeSymbols = edgeSymbols
                  NewGssVertices = newVertices
                  NewGssEdges = newEdges
                  PathIndexMatrix = piMatrix
                  ChangedCells = changedCells
                  InputVertex = inputVertex
                  CurrentLrState = currentLrState
                  CurrentDescriptor = currentDescriptor
                  HandledDescriptors = handledAccum
                  NewDescriptors = newDescriptors
                  AttemptedDescriptors = attemptedThisStep }
            )

        buildPathIndexCore ersm inputGraph onStep
        |> fun pi -> pi, steps.ToArray() |> List.ofArray
