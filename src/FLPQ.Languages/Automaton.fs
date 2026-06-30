namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra

/// Nondeterministic finite automaton with multiple start states and epsilon transitions.
type NFA<'t, 's when 't: comparison> =
    { states: 's list
      transitions: Matrix<Option<NonEmptySet<'t>>>
      epsTransitions: Set<int * int>
      startStates: Set<int>
      finalStates: Set<int> }

/// Deterministic finite automaton with exactly one start state and no epsilon transitions.
type DFA<'t, 's when 't: comparison> =
    { states: 's list
      transitions: Matrix<Option<NonEmptySet<'t>>>
      startState: int
      finalStates: Set<int> }

module Nfa =

    let buildMatrix (states: 's list) (transitionsList: (int * 't * int) list) : Matrix<Option<NonEmptySet<'t>>> =
        let n = states.Length
        let matrix = Matrix.init n n None

        for (fromIdx, sym, toIdx) in transitionsList do
            let current =
                match matrix.data.[fromIdx, toIdx] with
                | Some nes -> NonEmptySet.add sym nes
                | None -> NonEmptySet.singleton sym

            matrix.data.[fromIdx, toIdx] <- Some current

        matrix

    /// Build an NFA from a list of transitions.
    let fromTransitions
        (states: 's list)
        (transitionsList: (int * 't * int) list)
        (epsTransitions: Set<int * int>)
        (startStates: Set<int>)
        (finalStates: Set<int>)
        : NFA<'t, 's> =
        { states = states
          transitions = buildMatrix states transitionsList
          epsTransitions = epsTransitions
          startStates = startStates
          finalStates = finalStates }

    let stateCount (a: NFA<'t, 's>) = a.states.Length

    let alphabet (a: NFA<'t, 's>) : Set<'t> =
        let mutable result = Set.empty

        for i in 0 .. a.transitions.rows - 1 do
            for j in 0 .. a.transitions.cols - 1 do
                match a.transitions.data.[i, j] with
                | Some nes -> result <- Set.union result (NonEmptySet.toSet nes)
                | None -> ()

        result

    let move (a: NFA<'t, 's>) (stateIdx: int) (symbol: 't) : Set<int> =
        let mutable result = Set.empty

        for j in 0 .. a.transitions.cols - 1 do
            match a.transitions.data.[stateIdx, j] with
            | Some nes when NonEmptySet.contains symbol nes -> result <- Set.add j result
            | _ -> ()

        result

    let epsilonClosure (a: NFA<'t, 's>) (stateIdx: int) : Set<int> =
        let mutable closure = set [ stateIdx ]
        let mutable changed = true

        while changed do
            changed <- false

            for (fromIdx, toIdx) in a.epsTransitions do
                if Set.contains fromIdx closure && not (Set.contains toIdx closure) then
                    closure <- Set.add toIdx closure
                    changed <- true

        closure

    let moveSet (a: NFA<'t, 's>) (stateIndices: Set<int>) (symbol: 't) : Set<int> =
        stateIndices |> Set.toSeq |> Seq.collect (fun i -> move a i symbol) |> Set.ofSeq

    let private buildDfaMatrix
        (states: 's list)
        (transitionsList: (int * 't * int) list)
        : Matrix<Option<NonEmptySet<'t>>> =
        buildMatrix states transitionsList

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

        let n = List.length dfaStates
        let matrix = Matrix.init n n None

        for (fromIdx, sym, toIdx) in transitions do
            let current =
                match matrix.data.[fromIdx, toIdx] with
                | Some nes -> NonEmptySet.add sym nes
                | None -> NonEmptySet.singleton sym

            matrix.data.[fromIdx, toIdx] <- Some current

        { states = dfaStates
          transitions = matrix
          startState = 0
          finalStates = dfaFinalStates }

module Dfa =

    /// Build a DFA from a list of transitions.
    let fromTransitions
        (states: 's list)
        (transitionsList: (int * 't * int) list)
        (startState: int)
        (finalStates: Set<int>)
        : DFA<'t, 's> =
        { states = states
          transitions = Nfa.buildMatrix states transitionsList
          startState = startState
          finalStates = finalStates }

    let stateCount (a: DFA<'t, 's>) = a.states.Length

    let alphabet (a: DFA<'t, 's>) : Set<'t> =
        Nfa.alphabet
            { states = a.states
              transitions = a.transitions
              epsTransitions = Set.empty
              startStates = set [ a.startState ]
              finalStates = a.finalStates }

    let move (a: DFA<'t, 's>) (stateIdx: int) (symbol: 't) : int option =
        let mutable result = None

        for j in 0 .. a.transitions.cols - 1 do
            match a.transitions.data.[stateIdx, j] with
            | Some nes when NonEmptySet.contains symbol nes -> result <- Some j
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
                    | Some nes when NonEmptySet.contains sym nes -> count <- count + 1
                    | _ -> ()

                if count > 1 then
                    ok <- false

        ok
