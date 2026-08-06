module CykSummaryGoldenTests

open System.IO
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities
open Xunit

open GoldenHelpers

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex")

let private generateCykSummaryTex (grammarStr: string) (input: string) : string =
    let grammar = Grammar.parseGrammar grammarStr
    let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar
    let tokens = Tokenizer.tokenizeTerminals input
    let trace = Cyk.parseWithTrace Grammar.freshStringNonterminal grammar tokens

    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    Directory.CreateDirectory tmpDir |> ignore

    try
        File.WriteAllText(
            Path.Combine(tmpDir, "input.tex"),
            TeXRenderer.inputRow (SymbolTeX.terminalContent string) tokens -1
        )

        File.WriteAllText(Path.Combine(tmpDir, "grammar_original.tex"), GrammarTeX.grammarToTeX string string grammar)

        File.WriteAllText(Path.Combine(tmpDir, "grammar_cnf.tex"), GrammarTeX.grammarToTeX string string cnf)

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(tmpDir, sprintf "step_%d" idx)
            Directory.CreateDirectory stepDir |> ignore

            let tex =
                if step.Highlights.IsEmpty then
                    CykTeX.tableToTeX string step.Table
                else
                    CykTeX.tableToTeXStyled string step.Table step.Highlights

            File.WriteAllText(Path.Combine(stepDir, "table.tex"), tex)

        let content =
            SummaryTeX.buildContent
                "CYK"
                SummaryTeX.SummaryKind.TablePerStep
                tmpDir
                trace.Length
                None
                None
                []
                ""
                ""
                ""
                ""
                false
            |> String.concat "\n"

        let template = File.ReadAllText templatePath

        template.Replace("__ALGORITHM__", "CYK").Replace("__CONTENT__", content)
    finally
        try
            Directory.Delete(tmpDir, true)
        with _ ->
            ()

type ``CYK summary golden tests``() =

    [<Fact>]
    member _.``CYK summary grammar1 aababb``() =
        let tex = generateCykSummaryTex LanguageRegistry.Dyck1.Grammars.[0].Text "aababb"

        verifyGolden "cyk_grammar1_aababb_summary.tex" tex

    [<Fact>]
    member _.``CYK summary grammar7 x+x``() =
        let tex =
            generateCykSummaryTex LanguageRegistry.ArithExpr.Grammars.[1].Text "x add x"

        verifyGolden "cyk_grammar7_xplusx_summary.tex" tex
