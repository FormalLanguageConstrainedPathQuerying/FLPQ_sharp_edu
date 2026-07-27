module LRStepsGoldenTests

open FLPQ.Languages
open FLPQ.Printers
open Xunit

open GoldenHelpers

[<Fact>]
let ``LR steps dot grammar3 aa`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S
        S -> a
        "

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
    let g =
        Grammar.parseGrammar
            "
        E -> E + T
        E -> T
        T -> T * F
        T -> F
        F -> ( E )
        F -> x
        "

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol
    let tokens = Tokenizer.tokenizeTerminals "x + x"
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps (SymbolTeX.toLaTeX string string) steps
    let combined = combineStepsDot vizSteps

    verifyGolden "lr_grammar7_xplusx.dot" combined
