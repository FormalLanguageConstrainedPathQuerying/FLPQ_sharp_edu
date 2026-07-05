module AutomatonVisualizationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers


[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``simple automaton dot compiles`` () =
    let aut =
        Nfa.fromTransitions [ "q0"; "q1" ] [ (0, 'a', 1); (0, 'b', 0); (1, 'a', 1) ] Set.empty (set [ 0 ]) (set [ 1 ])

    let dot = AutomatonDot.nfaToDot string (fun i s -> s) aut
    Assert.Contains("digraph Automaton", dot)
    Assert.Contains("rankdir=LR", dot)
    Assert.Contains("fillcolor=green", dot)
    Assert.Contains("peripheries=2", dot)

    let info = ExternalTools.compileDotStringToInfo dot
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
        AutomatonDot.dfaToDot
            string
            (fun idx items ->
                let itemStrs =
                    items
                    |> Set.toSeq
                    |> Seq.map (fun (item: LR0Item<string, string>) ->
                        let lhs = item.lhs |> fun (Nonterminal n) -> n

                        let rhs =
                            item.rhs
                            |> List.mapi (fun i sym ->
                                let prefix = if i = item.dot then "·" else ""
                                let name = string sym
                                prefix + name)
                            |> String.concat " "

                        lhs + " -> " + rhs)

                    |> String.concat "\\n"

                itemStrs)
            aut

    Assert.Contains("digraph Automaton", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.True(info.nodeCount > 0)
    Assert.True(info.edgeCount > 0)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``automaton with no transitions compiles`` () =
    let aut = Nfa.fromTransitions [ "s0" ] [] Set.empty (set [ 0 ]) (set [ 0 ])

    let dot = AutomatonDot.nfaToDot string (fun i s -> s) aut

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(1, info.nodeCount)
    Assert.Equal(0, info.edgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``multiple start and final states`` () =
    let aut =
        Nfa.fromTransitions [ "q0"; "q1"; "q2" ] [ (0, 'x', 1); (1, 'y', 2) ] Set.empty (set [ 0; 1 ]) (set [ 1; 2 ])

    let dot = AutomatonDot.nfaToDot string (fun i s -> s) aut
    Assert.Contains("fillcolor=green", dot)
    Assert.Contains("peripheries=2", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(3, info.nodeCount)
    Assert.Equal(2, info.edgeCount)

let private tikzTemplatePath =
    System.IO.Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")

[<Fact>]
[<Trait("Category", "TeX")>]
let ``automaton with loop edges tikz compiles`` () =
    let aut =
        Nfa.fromTransitions [ "q0"; "q1" ] [ (0, 'a', 0); (0, 'b', 1); (1, 'c', 1) ] Set.empty (set [ 0 ]) (set [ 1 ])

    let tikz = AutomatonTikz.nfaToTikz string (fun _i s -> s) "circle" aut

    Assert.Contains("loop above", tikz)
    Assert.Contains("fill=green!30", tikz)
    Assert.Contains("double", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``automaton with epsilon loop tikz compiles`` () =
    let aut =
        Nfa.fromTransitions [ "q0" ] [] (Set.ofList [ (0, 0) ]) (set [ 0 ]) (set [ 0 ])

    let tikz = AutomatonTikz.nfaToTikz string (fun _i s -> s) "circle" aut

    Assert.Contains(@"\varepsilon", tikz)
    Assert.Contains("dotted", tikz)
    Assert.Contains("loop above", tikz)
    Assert.Contains("fill=green!30", tikz)
    Assert.Contains("double", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``simple automaton tikz compiles`` () =
    let aut =
        Nfa.fromTransitions [ "q0"; "q1" ] [ (0, 'a', 1); (0, 'b', 0); (1, 'a', 1) ] Set.empty (set [ 0 ]) (set [ 1 ])

    let tikz = AutomatonTikz.nfaToTikz string (fun _i s -> s) "circle" aut

    Assert.Contains(@"\begin{tikzpicture}", tikz)
    Assert.Contains(@"\graph", tikz)
    Assert.Contains("layered layout", tikz)
    Assert.Contains("grow'=right", tikz)
    Assert.Contains("fill=green!30", tikz)
    Assert.Contains("double", tikz)

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``automaton with no transitions tikz compiles`` () =
    let aut = Nfa.fromTransitions [ "s0" ] [] Set.empty (set [ 0 ]) (set [ 0 ])

    let tikz = AutomatonTikz.nfaToTikz string (fun _i s -> s) "circle" aut

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``multiple start and final states tikz compiles`` () =
    let aut =
        Nfa.fromTransitions [ "q0"; "q1"; "q2" ] [ (0, 'x', 1); (1, 'y', 2) ] Set.empty (set [ 0; 1 ]) (set [ 1; 2 ])

    let tikz = AutomatonTikz.nfaToTikz string (fun _i s -> s) "circle" aut

    Assert.Contains("fill=green!30", tikz)
    Assert.Contains("double", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``automaton with epsilon tikz compiles`` () =
    let aut =
        Nfa.fromTransitions [ "q0"; "q1" ] [ (0, 'a', 1) ] (Set.ofList [ (0, 1) ]) (set [ 0 ]) (set [ 1 ])

    let tikz = AutomatonTikz.nfaToTikz string (fun _i s -> s) "circle" aut

    Assert.Contains(@"\varepsilon", tikz)
    Assert.Contains("dotted", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``DFA tikz with rectangle shape compiles`` () =
    let aut = Dfa.fromTransitions [ "q0"; "q1" ] [ (0, 'a', 1) ] 0 (set [ 1 ])

    let tikz = AutomatonTikz.dfaToTikz string (fun _i s -> s) "rectangle" aut

    Assert.Contains("rectangle", tikz)
    Assert.Contains("double", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR(0) automaton Tikz compiles`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S
        S -> a
        "

    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let aut = LRAutomaton.buildLR0 aug

    let tikz = LRAutomatonTikz.lr0AutomatontoTikz string string aug aut

    Assert.Contains(@"\begin{tikzpicture}", tikz)
    Assert.Contains(@"rectangle", tikz)
    Assert.Contains(@"State 0", tikz)
    Assert.Contains(@"\begin{aligned}", tikz)
    Assert.Contains(@"\cdot", tikz)

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``SLR(1) automaton Tikz for grammar1 compiles`` () =
    let g = Grammar.parseGrammar "S -> a S b S\nS -> eps"

    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let aut = LRAutomaton.buildLR0 aug

    let tikz = LRAutomatonTikz.lr0AutomatontoTikz string string aug aut

    Assert.Contains(@"State 0", tikz)
    Assert.Contains(@"label=above:Start", tikz)
    Assert.Contains(@"fill=green!30", tikz)
    Assert.Contains("double", tikz)

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR(0) automaton Tikz has correct number of states`` () =
    let g = Grammar.parseGrammar "S -> a S b S\nS -> eps"

    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let aut = LRAutomaton.buildLR0 aug

    let tikz = LRAutomatonTikz.lr0AutomatontoTikz string string aug aut

    for i in 0 .. aut.states.Length - 1 do
        Assert.Contains(sprintf "State %d" i, tikz)

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)
