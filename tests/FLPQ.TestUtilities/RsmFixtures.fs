namespace FLPQ.TestUtilities

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra

/// Fixtures for hand-building RSMs in tests. The RSM builder never emits shapes like
/// missing blocks or epsilon edges, so tests construct them by hand.
module RsmFixtures =

    /// Builds an RSM from per-state info, transitions (one label per edge; duplicate
    /// edges are merged), block metadata, and final states.
    let mkRsm
        (stateInfo: RsmStateInfo<string> array)
        (transitions: Trans<AutomatonLabel<RsmSymbol<string, string>>> list)
        (blockStart: (Nonterminal<string> * int) list)
        (finalStates: int list)
        (startBlock: Nonterminal<string>)
        : RSM<string, string> =
        let n = stateInfo.Length
        let transitions' = Matrix.init n n None

        for t in transitions do
            match transitions'.[t.From, t.To] with
            | Some s -> transitions'.[t.From, t.To] <- Some(NonEmptySet.add t.Label s)
            | None -> transitions'.[t.From, t.To] <- Some(NonEmptySet.singleton t.Label)

        let blockStart' = Dictionary<Nonterminal<string>, int>()

        for k, v in blockStart do
            blockStart'.[k] <- v

        { Transitions = transitions'
          StateCount = n
          StateInfo = stateInfo
          BlockStart = blockStart'
          FinalStates = Set.ofList finalStates
          StartBlock = startBlock }
