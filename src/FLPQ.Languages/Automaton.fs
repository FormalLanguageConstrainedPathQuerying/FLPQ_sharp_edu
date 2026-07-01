namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

[<Struct>]
type Config = { state: int; position: int }

type AutomatonLabel<'t> =
    | ATerm of 't
    | AEpsilon

/// Nondeterministic finite automaton with multiple start states.
/// Epsilon transitions are stored in the transition matrix as AEpsilon-labeled edges.
/// Wraps a Graph where vertices are state labels and edges are transition symbol sets.
type NFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>
      startStates: Set<int>
      finalStates: Set<int> }

    member this.states = this.graph |> Graph.vertices |> List.map snd
    member this.transitions = this.graph.edges

/// Deterministic finite automaton with exactly one start state and no epsilon transitions.
/// Wraps a Graph where vertices are state labels and edges are transition symbol sets.
type DFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>
      startState: int
      finalStates: Set<int> }

    member this.states = this.graph |> Graph.vertices |> List.map snd
    member this.transitions = this.graph.edges

module Nfa =

    let buildMatrix
        (n: int)
        (transitionsList: (int * AutomatonLabel<'t> * int) list)
        : Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>> =
        let matrix = Matrix.init n n None

        for (fromIdx, sym, toIdx) in transitionsList do
            let current =
                match matrix.data.[fromIdx, toIdx] with
                | Some nes -> NonEmptySet.add sym nes
                | None -> NonEmptySet.singleton sym

            matrix.data.[fromIdx, toIdx] <- Some current

        matrix

    /// Build an NFA from a list of transitions.
    /// epsTransitions are merged into the matrix as AEpsilon-labeled edges.
    let fromTransitions
        (states: 's list)
        (transitionsList: (int * 't * int) list)
        (epsTransitions: Set<int * int>)
        (startStates: Set<int>)
        (finalStates: Set<int>)
        : NFA<'t, 's> =
        let termTransitions = transitionsList |> List.map (fun (f, s, t) -> (f, ATerm s, t))

        let epsT = epsTransitions |> Set.toList |> List.map (fun (f, t) -> (f, AEpsilon, t))

        let allTransitions = termTransitions @ epsT

        { graph = Graph.fromEdges states (buildMatrix states.Length allTransitions)
          startStates = startStates
          finalStates = finalStates }

    let stateCount (a: NFA<'t, 's>) = Graph.vertexCount a.graph

    let alphabet (a: NFA<'t, 's>) : Set<'t> =
        let mutable result = Set.empty

        for i in 0 .. a.transitions.rows - 1 do
            for j in 0 .. a.transitions.cols - 1 do
                match a.transitions.data.[i, j] with
                | Some nes ->
                    for label in NonEmptySet.toSeq nes do
                        match label with
                        | ATerm t -> result <- Set.add t result
                        | AEpsilon -> ()
                | None -> ()

        result

    let move (a: NFA<'t, 's>) (stateIdx: int) (symbol: 't) : Set<int> =
        let mutable result = Set.empty

        for j in 0 .. a.transitions.cols - 1 do
            match a.transitions.data.[stateIdx, j] with
            | Some nes when NonEmptySet.contains (ATerm symbol) nes -> result <- Set.add j result
            | _ -> ()

        result

    let epsilonClosure (a: NFA<'t, 's>) (stateIdx: int) : Set<int> =
        let mutable closure = set [ stateIdx ]
        let mutable changed = true

        while changed do
            changed <- false

            let n = stateCount a

            for fromIdx in closure |> Set.toList do
                for toIdx in 0 .. n - 1 do
                    match a.transitions.data.[fromIdx, toIdx] with
                    | Some nes when NonEmptySet.contains AEpsilon nes ->
                        if not (Set.contains toIdx closure) then
                            closure <- Set.add toIdx closure
                            changed <- true
                    | _ -> ()

        closure

    let moveSet (a: NFA<'t, 's>) (stateIndices: Set<int>) (symbol: 't) : Set<int> =
        stateIndices |> Set.toSeq |> Seq.collect (fun i -> move a i symbol) |> Set.ofSeq

    /// Subset construction: convert NFA to DFA.
    let toDfa (nfa: NFA<'t, 's>) : DFA<'t, Set<int>> =
        let initialSubset = nfa.startStates

        let mutable dfaStates: Set<int> list = [ initialSubset ]
        let mutable dfaStateMap = Map.ofList [ (initialSubset, 0) ]
        let mutable transitions: (int * 't * int) list = []
        let mutable queue = [ initialSubset ]

        while not (List.isEmpty queue) do
            let currentSubset = queue.Head
            let currentIdx = Map.find currentSubset dfaStateMap
            queue <- queue.Tail

            let syms = alphabet nfa

            for sym in syms do
                let targetSubset = moveSet nfa currentSubset sym

                if not (Set.isEmpty targetSubset) then
                    let targetIdx =
                        match Map.tryFind targetSubset dfaStateMap with
                        | Some idx -> idx
                        | None ->
                            let idx = List.length dfaStates
                            dfaStates <- dfaStates @ [ targetSubset ]
                            dfaStateMap <- Map.add targetSubset idx dfaStateMap
                            queue <- queue @ [ targetSubset ]
                            idx

                    transitions <- (currentIdx, sym, targetIdx) :: transitions

        let dfaFinalStates =
            dfaStates
            |> List.indexed
            |> List.choose (fun (idx, subset) ->
                if Set.intersect subset nfa.finalStates |> (not << Set.isEmpty) then
                    Some idx
                else
                    None)
            |> Set.ofList

        { graph =
            Graph.fromEdges
                dfaStates
                (buildMatrix dfaStates.Length (List.rev transitions |> List.map (fun (f, s, t) -> (f, ATerm s, t))))
          startState = 0
          finalStates = dfaFinalStates }

    /// Classical NFA acceptance with working set of configurations.
    /// Handles epsilon transitions via epsilon closure expansion.
    /// Uses a visited set to prevent infinite loops from epsilon cycles.
    let accept (nfa: NFA<'t, 's>) (input: Terminal<'t> list) : bool =
        let n = List.length input

        let rawInput = input |> List.map (fun (Terminal sym) -> sym)

        let initConfigs =
            [ for s in nfa.startStates do
                  for c in epsilonClosure nfa s -> { state = c; position = 0 } ]
            |> Set.ofList

        let mutable currentConfigs = initConfigs
        let mutable visited = initConfigs

        let mutable result = false

        while not (Set.isEmpty currentConfigs) && not result do
            let cfg = currentConfigs |> Set.minElement
            currentConfigs <- Set.remove cfg currentConfigs

            if cfg.position = n && Set.contains cfg.state nfa.finalStates then
                result <- true
            elif cfg.position < n then
                let sym = rawInput.[cfg.position]
                let targets = move nfa cfg.state sym

                for t in targets do
                    for ec in epsilonClosure nfa t do
                        let newCfg =
                            { state = ec
                              position = cfg.position + 1 }

                        if not (Set.contains newCfg visited) then
                            visited <- Set.add newCfg visited
                            currentConfigs <- Set.add newCfg currentConfigs

        result

    /// Element-wise set intersection of two optional non-empty sets of automaton labels.
    /// Used as the multiplication operation for Kronecker products of automaton transition matrices.
    let intersectEdgeSets
        (optA: Option<NonEmptySet<AutomatonLabel<'t>>>)
        (optB: Option<NonEmptySet<AutomatonLabel<'t>>>)
        : Option<NonEmptySet<AutomatonLabel<'t>>> =
        match optA, optB with
        | Some nesA, Some nesB ->
            let common = Set.intersect (NonEmptySet.toSet nesA) (NonEmptySet.toSet nesB)

            if Set.isEmpty common then
                None
            else
                Some(NonEmptySet.ofSet common)
        | _ -> None

    /// Intersect two NFAs without epsilon transitions using linear algebra.
    /// Algorithm:
    /// 1. Kronecker product of transition matrices with set intersection → productTransitions.
    /// 2. Forward MS-BFS from start pairs on boolean mask of productTransitions.
    /// 3. Backward MS-BFS from final pairs on transposed boolean mask.
    /// 4. Intersect forward and backward visited (via reduceByColumn) to find useful product states.
    /// 5. Keep only useful product states via Graph.keepVertices.
    /// Returns an NFA whose language is L(a) ∩ L(b).
    let intersect (a: NFA<'t, 's>) (b: NFA<'t, 'v>) : NFA<'t, int * int> =
        let nA = stateCount a
        let nB = stateCount b

        if nA = 0 || nB = 0 then
            { graph = Graph.fromEdges [] (buildMatrix 0 [])
              startStates = Set.empty
              finalStates = Set.empty }
        else
            let n = nA * nB

            let idx iA iB = iA * nB + iB

            let productTransitions =
                LinearAlgebra.kron a.transitions b.transitions intersectEdgeSets None

            let k = Matrix.map Option.isSome productTransitions

            let startPairs =
                [| for sA in a.startStates do
                       for sB in b.startStates -> idx sA sB |]

            let forwardVisited = MsBfs.msBfs startPairs k

            let kT = Matrix.transpose k

            let finalPairs =
                [| for fA in a.finalStates do
                       for fB in b.finalStates -> idx fA fB |]

            let backwardVisited = MsBfs.msBfs finalPairs kT

            let reachableFromStart = Matrix.reduceByColumn (||) false forwardVisited

            for sp in startPairs do
                reachableFromStart.[sp] <- true

            let canReachFinal = Matrix.reduceByColumn (||) false backwardVisited

            for fp in finalPairs do
                canReachFinal.[fp] <- true

            let usefulStates =
                [| for p in 0 .. n - 1 do
                       if reachableFromStart.[p] && canReachFinal.[p] then
                           p |]

            let usefulSet = Set.ofArray usefulStates

            let usefulStateMap =
                usefulStates |> Array.mapi (fun i prodIdx -> (prodIdx, i)) |> Map.ofArray

            let resultStartStates =
                startPairs
                |> Array.choose (fun sp -> Map.tryFind sp usefulStateMap)
                |> Set.ofArray

            let resultFinalStates =
                finalPairs
                |> Array.choose (fun fp -> Map.tryFind fp usefulStateMap)
                |> Set.ofArray

            { graph =
                [ 0 .. n - 1 ]
                |> List.map (fun p -> (p / nB, p % nB))
                |> fun labels -> Graph.fromEdges labels productTransitions
                |> Graph.keepVertices usefulSet
              startStates = resultStartStates
              finalStates = resultFinalStates }

module Dfa =

    /// Build a DFA from a list of transitions.
    let fromTransitions
        (states: 's list)
        (transitionsList: (int * 't * int) list)
        (startState: int)
        (finalStates: Set<int>)
        : DFA<'t, 's> =
        let labeledTransitions =
            transitionsList |> List.map (fun (f, s, t) -> (f, ATerm s, t))

        { graph = Graph.fromEdges states (Nfa.buildMatrix (List.length states) labeledTransitions)
          startState = startState
          finalStates = finalStates }

    let stateCount (a: DFA<'t, 's>) = Graph.vertexCount a.graph

    let alphabet (a: DFA<'t, 's>) : Set<'t> =
        let mutable result = Set.empty

        for i in 0 .. a.transitions.rows - 1 do
            for j in 0 .. a.transitions.cols - 1 do
                match a.transitions.data.[i, j] with
                | Some nes ->
                    for label in NonEmptySet.toSeq nes do
                        match label with
                        | ATerm t -> result <- Set.add t result
                        | AEpsilon -> ()
                | None -> ()

        result

    let move (a: DFA<'t, 's>) (stateIdx: int) (symbol: 't) : int option =
        let mutable result = None

        for j in 0 .. a.transitions.cols - 1 do
            match a.transitions.data.[stateIdx, j] with
            | Some nes when NonEmptySet.contains (ATerm symbol) nes -> result <- Some j
            | _ -> ()

        result

    let isDeterministic (a: DFA<'t, 's>) : bool =
        let n = stateCount a
        let alph = alphabet a

        let mutable ok = true

        for i in 0 .. n - 1 do
            for sym in alph do
                let mutable count = 0

                for j in 0 .. n - 1 do
                    match a.transitions.data.[i, j] with
                    | Some nes when NonEmptySet.contains (ATerm sym) nes -> count <- count + 1
                    | _ -> ()

                if count > 1 then
                    ok <- false

        ok

    /// DFA acceptance — sequential state transitions.
    /// Follows the input symbols one by one; accepts iff the final state is accepting.
    let accept (dfa: DFA<'t, 's>) (input: Terminal<'t> list) : bool =
        let mutable state = dfa.startState
        let mutable ok = true

        let mutable remaining = input

        while ok && not (List.isEmpty remaining) do
            let (Terminal sym) = List.head remaining
            remaining <- List.tail remaining

            match move dfa state sym with
            | Some next -> state <- next
            | None -> ok <- false

        ok && Set.contains state dfa.finalStates
