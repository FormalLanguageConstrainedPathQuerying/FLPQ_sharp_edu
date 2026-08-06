module LRStepsGoldenTests

open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities
open Xunit

open GoldenHelpers

[<Fact>]
let ``LR steps dot grammar3 aa`` () =
    let g = LanguageRegistry.APlus.Grammars.[0].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol
    let tokens = Tokenizer.tokenizeTerminals "a a"
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps (SymbolTeX.toLaTeX string string) steps
    let combined = combineStepsDot vizSteps

    verifyGolden "lr_grammar3_aa.dot" combined

[<Fact>]
let ``LR steps dot grammar7 x+x`` () =
    let g = LanguageRegistry.ArithExpr.Grammars.[1].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol
    let tokens = Tokenizer.tokenizeTerminals "x add x"
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps (SymbolTeX.toLaTeX string string) steps
    let combined = combineStepsDot vizSteps

    verifyGolden "lr_grammar7_xplusx.dot" combined
