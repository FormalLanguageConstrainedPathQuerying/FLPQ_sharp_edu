module ValiantTraceGoldenTests

open System.IO
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.TestUtilities
open Xunit

open GoldenHelpers

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_adjustbox_template.tex")

let private combineSteps (steps: string list) : string = steps |> String.concat "\n\n"

type ``Valiant trace TeX golden tests``() =

    [<Fact>]
    member _.``Valiant trace grammar1 abab``() =
        let grammar = LanguageRegistry.Dyck1.Grammars.[0].Grammar
        let tokens = Tokenizer.tokenizeTerminals "a b a b"

        let trace = Valiant.parseWithTrace Grammar.freshStringNonterminal grammar tokens

        let steps = trace |> List.map (ValiantTeX.stepToTeX string)

        let combined = combineSteps steps

        verifyGolden "valiant_grammar1_abab.tex" (wrapInTemplate templatePath combined)

    [<Fact>]
    member _.``Modified Valiant trace grammar1 ab``() =
        let grammar = LanguageRegistry.Dyck1.Grammars.[0].Grammar
        let tokens = Tokenizer.tokenizeTerminals "a b"

        let trace =
            Valiant.parseModifiedWithTrace Grammar.freshStringNonterminal grammar tokens

        let steps = trace |> List.map (ValiantTeX.modifiedStepToTeX string)

        let combined = combineSteps steps

        verifyGolden "valiant_modified_grammar1_ab.tex" (wrapInTemplate templatePath combined)
