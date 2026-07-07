namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

module Rnglr =

    let private invertRsmTransitions (block: RsmBlock<'t, 'nt>) : Map<int * RsmSymbol<'t, 'nt>, int list> =
        let n = Dfa.stateCount block.dfa
        let mutable result = Map.empty

        for fromIdx in 0 .. n - 1 do
            for toIdx in 0 .. n - 1 do
                match Matrix.get block.dfa.transitions fromIdx toIdx with
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
        { start: int
          finals: Set<int>
          invTrans: Map<int * RsmSymbol<'t, 'nt>, int list> }

    let private collectGraphEdges (g: Graph<int, Option<'t>>) : ResizeArray<'t * int>[] =
        let vc = Graph.vertexCount g
        let edges = Array.init vc (fun _ -> ResizeArray<'t * int>())

        for i in 0 .. vc - 1 do
            for j in 0 .. vc - 1 do
                match Matrix.get g.edges i j with
                | Some t -> edges.[i].Add(t, j)
                | None -> ()

        edges

    let buildPathIndex
        (freshStart: Nonterminal<'nt>)
        (rsm: RSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : PathIndex<'t, 'nt> =
        let extRsm = RSM.extendWithStart freshStart rsm
        let lrTable = RnglrLR.buildLR0Table extRsm
        let lrStateCount = Dfa.stateCount lrTable.automaton
        let vertexCount = Graph.vertexCount inputGraph
        let K = lrStateCount * vertexCount

        let pathIndex =
            { matrix = Matrix.init K K Set.empty
              stateCount = lrStateCount
              vertexCount = vertexCount }

        let gss = RnglrGSS.init lrStateCount vertexCount
        let graphEdges = collectGraphEdges inputGraph

        let blocks = RSM.blocks extRsm
        let blockMap = blocks |> List.map (fun b -> (b.nonterminal, b)) |> Map.ofList

        let invBlockData =
            blocks
            |> List.map (fun b ->
                let invTrans = invertRsmTransitions b

                let invData =
                    { InvBlockData.start = b.dfa.startState
                      finals = b.dfa.finalStates
                      invTrans = invTrans }

                (b.nonterminal, invData))
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
            let current = Matrix.get pathIndex.matrix fromIdx toIdx

            if not (Set.contains entry current) then
                Matrix.set pathIndex.matrix fromIdx toIdx (Set.add entry current)

        let getReduceNts (lrState: int) : Nonterminal<'nt> list =
            let items = lrTable.automaton.states.[lrState]

            items
            |> Set.toList
            |> List.choose (fun item ->
                if item.blockNonterminal = freshStart then
                    None
                else
                    match Map.tryFind item.blockNonterminal blockMap with
                    | Some block ->
                        if Set.contains item.rsmState block.dfa.finalStates then
                            Some item.blockNonterminal
                        else
                            None
                    | None -> None)

        let toRsmSym (sym: Symbol<'t, 'nt>) : RsmSymbol<'t, 'nt> option =
            match sym with
            | Symbol.T(Terminal t) -> Some(RsmSymbol.RTerm(Terminal t))
            | Symbol.N nt -> Some(RsmSymbol.RNonterm nt)
            | Symbol.Epsilon -> None

        /// Performs product BFS over (gssIdx, invState) pairs.
        /// Starts from the given (gssIdx, invState) pairs and follows GSS edges matched against
        /// inverted RSM transitions. Returns predecessors (gssIdx where invState = block start)
        /// and all visited (gssIdx, invState) intermediate pairs (excluding starters).
        let productBfs
            (invData: InvBlockData<'t, 'nt>)
            (starts: (int * int) list)
            : (int * int * int) list * (int * int) list =
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

                for (nextGss, sym) in RnglrGSS.outgoingEdges gss currGss do
                    match toRsmSym sym with
                    | Some rSym ->
                        match Map.tryFind (currInv, rSym) invData.invTrans with
                        | Some nextInvList ->
                            for nextInv in nextInvList do
                                let isStart = nextInv = invData.start

                                if isStart then
                                    let vx = Graph.getVertex nextGss gss.graph
                                    predecessors <- (nextGss, vx.lrState, vx.inputVertex) :: predecessors

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
        /// Returns (gssIdx, lrState, inputVertex) triples for each predecessor.
        let findPredecessors (gssIdx: int) (nt: Nonterminal<'nt>) : (int * int * int) list =
            match Map.tryFind nt invBlockData with
            | Some invData ->
                let starts =
                    invData.finals
                    |> Set.toList
                    |> List.map (fun finalState -> (gssIdx, finalState))

                let (preds, intermediates) = productBfs invData starts

                for (gssIx, inv) in intermediates do
                    let cur = RnglrGSS.getStoredStates gss gssIx
                    RnglrGSS.setStoredStates gss gssIx (Set.add (nt, inv) cur)

                if invData.finals |> Set.contains invData.start && preds.IsEmpty then
                    let vx = Graph.getVertex gssIdx gss.graph
                    [ (gssIdx, vx.lrState, vx.inputVertex) ]
                else
                    preds
            | None -> []

        let pending = Array.init vertexCount (fun _ -> Queue<int * int>())

        let processedGotos: Set<Nonterminal<'nt> * int> array = Array.create K Set.empty

        let rec processReduction
            (intermediateState: int)
            (intermediateVertex: int)
            (gotoVertex: int)
            (reduceNt: Nonterminal<'nt>)
            (lrStatePre: int)
            (gssIdxPre: int)
            (vPre: int)
            (depth: int)
            : unit =
            match Map.tryFind (lrStatePre, reduceNt) lrTable.goto with
            | Some gotoTarget ->
                let gotoGssIdx = linearIdx gotoTarget gotoVertex
                let dedupKey = (reduceNt, gssIdxPre)
                let existing = processedGotos.[gotoGssIdx]

                if not (Set.contains dedupKey existing) then
                    processedGotos.[gotoGssIdx] <- Set.add dedupKey existing

                    RnglrGSS.addEdge gss gotoGssIdx gssIdxPre (Symbol.N reduceNt) |> ignore

                    addToIndex lrStatePre vPre gotoTarget gotoVertex (PathIndexEntry.PNonterminal reduceNt)

                    addToIndex
                        lrStatePre
                        vPre
                        gotoTarget
                        gotoVertex
                        (PathIndexEntry.PIntermediate(intermediateState, intermediateVertex))

                    processNode gotoTarget gotoVertex (depth + 1)
            | None -> ()

        /// Processes reductions and shifts at a GSS node (lrState, vertex v), cascading recursively.
        and processNode (lrState: int) (v: int) (depth: int) : unit =
            if depth > 1000 then
                failwith "Reduction cascade depth exceeded"

            for reduceNt in getReduceNts lrState do
                let gssIdx = linearIdx lrState v
                let predecessors = findPredecessors gssIdx reduceNt

                for (gssIdxPre, lrStatePre, vPre) in predecessors do
                    processReduction lrState v v reduceNt lrStatePre gssIdxPre vPre depth

            if v < vertexCount - 1 then
                for (tVal, vNext) in graphEdges.[v] do
                    let shiftKey = (lrState, Symbol.T(Terminal tVal))

                    match Map.tryFind shiftKey lrTable.action with
                    | Some(RnglrAction.Shift targetLrState) ->
                        let gssIdx = linearIdx lrState v
                        let targetGssIdx = linearIdx targetLrState vNext

                        let consumedStates =
                            RnglrGSS.addEdge gss targetGssIdx gssIdx (Symbol.T(Terminal tVal))

                        addToIndex lrState v targetLrState vNext (PathIndexEntry.PTerminal(Terminal tVal))

                        for (storedNt, storedInv) in consumedStates do
                            match Map.tryFind storedNt invBlockData with
                            | Some invData ->
                                let (extPredecessors, extIntermediates) =
                                    productBfs invData [ (targetGssIdx, storedInv) ]

                                for (gssIx, inv) in extIntermediates do
                                    let cur = RnglrGSS.getStoredStates gss gssIx
                                    RnglrGSS.setStoredStates gss gssIx (Set.add (storedNt, inv) cur)

                                for (gssIdxPre, lrStatePre, vPre) in extPredecessors do
                                    processReduction lrState v vNext storedNt lrStatePre gssIdxPre vPre depth
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
        let K = pathIndex.stateCount * pathIndex.vertexCount
        let mutable result = false

        for i in 0 .. K - 1 do
            if not result then
                let fromVertex = i % vertexCount

                if fromVertex = 0 then
                    for j in 0 .. K - 1 do
                        if not result then
                            let toVertex = j % vertexCount

                            if toVertex = vertexCount - 1 then
                                let entries = Matrix.get pathIndex.matrix i j

                                for entry in entries do
                                    match entry with
                                    | PathIndexEntry.PNonterminal _ -> result <- true
                                    | _ -> ()

        result
