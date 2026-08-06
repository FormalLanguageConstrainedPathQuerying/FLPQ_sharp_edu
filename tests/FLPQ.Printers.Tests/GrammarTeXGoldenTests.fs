module GrammarTeXGoldenTests

open System.IO
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities
open Xunit

open GoldenHelpers

module private Grammars =
    let grammar1Bnf = LanguageRegistry.Dyck1.Grammars.[0].Grammar
    let grammar1Cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar1Bnf

    let grammar7Bnf = LanguageRegistry.ArithExpr.Grammars.[1].Grammar
    let grammar7Cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar7Bnf

    let grammar9Bnf = LanguageRegistry.TwoTrackDyck.Grammars.[0].Grammar
    let grammar9Cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar9Bnf

type ``Grammar to TeX golden tests``() =

    [<Fact>]
    member _.``grammar1 BNF plain``() =
        verifyGolden "grammar1_bnf_plain.tex" (GrammarTeX.grammarToTeX string string Grammars.grammar1Bnf)

    [<Fact>]
    member _.``grammar1 BNF numbered``() =
        verifyGolden "grammar1_bnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers string string Grammars.grammar1Bnf)

    [<Fact>]
    member _.``grammar1 CNF plain``() =
        verifyGolden "grammar1_cnf_plain.tex" (GrammarTeX.grammarToTeX string string Grammars.grammar1Cnf)

    [<Fact>]
    member _.``grammar1 CNF numbered``() =
        verifyGolden "grammar1_cnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers string string Grammars.grammar1Cnf)

    [<Fact>]
    member _.``grammar7 BNF plain``() =
        verifyGolden "grammar7_bnf_plain.tex" (GrammarTeX.grammarToTeX string string Grammars.grammar7Bnf)

    [<Fact>]
    member _.``grammar7 BNF numbered``() =
        verifyGolden "grammar7_bnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers string string Grammars.grammar7Bnf)

    [<Fact>]
    member _.``grammar7 CNF plain``() =
        verifyGolden "grammar7_cnf_plain.tex" (GrammarTeX.grammarToTeX string string Grammars.grammar7Cnf)

    [<Fact>]
    member _.``grammar7 CNF numbered``() =
        verifyGolden "grammar7_cnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers string string Grammars.grammar7Cnf)

    [<Fact>]
    member _.``grammar9 BNF plain``() =
        verifyGolden "grammar9_bnf_plain.tex" (GrammarTeX.grammarToTeX string string Grammars.grammar9Bnf)

    [<Fact>]
    member _.``grammar9 BNF numbered``() =
        verifyGolden "grammar9_bnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers string string Grammars.grammar9Bnf)

    [<Fact>]
    member _.``grammar9 CNF plain``() =
        verifyGolden "grammar9_cnf_plain.tex" (GrammarTeX.grammarToTeX string string Grammars.grammar9Cnf)

    [<Fact>]
    member _.``grammar9 CNF numbered``() =
        verifyGolden "grammar9_cnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers string string Grammars.grammar9Cnf)
