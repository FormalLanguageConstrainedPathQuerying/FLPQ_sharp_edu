namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

module Rnglr =

    [<Struct>]
    type private PredecessorInfo =
        { GssIdx: int
          LrState: int
          Vertex: int
          Aux: int }

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
                -> Set<Terminal<'t>>
                -> Set<Nonterminal<'nt>>
                -> Set<Nonterminal<'nt>>
                -> unit)
        : PathIndex<'t, 'nt> * ResizeArray<int * int> =
        let extRsm = ersm.ExtendedRsm
        let lrTable = RnglrLR.buildLR0Table extRsm
        let rsmStateCount = RSM.stateCount extRsm
        let lrStateCount = Dfa.stateCount lrTable.Automaton
        let vertexCount = Graph.vertexCount inputGraph
        let rsmK = rsmStateCount * vertexCount

        let pathIndex =
            { Matrix = Matrix.init rsmK rsmK Set.empty
              StateCount = rsmStateCount
              VertexCount = vertexCount }

        let gss = RnglrGSS.create ()
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

        let processedGotos = Dictionary<int, Set<Nonterminal<'nt> * int>>()

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

        let productBfs (invData: InvBlockData<'t, 'nt>) (starts: PredecessorInfo list) : PredecessorInfo list =
            let visited = HashSet<int * int>()
            let startSet = starts |> List.map (fun p -> (p.GssIdx, p.LrState)) |> set
            let queue = Queue<PredecessorInfo>()

            let mutable predecessors: PredecessorInfo list = []

            for p in starts do
                let key = (p.GssIdx, p.LrState)

                if visited.Add(key) then
                    queue.Enqueue(p)

            while queue.Count > 0 do
                let p = queue.Dequeue()
                let currGss = p.GssIdx
                let currInv = p.LrState
                let endState = p.Vertex
                let endVertex = p.Aux
                let (currLrState, vCurr) = RnglrGSS.getVertexInfo gss currGss
                let globalCurrInv = invData.GlobalOffset + currInv
                let globalEnd = invData.GlobalOffset + endState

                for (nextGss, sym) in RnglrGSS.outgoingEdges gss currGss do
                    match toRsmSym sym with
                    | Some rSym ->
                        match Map.tryFind (currInv, rSym) invData.InvTrans with
                        | Some nextInvList ->
                            let (nextLrState, vNext) = RnglrGSS.getVertexInfo gss nextGss

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
                                    predecessors <-
                                        { GssIdx = nextGss
                                          LrState = nextLrState
                                          Vertex = vNext
                                          Aux = currInv }
                                        :: predecessors

                                let np = (nextGss, nextInv)

                                if visited.Add(np) then
                                    if not (Set.contains np startSet) then
                                        let cur = RnglrGSS.getStoredStates gss nextGss

                                        RnglrGSS.setStoredStates
                                            gss
                                            nextGss
                                            (Set.add (invData.Nonterminal, nextInv, endState, endVertex) cur)

                                    queue.Enqueue(
                                        { GssIdx = nextGss
                                          LrState = nextInv
                                          Vertex = endState
                                          Aux = endVertex }
                                    )
                        | None -> ()
                    | None -> ()

            predecessors

        let findPredecessors (gssIdx: int) (nt: Nonterminal<'nt>) : PredecessorInfo list =
            match Map.tryFind nt invBlockData with
            | Some invData ->
                let (vxLrState, vxInputVertex) = RnglrGSS.getVertexInfo gss gssIdx

                let starts =
                    invData.Finals
                    |> Set.toList
                    |> List.map (fun finalState ->
                        { GssIdx = gssIdx
                          LrState = finalState
                          Vertex = finalState
                          Aux = vxInputVertex })

                let preds = productBfs invData starts

                let epsPredecessors =
                    if invData.Finals |> Set.contains invData.Start then
                        [ { GssIdx = gssIdx
                            LrState = vxLrState
                            Vertex = vxInputVertex
                            Aux = invData.GlobalOffset + invData.Start } ]
                    else
                        []

                preds @ epsPredecessors
            | None -> []

        let pending = Array.init vertexCount (fun _ -> Queue<RnglrDescriptor>())

        let mutable stepShiftTerminals = Set.empty<Terminal<'t>>
        let mutable stepReduceNt = Set.empty<Nonterminal<'nt>>
        let mutable levelReductions = Set.empty<Nonterminal<'nt>>
        let mutable prevInputVertex = -1

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
                let gotoGssIdx = RnglrGSS.getOrCreateVertex gss gotoTarget vEnd
                let dedupKey = (reduceNt, gssIdxPre)

                let existing =
                    match processedGotos.TryGetValue(gotoGssIdx) with
                    | true, s -> s
                    | false, _ -> Set.empty

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

            if v <> prevInputVertex then
                levelReductions <- Set.empty
                prevInputVertex <- v

            if v < vertexCount - 1 then
                for (tVal, vNext) in graphEdges.[v] do
                    let shiftKey = (lrState, Symbol.T(Terminal tVal))

                    match Map.tryFind shiftKey lrTable.Action with
                    | Some(LRAction.Shift targetLrState) ->
                        stepShiftTerminals <- Set.add (Terminal tVal) stepShiftTerminals
                        let shiftGssIdx = RnglrGSS.getOrCreateVertex gss lrState v
                        let targetGssIdx = RnglrGSS.getOrCreateVertex gss targetLrState vNext

                        let consumedStates =
                            RnglrGSS.addEdge gss targetGssIdx shiftGssIdx (Symbol.T(Terminal tVal))

                        for (storedNt, storedInv, storedEndState, storedEndVertex) in consumedStates do
                            match Map.tryFind storedNt invBlockData with
                            | Some invData ->
                                let extPredecessors =
                                    productBfs
                                        invData
                                        [ { GssIdx = targetGssIdx
                                            LrState = storedInv
                                            Vertex = storedEndState
                                            Aux = storedEndVertex } ]

                                for pred in extPredecessors do
                                    processReduction storedNt storedInv pred.LrState pred.GssIdx pred.Vertex vNext depth
                            | None -> ()

                        pending.[vNext].Enqueue
                            { LrState = targetLrState
                              Vertex = vNext
                              GssIdx = targetGssIdx }
                    | _ -> ()

            for (reduceNt, finalRsmState) in getReduceNtWithStates lrState do
                stepReduceNt <- Set.add reduceNt stepReduceNt
                levelReductions <- Set.add reduceNt levelReductions
                let gssIdx = RnglrGSS.getOrCreateVertex gss lrState v
                let predecessors = findPredecessors gssIdx reduceNt

                for pred in predecessors do
                    processReduction reduceNt finalRsmState pred.LrState pred.GssIdx pred.Vertex v depth

        let collectActiveGss () : Set<int> * Set<int * int> =
            GraphHelpers.collectActiveGssForDict gss.Edges

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
            Set.empty
            Set.empty
            Set.empty

        let initialGssIdx = RnglrGSS.getOrCreateVertex gss 0 0

        pending.[0].Enqueue
            { LrState = 0
              Vertex = 0
              GssIdx = initialGssIdx }

        for v in 0 .. vertexCount - 1 do
            let processed = HashSet<RnglrDescriptor>()
            let handledBefore = handledAccum

            while pending.[v].Count > 0 do
                let desc = pending.[v].Dequeue()

                if processed.Add(desc) then
                    handledAccum <- Set.add desc handledAccum

                    processNode desc.LrState v 0

            let activeVerts, activeEdges = collectActiveGss ()

            let stepChanged = changedCells.Value
            changedCells.Value <- Set.empty<int * int>

            let attemptedThisStep = handledAccum - handledBefore

            let edgeSymbols = collectEdgeSymbols activeVerts

            let capturedShifts = stepShiftTerminals
            let capturedReduces = stepReduceNt
            let capturedLevel = levelReductions

            stepShiftTerminals <- Set.empty
            stepReduceNt <- Set.empty

            onStep
                (pendingSnapshot ())
                activeVerts
                activeEdges
                edgeSymbols
                (Matrix.copy pathIndex.Matrix)
                stepChanged
                v
                None
                None
                handledAccum
                attemptedThisStep
                capturedShifts
                capturedReduces
                capturedLevel

        pathIndex, gss.VertexInfo

    let buildPathIndex
        (_freshStart: Nonterminal<'nt>)
        (ersm: ExtendedRSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : PathIndex<'t, 'nt> =
        buildPathIndexCore ersm inputGraph (fun _ _ _ _ _ _ _ _ _ _ _ _ _ _ -> ())
        |> fst

    let buildPathIndexWithSteps
        (freshStart: Nonterminal<'nt>)
        (ersm: ExtendedRSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : RnglrResult<'t, 'nt> =
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
            shiftTerminals
            reduceNonterminals
            levelReds
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
                  AttemptedDescriptors = attemptedThisStep
                  ActiveShiftTerminals = shiftTerminals
                  ActiveReduceNonterminals = reduceNonterminals
                  LevelReductions = levelReds }
            )

        buildPathIndexCore ersm inputGraph onStep
        |> fun (pi, vertexInfo) ->
            { PathIndex = pi
              Steps = steps.ToArray() |> List.ofArray
              VertexInfo = vertexInfo }
