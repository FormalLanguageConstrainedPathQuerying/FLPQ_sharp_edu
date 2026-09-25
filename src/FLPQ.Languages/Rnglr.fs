namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

module Rnglr =

    [<Struct>]
    type private PredecessorInfo =
        {
            GssIdx: int
            LrState: int
            Vertex: int
            Aux: int
            /// LR state that originally triggered the reduction (row of the r_A action cell);
            /// carried through the product BFS and passing-reduction cascades unchanged.
            TriggerLrState: int
        }

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
        (onSubstep: RnglrParsingStep<'t, 'nt> -> unit)
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

        // GSS vertices at which passing-reduction handling triggered: a new edge was added
        // from the vertex and its stored states were non-empty (book: sec:CFPQ_GLR AddEdge).
        let passingReductionVertices = ref Set.empty<int>

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

        // Substep emission: one snapshot after exactly one action (one new GSS edge), plus the
        // initial state. NewGssVertices / NewGssEdges differ against the previous substep;
        // ChangedCells and PassingReductionVertices accumulate over this substep only.
        let mutable prevVertices = Set.empty<int>
        let mutable prevEdges = Set.empty<int * int>

        let emit (action: RnglrAction<'t, 'nt> option) (inputVertex: int) : unit =
            let activeVerts, activeEdges = collectActiveGss ()
            let edgeSymbols = collectEdgeSymbols activeVerts
            let stepChanged = changedCells.Value
            changedCells.Value <- Set.empty<int * int>
            let stepPassing = passingReductionVertices.Value
            passingReductionVertices.Value <- Set.empty<int>
            let newVertices = Set.difference activeVerts prevVertices
            let newEdges = Set.difference activeEdges prevEdges

            prevVertices <- activeVerts
            prevEdges <- activeEdges

            onSubstep
                { ActiveGssVertices = activeVerts
                  ActiveGssEdges = activeEdges
                  ActiveGssEdgeSymbols = edgeSymbols
                  NewGssVertices = newVertices
                  NewGssEdges = newEdges
                  PathIndexMatrix = Matrix.copy pathIndex.Matrix
                  ChangedCells = stepChanged
                  InputVertex = inputVertex
                  Action = action
                  PassingReductionVertices = stepPassing }

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
                                          Aux = currInv
                                          TriggerLrState = p.TriggerLrState }
                                        :: predecessors

                                let np = (nextGss, nextInv)

                                if visited.Add(np) then
                                    if not (Set.contains np startSet) then
                                        let cur = RnglrGSS.getStoredStates gss nextGss

                                        RnglrGSS.setStoredStates
                                            gss
                                            nextGss
                                            (Set.add
                                                { Nt = invData.Nonterminal
                                                  InvState = nextInv
                                                  RangeEndState = endState
                                                  RangeEndVertex = endVertex
                                                  TriggerLrState = p.TriggerLrState }
                                                cur)

                                    queue.Enqueue(
                                        { GssIdx = nextGss
                                          LrState = nextInv
                                          Vertex = endState
                                          Aux = endVertex
                                          TriggerLrState = p.TriggerLrState }
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
                          Aux = vxInputVertex
                          TriggerLrState = vxLrState })

                let preds = productBfs invData starts

                let epsPredecessors =
                    if invData.Finals |> Set.contains invData.Start then
                        [ { GssIdx = gssIdx
                            LrState = vxLrState
                            Vertex = vxInputVertex
                            Aux = invData.GlobalOffset + invData.Start
                            TriggerLrState = vxLrState } ]
                    else
                        []

                preds @ epsPredecessors
            | None -> []

        /// Applies a single reduction: looks up the goto state, adds the GSS edge labeled with
        /// the reduced nonterminal, and records the epsilon entry for direct epsilon derivations.
        /// Emits a reduce substep when a new GSS edge is added (triggerLrState is the LR state
        /// holding the complete item — the row of the r_A action cell).
        /// Returns true when a new GSS edge was added (the dedup key was new).
        let processReduction
            (reduceNt: Nonterminal<'nt>)
            (finalRsmState: int)
            (lrStatePre: int)
            (gssIdxPre: int)
            (vPre: int)
            (vEnd: int)
            (triggerLrState: int)
            : bool =
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

                    let consumedStates = RnglrGSS.addEdge gss gotoGssIdx gssIdxPre (Symbol.N reduceNt)

                    if not (Set.isEmpty consumedStates) then
                        passingReductionVertices := Set.add gotoGssIdx !passingReductionVertices

                    emit (Some(RnglrAction.Reduce(reduceNt, triggerLrState, lrStatePre))) vEnd

                match invBlockData.TryGetValue(reduceNt) with
                | true, invData ->
                    let globalStart = invData.GlobalOffset + invData.Start

                    if vPre = vEnd && finalRsmState = globalStart then
                        addToIndex globalStart vPre finalRsmState vEnd (PathIndexEntry.PEpsilonNonterminal reduceNt)
                | false, _ -> ()

                isNew
            | None -> false

        /// Shift phase for a single GSS vertex (lrState, v): follows every terminal transition
        /// of the LR table along the input edges from v, creates target vertices at vNext, and
        /// consumes stored states for passing-reduction continuations. Emits a shift substep
        /// after each shift edge creation; cascade reductions emit right after the shift that
        /// consumed their stored states.
        let shiftNode (lrState: int) (v: int) : unit =
            if v < vertexCount - 1 then
                for (tVal, vNext) in graphEdges.[v] do
                    let shiftKey = (lrState, Symbol.T(Terminal tVal))

                    match Map.tryFind shiftKey lrTable.Action with
                    | Some(LRAction.Shift targetLrState) ->
                        let shiftGssIdx = RnglrGSS.getOrCreateVertex gss lrState v
                        let targetGssIdx = RnglrGSS.getOrCreateVertex gss targetLrState vNext

                        let consumedStates =
                            RnglrGSS.addEdge gss targetGssIdx shiftGssIdx (Symbol.T(Terminal tVal))

                        if not (Set.isEmpty consumedStates) then
                            passingReductionVertices := Set.add targetGssIdx !passingReductionVertices

                        emit (Some(RnglrAction.Shift(Terminal tVal, lrState))) v

                        for st in consumedStates do
                            match Map.tryFind st.Nt invBlockData with
                            | Some invData ->
                                let extPredecessors =
                                    productBfs
                                        invData
                                        [ { GssIdx = targetGssIdx
                                            LrState = st.InvState
                                            Vertex = st.RangeEndState
                                            Aux = st.RangeEndVertex
                                            TriggerLrState = st.TriggerLrState } ]

                                for pred in extPredecessors do
                                    processReduction
                                        st.Nt
                                        st.InvState
                                        pred.LrState
                                        pred.GssIdx
                                        pred.Vertex
                                        vNext
                                        st.TriggerLrState
                                    |> ignore
                            | None -> ()
                    | _ -> ()

        /// Reduction phase at level v: a full-rescan fixpoint over all reducible GSS vertices at
        /// position v. Repeats until no new GSS edge is added, so every reduction's product BFS
        /// runs over the current (growing) GSS. Runs before the shift pass of the level — the
        /// canonical order (Scott & Johnstone 2006, Algorithm 1e PARSE SYMBOL; book sec:CFPQ_GLR).
        let reduceAtLevel (v: int) : unit =
            let mutable changed = true

            while changed do
                changed <- false

                for lrState in RnglrGSS.verticesAt gss v do
                    for (reduceNt, finalRsmState) in getReduceNtWithStates lrState do
                        let gssIdx = RnglrGSS.getOrCreateVertex gss lrState v
                        let predecessors = findPredecessors gssIdx reduceNt

                        for pred in predecessors do
                            let newEdge =
                                processReduction reduceNt finalRsmState pred.LrState pred.GssIdx pred.Vertex v lrState

                            if newEdge then
                                changed <- true

        emit None -1

        RnglrGSS.getOrCreateVertex gss 0 0 |> ignore

        // Level-based driver in canonical order: each step processes one input position (level) —
        // first apply all possible reductions at v to fixpoint, then execute shifts from all
        // vertices of the level in a single pass; then move to the next level. Shifts create no
        // new vertices at v, so one shift pass suffices — no rounds.
        // Book reference: sec:CFPQ_GLR (per-vertex MakeReductions/Push/ApplyPassingReductions
        // specialized to a single input string), Scott & Johnstone 2006 Algorithm 1e PARSE SYMBOL,
        // sec:CFPQ_RNGLR.
        for v in 0 .. vertexCount - 1 do
            reduceAtLevel v

            for lrState in RnglrGSS.verticesAt gss v do
                shiftNode lrState v

        pathIndex, gss.VertexInfo

    /// Core RNGLR algorithm — builds the path index through the level-based driver in canonical
    /// order: for each input position, apply all possible reductions to fixpoint, then execute
    /// shifts from all vertices of the level in a single pass.
    /// Book reference: sec:CFPQ_RNGLR.
    let buildPathIndex
        (_freshStart: Nonterminal<'nt>)
        (ersm: ExtendedRSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : PathIndex<'t, 'nt> =
        buildPathIndexCore ersm inputGraph ignore |> fst

    /// Same algorithm as buildPathIndex, additionally recording one RnglrParsingStep snapshot
    /// per action substep (plus the initial state) for step-by-step visualization.
    /// Book reference: sec:CFPQ_RNGLR.
    let buildPathIndexWithSteps
        (freshStart: Nonterminal<'nt>)
        (ersm: ExtendedRSM<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : RnglrResult<'t, 'nt> =
        let steps = ResizeArray<RnglrParsingStep<'t, 'nt>>()

        buildPathIndexCore ersm inputGraph steps.Add
        |> fun (pi, vertexInfo) ->
            { PathIndex = pi
              Steps = steps.ToArray() |> List.ofArray
              VertexInfo = vertexInfo }
