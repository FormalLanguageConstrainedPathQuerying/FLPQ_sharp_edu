namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra

/// Finite automaton with states parameterized by type 's.
/// Transitions are represented as a Matrix over Option<NonEmptySet<'t>>.
/// Epsilon transitions are explicit.
type Automaton<'t, 's when 't: comparison> =
    { states: 's list
      transitions: Matrix<Option<NonEmptySet<'t>>>
      epsTransitions: Set<int * int>
      startStates: Set<int>
      finalStates: Set<int> }

module Automaton =

    let stateCount (a: Automaton<'t, 's>) = a.states.Length

    let alphabet (a: Automaton<'t, 's>) : Set<'t> =
        let mutable result = Set.empty

        for i in 0 .. a.transitions.rows - 1 do
            for j in 0 .. a.transitions.cols - 1 do
                match a.transitions.data.[i, j] with
                | Some nes -> result <- Set.union result (NonEmptySet.toSet nes)
                | None -> ()

        result

    /// All states reachable from a given state by a specific symbol.
    let move (a: Automaton<'t, 's>) (stateIdx: int) (symbol: 't) : Set<int> =
        let mutable result = Set.empty

        for j in 0 .. a.transitions.cols - 1 do
            match a.transitions.data.[stateIdx, j] with
            | Some nes when NonEmptySet.contains symbol nes -> result <- Set.add j result
            | _ -> ()

        result

    /// All states reachable from a given state via epsilon transitions.
    let epsilonClosure (a: Automaton<'t, 's>) (stateIdx: int) : Set<int> =
        let mutable closure = set [ stateIdx ]
        let mutable changed = true

        while changed do
            changed <- false

            for (fromIdx, toIdx) in a.epsTransitions do
                if Set.contains fromIdx closure && not (Set.contains toIdx closure) then
                    closure <- Set.add toIdx closure
                    changed <- true

        closure

    /// All states reachable from a set of states by a specific symbol.
    let moveSet (a: Automaton<'t, 's>) (stateIndices: Set<int>) (symbol: 't) : Set<int> =
        stateIndices |> Set.toSeq |> Seq.collect (fun i -> move a i symbol) |> Set.ofSeq

    /// Build an automaton from a list of transitions.
    /// Each transition is (fromStateIdx, symbol, toStateIdx).
    let fromTransitions
        (states: 's list)
        (transitionsList: (int * 't * int) list)
        (epsTransitions: Set<int * int>)
        (startStates: Set<int>)
        (finalStates: Set<int>)
        : Automaton<'t, 's> =
        let n = states.Length
        let matrix = Matrix.init n n None

        for (fromIdx, sym, toIdx) in transitionsList do
            let current =
                match matrix.data.[fromIdx, toIdx] with
                | Some nes -> NonEmptySet.add sym nes
                | None -> NonEmptySet.singleton sym

            matrix.data.[fromIdx, toIdx] <- Some current

        { states = states
          transitions = matrix
          epsTransitions = epsTransitions
          startStates = startStates
          finalStates = finalStates }

    /// Subset construction: convert NFA to DFA.
    /// Returns a new automaton where each state is a Set<int> (set of original state indices).
    let toDfa (nfa: Automaton<'t, 's>) : Automaton<'t, Set<int>> =
        let initialSubset = nfa.startStates

        let mutable dfaStates: Set<int> list = [ initialSubset ]
        let mutable dfaStateMap = Map.ofList [ (initialSubset, 0) ]
        let mutable transitions: (int * 't * int) list = []
        let mutable epsTransitions: Set<int * int> = Set.empty
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

        fromTransitions dfaStates (List.rev transitions) epsTransitions (set [ 0 ]) dfaFinalStates

    /// Check whether an automaton is deterministic.
    let isDeterministic (a: Automaton<'t, 's>) : bool =
        a.startStates.Count = 1
        && a.epsTransitions.IsEmpty
        && (let mutable ok = true

            for i in 0 .. a.transitions.rows - 1 do
                for sym in alphabet a do
                    let targets = move a i sym

                    if targets.Count > 1 then
                        ok <- false

            ok)
