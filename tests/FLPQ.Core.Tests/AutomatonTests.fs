module AutomatonTests

open Xunit
open FLPQ.Core

module FactTests =

    [<Fact>]
    let ``fromTransitions builds correct automaton`` () =
        let a =
            Automaton.fromTransitions [ "q0"; "q1" ] [ (0, "a", 1) ] (set [ 0 ]) (set [ 1 ])

        Assert.Equal(2, Automaton.stateCount a)
        Assert.Equal<string>(set [ "a" ], Automaton.alphabet a)

    [<Fact>]
    let ``move returns correct targets`` () =
        let a =
            Automaton.fromTransitions
                [ "q0"; "q1"; "q2" ]
                [ (0, "a", 1); (0, "a", 2); (1, "b", 2) ]
                (set [ 0 ])
                (set [ 2 ])

        let targets = Automaton.move a 0 "a"
        Assert.Equal<int>(set [ 1; 2 ], targets)
        Assert.Equal<int>(set [ 2 ], Automaton.move a 1 "b")
        Assert.Empty(Automaton.move a 0 "b")

    [<Fact>]
    let ``moveSet handles multiple states`` () =
        let a =
            Automaton.fromTransitions [ "q0"; "q1"; "q2" ] [ (0, "a", 2); (1, "a", 2) ] (set [ 0 ]) (set [ 2 ])

        let targets = Automaton.moveSet a (set [ 0; 1 ]) "a"
        Assert.Equal<int>(set [ 2 ], targets)

    [<Fact>]
    let ``toDfa converts NFA to DFA`` () =
        let nfa =
            Automaton.fromTransitions
                [ "q0"; "q1"; "q2" ]
                [ (0, "a", 1); (0, "a", 2); (1, "b", 0); (2, "b", 0) ]
                (set [ 0 ])
                (set [ 0 ])

        let dfa = Automaton.toDfa nfa
        Assert.True(Automaton.isDeterministic dfa)
        Assert.Equal(1, dfa.startStates.Count)

    [<Fact>]
    let ``toDfa preserves language for simple NFA`` () =
        let nfa =
            Automaton.fromTransitions
                [ "q0"; "q1"; "q2" ]
                [ (0, "a", 1); (0, "a", 2); (1, "b", 2) ]
                (set [ 0 ])
                (set [ 2 ])

        let dfa = Automaton.toDfa nfa
        Assert.True(Automaton.isDeterministic dfa)

    [<Fact>]
    let ``isDeterministic returns false for NFA`` () =
        let nfa =
            Automaton.fromTransitions [ "q0"; "q1"; "q2" ] [ (0, "a", 1); (0, "a", 2) ] (set [ 0 ]) (set [ 2 ])

        Assert.False(Automaton.isDeterministic nfa)

    [<Fact>]
    let ``isDeterministic returns true for DFA`` () =
        let dfa =
            Automaton.fromTransitions [ "q0"; "q1" ] [ (0, "a", 1); (1, "b", 0) ] (set [ 0 ]) (set [ 1 ])

        Assert.True(Automaton.isDeterministic dfa)

    [<Fact>]
    let ``alphabet collects all symbols`` () =
        let a =
            Automaton.fromTransitions [ "q0"; "q1" ] [ (0, "a", 1); (1, "b", 0); (1, "c", 0) ] (set [ 0 ]) (set [ 1 ])

        Assert.Equal<string>(set [ "a"; "b"; "c" ], Automaton.alphabet a)

    [<Fact>]
    let ``stateCount returns correct number`` () =
        let a =
            Automaton.fromTransitions [ "q0"; "q1"; "q2" ] [ (0, "a", 1) ] (set [ 0 ]) (set [ 1 ])

        Assert.Equal(3, Automaton.stateCount a)
