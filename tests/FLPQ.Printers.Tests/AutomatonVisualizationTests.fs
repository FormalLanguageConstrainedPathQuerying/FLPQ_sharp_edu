module AutomatonVisualizationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.TestUtilities


[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``simple automaton dot compiles`` () =
    let aut =
        Nfa.fromTransitions
            [ "q0"; "q1" ]
            [ { From = 0; Label = 'a'; To = 1 }
              { From = 0; Label = 'b'; To = 0 }
              { From = 1; Label = 'a'; To = 1 } ]
            Set.empty
            (set [ 0 ])
            (set [ 1 ])

    let dot = AutomatonDot.nfaToDot string (fun i s -> s) aut
    Assert.Contains("digraph Automaton", dot)
    Assert.Contains("rankdir=LR", dot)
    Assert.Contains("fillcolor=green", dot)
    Assert.Contains("peripheries=2", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(2, info.NodeCount)
    Assert.Equal(3, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``DFA from LR(0) automaton dot compiles`` () =
    let g = LanguageRegistry.APlus.Grammars.[0].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
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

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.True(info.NodeCount > 0)
    Assert.True(info.EdgeCount > 0)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``automaton with no transitions compiles`` () =
    let aut = Nfa.fromTransitions [ "s0" ] [] Set.empty (set [ 0 ]) (set [ 0 ])

    let dot = AutomatonDot.nfaToDot string (fun i s -> s) aut

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(1, info.NodeCount)
    Assert.Equal(0, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``multiple start and final states`` () =
    let aut =
        Nfa.fromTransitions
            [ "q0"; "q1"; "q2" ]
            [ { From = 0; Label = 'x'; To = 1 }; { From = 1; Label = 'y'; To = 2 } ]
            Set.empty
            (set [ 0; 1 ])
            (set [ 1; 2 ])

    let dot = AutomatonDot.nfaToDot string (fun i s -> s) aut
    Assert.Contains("fillcolor=green", dot)
    Assert.Contains("peripheries=2", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(3, info.NodeCount)
    Assert.Equal(2, info.EdgeCount)

let private tikzTemplatePath =
    System.IO.Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")

[<Fact>]
[<Trait("Category", "TeX")>]
let ``automaton with loop edges tikz compiles`` () =
    let aut =
        Nfa.fromTransitions
            [ "q0"; "q1" ]
            [ { From = 0; Label = 'a'; To = 0 }
              { From = 0; Label = 'b'; To = 1 }
              { From = 1; Label = 'c'; To = 1 } ]
            Set.empty
            (set [ 0 ])
            (set [ 1 ])

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
        Nfa.fromTransitions
            [ "q0"; "q1" ]
            [ { From = 0; Label = 'a'; To = 1 }
              { From = 0; Label = 'b'; To = 0 }
              { From = 1; Label = 'a'; To = 1 } ]
            Set.empty
            (set [ 0 ])
            (set [ 1 ])

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
        Nfa.fromTransitions
            [ "q0"; "q1"; "q2" ]
            [ { From = 0; Label = 'x'; To = 1 }; { From = 1; Label = 'y'; To = 2 } ]
            Set.empty
            (set [ 0; 1 ])
            (set [ 1; 2 ])

    let tikz = AutomatonTikz.nfaToTikz string (fun _i s -> s) "circle" aut

    Assert.Contains("fill=green!30", tikz)
    Assert.Contains("double", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``automaton with epsilon tikz compiles`` () =
    let aut =
        Nfa.fromTransitions
            [ "q0"; "q1" ]
            [ { From = 0; Label = 'a'; To = 1 } ]
            (Set.ofList [ (0, 1) ])
            (set [ 0 ])
            (set [ 1 ])

    let tikz = AutomatonTikz.nfaToTikz string (fun _i s -> s) "circle" aut

    Assert.Contains(@"\varepsilon", tikz)
    Assert.Contains("dotted", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``DFA tikz with rectangle shape compiles`` () =
    let aut =
        Dfa.fromTransitions [ "q0"; "q1" ] [ { From = 0; Label = 'a'; To = 1 } ] 0 (set [ 1 ])

    let tikz = AutomatonTikz.dfaToTikz string (fun _i s -> s) "rectangle" aut

    Assert.Contains("rectangle", tikz)
    Assert.Contains("double", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR(0) automaton Tikz compiles`` () =
    let g = LanguageRegistry.APlus.Grammars.[0].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let aut = LRAutomaton.buildLR0 aug

    let labelPrinter = SymbolTeX.toLaTeX string string

    let stateVisualizer stateIdx items =
        LRAutomatonTikz.stateContentToTikzAs (LRAutomatonTikz.renderLR0StateContent string string stateIdx items)

    let tikz =
        LRAutomatonTikz.lr0AutomatonToTikz labelPrinter stateVisualizer "rectangle" aut

    Assert.Contains(@"\begin{tikzpicture}", tikz)
    Assert.Contains(@"rectangle", tikz)
    Assert.Contains(@"State 0", tikz)
    Assert.Contains(@"\begin{aligned}", tikz)
    Assert.Contains(@"\cdot", tikz)

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``SLR(1) automaton Tikz for grammar1 compiles`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let aut = LRAutomaton.buildLR0 aug

    let labelPrinter = SymbolTeX.toLaTeX string string

    let stateVisualizer stateIdx items =
        LRAutomatonTikz.stateContentToTikzAs (LRAutomatonTikz.renderLR0StateContent string string stateIdx items)

    let tikz =
        LRAutomatonTikz.lr0AutomatonToTikz labelPrinter stateVisualizer "rectangle" aut

    Assert.Contains(@"State 0", tikz)
    Assert.Contains(@"label=above:Start", tikz)
    Assert.Contains(@"fill=green!30", tikz)
    Assert.Contains("double", tikz)

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR(0) automaton Tikz has correct number of states`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let aut = LRAutomaton.buildLR0 aug

    let labelPrinter = SymbolTeX.toLaTeX string string

    let stateVisualizer stateIdx items =
        LRAutomatonTikz.stateContentToTikzAs (LRAutomatonTikz.renderLR0StateContent string string stateIdx items)

    let tikz =
        LRAutomatonTikz.lr0AutomatonToTikz labelPrinter stateVisualizer "rectangle" aut

    for i in 0 .. aut.States.Length - 1 do
        Assert.Contains(sprintf "State %d" i, tikz)

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)


module DotParseabilityPropertyTests =

    open FsCheck
    open FsCheck.Xunit
    open FLPQ.TestUtilities

    [<Properties(Arbitrary = [| typeof<IntersectionGenerators> |])>]
    module DotParseability =

        [<Property>]
        [<Trait("Category", "Graphviz")>]
        let ``NFA dot output is syntactically valid`` (nfa: NFA<string, int>) =
            let dot = AutomatonDot.nfaToDot string (fun _i s -> string s) nfa
            Assert.Contains("digraph", dot)
            Assert.Contains("{", dot)
            Assert.Contains("}", dot)
            let info = ExternalTools.compileDotStringToInfo dot
            info.NodeCount >= 0 && info.EdgeCount >= 0

        [<Property(MaxTest = 50)>]
        [<Trait("Category", "Graphviz")>]
        let ``NFA dot output parses with graphviz`` (nfa: NFA<string, int>) =
            let dot = AutomatonDot.nfaToDot string (fun _i s -> string s) nfa
            Assert.Contains("digraph", dot)
            let _info = ExternalTools.compileDotStringToInfo dot
            true


module AutomatonGoldenTests =

    open GoldenHelpers

    [<Fact>]
    let ``NFA a+ dot golden`` () =
        let aut =
            Nfa.fromTransitions
                [ "q0"; "q1" ]
                [ { From = 0; Label = 'a'; To = 1 }; { From = 1; Label = 'a'; To = 1 } ]
                Set.empty
                (set [ 0 ])
                (set [ 1 ])

        let dot = AutomatonDot.nfaToDot string (fun _i s -> s) aut
        verifyGolden "nfa_aplus.Dot" dot

    [<Fact>]
    let ``DFA a+ dot golden`` () =
        let aut =
            Dfa.fromTransitions
                [ "q0"; "q1" ]
                [ { From = 0; Label = 'a'; To = 1 }; { From = 1; Label = 'a'; To = 1 } ]
                0
                (set [ 1 ])

        let dot = AutomatonDot.dfaToDot string (fun _i s -> s) aut
        verifyGolden "dfa_aplus.Dot" dot

    [<Fact>]
    let ``NFA a+ tikz golden`` () =
        let aut =
            Nfa.fromTransitions
                [ "q0"; "q1" ]
                [ { From = 0; Label = 'a'; To = 1 }; { From = 1; Label = 'a'; To = 1 } ]
                Set.empty
                (set [ 0 ])
                (set [ 1 ])

        let tikz = AutomatonTikz.nfaToTikz string (fun _i s -> s) "circle" aut
        verifyGolden "nfa_aplus.tikz" tikz
