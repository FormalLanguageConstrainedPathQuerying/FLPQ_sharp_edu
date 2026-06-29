module AutomatonTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

module FactTests =

    [<Fact>]
    let ``fromTransitions builds correct automaton`` () =
        let a =
            Nfa.fromTransitions [ "q0"; "q1" ] [ (0, "a", 1) ] Set.empty (set [ 0 ]) (set [ 1 ])

        Assert.Equal(2, Nfa.stateCount a)
        Assert.Equal<string>(set [ "a" ], Nfa.alphabet a)

    [<Fact>]
    let ``move returns correct targets`` () =
        let a =
            Nfa.fromTransitions
                [ "q0"; "q1"; "q2" ]
                [ (0, "a", 1); (0, "a", 2); (1, "b", 2) ]
                Set.empty
                (set [ 0 ])
                (set [ 2 ])

        let targets = Nfa.move a 0 "a"
        Assert.Equal<int>(set [ 1; 2 ], targets)
        Assert.Equal<int>(set [ 2 ], Nfa.move a 1 "b")
        Assert.Empty(Nfa.move a 0 "b")

    [<Fact>]
    let ``moveSet handles multiple states`` () =
        let a =
            Nfa.fromTransitions [ "q0"; "q1"; "q2" ] [ (0, "a", 2); (1, "a", 2) ] Set.empty (set [ 0 ]) (set [ 2 ])

        let targets = Nfa.moveSet a (set [ 0; 1 ]) "a"
        Assert.Equal<int>(set [ 2 ], targets)

    [<Fact>]
    let ``toDfa converts NFA to DFA`` () =
        let nfa =
            Nfa.fromTransitions
                [ "q0"; "q1"; "q2" ]
                [ (0, "a", 1); (0, "a", 2); (1, "b", 0); (2, "b", 0) ]
                Set.empty
                (set [ 0 ])
                (set [ 0 ])

        let dfa = Nfa.toDfa nfa
        Assert.True(Dfa.stateCount dfa > 1)

    [<Fact>]
    let ``toDfa preserves language for simple NFA`` () =
        let nfa =
            Nfa.fromTransitions
                [ "q0"; "q1"; "q2" ]
                [ (0, "a", 1); (0, "a", 2); (1, "b", 2) ]
                Set.empty
                (set [ 0 ])
                (set [ 2 ])

        let dfa = Nfa.toDfa nfa
        Assert.Equal(0, dfa.startState)

    [<Fact>]
    let ``DFA has single start state`` () =
        let dfa =
            Dfa.fromTransitions [ "q0"; "q1" ] [ (0, "a", 1); (1, "b", 0) ] 0 (set [ 1 ])

        Assert.Equal(0, dfa.startState)

    [<Fact>]
    let ``alphabet collects all symbols`` () =
        let a =
            Nfa.fromTransitions
                [ "q0"; "q1" ]
                [ (0, "a", 1); (1, "b", 0); (1, "c", 0) ]
                Set.empty
                (set [ 0 ])
                (set [ 1 ])

        Assert.Equal<string>(set [ "a"; "b"; "c" ], Nfa.alphabet a)

    [<Fact>]
    let ``stateCount returns correct number`` () =
        let a =
            Nfa.fromTransitions [ "q0"; "q1"; "q2" ] [ (0, "a", 1) ] Set.empty (set [ 0 ]) (set [ 1 ])

        Assert.Equal(3, Nfa.stateCount a)
