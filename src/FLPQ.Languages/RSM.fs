namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra

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
