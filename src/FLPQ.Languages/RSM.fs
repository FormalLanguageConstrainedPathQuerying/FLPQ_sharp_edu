namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// Symbol labeling a transition in an RSM block.
/// Either a terminal (read input character) or a nonterminal (recursive call to another block).
[<RequireQualifiedAccess>]
type RsmSymbol<'t, 'nt when 't: comparison and 'nt: comparison> =
    | RTerm of Terminal<'t>
    | RNonterm of Nonterminal<'nt>

/// Type alias for a DFA over RsmSymbol alphabet, used in RSM blocks.
type RsmDfa<'t, 'nt when 't: comparison and 'nt: comparison> = DFA<RsmSymbol<'t, 'nt>, int>

/// A single block in an RSM — a deterministic finite automaton for one nonterminal.
/// Transitions are over the alphabet Σ ∪ Q_S.
/// Reconstructed on demand from flat RSM storage.
type RsmBlock<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Nonterminal: Nonterminal<'nt>
      Dfa: RsmDfa<'t, 'nt> }

/// Mapping from global RSM state index to block information.
/// Book reference: sec:CFPQ_GLL.
[<Struct>]
type RsmStateInfo<'nt when 'nt: comparison> =
    { BlockNonterminal: Nonterminal<'nt>
      LocalState: int
      IsFinal: bool }

/// Recursive State Machine: a tuple ⟨N, Σ, B, B_S, Q, Q_S⟩ stored as a single flat automaton
/// with globally unique state numbers across all blocks.
/// Transitions is the combined adjacency matrix for all states (size StateCount × StateCount).
/// StateInfo maps each global state to its block context.
/// BlockStart maps each nonterminal to its global start state.
/// FinalStates is the global set of final states across all blocks.
/// Nodes are assigned globally unique indices during construction; no remapping is needed.
/// Book reference: sec:CFPQ_GLL, sec:CFPQ_RNGLR.
type RSM<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Transitions: Matrix<Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>>>
      StateCount: int
      StateInfo: RsmStateInfo<'nt> array
      BlockStart: Dictionary<Nonterminal<'nt>, int>
      FinalStates: Set<int>
      StartBlock: Nonterminal<'nt> }


