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
                match Matrix.get block.Dfa.Transitions fromIdx toIdx with
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
        { Start: int
          Finals: Set<int>
          InvTrans: Map<int * RsmSymbol<'t, 'nt>, int list>
          GlobalOffset: int }

    let buildPathIndex
        (freshStart: Nonterminal<'nt>)
        (rsm: RSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : PathIndex<'t, 'nt> =
        let extRsm = RSM.extendWithStart freshStart rsm
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
                    { Start = b.Dfa.StartState
                      Finals = b.Dfa.FinalStates
                      InvTrans = invTrans
                      GlobalOffset = blockGlobalOffset.[b.Nonterminal] }

                (b.Nonterminal, invData))
            |> Map.ofList

        let linearIdx (state: int) (vertex: int) = state * vertexCount + vertex

        let addToIndex
            (fromState: int)
            (fromVertex: int)
            (toState: int)
            (toVertex: int)
            (entry: PathIndexEntry<'t, 'nt>)
            =
            let fromIdx = linearIdx fromState fromVertex
            let toIdx = linearIdx toState toVertex
            let current = Matrix.get pathIndex.Matrix fromIdx toIdx

            if not (Set.contains entry current) then
                Matrix.set pathIndex.Matrix fromIdx toIdx (Set.add entry current)

        let getReduceNtWithStates (lrState: int) : (Nonterminal<'nt> * int) list =
            let items = lrTable.Automaton.States.[lrState]

            items
            |> Set.toList
            |> List.choose (fun item ->
                if item.BlockNonterminal = freshStart then
                    None
                else
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

        /// Performs product BFS over (gssIdx, invState) pairs, filling path index entries.
        let productBfs
            (invData: InvBlockData<'t, 'nt>)
            (starts: (int * int) list)
            : (int * int * int * int) list * (int * int) list =
            let visited = HashSet<int * int>()
            let queue = Queue<int * int>()
            let mutable predecessors = []
            let mutable intermediates = []

            for (gssIdx, invState) in starts do
                let p = (gssIdx, invState)

                if visited.Add(p) then
                    queue.Enqueue(p)

            while queue.Count > 0 do
                let (currGss, currInv) = queue.Dequeue()
                let currVx = Graph.getVertex currGss gss.GssGraph
                let vCurr = currVx.InputVertex

                for (nextGss, sym) in RnglrGSS.outgoingEdges gss currGss do
                    match toRsmSym sym with
                    | Some rSym ->
                        match Map.tryFind (currInv, rSym) invData.InvTrans with
                        | Some nextInvList ->
                            let nextVx = Graph.getVertex nextGss gss.GssGraph
                            let vNext = nextVx.InputVertex

                            for nextInv in nextInvList do
                                match rSym with
                                | RsmSymbol.RTerm(Terminal t) ->
                                    let globalCurrInv = invData.GlobalOffset + currInv
                                    let globalNextInv = invData.GlobalOffset + nextInv

                                    addToIndex
                                        globalNextInv
                                        vNext
                                        globalCurrInv
                                        vCurr
                                        (PathIndexEntry.PTerminal(Terminal t))
                                | _ -> ()

                                let isStart = nextInv = invData.Start

                                if isStart then
                                    predecessors <- (nextGss, nextVx.LrState, vNext, currInv) :: predecessors

                                    let np = (nextGss, nextInv)

                                    if visited.Add(np) then
                                        intermediates <- np :: intermediates
                                        queue.Enqueue(np)
                                else
                                    let np = (nextGss, nextInv)

                                    if visited.Add(np) then
                                        intermediates <- np :: intermediates
                                        queue.Enqueue(np)
                        | None -> ()
                    | None -> ()

            (predecessors, intermediates)

        /// Finds predecessors by running product BFS from (gssIdx, block final states).
        /// Returns (predecessors, intermediateGlobalPoints) where intermediateGlobalPoints are
        /// (globalRsmState, vertex) for each intermediate node visited in the BFS.
        let findPredecessors (gssIdx: int) (nt: Nonterminal<'nt>) : (int * int * int * int) list * (int * int) list =
            match Map.tryFind nt invBlockData with
            | Some invData ->
                let starts =
                    invData.Finals
                    |> Set.toList
                    |> List.map (fun finalState -> (gssIdx, finalState))

                let (preds, intermediates) = productBfs invData starts

                let globalIntermediates =
                    intermediates
                    |> List.map (fun (gssIx, inv) ->
                        let vx = Graph.getVertex gssIx gss.GssGraph
                        let cur = RnglrGSS.getStoredStates gss gssIx
                        RnglrGSS.setStoredStates gss gssIx (Set.add (nt, inv) cur)
                        (invData.GlobalOffset + inv, vx.InputVertex))

                let resultPreds =
                    if invData.Finals |> Set.contains invData.Start && preds.IsEmpty then
                        let vx = Graph.getVertex gssIdx gss.GssGraph
                        [ (gssIdx, vx.LrState, vx.InputVertex, invData.GlobalOffset + invData.Start) ]
                    else
                        preds

                (resultPreds, globalIntermediates)
            | None -> ([], [])


        let pending = Array.init vertexCount (fun _ -> Queue<int * int>())

        let processedGotos: Set<Nonterminal<'nt> * int> array = Array.create lrK Set.empty

        let rec processReduction
            (reduceNt: Nonterminal<'nt>)
            (finalRsmState: int)
            (lrStatePre: int)
            (gssIdxPre: int)
            (vPre: int)
            (vEnd: int)
            (depth: int)
            (intermediates: (int * int) list)
            : unit =
            match Map.tryFind (lrStatePre, reduceNt) lrTable.Goto with
            | Some gotoTarget ->
                let gotoGssIdx = linearIdx gotoTarget vEnd
                let dedupKey = (reduceNt, gssIdxPre)
                let existing = processedGotos.[gotoGssIdx]

                if not (Set.contains dedupKey existing) then
                    processedGotos.[gotoGssIdx] <- Set.add dedupKey existing

                    RnglrGSS.addEdge gss gotoGssIdx gssIdxPre (Symbol.N reduceNt) |> ignore

                    match invBlockData.TryGetValue(reduceNt) with
                    | true, invData ->
                        let globalStart = invData.GlobalOffset + invData.Start

                        if vPre = vEnd then
                            addToIndex globalStart vPre finalRsmState vEnd (PathIndexEntry.PEpsilonNonterminal reduceNt)
                        else
                            addToIndex globalStart vPre finalRsmState vEnd (PathIndexEntry.PNonterminal reduceNt)

                            for (interState, interVertex) in intermediates do
                                if interVertex >= vPre && interVertex <= vEnd then
                                    addToIndex
                                        globalStart
                                        vPre
                                        finalRsmState
                                        vEnd
                                        (PathIndexEntry.PIntermediate(interState, interVertex))

                        let callerItems = lrTable.Automaton.States.[lrStatePre]

                        for callerItem in callerItems do
                            if callerItem.BlockNonterminal <> freshStart then
                                match Map.tryFind callerItem.BlockNonterminal blockMap with
                                | Some callerBlock ->
                                    let trans = callerBlock.Dfa.Transitions

                                    for callTarget in 0 .. Dfa.stateCount callerBlock.Dfa - 1 do
                                        match Matrix.get trans callerItem.RsmState callTarget with
                                        | Some labels ->
                                            for label in NonEmptySet.toSeq labels do
                                                match label with
                                                | AutomatonLabel.ATerm(RsmSymbol.RNonterm nt) when nt = reduceNt ->
                                                    let callerOffset = blockGlobalOffset.[callerItem.BlockNonterminal]
                                                    let callGlobalState = callerOffset + callerItem.RsmState
                                                    let returnGlobalState = callerOffset + callTarget

                                                    if vPre = vEnd then
                                                        addToIndex
                                                            callGlobalState
                                                            vPre
                                                            returnGlobalState
                                                            vEnd
                                                            (PathIndexEntry.PEpsilonNonterminal reduceNt)
                                                    else
                                                        addToIndex
                                                            callGlobalState
                                                            vPre
                                                            returnGlobalState
                                                            vEnd
                                                            (PathIndexEntry.PNonterminal reduceNt)
                                                | _ -> ()
                                        | None -> ()
                                | None -> ()
                    | false, _ -> ()

                    processNode gotoTarget vEnd (depth + 1)
            | None -> ()

        /// Processes reductions and shifts at a GSS node (lrState, vertex v), cascading recursively.
        and processNode (lrState: int) (v: int) (depth: int) : unit =
            if depth > 1000 then
                failwith "Reduction cascade depth exceeded"

            for (reduceNt, finalRsmState) in getReduceNtWithStates lrState do
                let gssIdx = linearIdx lrState v
                let (predecessors, intermediates) = findPredecessors gssIdx reduceNt

                for (gssIdxPre, lrStatePre, vPre, _finalRsmState) in predecessors do
                    processReduction reduceNt finalRsmState lrStatePre gssIdxPre vPre v depth intermediates

            if v < vertexCount - 1 then
                for (tVal, vNext) in graphEdges.[v] do
                    let shiftKey = (lrState, Symbol.T(Terminal tVal))

                    match Map.tryFind shiftKey lrTable.Action with
                    | Some(RnglrAction.Shift targetLrState) ->
                        let gssIdx = linearIdx lrState v
                        let targetGssIdx = linearIdx targetLrState vNext

                        let consumedStates =
                            RnglrGSS.addEdge gss targetGssIdx gssIdx (Symbol.T(Terminal tVal))

                        for (storedNt, storedInv) in consumedStates do
                            match Map.tryFind storedNt invBlockData with
                            | Some invData ->
                                let (extPredecessors, extIntermediates) =
                                    productBfs invData [ (targetGssIdx, storedInv) ]

                                let globalExtIntermediates =
                                    extIntermediates
                                    |> List.map (fun (gssIx, inv) ->
                                        let vx = Graph.getVertex gssIx gss.GssGraph
                                        let cur = RnglrGSS.getStoredStates gss gssIx
                                        RnglrGSS.setStoredStates gss gssIx (Set.add (storedNt, inv) cur)
                                        (invData.GlobalOffset + inv, vx.InputVertex))

                                for (gssIdxPre, lrStatePre, vPre, _finalRsmState) in extPredecessors do
                                    processReduction
                                        storedNt
                                        storedInv
                                        lrStatePre
                                        gssIdxPre
                                        vPre
                                        vNext
                                        depth
                                        globalExtIntermediates
                            | None -> ()

                        pending.[vNext].Enqueue(targetLrState, targetGssIdx)
                    | _ -> ()

        pending.[0].Enqueue(0, linearIdx 0 0)

        for v in 0 .. vertexCount - 1 do
            let processed = HashSet<int * int>()

            while pending.[v].Count > 0 do
                let (lrState, _gssIdx) = pending.[v].Dequeue()

                if processed.Add((lrState, v)) then
                    processNode lrState v 0

        pathIndex

    let isAccepted (pathIndex: PathIndex<'t, 'nt>) (vertexCount: int) : bool =
        let k = pathIndex.StateCount * pathIndex.VertexCount
        let mutable result = false

        for i in 0 .. k - 1 do
            if not result then
                let fromVertex = i % vertexCount

                if fromVertex = 0 then
                    for j in 0 .. k - 1 do
                        if not result then
                            let toVertex = j % vertexCount

                            if toVertex = vertexCount - 1 then
                                let entries = Matrix.get pathIndex.Matrix i j

                                for entry in entries do
                                    match entry with
                                    | PathIndexEntry.PNonterminal _ -> result <- true
                                    | PathIndexEntry.PEpsilonNonterminal _ -> result <- true
                                    | _ -> ()

        result
