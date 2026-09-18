module LRTableTeXGoldenTests

open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities
open Xunit

open GoldenHelpers

module private Grammars =
    let grammar1Bnf = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let grammar7Bnf = LanguageRegistry.ArithExpr.Grammars.[1].Grammar

    let private freshStart grammar =
        Nonterminal(grammar.Start |> fun (Nonterminal n) -> n + "'")

    let private augGrammar1 =
        LRAutomaton.augmentGrammar (freshStart grammar1Bnf) grammar1Bnf

    let private augGrammar7 =
        LRAutomaton.augmentGrammar (freshStart grammar7Bnf) grammar7Bnf

    let lr0TableGrammar1 = LRParser.buildLR0Table augGrammar1 Grammar.eoiSymbol
    let slr1TableGrammar1 = LRParser.buildSLR1Table augGrammar1 Grammar.eoiSymbol
    let clr1TableGrammar1 = LRParser.buildCLR1Table augGrammar1

    let lr0TableGrammar7 = LRParser.buildLR0Table augGrammar7 Grammar.eoiSymbol
    let slr1TableGrammar7 = LRParser.buildSLR1Table augGrammar7 Grammar.eoiSymbol
    let clr1TableGrammar7 = LRParser.buildCLR1Table augGrammar7

    let texLr0Grammar1 =
        LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) augGrammar1 lr0TableGrammar1

    let texSlr1Grammar1 =
        LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) augGrammar1 slr1TableGrammar1

    let texClr1Grammar1 =
        LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) augGrammar1 clr1TableGrammar1

    let texLr0Grammar7 =
        LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) augGrammar7 lr0TableGrammar7

    let texSlr1Grammar7 =
        LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) augGrammar7 slr1TableGrammar7

    let texClr1Grammar7 =
        LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) augGrammar7 clr1TableGrammar7

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


type ``LR table TeX conflict and degenerate tests``() =

    [<Fact>]
    member _.``ReduceReduce conflict renders both reduce actions in one cell``() =
        // Grammar `S -> A; A -> S` yields a ReduceReduce conflict in the accept state
        // (see AcceptStateConflicts in FLPQ.Languages.Tests): both completed items
        // reduce on Epsilon.
        let tex =
            LRTableTeX.tableToTeX
                (SymbolTeX.toLaTeX string string)
                LrConflictFixture.augmented
                LrConflictFixture.lr0Table

        Assert.Contains(@"$r_2$, $r_-1$", tex)

    [<Fact>]
    member _.``grammar without nonterminals renders an empty goto column spec``() =
        let emptyG: Grammar<string, string> = { Rules = []; Start = Nonterminal "S" }
        let aug = LRAutomaton.augmentGrammar (Nonterminal "S'") emptyG
        let table = LRParser.buildLR0Table aug Symbol.Epsilon

        let tex = LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) aug table

        Assert.Contains(@"\begin{tabular}{ c || c ||  }", tex)
