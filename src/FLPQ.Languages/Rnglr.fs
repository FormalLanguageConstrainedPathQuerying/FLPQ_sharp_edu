namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

module Rnglr =

    let private invertRsmTransitions (block: RsmBlock<'t, 'nt>) : (int * RsmSymbol<'t, 'nt> * int) list =
        let n = Dfa.stateCount block.dfa

        [ for fromIdx in 0 .. n - 1 do
              for toIdx in 0 .. n - 1 do
                  match Matrix.get block.dfa.transitions fromIdx toIdx with
                  | Some labels ->
                      for label in NonEmptySet.toSeq labels do
                          match label with
                          | AutomatonLabel.ATerm(sym) -> (toIdx, sym, fromIdx)
                          | _ -> ()
                  | None -> () ]

    let private collectRsmData (rsm: RSM<'t, 'nt>) =
        let blocks = RSM.blocks rsm
        let stateCount = RSM.stateCount rsm
        let stateInfo = Array.zeroCreate<RsmStateInfo<'nt>> stateCount
        let blockStart = Dictionary<Nonterminal<'nt>, int>()

        let mutable globalOffset = 0

        for block in blocks do
            let localSize = Dfa.stateCount block.dfa
            blockStart.[block.nonterminal] <- globalOffset + block.dfa.startState

            for localState in 0 .. localSize - 1 do
                stateInfo.[globalOffset + localState] <-
                    { blockNonterminal = block.nonterminal
                      localState = localState
                      isFinal = Set.contains localState block.dfa.finalStates }

            globalOffset <- globalOffset + localSize

        stateInfo, blockStart

    let private collectGraphEdges (g: Graph<int, Option<'t>>) : ResizeArray<'t * int>[] =
        let vc = Graph.vertexCount g
        let edges = Array.init vc (fun _ -> ResizeArray<'t * int>())

        for i in 0 .. vc - 1 do
            for j in 0 .. vc - 1 do
                match Matrix.get g.edges i j with
                | Some t -> edges.[i].Add(t, j)
                | None -> ()

        edges

    let buildPathIndex (rsm: RSM<'t, 'nt>) (inputGraph: Graph<int, Option<'t>>) : PathIndex<'t, 'nt> =
        let vertexCount = Graph.vertexCount inputGraph
        let graphEdges = collectGraphEdges inputGraph
        let stateInfo, blockStart = collectRsmData rsm

        let lrTable = RnglrLR.buildLR0Table rsm
        let lrStateCount = Dfa.stateCount lrTable.automaton
        let K = lrStateCount * vertexCount

        let pathIndex =
            { matrix = Matrix.init K K Set.empty
              stateCount = lrStateCount
              vertexCount = vertexCount }

        let gss = RnglrGSS.init lrStateCount vertexCount

        let blocks = RSM.blocks rsm
        let blockMap = blocks |> List.map (fun b -> (b.nonterminal, b)) |> Map.ofList

        let addToIndex (fs: int) (fv: int) (ts: int) (tv: int) (entry: PathIndexEntry<'t, 'nt>) =
            let fromIdx = PathIndex.linearIndex pathIndex fs fv
            let toIdx = PathIndex.linearIndex pathIndex ts tv
            let current = Matrix.get pathIndex.matrix fromIdx toIdx

            if not (Set.contains entry current) then
                Matrix.set pathIndex.matrix fromIdx toIdx (Set.add entry current)

        let getReduceNts (lrState: int) : Nonterminal<'nt> list =
            let items = lrTable.automaton.states.[lrState]

            items
            |> Set.toList
            |> List.choose (fun item ->
                match Map.tryFind item.blockNonterminal blockMap with
                | Some block ->
                    if Set.contains item.rsmState block.dfa.finalStates then
                        Some item.blockNonterminal
                    else
                        None
                | None -> None)

        let queue = Queue<int * int>()
        let enqueued = HashSet<int * int>()

        let tryEnqueue (lrState: int) (vertex: int) =
            if enqueued.Add((lrState, vertex)) then
                queue.Enqueue((lrState, vertex))

        /// BFS on product (GSS vertex × inverted RSM state) to find predecessors.
        let findPredecessors (gssIdx: int) (nt: Nonterminal<'nt>) : Set<int * int * int> =
            match Map.tryFind nt blockMap with
            | None -> Set.empty
            | Some block ->
                let invTrans = invertRsmTransitions block
                let finalLocalStates = block.dfa.finalStates
                let startLocalState = block.dfa.startState

                let invLookup =
                    invTrans
                    |> List.groupBy (fun (f, _, _) -> f)
                    |> List.map (fun (f, tr) -> (f, tr |> List.map (fun (_, s, t) -> (s, t))))
                    |> Map.ofList

                let visited = HashSet<int * int>()
                let q = Queue<int * int>()

                for finalLocal in finalLocalStates do
                    let p = (gssIdx, finalLocal)
                    visited.Add(p) |> ignore
                    q.Enqueue(p)

                let mutable found = Set.empty<int * int * int>

                while q.Count > 0 do
                    let (currGssIdx, currInvState) = q.Dequeue()

                    if currInvState = startLocalState then
                        let vx = Graph.getVertex currGssIdx gss.graph
                        found <- Set.add (vx.lrState, currGssIdx, vx.inputVertex) found

                    for (nextGssIdx, symbol) in RnglrGSS.outgoingEdges gss currGssIdx do
                        let rsmSym =
                            match symbol with
                            | Symbol.T(Terminal t) -> Some(RsmSymbol.RTerm(Terminal t))
                            | Symbol.N n -> Some(RsmSymbol.RNonterm n)
                            | Symbol.Epsilon -> None

                        match rsmSym with
                        | Some rSym ->
                            match Map.tryFind currInvState invLookup with
                            | Some trans ->
                                for (trSym, nextInvState) in trans do
                                    if trSym = rSym then
                                        let np = (nextGssIdx, nextInvState)

                                        if visited.Add(np) then
                                            q.Enqueue(np)
                            | None -> ()
                        | None -> ()

                found

        tryEnqueue 0 0

        // Handle epsilon acceptance at layer 0: the initial LR state may reduce
        // by the start nonterminal without any predecessors
        let initialReduceNts = getReduceNts 0

        for nt in initialReduceNts do
            // Record PNonterminal at the initial LR state range (0,0)→(0,0)
            addToIndex 0 0 0 0 (PathIndexEntry.PNonterminal nt)

        while queue.Count > 0 do
            let (lrState, v) = queue.Dequeue()
            let gssIdx = RnglrGSS.linearIndex vertexCount lrState v

            // SHIFT
            for (tVal, vNext) in graphEdges.[v] do
                let shiftKey = (lrState, Symbol.T(Terminal tVal))

                match Map.tryFind shiftKey lrTable.action with
                | Some(RnglrAction.Shift targetLrState) ->
                    let targetGssIdx = RnglrGSS.linearIndex vertexCount targetLrState vNext
                    let stored = RnglrGSS.addEdge gss targetGssIdx gssIdx (Symbol.T(Terminal tVal))
                    addToIndex lrState v targetLrState vNext (PathIndexEntry.PTerminal(Terminal tVal))

                    // Handle passed reductions
                    for (KeyValue(nt, predSet)) in stored do
                        for (lrStatePre, gssIdxPre, _vPre) in predSet do
                            match Map.tryFind (lrStatePre, nt) lrTable.goto with
                            | Some gotoTarget ->
                                let gotoGssIdx = RnglrGSS.linearIndex vertexCount gotoTarget vNext
                                RnglrGSS.addEdge gss gotoGssIdx gssIdxPre (Symbol.N nt) |> ignore
                                addToIndex lrStatePre vNext gotoTarget vNext (PathIndexEntry.PNonterminal nt)
                                addToIndex lrStatePre vNext gotoTarget vNext (PathIndexEntry.PIntermediate(lrState, v))
                                tryEnqueue gotoTarget vNext
                            | None -> ()

                    tryEnqueue targetLrState vNext
                | _ -> ()

            // REDUCE
            for nt in getReduceNts lrState do
                let predecessors = findPredecessors gssIdx nt
                let startNt = (RSM.startBlock rsm).nonterminal

                if Set.isEmpty predecessors && nt = startNt then
                    // Direct acceptance: start nonterminal reduced at the initial state
                    addToIndex 0 0 lrState v (PathIndexEntry.PNonterminal nt)
                else
                    for (lrStatePre, gssIdxPre, vPre) in predecessors do
                        match Map.tryFind (lrStatePre, nt) lrTable.goto with
                        | Some gotoTarget ->
                            let gotoGssIdx = RnglrGSS.linearIndex vertexCount gotoTarget v
                            RnglrGSS.addEdge gss gotoGssIdx gssIdxPre (Symbol.N nt) |> ignore

                            let currentStored =
                                RnglrGSS.getStoredReductions gss (RnglrGSS.linearIndex vertexCount lrState v)

                            let updated =
                                match Map.tryFind nt currentStored with
                                | Some existing ->
                                    Map.add nt (Set.add (lrStatePre, gssIdxPre, vPre) existing) currentStored
                                | None -> Map.add nt (Set.singleton (lrStatePre, gssIdxPre, vPre)) currentStored

                            RnglrGSS.setStoredReductions gss (RnglrGSS.linearIndex vertexCount lrState v) updated

                            addToIndex lrStatePre vPre gotoTarget v (PathIndexEntry.PNonterminal nt)
                            addToIndex lrStatePre vPre gotoTarget v (PathIndexEntry.PIntermediate(lrState, v))

                            tryEnqueue gotoTarget v
                        | None ->
                            // No goto entry: this reduction reached the initial state directly
                            // This is the augmented accept case
                            if lrStatePre = 0 && vPre = 0 then
                                addToIndex 0 0 lrState v (PathIndexEntry.PNonterminal nt)

        pathIndex

    let isAccepted (pathIndex: PathIndex<'t, 'nt>) (vertexCount: int) : bool =
        let K = pathIndex.stateCount * pathIndex.vertexCount
        let mutable result = false

        for i in 0 .. K - 1 do
            if not result then
                for j in 0 .. K - 1 do
                    if not result then
                        let fromVertex = i % vertexCount
                        let toVertex = j % vertexCount

                        if fromVertex = 0 && toVertex = vertexCount - 1 then
                            let entries = Matrix.get pathIndex.matrix i j

                            for entry in entries do
                                match entry with
                                | PathIndexEntry.PTerminal _
                                | PathIndexEntry.PNonterminal _ -> result <- true
                                | _ -> ()

        result
