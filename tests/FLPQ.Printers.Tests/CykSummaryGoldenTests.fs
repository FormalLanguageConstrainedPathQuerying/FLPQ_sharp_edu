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
    let trace = Cyk.parseWithSppfTrace Grammar.freshStringNonterminal grammar tokens

    TestHelpers.withTempDir (fun tmpDir ->
        File.WriteAllText(
            Path.Combine(tmpDir, "input.tex"),
            TeXRenderer.inputRow (SymbolTeX.terminalContent string) tokens -1
        )

        File.WriteAllText(
            Path.Combine(tmpDir, "grammar_original.tex"),
            GrammarTeX.grammarToTeXWithNumbers string string grammar
        )

        File.WriteAllText(
            Path.Combine(tmpDir, "grammar_cnf.tex"),
            GrammarTeX.grammarToTeXWithNumbers string string cnf
        )

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(tmpDir, sprintf "step_%d" idx)
            Directory.CreateDirectory stepDir |> ignore

            let tex =
                if step.Highlights.IsEmpty then
                    CykTeX.sppfTableToTeX string step.Table
                else
                    CykTeX.sppfTableToTeXStyled string step.Table step.Highlights

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

        template.Replace("__ALGORITHM__", "CYK").Replace("__CONTENT__", content))

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

    [<Fact>]
    member _.``sppfTableToTeXAsNt renders nonterminal names without entry tuples``() =
        let entry: SppfParsingEntry<string> =
            { Nt = Nonterminal "A"
              SplitPoint = 1
              ProdIdx = 0 }

        let table =
            FLPQ.LinearAlgebra.Matrix.create 2 2 (fun i j -> if i = 0 && j = 1 then set [ entry ] else Set.empty)

        let tex = CykTeX.sppfTableToTeXAsNt string table

        Assert.Contains(@"\{A\}", tex)
        Assert.DoesNotContain("(A, 1, 0)", tex)
