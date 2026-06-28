namespace FLPQ.Languages

open FLPQ.LinearAlgebra

/// Finite automaton with states parameterized by type 's.
/// Transitions are represented as a Matrix over sets of terminal symbols.
type Automaton<'t, 's when 't: comparison> =
    { states: 's list
      transitions: Matrix<Set<'t>>
      startStates: Set<int>
      finalStates: Set<int> }

module Automaton =

    let stateCount (a: Automaton<'t, 's>) = a.states.Length

    let alphabet (a: Automaton<'t, 's>) : Set<'t> =
        let mutable result = Set.empty

        for i in 0 .. a.transitions.rows - 1 do
            for j in 0 .. a.transitions.cols - 1 do
                result <- Set.union result a.transitions.data.[i, j]

        result

    /// All states reachable from a given state by a specific symbol.
    let move (a: Automaton<'t, 's>) (stateIdx: int) (symbol: 't) : Set<int> =
        let mutable result = Set.empty

        for j in 0 .. a.transitions.cols - 1 do
            if Set.contains symbol a.transitions.data.[stateIdx, j] then
                result <- Set.add j result

        result

    /// All states reachable from a set of states by a specific symbol.
    let moveSet (a: Automaton<'t, 's>) (stateIndices: Set<int>) (symbol: 't) : Set<int> =
        stateIndices |> Set.toSeq |> Seq.collect (fun i -> move a i symbol) |> Set.ofSeq

    /// Build an automaton from a list of transitions.
    /// Each transition is (fromStateIdx, symbol, toStateIdx).
    let fromTransitions
        (states: 's list)
        (transitionsList: (int * 't * int) list)
        (startStates: Set<int>)
        (finalStates: Set<int>)
        : Automaton<'t, 's> =
        let n = states.Length
        let matrix = Matrix.init n n Set.empty

        for (fromIdx, sym, toIdx) in transitionsList do
            let current = Set.add sym matrix.data.[fromIdx, toIdx]
            matrix.data.[fromIdx, toIdx] <- current

        { states = states
          transitions = matrix
          startStates = startStates
          finalStates = finalStates }

    /// Subset construction: convert NFA to DFA.
    /// Returns a new automaton where each state is a Set<int> (set of original state indices).
    let toDfa (nfa: Automaton<'t, 's>) : Automaton<'t, Set<int>> =
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

        fromTransitions dfaStates (List.rev transitions) (set [ 0 ]) dfaFinalStates

    /// Check whether an automaton is deterministic.
    let isDeterministic (a: Automaton<'t, 's>) : bool =
        a.startStates.Count = 1
        && (let mutable ok = true

            for i in 0 .. a.transitions.rows - 1 do
                for sym in alphabet a do
                    let targets = move a i sym

                    if targets.Count > 1 then
                        ok <- false

            ok)
