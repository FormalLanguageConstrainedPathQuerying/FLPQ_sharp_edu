module GrammarTeXGoldenTests

open System.IO
open FLPQ.Languages
open FLPQ.Printers
open Xunit

open GoldenHelpers

module private Grammars =
    let grammar1Bnf = Grammar.parseGrammar "S -> a S b S\nS -> eps"
    let grammar1Cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar1Bnf

    let grammar7Bnf =
        Grammar.parseGrammar "E -> E + T\nE -> T\nT -> T * F\nT -> F\nF -> ( E )\nF -> x"

    let grammar7Cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar7Bnf

    let grammar9Bnf =
        Grammar.parseGrammar "S -> S1\nS -> S2\nS1 -> a b S c\nS1 -> eps\nS2 -> a x S y\nS2 -> eps"

    let grammar9Cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar9Bnf

type ``Grammar to TeX golden tests``() =

    [<Fact>]
    member _.``grammar1 BNF plain``() =
        verifyGolden "grammar1_bnf_plain.tex" (GrammarTeX.grammarToTeX Grammars.grammar1Bnf)

    [<Fact>]
    member _.``grammar1 BNF numbered``() =
        verifyGolden "grammar1_bnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers Grammars.grammar1Bnf)

    [<Fact>]
    member _.``grammar1 CNF plain``() =
        verifyGolden "grammar1_cnf_plain.tex" (GrammarTeX.grammarToTeX Grammars.grammar1Cnf)

    [<Fact>]
    member _.``grammar1 CNF numbered``() =
        verifyGolden "grammar1_cnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers Grammars.grammar1Cnf)

    [<Fact>]
    member _.``grammar7 BNF plain``() =
        verifyGolden "grammar7_bnf_plain.tex" (GrammarTeX.grammarToTeX Grammars.grammar7Bnf)

    [<Fact>]
    member _.``grammar7 BNF numbered``() =
        verifyGolden "grammar7_bnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers Grammars.grammar7Bnf)

    [<Fact>]
    member _.``grammar7 CNF plain``() =
        verifyGolden "grammar7_cnf_plain.tex" (GrammarTeX.grammarToTeX Grammars.grammar7Cnf)

    [<Fact>]
    member _.``grammar7 CNF numbered``() =
        verifyGolden "grammar7_cnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers Grammars.grammar7Cnf)

    [<Fact>]
    member _.``grammar9 BNF plain``() =
        verifyGolden "grammar9_bnf_plain.tex" (GrammarTeX.grammarToTeX Grammars.grammar9Bnf)

    [<Fact>]
    member _.``grammar9 BNF numbered``() =
        verifyGolden "grammar9_bnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers Grammars.grammar9Bnf)

    [<Fact>]
    member _.``grammar9 CNF plain``() =
        verifyGolden "grammar9_cnf_plain.tex" (GrammarTeX.grammarToTeX Grammars.grammar9Cnf)

    [<Fact>]
    member _.``grammar9 CNF numbered``() =
        verifyGolden "grammar9_cnf_numbered.tex" (GrammarTeX.grammarToTeXWithNumbers Grammars.grammar9Cnf)
