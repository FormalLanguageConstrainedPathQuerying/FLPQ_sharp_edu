module RnglrTableTexTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

open GoldenHelpers

/// RNGLR table with three goto nonterminals (A, S, B), so the header row needs separators.
let private table =
    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.SingleAB "threeRule").Rsm
    let ersm = ExtendedRSM.create (Nonterminal "S'") rsm
    RnglrLR.buildLR0Table (ExtendedRSM.extRsm ersm)

/// Degenerate table with no actions and no goto entries.
let private emptyTable: RnglrTable<string, string> =
    { Action = Map.empty
      Goto = Map.empty
      Automaton = Dfa.fromTransitions [ Set.empty ] [] 0 (set [ 0 ]) }

[<Fact>]
let ``tableToTeX wraps the tabular in a center environment`` () =
    let tex = RnglrTableTeX.tableToTeX string string table
    Assert.StartsWith(@"\begin{center}", tex)
    Assert.EndsWith(@"\end{center}", tex)

[<Fact>]
let ``tableToTeXTabularOnly emits no wrapper`` () =
    let tex = RnglrTableTeX.tableToTeXTabularOnly string string table
    Assert.DoesNotContain("center", tex)
    Assert.StartsWith(@"\begin{tabular}", tex)

[<Fact>]
let ``table with multiple goto nonterminals separates the header columns`` () =
    let tex = RnglrTableTeX.tableToTeXTabularOnly string string table
    Assert.Contains(@"A & S & B", tex)

[<Fact>]
let ``table with zero nonterminals has an empty goto column spec`` () =
    let tex = RnglrTableTeX.tableToTeXTabularOnly string string emptyTable
    Assert.Contains(@"\begin{tabular}{ c || c ||  }", tex)

[<Fact>]
let ``highlighted table with zero nonterminals has an empty goto column spec`` () =
    let tex =
        RnglrTableTeX.tableToTeXWithHighlightsTabularOnly string string emptyTable None Set.empty Set.empty

    Assert.Contains(@"\begin{tabular}{ c || c ||  }", tex)

[<Fact>]
let ``currentLrState highlights the row with rowcolor`` () =
    let tex =
        RnglrTableTeX.tableToTeXWithHighlights string string table (Some 0) Set.empty Set.empty

    Assert.Contains(@"\rowcolor{yellow!20} \hline 0", tex)

[<Fact>]
let ``currentLrState None leaves all rows unhighlighted`` () =
    let tex =
        RnglrTableTeX.tableToTeXWithHighlights string string table None Set.empty Set.empty

    Assert.DoesNotContain(@"\rowcolor", tex)

[<Fact>]
let ``active nonterminal action highlights goto cells green`` () =
    let tex =
        RnglrTableTeX.tableToTeXWithHighlights string string table None (set [ Symbol.N(Nonterminal "A") ]) Set.empty

    Assert.Contains(@"\cellcolor{green!20}", tex)

[<Fact>]
let ``level reduction overrides the green goto highlight with red`` () =
    let tex =
        RnglrTableTeX.tableToTeXWithHighlights
            string
            string
            table
            None
            (set [ Symbol.N(Nonterminal "A") ])
            (set [ Nonterminal "A" ])

    Assert.Contains(@"\cellcolor{red!20}", tex)

[<Fact>]
let ``tableToTeXWithHighlights wraps the tabular in a center environment`` () =
    let tex =
        RnglrTableTeX.tableToTeXWithHighlights string string table (Some 1) Set.empty Set.empty

    Assert.StartsWith(@"\begin{center}", tex)
    Assert.EndsWith(@"\end{center}", tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``highlighted RNGLR table compiles with lualatex`` () =
    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_table_color_template.tex")

    let tex =
        RnglrTableTeX.tableToTeXWithHighlights
            string
            string
            table
            (Some 0)
            (set [ Symbol.T(Terminal "a"); Symbol.N(Nonterminal "A") ])
            (set [ Nonterminal "B" ])

    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)
