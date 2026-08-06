module LLTableTeXGoldenTests

open System.IO
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities
open Xunit

open GoldenHelpers

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_tabular_template.tex")

let private generateLLTableTeX (grammar: Grammar<string, string>) (k: int) : string =
    let first = FirstFollow.firstK grammar k
    let follow = FirstFollow.followK grammar k
    let table = LLParser.buildTable grammar k

    LLTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) grammar k first follow table

type ``LL table TeX golden tests``() =

    [<Fact>]
    member _.``LL(1) table grammar1``() =
        let tex = generateLLTableTeX LanguageRegistry.Dyck1.Grammars.[0].Grammar 1
        verifyGolden "ll_grammar1_table.tex" (wrapInTemplate templatePath tex)

    [<Fact>]
    member _.``LL(2) table for k=2 grammar``() =
        let tex = generateLLTableTeX LanguageRegistry.LL2Test.Grammars.[0].Grammar 2
        verifyGolden "ll_k2_table.tex" (wrapInTemplate templatePath tex)
