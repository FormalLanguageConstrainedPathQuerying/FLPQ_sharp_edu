namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

[<Struct>]
type Config = { State: int; Position: int }

type AutomatonLabel<'t> =
    | ATerm of 't
    | AEpsilon

/// Nondeterministic finite automaton with multiple start states.
/// Epsilon transitions are stored in the transition matrix as AEpsilon-labeled edges.
/// Wraps a Graph where vertices are state labels and edges are transition symbol sets.
type NFA<'t, 's when 't: comparison> =
    { Graph: Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>
      StartStates: Set<int>
      FinalStates: Set<int> }

    member this.States = this.Graph |> Graph.vertices |> List.map snd
    member this.Transitions = this.Graph.Edges

/// Deterministic finite automaton with exactly one start state and no epsilon transitions.
/// Wraps a Graph where vertices are state labels and edges are transition symbol sets.
type DFA<'t, 's when 't: comparison> =
    { Graph: Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>
      StartState: int
      FinalStates: Set<int> }

    member this.States = this.Graph |> Graph.vertices |> List.map snd
    member this.Transitions = this.Graph.Edges

module Nfa =

    /// Collects all terminal symbols (excluding epsilon) from the transition matrix.
    /// Iterates over all matrix entries and extracts ATerm labels.
    let collectAlphabet (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>) : Set<'t> =
        let mutable result = Set.empty

        for i in 0 .. Matrix.rows transitions - 1 do
            for j in 0 .. Matrix.cols transitions - 1 do
                match Matrix.get transitions i j with
                | Some nes ->
                    for label in NonEmptySet.toSeq nes do
                        match label with
                        | ATerm t -> result <- Set.add t result
                        | AEpsilon -> ()
                | None -> ()

        result

    /// Builds a transition matrix from a list of labeled transitions.
    /// Multiple transitions between the same pair of states are merged into a non-empty set.
    let buildMatrix
        (n: int)
        (transitionsList: (int * AutomatonLabel<'t> * int) list)
        : Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>> =
        let matrix = Matrix.init n n None

        for (fromIdx, sym, toIdx) in transitionsList do
            let current =
                match Matrix.get matrix fromIdx toIdx with
                | Some nes -> NonEmptySet.add sym nes
                | None -> NonEmptySet.singleton sym

            Matrix.set matrix fromIdx toIdx (Some current)

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

        { Graph = Graph.fromEdges states (buildMatrix states.Length allTransitions)
          StartStates = startStates
          FinalStates = finalStates }

    /// Returns the number of states in the NFA.
    let stateCount (a: NFA<'t, 's>) = Graph.vertexCount a.Graph

    /// Returns the alphabet (set of all terminal symbols) of the NFA.
    let alphabet (a: NFA<'t, 's>) : Set<'t> = collectAlphabet a.Transitions

    /// Returns the set of states reachable from the given state via a single transition on the given symbol.
    /// Does not include epsilon-transitions.
    let move (a: NFA<'t, 's>) (stateIdx: int) (symbol: 't) : Set<int> =
        let mutable result = Set.empty

        for j in 0 .. Matrix.cols a.Transitions - 1 do
            match Matrix.get a.Transitions stateIdx j with
            | Some nes when NonEmptySet.contains (ATerm symbol) nes -> result <- Set.add j result
            | _ -> ()

        result

    /// Computes the epsilon-closure of a state: the set of all states reachable via zero or more epsilon-transitions.
    /// Uses a worklist algorithm; terminates even in the presence of epsilon cycles.
    let epsilonClosure (a: NFA<'t, 's>) (stateIdx: int) : Set<int> =
        let mutable closure = set [ stateIdx ]
        let mutable changed = true

        while changed do
            changed <- false

            let n = stateCount a

            for fromIdx in closure |> Set.toList do
                for toIdx in 0 .. n - 1 do
                    match Matrix.get a.Transitions fromIdx toIdx with
                    | Some nes when NonEmptySet.contains AEpsilon nes ->
                        if not (Set.contains toIdx closure) then
                            closure <- Set.add toIdx closure
                            changed <- true
                    | _ -> ()

        closure

    /// Returns the set of states reachable from any state in the given set via a single transition on the given symbol.
    /// Does not include epsilon-transitions.
    let moveSet (a: NFA<'t, 's>) (stateIndices: Set<int>) (symbol: 't) : Set<int> =
        stateIndices |> Set.toSeq |> Seq.collect (fun i -> move a i symbol) |> Set.ofSeq

    /// Classical NFA acceptance with working set of configurations.
    /// Handles epsilon transitions via epsilon closure expansion.
    /// Uses a visited set to prevent infinite loops from epsilon cycles.
    let accept (nfa: NFA<'t, 's>) (input: Terminal<'t> list) : bool =
        let n = List.length input

        let rawInput = input |> List.map (fun (Terminal sym) -> sym)

        let initConfigs =
            [ for s in nfa.StartStates do
                  for c in epsilonClosure nfa s -> { State = c; Position = 0 } ]
            |> Set.ofList

        let mutable currentConfigs = initConfigs
        let mutable visited = initConfigs

        let mutable result = false

        while not (Set.isEmpty currentConfigs) && not result do
            let cfg = currentConfigs |> Set.minElement
            currentConfigs <- Set.remove cfg currentConfigs

            if cfg.Position = n && Set.contains cfg.State nfa.FinalStates then
                result <- true
            elif cfg.Position < n then
                let sym = rawInput.[cfg.Position]
                let targets = move nfa cfg.State sym

                for t in targets do
                    for ec in epsilonClosure nfa t do
                        let newCfg =
                            { State = ec
                              Position = cfg.Position + 1 }

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
    /// Returns an NFA whose language is L(a) ∩ L(b) with product state labels ('s * 'v).
    let intersect (a: NFA<'t, 's>) (b: NFA<'t, 'v>) : NFA<'t, 's * 'v> =
        let nA = stateCount a
        let nB = stateCount b

        let labelsA = a.Graph |> Graph.vertices |> List.map snd |> Array.ofList
        let labelsB = b.Graph |> Graph.vertices |> List.map snd |> Array.ofList

        if nA = 0 || nB = 0 then
            { Graph = Graph.fromEdges [] (buildMatrix 0 [])
              StartStates = Set.empty
              FinalStates = Set.empty }
        else
            let n = nA * nB

            let idx iA iB = iA * nB + iB

            let productTransitions =
                LinearAlgebra.kron a.Transitions b.Transitions intersectEdgeSets None

            let k = Matrix.map Option.isSome productTransitions

            let startPairs =
                [| for sA in a.StartStates do
                       for sB in b.StartStates -> idx sA sB |]

            let forwardVisited = MsBfs.msBfs startPairs k

            let kT = Matrix.transpose k

            let finalPairs =
                [| for fA in a.FinalStates do
                       for fB in b.FinalStates -> idx fA fB |]

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

            { Graph =
                [ 0 .. n - 1 ]
                |> List.map (fun p -> (labelsA.[p / nB], labelsB.[p % nB]))
                |> fun labels -> Graph.fromEdges labels productTransitions
                |> Graph.keepVertices usefulSet
              StartStates = resultStartStates
              FinalStates = resultFinalStates }

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

        { Graph = Graph.fromEdges states (Nfa.buildMatrix (List.length states) labeledTransitions)
          StartState = startState
          FinalStates = finalStates }

    /// Returns the number of states in the DFA.
    let stateCount (a: DFA<'t, 's>) = Graph.vertexCount a.Graph

    /// Returns the alphabet (set of all terminal symbols) of the DFA.
    let alphabet (a: DFA<'t, 's>) : Set<'t> = Nfa.collectAlphabet a.Transitions

    /// Returns Some targetState for a transition on the given symbol, or None if no such transition exists.
    /// In a deterministic automaton, at most one transition per symbol exists from any state.
    let move (a: DFA<'t, 's>) (stateIdx: int) (symbol: 't) : int option =
        let mutable result = None

        for j in 0 .. Matrix.cols a.Transitions - 1 do
            match Matrix.get a.Transitions stateIdx j with
            | Some nes when NonEmptySet.contains (ATerm symbol) nes -> result <- Some j
            | _ -> ()

        result

    /// Verifies that the DFA is truly deterministic: for each state and each symbol,
    /// at most one transition exists. Returns true if deterministic, false otherwise.
    let isDeterministic (a: DFA<'t, 's>) : bool =
        let n = stateCount a
        let alph = alphabet a

        let mutable ok = true

        for i in 0 .. n - 1 do
            for sym in alph do
                let mutable count = 0

                for j in 0 .. n - 1 do
                    match Matrix.get a.Transitions i j with
                    | Some nes when NonEmptySet.contains (ATerm sym) nes -> count <- count + 1
                    | _ -> ()

                if count > 1 then
                    ok <- false

        ok

    /// DFA acceptance — sequential state transitions.
    /// Follows the input symbols one by one; accepts iff the final state is accepting.
    let accept (dfa: DFA<'t, 's>) (input: Terminal<'t> list) : bool =
        let mutable state = dfa.StartState
        let mutable ok = true

        let mutable remaining = input

        while ok && not (List.isEmpty remaining) do
            let (Terminal sym) = List.head remaining
            remaining <- List.tail remaining

            match move dfa state sym with
            | Some next -> state <- next
            | None -> ok <- false

        ok && Set.contains state dfa.FinalStates

/// Generic BFS-based automaton construction.
/// Explores all reachable states from a start set using a worklist algorithm.
/// Parameterized by getSymbols and goto functions and an accept-state predicate.
module Automaton =

    /// Build a DFA from a set of start states, using BFS exploration.
    /// getSymbols returns all outgoing symbols from a state.
    /// goto computes the target state for a given symbol.
    /// isAcceptState determines whether a state is final.
    let buildAutomaton
        (startItems: Set<'item>)
        (getSymbols: Set<'item> -> 'sym list)
        (goto: Set<'item> -> 'sym -> Set<'item>)
        (isAcceptState: Set<'item> -> bool)
        : DFA<'sym, Set<'item>> =
        let mutable states = [ startItems ]
        let mutable transitions: (int * 'sym * int) list = []
        let mutable queue = [ startItems ]

        while not (List.isEmpty queue) do
            let state = queue.Head
            let stateIdx = states |> List.findIndex (fun s -> s = state)
            queue <- queue.Tail

            let symbols = getSymbols state

            for sym in symbols do
                let target = goto state sym

                if not (Set.isEmpty target) then
                    let targetIdx =
                        match states |> List.tryFindIndex (fun s -> s = target) with
                        | Some idx -> idx
                        | None ->
                            let idx = List.length states
                            states <- states @ [ target ]
                            queue <- queue @ [ target ]
                            idx

                    transitions <- (stateIdx, sym, targetIdx) :: transitions

        let finalStates =
            states
            |> List.indexed
            |> List.choose (fun (idx, s) -> if isAcceptState s then Some idx else None)
            |> Set.ofList

        Dfa.fromTransitions states (List.rev transitions) 0 finalStates

    /// Subset construction: convert NFA to DFA using the generic buildAutomaton.
    /// State labels in the resulting DFA are subsets of NFA state indices.
    let toDfa (nfa: NFA<'t, 's>) : DFA<'t, Set<int>> =
        let syms = Nfa.collectAlphabet nfa.Transitions

        buildAutomaton nfa.StartStates (fun _ -> Set.toList syms) (Nfa.moveSet nfa) (fun subset ->
            Set.intersect subset nfa.FinalStates |> Set.isEmpty |> not)
