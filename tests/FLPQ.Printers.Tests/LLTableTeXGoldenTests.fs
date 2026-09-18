module LLTableTeXGoldenTests

open System.IO
open FSharpPlus.Data
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

    [<Fact>]
    member _.``missing First and Follow sets render as varnothing``() =
        let grammar = LanguageRegistry.Dyck1.Grammars.[0].Grammar
        let table = LLParser.buildTable grammar 1

        let tex =
            LLTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) grammar 1 Map.empty Map.empty table

        Assert.Contains(@"$\varnothing$", tex)

    [<Fact>]
    member _.``rule with explicit epsilon symbol in rhs renders varepsilon``() =
        let rule: Rule<string, string> =
            { Lhs = Nonterminal "A"
              Rhs = NonEmptyList.ofList [ Symbol.Epsilon; Symbol.T(Terminal "b") ] |> Symbols }

        let grammar: Grammar<string, string> =
            { Rules = [ rule ]
              Start = Nonterminal "A" }

        let table = Map.ofList [ (Nonterminal "A", [ Symbol.T(Terminal "b") ]), 0 ]

        let tex =
            LLTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) grammar 1 Map.empty Map.empty table

        Assert.Contains(@"\varepsilon b", tex)
