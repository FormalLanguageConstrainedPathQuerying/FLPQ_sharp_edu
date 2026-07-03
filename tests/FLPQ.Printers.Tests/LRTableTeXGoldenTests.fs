module LRTableTeXGoldenTests

open System.IO
open FLPQ.Languages
open FLPQ.Printers
open Xunit

let private goldenDataDir =
    Path.Combine(Directory.GetCurrentDirectory(), "GoldenData")

let private verifyGolden (goldenFileName: string) (actualContent: string) =
    let goldenPath = Path.Combine(goldenDataDir, goldenFileName)

    if File.Exists goldenPath then
        let expected = File.ReadAllText goldenPath
        Assert.Equal(expected, actualContent)
    else
        Directory.CreateDirectory goldenDataDir |> ignore
        File.WriteAllText(goldenPath, actualContent)

        Assert.True(
            false,
            $"Golden file '{goldenFileName}' was created in output/GoldenData/.\n"
            + "Copy it to tests/FLPQ.Printers.Tests/GoldenData/ and re-run tests."
        )

module private Grammars =
    let grammar1Bnf = Grammar.parseGrammar "S -> a S b S\nS -> eps"

    let grammar7Bnf =
        Grammar.parseGrammar "E -> E + T\nE -> T\nT -> T * F\nT -> F\nF -> ( E )\nF -> x"

    let private freshStart grammar =
        Nonterminal(grammar.start |> fun (Nonterminal n) -> n + "'")

    let private augGrammar1 =
        LRAutomaton.augmentGrammar (freshStart grammar1Bnf) grammar1Bnf

    let private augGrammar7 =
        LRAutomaton.augmentGrammar (freshStart grammar7Bnf) grammar7Bnf

    let lr0TableGrammar1 = LRParser.buildLR0Table augGrammar1
    let slr1TableGrammar1 = LRParser.buildSLR1Table augGrammar1
    let clr1TableGrammar1 = LRParser.buildCLR1Table augGrammar1

    let lr0TableGrammar7 = LRParser.buildLR0Table augGrammar7
    let slr1TableGrammar7 = LRParser.buildSLR1Table augGrammar7
    let clr1TableGrammar7 = LRParser.buildCLR1Table augGrammar7

    let texLr0Grammar1 =
        LRTableTeX.tableToTeX SymbolTeX.toLaTeX augGrammar1 lr0TableGrammar1

    let texSlr1Grammar1 =
        LRTableTeX.tableToTeX SymbolTeX.toLaTeX augGrammar1 slr1TableGrammar1

    let texClr1Grammar1 =
        LRTableTeX.tableToTeX SymbolTeX.toLaTeX augGrammar1 clr1TableGrammar1

    let texLr0Grammar7 =
        LRTableTeX.tableToTeX SymbolTeX.toLaTeX augGrammar7 lr0TableGrammar7

    let texSlr1Grammar7 =
        LRTableTeX.tableToTeX SymbolTeX.toLaTeX augGrammar7 slr1TableGrammar7

    let texClr1Grammar7 =
        LRTableTeX.tableToTeX SymbolTeX.toLaTeX augGrammar7 clr1TableGrammar7

type ``LR table TeX golden tests``() =

    [<Fact>]
    member _.``LR(0) table for grammar1``() =
        verifyGolden "lr0_grammar1_table.tex" Grammars.texLr0Grammar1

    [<Fact>]
    member _.``SLR(1) table for grammar1``() =
        verifyGolden "slr1_grammar1_table.tex" Grammars.texSlr1Grammar1

    [<Fact>]
    member _.``CLR(1) table for grammar1``() =
        verifyGolden "clr1_grammar1_table.tex" Grammars.texClr1Grammar1

    [<Fact>]
    member _.``LR(0) table for grammar7``() =
        verifyGolden "lr0_grammar7_table.tex" Grammars.texLr0Grammar7

    [<Fact>]
    member _.``SLR(1) table for grammar7``() =
        verifyGolden "slr1_grammar7_table.tex" Grammars.texSlr1Grammar7

    [<Fact>]
    member _.``CLR(1) table for grammar7``() =
        verifyGolden "clr1_grammar7_table.tex" Grammars.texClr1Grammar7
