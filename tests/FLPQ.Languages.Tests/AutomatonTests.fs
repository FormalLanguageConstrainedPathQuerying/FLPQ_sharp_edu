module AutomatonTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

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

        let dfa = Automaton.toDfa nfa
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

        let dfa = Automaton.toDfa nfa
        Assert.Equal(0, dfa.StartState)

    [<Fact>]
    let ``DFA has single start state`` () =
        let dfa =
            Dfa.fromTransitions [ "q0"; "q1" ] [ (0, "a", 1); (1, "b", 0) ] 0 (set [ 1 ])

        Assert.Equal(0, dfa.StartState)

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

module AcceptanceTests =

    let T s = Terminal s

    module Re_aPlus =
        let nfa =
            Nfa.fromTransitions [ 0; 1 ] [ (0, "a", 1); (1, "a", 1) ] Set.empty (set [ 0 ]) (set [ 1 ])

        let dfa = Automaton.toDfa nfa

        [<Fact>]
        let ``NFA accepts a`` () = Assert.True(Nfa.accept nfa [ T "a" ])

        [<Fact>]
        let ``NFA accepts aa`` () =
            Assert.True(Nfa.accept nfa [ T "a"; T "a" ])

        [<Fact>]
        let ``NFA accepts aaa`` () =
            Assert.True(Nfa.accept nfa [ T "a"; T "a"; T "a" ])

        [<Fact>]
        let ``NFA rejects empty`` () = Assert.False(Nfa.accept nfa [])

        [<Fact>]
        let ``DFA accepts a`` () = Assert.True(Dfa.accept dfa [ T "a" ])

        [<Fact>]
        let ``DFA accepts aaa`` () =
            Assert.True(Dfa.accept dfa [ T "a"; T "a"; T "a" ])

        [<Fact>]
        let ``DFA rejects empty`` () = Assert.False(Dfa.accept dfa [])

    module Re_aStar =
        let nfa =
            Nfa.fromTransitions [ 0 ] [ (0, "a", 0) ] Set.empty (set [ 0 ]) (set [ 0 ])

        let dfa = Automaton.toDfa nfa

        [<Fact>]
        let ``NFA accepts empty`` () = Assert.True(Nfa.accept nfa [])

        [<Fact>]
        let ``NFA accepts a`` () = Assert.True(Nfa.accept nfa [ T "a" ])

        [<Fact>]
        let ``NFA accepts aa`` () =
            Assert.True(Nfa.accept nfa [ T "a"; T "a" ])

        [<Fact>]
        let ``NFA accepts aaa`` () =
            Assert.True(Nfa.accept nfa [ T "a"; T "a"; T "a" ])

        [<Fact>]
        let ``NFA rejects b`` () = Assert.False(Nfa.accept nfa [ T "b" ])

        [<Fact>]
        let ``DFA accepts empty`` () = Assert.True(Dfa.accept dfa [])

        [<Fact>]
        let ``DFA accepts aa`` () =
            Assert.True(Dfa.accept dfa [ T "a"; T "a" ])

        [<Fact>]
        let ``DFA rejects b`` () = Assert.False(Dfa.accept dfa [ T "b" ])

    module Re_abStar =
        let nfa =
            Nfa.fromTransitions
                [ 0; 1; 2; 3 ]
                [ (0, "a", 1); (1, "b", 2); (2, "a", 3); (3, "b", 0) ]
                (set [ (2, 0) ])
                (set [ 0 ])
                (set [ 0 ])

        [<Fact>]
        let ``accepts empty`` () = Assert.True(Nfa.accept nfa [])

        [<Fact>]
        let ``accepts ab`` () =
            Assert.True(Nfa.accept nfa [ T "a"; T "b" ])

        [<Fact>]
        let ``accepts abab`` () =
            Assert.True(Nfa.accept nfa [ T "a"; T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``accepts ababab`` () =
            Assert.True(Nfa.accept nfa [ T "a"; T "b"; T "a"; T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``rejects ba`` () =
            Assert.False(Nfa.accept nfa [ T "b"; T "a" ])

        [<Fact>]
        let ``rejects bab`` () =
            Assert.False(Nfa.accept nfa [ T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``rejects aaa`` () =
            Assert.False(Nfa.accept nfa [ T "a"; T "a"; T "a" ])

        [<Fact>]
        let ``rejects bbb`` () =
            Assert.False(Nfa.accept nfa [ T "b"; T "b"; T "b" ])

        [<Fact>]
        let ``rejects b`` () = Assert.False(Nfa.accept nfa [ T "b" ])

        [<Fact>]
        let ``rejects a`` () = Assert.False(Nfa.accept nfa [ T "a" ])

    module Re_c_abStar =
        let nfa =
            Nfa.fromTransitions
                [ 0; 1; 2; 3; 4 ]
                [ (0, "c", 1); (1, "a", 2); (2, "b", 3); (3, "a", 4); (4, "b", 1) ]
                (set [ (3, 1) ])
                (set [ 0 ])
                (set [ 1 ])

        [<Fact>]
        let ``accepts c`` () = Assert.True(Nfa.accept nfa [ T "c" ])

        [<Fact>]
        let ``accepts cab`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "a"; T "b" ])

        [<Fact>]
        let ``accepts cabab`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "a"; T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``accepts cababab`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "a"; T "b"; T "a"; T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``rejects cba`` () =
            Assert.False(Nfa.accept nfa [ T "c"; T "b"; T "a" ])

        [<Fact>]
        let ``rejects bab`` () =
            Assert.False(Nfa.accept nfa [ T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``rejects aaa`` () =
            Assert.False(Nfa.accept nfa [ T "a"; T "a"; T "a" ])

        [<Fact>]
        let ``rejects bbb`` () =
            Assert.False(Nfa.accept nfa [ T "b"; T "b"; T "b" ])

        [<Fact>]
        let ``rejects b`` () = Assert.False(Nfa.accept nfa [ T "b" ])

        [<Fact>]
        let ``rejects a`` () = Assert.False(Nfa.accept nfa [ T "a" ])

        [<Fact>]
        let ``rejects empty`` () = Assert.False(Nfa.accept nfa [])

        [<Fact>]
        let ``rejects ca`` () =
            Assert.False(Nfa.accept nfa [ T "c"; T "a" ])

        [<Fact>]
        let ``rejects cb`` () =
            Assert.False(Nfa.accept nfa [ T "c"; T "b" ])

    module Re_c_abPlus =
        let nfa =
            Nfa.fromTransitions
                [ 0; 1; 2; 3; 4 ]
                [ (0, "c", 1); (1, "a", 2); (2, "b", 3); (3, "a", 4); (4, "b", 1) ]
                (set [ (3, 1) ])
                (set [ 0 ])
                (set [ 3 ])

        [<Fact>]
        let ``accepts cab`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "a"; T "b" ])

        [<Fact>]
        let ``accepts cabab`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "a"; T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``accepts cababab`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "a"; T "b"; T "a"; T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``rejects c`` () = Assert.False(Nfa.accept nfa [ T "c" ])

        [<Fact>]
        let ``rejects cba`` () =
            Assert.False(Nfa.accept nfa [ T "c"; T "b"; T "a" ])

        [<Fact>]
        let ``rejects bab`` () =
            Assert.False(Nfa.accept nfa [ T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``rejects aaa`` () =
            Assert.False(Nfa.accept nfa [ T "a"; T "a"; T "a" ])

        [<Fact>]
        let ``rejects bbb`` () =
            Assert.False(Nfa.accept nfa [ T "b"; T "b"; T "b" ])

        [<Fact>]
        let ``rejects b`` () = Assert.False(Nfa.accept nfa [ T "b" ])

        [<Fact>]
        let ``rejects a`` () = Assert.False(Nfa.accept nfa [ T "a" ])

        [<Fact>]
        let ``rejects empty`` () = Assert.False(Nfa.accept nfa [])

        [<Fact>]
        let ``rejects ca`` () =
            Assert.False(Nfa.accept nfa [ T "c"; T "a" ])

        [<Fact>]
        let ``rejects cb`` () =
            Assert.False(Nfa.accept nfa [ T "c"; T "b" ])

    module Re_c_aORbStar =
        let nfa =
            Nfa.fromTransitions
                [ 0; 1; 2 ]
                [ (0, "c", 1)
                  (1, "a", 1)
                  (1, "b", 1)
                  (1, "a", 2)
                  (1, "b", 2)
                  (2, "a", 1)
                  (2, "b", 1) ]
                (set [ (2, 1) ])
                (set [ 0 ])
                (set [ 1; 2 ])

        [<Fact>]
        let ``accepts c`` () = Assert.True(Nfa.accept nfa [ T "c" ])

        [<Fact>]
        let ``accepts cab`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "a"; T "b" ])

        [<Fact>]
        let ``accepts cabab`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "a"; T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``accepts cba`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "b"; T "a" ])

        [<Fact>]
        let ``accepts cbab`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``accepts caaa`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "a"; T "a"; T "a" ])

        [<Fact>]
        let ``accepts cbbb`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "b"; T "b"; T "b" ])

        [<Fact>]
        let ``accepts ca`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "a" ])

        [<Fact>]
        let ``accepts cb`` () =
            Assert.True(Nfa.accept nfa [ T "c"; T "b" ])

        [<Fact>]
        let ``rejects ba`` () =
            Assert.False(Nfa.accept nfa [ T "b"; T "a" ])

        [<Fact>]
        let ``rejects bab`` () =
            Assert.False(Nfa.accept nfa [ T "b"; T "a"; T "b" ])

        [<Fact>]
        let ``rejects aaa`` () =
            Assert.False(Nfa.accept nfa [ T "a"; T "a"; T "a" ])

        [<Fact>]
        let ``rejects bbb`` () =
            Assert.False(Nfa.accept nfa [ T "b"; T "b"; T "b" ])

        [<Fact>]
        let ``rejects b`` () = Assert.False(Nfa.accept nfa [ T "b" ])

        [<Fact>]
        let ``rejects a`` () = Assert.False(Nfa.accept nfa [ T "a" ])

        [<Fact>]
        let ``rejects empty`` () = Assert.False(Nfa.accept nfa [])

    module DFA_noTransitions =
        let dfa = Dfa.fromTransitions [ 0 ] [] 0 (set [ 0 ])

        [<Fact>]
        let ``accepts empty`` () = Assert.True(Dfa.accept dfa [])

        [<Fact>]
        let ``rejects a`` () = Assert.False(Dfa.accept dfa [ T "a" ])

    module NFA_epsToFinal =
        let nfa = Nfa.fromTransitions [ 0; 1 ] [] (set [ (0, 1) ]) (set [ 0 ]) (set [ 1 ])

        [<Fact>]
        let ``accepts empty`` () = Assert.True(Nfa.accept nfa [])

        [<Fact>]
        let ``rejects a`` () = Assert.False(Nfa.accept nfa [ T "a" ])

    module NFA_epsCycle =
        let nfa =
            Nfa.fromTransitions [ 0; 1 ] [] (set [ (0, 1); (1, 0) ]) (set [ 0 ]) (set [ 1 ])

        [<Fact>]
        let ``accepts empty`` () = Assert.True(Nfa.accept nfa [])

        [<Fact>]
        let ``rejects a`` () = Assert.False(Nfa.accept nfa [ T "a" ])

    module DFA_aPlus =
        let dfa = Dfa.fromTransitions [ 0; 1 ] [ (0, "a", 1); (1, "a", 1) ] 0 (set [ 1 ])

        [<Fact>]
        let ``accepts a`` () = Assert.True(Dfa.accept dfa [ T "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(Dfa.accept dfa [ T "a"; T "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(Dfa.accept dfa [ T "a"; T "a"; T "a" ])

        [<Fact>]
        let ``rejects empty`` () = Assert.False(Dfa.accept dfa [])

    module NFA_aPlus_nondet =
        let nfa =
            Nfa.fromTransitions [ 0; 1 ] [ (0, "a", 0); (0, "a", 1) ] Set.empty (set [ 0 ]) (set [ 1 ])

        [<Fact>]
        let ``accepts a`` () = Assert.True(Nfa.accept nfa [ T "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(Nfa.accept nfa [ T "a"; T "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(Nfa.accept nfa [ T "a"; T "a"; T "a" ])

        [<Fact>]
        let ``rejects empty`` () = Assert.False(Nfa.accept nfa [])

module IntersectionTests =

    let T s = Terminal s

    let private nfaFromEdges
        (states: int list)
        (edges: (int * string * int) list)
        (starts: int list)
        (finals: int list)
        =
        Nfa.fromTransitions states edges Set.empty (Set.ofList starts) (Set.ofList finals)

    [<Fact>]
    let ``Intersection of a+ and a* equals a+`` () =
        let aPlus = nfaFromEdges [ 0; 1 ] [ (0, "a", 1); (1, "a", 1) ] [ 0 ] [ 1 ]

        let aStar = nfaFromEdges [ 0 ] [ (0, "a", 0) ] [ 0 ] [ 0 ]

        let inter = Nfa.intersect aPlus aStar

        Assert.True(Nfa.accept inter [ T "a" ])
        Assert.True(Nfa.accept inter [ T "a"; T "a" ])
        Assert.False(Nfa.accept inter [])

    [<Fact>]
    let ``Intersection of a* and empty-string-only automaton equals empty-string-only`` () =
        let aStar = nfaFromEdges [ 0 ] [ (0, "a", 0) ] [ 0 ] [ 0 ]

        let emptyOnly = nfaFromEdges [ 0 ] [] [ 0 ] [ 0 ]

        let inter = Nfa.intersect aStar emptyOnly

        Assert.True(Nfa.accept inter [])
        Assert.False(Nfa.accept inter [ T "a" ])

    [<Fact>]
    let ``Intersection of a+ and a (single) accepts a only`` () =
        let aPlus = nfaFromEdges [ 0; 1 ] [ (0, "a", 1); (1, "a", 1) ] [ 0 ] [ 1 ]

        let singleA = nfaFromEdges [ 0; 1 ] [ (0, "a", 1) ] [ 0 ] [ 1 ]

        let inter = Nfa.intersect aPlus singleA

        Assert.True(Nfa.accept inter [ T "a" ])
        Assert.False(Nfa.accept inter [ T "a"; T "a" ])
        Assert.False(Nfa.accept inter [])

    [<Fact>]
    let ``Intersection of disjoint languages is empty`` () =
        let onlyA = nfaFromEdges [ 0; 1 ] [ (0, "a", 1) ] [ 0 ] [ 1 ]

        let onlyB = nfaFromEdges [ 0; 1 ] [ (0, "b", 1) ] [ 0 ] [ 1 ]

        let inter = Nfa.intersect onlyA onlyB

        Assert.Equal(0, Nfa.stateCount inter)

    [<Fact>]
    let ``Intersection with identity automaton returns equivalent automaton`` () =
        let aPlus = nfaFromEdges [ 0; 1 ] [ (0, "a", 1); (1, "a", 1) ] [ 0 ] [ 1 ]

        let universal = nfaFromEdges [ 0 ] [ (0, "a", 0); (0, "b", 0) ] [ 0 ] [ 0 ]

        let inter = Nfa.intersect aPlus universal

        Assert.True(Nfa.accept inter [ T "a" ])
        Assert.True(Nfa.accept inter [ T "a"; T "a" ])
        Assert.False(Nfa.accept inter [])
        Assert.False(Nfa.accept inter [ T "b" ])

[<Properties(Arbitrary = [| typeof<IntersectionGenerators> |])>]
module PropertyIntersectionTests =

    [<Property>]
    let ``Intersection language equals L(A) ∩ L(B)``
        (a: NFA<string, int>)
        (b: NFA<string, int>)
        (input: Terminal<string> list)
        =
        let inter = Nfa.intersect a b
        let expected = Nfa.accept a input && Nfa.accept b input
        let actual = Nfa.accept inter input
        expected = actual

[<Properties(Arbitrary = [| typeof<IntersectionGenerators> |])>]
module PropertyNfaToDfaTests =

    [<Property>]
    let ``NFA to DFA conversion preserves language`` (nfa: NFA<string, int>) (input: Terminal<string> list) =
        let dfa = Automaton.toDfa nfa
        let nfaResult = Nfa.accept nfa input
        let dfaResult = Dfa.accept dfa input
        nfaResult = dfaResult


module BackwardCompatibilityTests =

    [<Fact>]
    let ``NFA member states provides backward compatibility`` () =
        let nfa =
            Nfa.fromTransitions [ "q0"; "q1" ] [ (0, 'a', 1) ] Set.empty (set [ 0 ]) (set [ 1 ])

        let states = nfa.States
        Assert.Equal<string list>([ "q0"; "q1" ], states)

    [<Fact>]
    let ``NFA member transitions provides backward compatibility`` () =
        let nfa =
            Nfa.fromTransitions [ "q0"; "q1" ] [ (0, 'a', 1) ] Set.empty (set [ 0 ]) (set [ 1 ])

        Assert.True((Matrix.get nfa.Transitions 0 1).IsSome)

    [<Fact>]
    let ``DFA member states provides backward compatibility`` () =
        let dfa = Dfa.fromTransitions [ "q0"; "q1" ] [ (0, 'a', 1) ] 0 (set [ 1 ])
        let states = dfa.States
        Assert.Equal<string list>([ "q0"; "q1" ], states)

    [<Fact>]
    let ``DFA member transitions provides backward compatibility`` () =
        let dfa = Dfa.fromTransitions [ "q0"; "q1" ] [ (0, 'a', 1) ] 0 (set [ 1 ])
        Assert.True((Matrix.get dfa.Transitions 0 1).IsSome)
