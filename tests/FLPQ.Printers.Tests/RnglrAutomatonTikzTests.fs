module RnglrAutomatonTikzTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

let private tikzTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")

/// The real RNGLR LR automaton for the registry grammar S -> a S b | eps.
let private anbnTable: RnglrTable<string, string> =
    let rsm = LanguageRegistry.ANBN.Grammars.[0].Rsm
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm

    RnglrLR.buildLR0Table (ExtendedRSM.extRsm ersm)

[<Fact>]
let ``renderRnglrItem renders the nonterminal and the RSM state`` () =
    let item: RnglrItem<string> =
        { BlockNonterminal = Nonterminal "S"
          RsmState = 3 }

    Assert.Equal("S : 3", RnglrAutomatonTikz.renderRnglrItem string item)

[<Fact>]
let ``renderRnglrStateContent has a state header and one line per item`` () =
    let items: Set<RnglrItem<string>> =
        set
            [ { BlockNonterminal = Nonterminal "S"
                RsmState = 1 }
              { BlockNonterminal = Nonterminal "A"
                RsmState = 0 } ]

    let content = RnglrAutomatonTikz.renderRnglrStateContent string 2 items

    Assert.StartsWith(@"\text{State 2}\\", content)
    Assert.Contains(@"A : 0 \\", content)
    Assert.Contains(@"S : 1 \\", content)
    // Set order: A before S.
    Assert.True(content.IndexOf("A : 0") < content.IndexOf("S : 1"), "items must appear in set (sorted) order")

[<Fact>]
let ``rnglrAutomatonToTikz renders rectangle states with aligned items and symbol edge labels`` () =
    let tikz =
        RnglrAutomatonTikz.rnglrAutomatonToTikz (SymbolTeX.toLaTeX string string) string anbnTable.Automaton

    Assert.Contains(@"\begin{tikzpicture}", tikz)
    Assert.Contains("rectangle", tikz)
    Assert.Contains(@"\text{State 0}", tikz)
    Assert.Contains(@"\begin{aligned}", tikz)
    // Start state styling and final-state double border.
    Assert.Contains("fill=green!30", tikz)
    Assert.Contains("double", tikz)
    // Edge labels for the terminal and nonterminal symbols of the grammar.
    Assert.Contains("s0 ->[", tikz)
    Assert.Contains(@"""a""", tikz)
    Assert.Contains(@"""S""", tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``RNGLR LR automaton Tikz compiles with lualatex`` () =
    let tikz =
        RnglrAutomatonTikz.rnglrAutomatonToTikz (SymbolTeX.toLaTeX string string) string anbnTable.Automaton

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)
