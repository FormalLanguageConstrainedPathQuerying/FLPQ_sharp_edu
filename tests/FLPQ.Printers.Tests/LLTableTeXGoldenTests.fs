module LLTableTeXGoldenTests

open System.IO
open FLPQ.Languages
open FLPQ.Printers
open Xunit

open GoldenHelpers

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_tabular_template.tex")

let private generateLLTableTeX (grammarStr: string) (k: int) : string =
    let grammar = Grammar.parseGrammar grammarStr
    let first = FirstFollow.firstK grammar k
    let follow = FirstFollow.followK grammar k
    let table = LLParser.buildTable grammar k

    LLTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) grammar k first follow table

type ``LL table TeX golden tests``() =

    [<Fact>]
    member _.``LL(1) table grammar1``() =
        let tex = generateLLTableTeX "S -> a S b S\nS -> eps" 1
        verifyGolden "ll_grammar1_table.tex" (wrapInTemplate templatePath tex)

    [<Fact>]
    member _.``LL(2) table for k=2 grammar``() =
        let tex = generateLLTableTeX "S -> a b A\nS -> a a B\nA -> c\nB -> d" 2
        verifyGolden "ll_k2_table.tex" (wrapInTemplate templatePath tex)
