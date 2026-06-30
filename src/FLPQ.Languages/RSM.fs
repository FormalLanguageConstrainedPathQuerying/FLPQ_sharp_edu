namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra

/// Symbol labeling a transition in an RSM block.
/// Either a terminal (read input character) or a nonterminal (recursive call to another block).
[<RequireQualifiedAccess>]
type RsmSymbol<'t, 'nt when 't: comparison and 'nt: comparison> =
    | RTerm of Terminal<'t>
    | RNonterm of Nonterminal<'nt>

/// A single block in an RSM — a deterministic finite automaton for one nonterminal.
/// Transitions are over the alphabet Σ ∪ Q_S.
type RsmBlock<'t, 'nt when 't: comparison and 'nt: comparison> =
    { nonterminal: Nonterminal<'nt>
      dfa: DFA<RsmSymbol<'t, 'nt>, int> }

/// Recursive State Machine: a tuple ⟨N, Σ, B, B_S, Q, Q_S⟩ where each block B_{N_i}
/// is a deterministic finite automaton over Σ ∪ Q_S.
type RSM<'t, 'nt when 't: comparison and 'nt: comparison> =
    { blocks: RsmBlock<'t, 'nt> list
      startBlock: Nonterminal<'nt> }

module RSM =
    let blocks (rsm: RSM<'t, 'nt>) : RsmBlock<'t, 'nt> list = rsm.blocks

    let blockOf (nt: Nonterminal<'nt>) (rsm: RSM<'t, 'nt>) : RsmBlock<'t, 'nt> option =
        rsm.blocks |> List.tryFind (fun b -> b.nonterminal = nt)

    let startBlock (rsm: RSM<'t, 'nt>) : RsmBlock<'t, 'nt> =
        rsm.blocks |> List.find (fun b -> b.nonterminal = rsm.startBlock)

    let nonterminals (rsm: RSM<'t, 'nt>) : Nonterminal<'nt> list =
        rsm.blocks |> List.map (fun b -> b.nonterminal)

    let terminals (rsm: RSM<'t, 'nt>) : Terminal<'t> list =
        rsm.blocks
        |> List.collect (fun b ->
            Dfa.alphabet b.dfa
            |> Set.toList
            |> List.choose (function
                | RsmSymbol.RTerm t -> Some t
                | _ -> None))
        |> List.distinct

    let startStates (rsm: RSM<'t, 'nt>) : Set<int> =
        rsm.blocks |> List.map (fun b -> b.dfa.startState) |> Set.ofList

    let stateCount (rsm: RSM<'t, 'nt>) : int =
        rsm.blocks |> List.sumBy (fun b -> Dfa.stateCount b.dfa)
