namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.GraphAnalysis
open FLPQ.LinearAlgebra

/// LR(0) table construction for RNGLR operating directly on RSM blocks.
/// Book reference: sec:CFPQ_RNGLR.
module RnglrLR =

    /// Maps (localState, Nonterminal) to next state in the same block.
    let private transitionsForBlock (block: RsmBlock<'t, 'nt>) : Map<int * Symbol<'t, 'nt>, int> =
        let localSize = Dfa.stateCount block.Dfa
        let mutable result = Map.empty

        for localState in 0 .. localSize - 1 do
            for localTarget in 0 .. localSize - 1 do
                match Matrix.get block.Dfa.Transitions localState localTarget with
                | Some labels ->
                    for label in NonEmptySet.toSeq labels do
                        match label with
                        | AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal t)) ->
                            result <- Map.add (localState, Symbol.T(Terminal t)) localTarget result
                        | AutomatonLabel.ATerm(RsmSymbol.RNonterm nt) ->
                            result <- Map.add (localState, Symbol.N nt) localTarget result
                        | _ -> ()
                | None -> ()

        result

    /// LR(0) closure: for each item (N,q) in the set, if block N at state q has
    /// a nonterminal transition (q --RNonterm(M)--> _), add (M, startState_M).
    let private closure
        (blocks: RsmBlock<'t, 'nt> list)
        (blockTransitions: Map<Nonterminal<'nt>, Map<int * Symbol<'t, 'nt>, int>>)
        (blockStartStates: Map<Nonterminal<'nt>, int>)
        (items: Set<RnglrItem<'nt>>)
        : Set<RnglrItem<'nt>> =
        let mutable result = items
        let mutable changed = true

        while changed do
            changed <- false

            for item in Set.toList result do
                let trans =
                    match blockTransitions.TryGetValue(item.blockNonterminal) with
                    | true, t -> t
                    | false, _ -> Map.empty

                for (fromState, sym) in Map.keys trans do
                    if fromState = item.rsmState then
                        match sym with
                        | Symbol.N nt ->
                            match blockStartStates.TryGetValue(nt) with
                            | true, startState ->
                                let newItem =
                                    { blockNonterminal = nt
                                      rsmState = startState }

                                if not (Set.contains newItem result) then
                                    result <- Set.add newItem result
                                    changed <- true
                            | false, _ -> ()
                        | _ -> ()

        result

    /// LR(0) goto: advance each item by following transitions matching the given symbol.
    /// For terminal symbol X: if item (N,q) has transition q --X--> qNext, add (N, qNext).
    /// Then take closure.
    let private goto
        (blocks: RsmBlock<'t, 'nt> list)
        (blockTransitions: Map<Nonterminal<'nt>, Map<int * Symbol<'t, 'nt>, int>>)
        (blockStartStates: Map<Nonterminal<'nt>, int>)
        (items: Set<RnglrItem<'nt>>)
        (sym: Symbol<'t, 'nt>)
        : Set<RnglrItem<'nt>> =
        let mutable advanced = Set.empty

        for item in items do
            match blockTransitions.TryGetValue(item.blockNonterminal) with
            | true, trans ->
                match Map.tryFind (item.rsmState, sym) trans with
                | Some nextState ->
                    advanced <-
                        Set.add
                            { blockNonterminal = item.blockNonterminal
                              rsmState = nextState }
                            advanced
                | None -> ()
            | false, _ -> ()

        closure blocks blockTransitions blockStartStates advanced

    /// Returns the set of symbols that can appear from the given LR state.
    let private getSymbols
        (blockTransitions: Map<Nonterminal<'nt>, Map<int * Symbol<'t, 'nt>, int>>)
        (items: Set<RnglrItem<'nt>>)
        : Symbol<'t, 'nt> list =
        let mutable syms = Set.empty

        for item in items do
            match blockTransitions.TryGetValue(item.blockNonterminal) with
            | true, trans ->
                for (fromState, sym) in Map.keys trans do
                    if fromState = item.rsmState then
                        syms <- Set.add sym syms
            | false, _ -> ()

        Set.toList syms

    /// Build an LR(0) parsing table from an RSM.
    /// Returns the table (action, goto) and the constructed LR automaton.
    /// Book reference: sec:CFPQ_RNGLR.
    let buildLR0Table (rsm: RSM<'t, 'nt>) : RnglrTable<'t, 'nt> =
        let blocks = RSM.blocks rsm
        let startBlock = RSM.startBlock rsm

        // Build per-block transition maps and start state maps
        let blockTransitions =
            blocks
            |> List.map (fun b -> (b.Nonterminal, transitionsForBlock b))
            |> Map.ofList

        let blockStartStates =
            blocks |> List.map (fun b -> (b.Nonterminal, b.Dfa.StartState)) |> Map.ofList

        let blockFinalStates =
            blocks |> List.map (fun b -> (b.Nonterminal, b.Dfa.FinalStates)) |> Map.ofList

        // Build augmented start state
        let augNonterm = startBlock.Nonterminal
        let augStartState = blockStartStates.[augNonterm]

        let startItems =
            Set.singleton
                { blockNonterminal = augNonterm
                  rsmState = augStartState }
            |> closure blocks blockTransitions blockStartStates

        // Build the LR automaton via BFS
        let getSyms (items: Set<RnglrItem<'nt>>) : Symbol<'t, 'nt> list = getSymbols blockTransitions items

        let go (items: Set<RnglrItem<'nt>>) (sym: Symbol<'t, 'nt>) : Set<RnglrItem<'nt>> =
            goto blocks blockTransitions blockStartStates items sym

        let isAccept (items: Set<RnglrItem<'nt>>) : bool =
            items
            |> Set.exists (fun item ->
                item.blockNonterminal = augNonterm
                && Set.contains item.rsmState blockFinalStates.[augNonterm])

        let lrAutomaton = Automaton.buildAutomaton startItems getSyms go isAccept

        // Build action and goto tables
        let stateCount = Dfa.stateCount lrAutomaton
        let mutable action = Map.empty
        let mutable goTo = Map.empty

        for lrState in 0 .. stateCount - 1 do
            let items = lrAutomaton.States.[lrState]

            // Shift actions for terminal transitions
            for sym in getSymbols blockTransitions items do
                match sym with
                | Symbol.T _ ->
                    match Dfa.move lrAutomaton lrState sym with
                    | Some target -> action <- Map.add (lrState, sym) (RnglrAction.Shift target) action
                    | None -> ()
                | Symbol.N nt ->
                    match Dfa.move lrAutomaton lrState sym with
                    | Some target -> goTo <- Map.add (lrState, nt) target goTo
                    | None -> ()
                | Symbol.Epsilon -> ()

            // Reduce actions for items at final positions
            for item in items do
                let finals = blockFinalStates.[item.blockNonterminal]

                if Set.contains item.rsmState finals then
                    if item.blockNonterminal = augNonterm then
                        // Accept action for augmented start nonterminal at final state
                        action <- Map.add (lrState, Symbol.Epsilon) RnglrAction.Accept action
                    else
                        // Reduce by nonterminal for all possible lookaheads (LR(0): reduce on everything)
                        let block = blocks |> List.find (fun b -> b.Nonterminal = item.blockNonterminal)

                        let allTerminals =
                            RSM.terminals rsm |> List.map (fun (Terminal t) -> Symbol.T(Terminal t))

                        let reduceAction = RnglrAction.Reduce item.blockNonterminal

                        for term in allTerminals do
                            let key = (lrState, term)

                            match Map.tryFind key action with
                            | Some _ -> () // Conflict: keep existing action
                            | None -> action <- Map.add key reduceAction action

                        // Also reduce on epsilon (end-of-input marker)
                        let epsKey = (lrState, Symbol.Epsilon)

                        match Map.tryFind epsKey action with
                        | Some _ -> ()
                        | None -> action <- Map.add epsKey reduceAction action

        { action = action
          goto = goTo
          automaton = lrAutomaton }
