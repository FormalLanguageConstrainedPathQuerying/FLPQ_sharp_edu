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
type RsmBlock<'t, 'nt when 't: comparison and 'nt: comparison> =
    { nonterminal: Nonterminal<'nt>
      dfa: RsmDfa<'t, 'nt> }

/// Recursive State Machine: a tuple ⟨N, Σ, B, B_S, Q, Q_S⟩ where each block B_{N_i}
/// is a deterministic finite automaton over Σ ∪ Q_S.
type RSM<'t, 'nt when 't: comparison and 'nt: comparison> =
    { blocks: RsmBlock<'t, 'nt> list
      startBlock: Nonterminal<'nt> }

/// Mapping from global RSM state index to block information.
/// Book reference: sec:CFPQ_GLL.
[<Struct>]
type RsmStateInfo<'nt when 'nt: comparison> =
    { blockNonterminal: Nonterminal<'nt>
      localState: int
      isFinal: bool }

/// Pre-computed flattened representation of an RSM for efficient lookup during parsing.
/// Book reference: sec:CFPQ_GLL, sec:CFPQ_RNGLR.
type FlattenedRsm<'t, 'nt when 't: comparison and 'nt: comparison> =
    { stateInfo: RsmStateInfo<'nt> array
      blockStart: Dictionary<Nonterminal<'nt>, int>
      finalStates: Set<int>
      termTrans: ResizeArray<Terminal<'t> * int> array
      nontermTrans: ResizeArray<Nonterminal<'nt> * int> array }

module RSM =

    /// Returns the list of all blocks in the RSM.
    let blocks (rsm: RSM<'t, 'nt>) : RsmBlock<'t, 'nt> list = rsm.blocks

    /// Finds the block associated with the given nonterminal, or None if not found.
    let blockOf (nt: Nonterminal<'nt>) (rsm: RSM<'t, 'nt>) : RsmBlock<'t, 'nt> option =
        rsm.blocks |> List.tryFind (fun b -> b.nonterminal = nt)

    /// Returns the start block of the RSM. Throws if the start block is not found.
    let startBlock (rsm: RSM<'t, 'nt>) : RsmBlock<'t, 'nt> =
        rsm.blocks |> List.find (fun b -> b.nonterminal = rsm.startBlock)

    /// Returns the list of all nonterminals (block names) in the RSM.
    let nonterminals (rsm: RSM<'t, 'nt>) : Nonterminal<'nt> list =
        rsm.blocks |> List.map (fun b -> b.nonterminal)

    /// Returns the list of all distinct terminal symbols across all blocks.
    let terminals (rsm: RSM<'t, 'nt>) : Terminal<'t> list =
        rsm.blocks
        |> List.collect (fun b ->
            Dfa.alphabet b.dfa
            |> Set.toList
            |> List.choose (function
                | RsmSymbol.RTerm t -> Some t
                | _ -> None))
        |> List.distinct

    /// Returns the set of start state indices across all blocks.
    let startStates (rsm: RSM<'t, 'nt>) : Set<int> =
        rsm.blocks |> List.map (fun b -> b.dfa.startState) |> Set.ofList

    /// Returns the total number of states across all blocks.
    let stateCount (rsm: RSM<'t, 'nt>) : int =
        rsm.blocks |> List.sumBy (fun b -> Dfa.stateCount b.dfa)

    /// Flattens the RSM into pre-computed arrays for efficient lookup during parsing.
    /// Collapses all blocks into a single state space with:
    /// - stateInfo: global state → block info (nonterminal, local state, isFinal)
    /// - blockStart: nonterminal → global start state
    /// - finalStates: set of global final state indices
    /// - termTrans: global state → list of (terminal, nextGlobalState)
    /// - nontermTrans: global state → list of (nonterminal, nextGlobalState)
    let flattenRsm (rsm: RSM<'t, 'nt>) : FlattenedRsm<'t, 'nt> =
        let blocksList = rsm.blocks
        let totalStates = stateCount rsm

        let stateInfo = Array.zeroCreate<RsmStateInfo<'nt>> totalStates
        let blockStart = Dictionary<Nonterminal<'nt>, int>()
        let mutable finalStates = Set.empty<int>

        let termTrans = Array.init totalStates (fun _ -> ResizeArray<Terminal<'t> * int>())

        let nontermTrans =
            Array.init totalStates (fun _ -> ResizeArray<Nonterminal<'nt> * int>())

        let mutable globalOffset = 0

        for block in blocksList do
            let dfa = block.dfa
            let localSize = Dfa.stateCount dfa
            let localFinal = dfa.finalStates

            blockStart.[block.nonterminal] <- globalOffset + dfa.startState

            for localState in 0 .. localSize - 1 do
                let globalState = globalOffset + localState
                let isFinal = Set.contains localState localFinal

                stateInfo.[globalState] <-
                    { blockNonterminal = block.nonterminal
                      localState = localState
                      isFinal = isFinal }

                if isFinal then
                    finalStates <- Set.add globalState finalStates

                for localTarget in 0 .. localSize - 1 do
                    match Matrix.get dfa.transitions localState localTarget with
                    | Some labels ->
                        let targetGlobal = globalOffset + localTarget

                        for label in NonEmptySet.toSeq labels do
                            match label with
                            | AutomatonLabel.ATerm(RsmSymbol.RTerm t) -> termTrans.[globalState].Add(t, targetGlobal)
                            | AutomatonLabel.ATerm(RsmSymbol.RNonterm nt) ->
                                nontermTrans.[globalState].Add(nt, targetGlobal)
                            | AutomatonLabel.AEpsilon -> ()
                    | None -> ()

            globalOffset <- globalOffset + localSize

        { stateInfo = stateInfo
          blockStart = blockStart
          finalStates = finalStates
          termTrans = termTrans
          nontermTrans = nontermTrans }

    /// Extends the RSM with an augmented start block S' -> originalStart.
    /// The new block S' has one transition: start --RNonterm(originalStart)--> final.
    /// This ensures LR automaton has goto entries for the original start nonterminal.
    /// Book reference: sec:CFPQ_RNGLR.
    let extendWithStart (freshStart: Nonterminal<'nt>) (rsm: RSM<'t, 'nt>) : RSM<'t, 'nt> =
        let originalStart = rsm.startBlock

        let startBlock' =
            let transitions = [ (0, RsmSymbol.RNonterm originalStart, 1) ]

            let dfa = Dfa.fromTransitions [ 0; 1 ] transitions 0 (Set.singleton 1)

            { nonterminal = freshStart; dfa = dfa }

        { blocks = startBlock' :: rsm.blocks
          startBlock = freshStart }
