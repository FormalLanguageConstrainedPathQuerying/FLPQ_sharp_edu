module AutomatonVisualizationTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra


[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``simple automaton dot compiles`` () =
    let aut =
        Automaton.fromTransitions [ "q0"; "q1" ] [ (0, 'a', 1); (0, 'b', 0); (1, 'a', 1) ] (set [ 0 ]) (set [ 1 ])

    let dot = AutomatonVisualizer.toDot (fun i s -> s) aut
    Assert.Contains("digraph Automaton", dot)
    Assert.Contains("rankdir=LR", dot)
    Assert.Contains("fillcolor=green", dot)
    Assert.Contains("peripheries=2", dot)

    let info = TestUtils.checkDotCompilesWithInfo dot
    Assert.Equal(2, info.nodeCount)
    Assert.Equal(3, info.edgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``DFA from LR(0) automaton dot compiles`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S
        S -> a
        "

    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let aut = LRAutomaton.buildLR0 aug

    let dot =
        AutomatonVisualizer.toDot
            (fun idx items ->
                let itemStrs =
                    items
                    |> Set.toSeq
                    |> Seq.map (fun (item: LR0Item<string, string>) ->
                        let lhs = item.Lhs |> fun (Nonterminal n) -> n

                        let rhs =
                            item.Rhs
                            |> List.mapi (fun i sym ->
                                let prefix = if i = item.Dot then "·" else ""
                                let name = string sym
                                prefix + name)
                            |> String.concat " "

                        lhs + " -> " + rhs)

                    |> String.concat "\\n"

                itemStrs)
            aut

    Assert.Contains("digraph Automaton", dot)

    let info = TestUtils.checkDotCompilesWithInfo dot
    Assert.True(info.nodeCount > 0)
    Assert.True(info.edgeCount > 0)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``automaton with no transitions compiles`` () =
    let aut = Automaton.fromTransitions [ "s0" ] [] (set [ 0 ]) (set [ 0 ])

    let dot = AutomatonVisualizer.toDot (fun i s -> s) aut

    let info = TestUtils.checkDotCompilesWithInfo dot
    Assert.Equal(1, info.nodeCount)
    Assert.Equal(0, info.edgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``multiple start and final states`` () =
    let aut =
        Automaton.fromTransitions [ "q0"; "q1"; "q2" ] [ (0, 'x', 1); (1, 'y', 2) ] (set [ 0; 1 ]) (set [ 1; 2 ])

    let dot = AutomatonVisualizer.toDot (fun i s -> s) aut
    Assert.Contains("fillcolor=green", dot)
    Assert.Contains("peripheries=2", dot)

    let info = TestUtils.checkDotCompilesWithInfo dot
    Assert.Equal(3, info.nodeCount)
    Assert.Equal(2, info.edgeCount)
