module RsmToGrammarTests

open Xunit
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open FLPQ.TestUtilities

let private dyck1 = LanguageRegistry.Dyck1

let private grammar1 = LanguageRegistry.findGrammar dyck1 "ambiguousEps"
let private grammarEbnfStar = LanguageRegistry.findGrammar dyck1 "ebnfStar"

let private opExprRightAssoc =
    LanguageRegistry.findGrammar LanguageRegistry.OpExpr "rightAssoc"

let private grammar1Grammar = grammar1.Grammar
let private grammar1Accept = dyck1.AcceptStrings
let private grammar1Reject = dyck1.RejectStrings

let private checkAccept (g: Grammar<string, string>) (input: Terminal<string> list) =
    Assert.True(Cyk.parse Grammar.freshStringNonterminal g input, $"{input}")

let private checkReject (g: Grammar<string, string>) (input: Terminal<string> list) =
    Assert.False(Cyk.parse Grammar.freshStringNonterminal g input, $"{input}")

let private checkAll (g: Grammar<string, string>) (lang: Language) =
    for s in lang.AcceptStrings do
        checkAccept g s

    for s in lang.RejectStrings do
        checkReject g s

module ConversionTests =

    [<Fact>]
    let ``RSM to grammar for S -> eps`` () =
        let rsm =
            (LanguageRegistry.findGrammar LanguageRegistry.EpsilonOnly "singleRule").Rsm

        let g = RsmToGrammar.convert rsm

        Assert.Equal(1, List.length g.Rules)
        checkAll g LanguageRegistry.EpsilonOnly

    [<Fact>]
    let ``RSM to grammar for S -> a*`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a*"
        let g = RsmToGrammar.convert rsm

        checkAll g LanguageRegistry.AStar

    [<Fact>]
    let ``RSM to grammar for S -> a b`` () =
        let rsm = (LanguageRegistry.findGrammar LanguageRegistry.SingleAB "singleRule").Rsm
        let g = RsmToGrammar.convert rsm

        checkAll g LanguageRegistry.SingleAB

    [<Fact>]
    let ``RSM to grammar for Dyck language`` () =
        let rsm = grammarEbnfStar.Rsm
        let g = RsmToGrammar.convert rsm

        checkAll g LanguageRegistry.Dyck1

    [<Fact>]
    let ``RSM to grammar for S -> a S b S | eps`` () =
        let rsm = grammar1.Rsm
        let g = RsmToGrammar.convert rsm

        checkAll g LanguageRegistry.Dyck1

    [<Fact>]
    let ``Round-trip: BNF -> RSM -> BNF matches original for grammar1`` () =
        let roundtrip = RsmToGrammar.convert grammar1.Rsm

        for s in grammar1Accept do
            Assert.Equal(
                Cyk.parse Grammar.freshStringNonterminal grammar1Grammar s,
                Cyk.parse Grammar.freshStringNonterminal roundtrip s
            )

        for s in grammar1Reject do
            Assert.Equal(
                Cyk.parse Grammar.freshStringNonterminal grammar1Grammar s,
                Cyk.parse Grammar.freshStringNonterminal roundtrip s
            )

    [<Fact>]
    let ``Round-trip: EBNF Dyck matches BNF Dyck`` () =
        let roundtrip = RsmToGrammar.convert grammarEbnfStar.Rsm

        for s in grammar1Accept do
            Assert.Equal(
                Cyk.parse Grammar.freshStringNonterminal grammar1Grammar s,
                Cyk.parse Grammar.freshStringNonterminal roundtrip s
            )

    [<Fact>]
    let ``Round-trip for expression grammar`` () =
        let opExpr = LanguageRegistry.OpExpr
        let roundtrip = RsmToGrammar.convert opExprRightAssoc.Rsm

        for s in opExpr.AcceptStrings @ opExpr.RejectStrings do
            Assert.Equal(
                Cyk.parse Grammar.freshStringNonterminal opExprRightAssoc.Grammar s,
                Cyk.parse Grammar.freshStringNonterminal roundtrip s
            )

    [<Fact>]
    let ``RSM to grammar produces non-empty grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a+"
        let g = RsmToGrammar.convert rsm

        Assert.NotEmpty(g.Rules)
        checkAll g LanguageRegistry.APlus


module PropertyTests =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module RoundTripPropertyTests =

        [<Property>]
        let ``EBNF -> RSM -> BNF matches original BNF grammar1`` (s: string) =
            let bnf = grammar1Grammar
            let roundtrip = RsmToGrammar.convert grammar1.Rsm

            Cyk.parse Grammar.freshStringNonterminal bnf (Tokenizer.tokenizeTerminals s) = Cyk.parse
                Grammar.freshStringNonterminal
                roundtrip
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``EBNF Dyck -> RSM -> BNF matches BNF grammar1`` (s: string) =
            let bnf = grammar1Grammar
            let roundtrip = RsmToGrammar.convert grammarEbnfStar.Rsm

            Cyk.parse Grammar.freshStringNonterminal bnf (Tokenizer.tokenizeTerminals s) = Cyk.parse
                Grammar.freshStringNonterminal
                roundtrip
                (Tokenizer.tokenizeTerminals s)

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.ExprString> |])>]
    module ExprPropertyTests =

        [<Property>]
        let ``EBNF expr -> RSM -> BNF matches equivalent BNF grammar`` (s: string) =
            if s.Contains("(") || s.Contains(")") then
                true
            else
                let bnf = opExprRightAssoc.Grammar
                let roundtrip = RsmToGrammar.convert opExprRightAssoc.Rsm

                let sWithOps = s.Replace("+", "op_plus").Replace("*", "op_mul")

                Cyk.parse Grammar.freshStringNonterminal bnf (Tokenizer.tokenizeTerminals sWithOps) = Cyk.parse
                    Grammar.freshStringNonterminal
                    roundtrip
                    (Tokenizer.tokenizeTerminals sWithOps)
