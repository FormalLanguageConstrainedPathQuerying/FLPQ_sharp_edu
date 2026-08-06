module LLStepsGoldenTests

open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities
open Xunit

open GoldenHelpers

[<Fact>]
let ``LL steps dot grammar1 ab`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps (SymbolTeX.toLaTeX string string) steps
    let combined = combineStepsDot vizSteps

    verifyGolden "ll_grammar1_ab.dot" combined

[<Fact>]
let ``LL steps dot grammar1 aababb`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a a b a b b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps (SymbolTeX.toLaTeX string string) steps
    let combined = combineStepsDot vizSteps

    verifyGolden "ll_grammar1_aababb.dot" combined