module RSM =

    /// Returns the list of all nonterminals (block names) in the RSM,
    /// ordered by global state appearance.
    let nonterminals (rsm: RSM<'t, 'nt>) : Nonterminal<'nt> list =
        let mutable seen = Set.empty
        let mutable result = []

        for info in rsm.StateInfo do
            if not (Set.contains info.BlockNonterminal seen) then
                seen <- Set.add info.BlockNonterminal seen
                result <- info.BlockNonterminal :: result

        List.rev result

    /// Returns the list of all distinct terminal symbols across all blocks.
    let terminals (rsm: RSM<'t, 'nt>) : Terminal<'t> list =
        let mutable result = []

        for i in 0 .. rsm.StateCount - 1 do
            for j in 0 .. rsm.StateCount - 1 do
                match Matrix.get rsm.Transitions i j with
                | Some labels ->
                    for label in NonEmptySet.toSeq labels do
                        match label with
                        | AutomatonLabel.ATerm(RsmSymbol.RTerm t) ->
                            if not (List.contains t result) then
                                result <- t :: result
                        | _ -> ()
                | None -> ()

        List.rev result

    /// Returns the total number of states across all blocks.
    let stateCount (rsm: RSM<'t, 'nt>) : int = rsm.StateCount

    /// Returns the set of start state indices across all blocks.
    let startStates (rsm: RSM<'t, 'nt>) : Set<int> =
        rsm.BlockStart.Values |> Seq.toList |> Set.ofList

    /// Computes block offsets (global start state for each block) ordered by state appearance.
    let private blockOffsets (rsm: RSM<'t, 'nt>) : (Nonterminal<'nt> * int) list =
        nonterminals rsm
        |> List.map (fun nt ->
            match rsm.BlockStart.TryGetValue(nt) with
            | true, gs -> (nt, gs)
            | false, _ -> failwithf "Block %A not found in BlockStart" nt)

    /// Reconstructs a single RsmBlock from flat RSM data for the given nonterminal.
    let private reconstructBlock (nt: Nonterminal<'nt>) (rsm: RSM<'t, 'nt>) : RsmBlock<'t, 'nt> option =
        match rsm.BlockStart.TryGetValue(nt) with
        | false, _ -> None
        | true, startGlobal ->

            let globalIndices =
                rsm.StateInfo
                |> Array.indexed
                |> Array.choose (fun (g, info) -> if info.BlockNonterminal = nt then Some(g, info) else None)

            if globalIndices.Length = 0 then
                None
            else

                let globalToLocal =
                    globalIndices |> Array.mapi (fun i (g, _) -> (g, i)) |> Map.ofArray

                let localSize = globalIndices.Length
                let startLocal = Map.find startGlobal globalToLocal

                let finalsLocal =
                    globalIndices
                    |> Array.choose (fun (g, info) -> if info.IsFinal then Map.tryFind g globalToLocal else None)
                    |> Set.ofArray

                let subTransitions = Matrix.init localSize localSize None

                for (globalFrom, _) in globalIndices do
                    for (globalTo, _) in globalIndices do
                        match Matrix.get rsm.Transitions globalFrom globalTo with
                        | Some labels ->
                            Matrix.set
                                subTransitions
                                (globalToLocal.[globalFrom])
                                (globalToLocal.[globalTo])
                                (Some labels)
                        | None -> ()

                let localTransitions =
                    [ for fromLocal in 0 .. localSize - 1 do
                          for toLocal in 0 .. localSize - 1 do
                              match Matrix.get subTransitions fromLocal toLocal with
                              | Some labels ->
                                  for label in NonEmptySet.toSeq labels do
                                      match label with
                                      | AutomatonLabel.ATerm sym -> (fromLocal, sym, toLocal)
                                      | _ -> ()
                              | None -> () ]

                let dfa =
                    Dfa.fromTransitions [ 0 .. localSize - 1 ] localTransitions startLocal finalsLocal

                Some { Nonterminal = nt; Dfa = dfa }

    /// Returns the list of all blocks in the RSM, reconstructed from flat storage.
    let blocks (rsm: RSM<'t, 'nt>) : RsmBlock<'t, 'nt> list =
        nonterminals rsm |> List.choose (fun nt -> reconstructBlock nt rsm)

    /// Finds the block associated with the given nonterminal, or None if not found.
    let blockOf (nt: Nonterminal<'nt>) (rsm: RSM<'t, 'nt>) : RsmBlock<'t, 'nt> option = reconstructBlock nt rsm

    /// Returns the start block of the RSM. Throws if the start block is not found.
    let startBlock (rsm: RSM<'t, 'nt>) : RsmBlock<'t, 'nt> =
        match reconstructBlock rsm.StartBlock rsm with
        | Some b -> b
        | None -> failwithf "Start block %A not found in RSM" rsm.StartBlock

    /// Returns the pre-computed flat lookup arrays for efficient parsing.
    /// Since RSM is already flat, this wraps the intrinsic flat data.
    let termTransitions (rsm: RSM<'t, 'nt>) : ResizeArray<Terminal<'t> * int> array =
        Array.init rsm.StateCount (fun i ->
            let arr = ResizeArray<Terminal<'t> * int>()

            for j in 0 .. rsm.StateCount - 1 do
                match Matrix.get rsm.Transitions i j with
                | Some labels ->
                    for label in NonEmptySet.toSeq labels do
                        match label with
                        | AutomatonLabel.ATerm(RsmSymbol.RTerm t) -> arr.Add(t, j)
                        | _ -> ()
                | None -> ()

            arr)

    /// Returns the pre-computed flat lookup arrays for nonterminal transitions.
    let nontermTransitions (rsm: RSM<'t, 'nt>) : ResizeArray<Nonterminal<'nt> * int> array =
        Array.init rsm.StateCount (fun i ->
            let arr = ResizeArray<Nonterminal<'nt> * int>()

            for j in 0 .. rsm.StateCount - 1 do
                match Matrix.get rsm.Transitions i j with
                | Some labels ->
                    for label in NonEmptySet.toSeq labels do
                        match label with
                        | AutomatonLabel.ATerm(RsmSymbol.RNonterm nt) -> arr.Add(nt, j)
                        | _ -> ()
                | None -> ()

            arr)

    /// Extends the RSM with an augmented start block S' -> originalStart.
    /// The new block states are added at the end of the state range to avoid
    /// renumbering existing states.
    /// Book reference: sec:CFPQ_RNGLR.
    let extendWithStart (freshStart: Nonterminal<'nt>) (rsm: RSM<'t, 'nt>) : RSM<'t, 'nt> =
        let originalStart = rsm.StartBlock
        let oldCount = rsm.StateCount
        let newCount = oldCount + 2

        let newTransitions = Matrix.init newCount newCount None

        for i in 0 .. oldCount - 1 do
            for j in 0 .. oldCount - 1 do
                match Matrix.get rsm.Transitions i j with
                | Some labels -> Matrix.set newTransitions i j (Some labels)
                | None -> ()

        let sLabel =
            AutomatonLabel.ATerm(RsmSymbol.RNonterm originalStart)
            |> NonEmptySet.singleton
            |> Some

        Matrix.set newTransitions oldCount (oldCount + 1) sLabel

        let newStateInfo = Array.zeroCreate newCount

        for i in 0 .. oldCount - 1 do
            newStateInfo.[i] <- rsm.StateInfo.[i]

        newStateInfo.[oldCount] <-
            { BlockNonterminal = freshStart
              LocalState = 0
              IsFinal = false }

        newStateInfo.[oldCount + 1] <-
            { BlockNonterminal = freshStart
              LocalState = 1
              IsFinal = true }

        let newBlockStart = Dictionary<Nonterminal<'nt>, int>(rsm.BlockStart)
        newBlockStart.[freshStart] <- oldCount

        let newFinalStates = Set.add (oldCount + 1) rsm.FinalStates

        { Transitions = newTransitions
          StateCount = newCount
          StateInfo = newStateInfo
          BlockStart = newBlockStart
          FinalStates = newFinalStates
          StartBlock = freshStart }


/// An extended RSM: the original RSM augmented with a fresh start nonterminal S'.
/// S' has a single transition: start --RNonterm(originalStart)--> final.
/// The type preserves the relationship between the original and augmented RSMs,
/// providing uniform access to the original start block regardless of extension.
/// Book reference: sec:CFPQ_RNGLR.
type ExtendedRSM<'t, 'nt when 't: comparison and 'nt: comparison> =
    { OriginalRsm: RSM<'t, 'nt>
      FreshStart: Nonterminal<'nt>
      ExtendedRsm: RSM<'t, 'nt> }


module ExtendedRSM =

    /// Creates an extended RSM by augmenting the given RSM with a fresh start nonterminal.
    let create (freshStart: Nonterminal<'nt>) (rsm: RSM<'t, 'nt>) : ExtendedRSM<'t, 'nt> =
        let extRsm = RSM.extendWithStart freshStart rsm

        { OriginalRsm = rsm
          FreshStart = freshStart
          ExtendedRsm = extRsm }

    /// Returns the original (non-extended) RSM.
    let originalRsm (ersm: ExtendedRSM<'t, 'nt>) : RSM<'t, 'nt> = ersm.OriginalRsm

    /// Returns the fresh start nonterminal (S') used for augmentation.
    let freshStart (ersm: ExtendedRSM<'t, 'nt>) : Nonterminal<'nt> = ersm.FreshStart

    /// Returns the extended (augmented) RSM.
    let extRsm (ersm: ExtendedRSM<'t, 'nt>) : RSM<'t, 'nt> = ersm.ExtendedRsm

    /// Returns the start block of the original RSM.
    let originalStartBlock (ersm: ExtendedRSM<'t, 'nt>) : RsmBlock<'t, 'nt> = RSM.startBlock ersm.OriginalRsm

    /// Returns the start nonterminal of the original RSM.
    let originalStartNonterminal (ersm: ExtendedRSM<'t, 'nt>) : Nonterminal<'nt> =
        (RSM.startBlock ersm.OriginalRsm).Nonterminal

    /// Returns the state count of the extended RSM.
    let stateCount (ersm: ExtendedRSM<'t, 'nt>) : int = RSM.stateCount ersm.ExtendedRsm

    /// Returns the blocks of the extended RSM.
    let extBlocks (ersm: ExtendedRSM<'t, 'nt>) : RsmBlock<'t, 'nt> list = RSM.blocks ersm.ExtendedRsm

    /// Returns the term transitions of the extended RSM for efficient lookup.
    let termTransitions (ersm: ExtendedRSM<'t, 'nt>) : ResizeArray<Terminal<'t> * int> array =
        RSM.termTransitions ersm.ExtendedRsm

    /// Returns the nonterm transitions of the extended RSM for efficient lookup.
    let nontermTransitions (ersm: ExtendedRSM<'t, 'nt>) : ResizeArray<Nonterminal<'nt> * int> array =
        RSM.nontermTransitions ersm.ExtendedRsm
