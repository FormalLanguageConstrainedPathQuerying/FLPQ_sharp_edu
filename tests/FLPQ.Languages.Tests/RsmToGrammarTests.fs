module RsmToGrammarTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open TestGrammars

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb


module ConversionTests =

    [<Fact>]
    let ``RSM to grammar for S -> eps`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> eps"
        let g = RsmToGrammar.convert rsm
        Assert.Equal(1, List.length g.rules)
        Assert.True(Cyk.parse g (Tokenizer.tokenize ""))
        Assert.False(Cyk.parse g (Tokenizer.tokenize "a"))

    [<Fact>]
    let ``RSM to grammar for S -> a*`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a*"
        let g = RsmToGrammar.convert rsm

        Assert.True(Cyk.parse g (Tokenizer.tokenize ""))
        Assert.True(Cyk.parse g (Tokenizer.tokenize "a"))
        Assert.True(Cyk.parse g (Tokenizer.tokenize "a a a"))
        Assert.False(Cyk.parse g (Tokenizer.tokenize "b"))

    [<Fact>]
    let ``RSM to grammar for S -> a b`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a b"
        let g = RsmToGrammar.convert rsm

        Assert.True(Cyk.parse g (Tokenizer.tokenize "a b"))
        Assert.False(Cyk.parse g (Tokenizer.tokenize "a"))
        Assert.False(Cyk.parse g (Tokenizer.tokenize "b"))
        Assert.False(Cyk.parse g (Tokenizer.tokenize ""))

    [<Fact>]
    let ``RSM to grammar for Dyck language`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> ( a S b )*"
        let g = RsmToGrammar.convert rsm

        Assert.True(Cyk.parse g (Tokenizer.tokenize ""))
        Assert.True(Cyk.parse g (Tokenizer.tokenize "a b"))
        Assert.True(Cyk.parse g (Tokenizer.tokenize "a a b b"))
        Assert.False(Cyk.parse g (Tokenizer.tokenize "a"))
        Assert.False(Cyk.parse g (Tokenizer.tokenize "b"))

    [<Fact>]
    let ``RSM to grammar for S -> a S b S | eps`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a S b S | eps"
        let g = RsmToGrammar.convert rsm

        Assert.True(Cyk.parse g (Tokenizer.tokenize ""))
        Assert.True(Cyk.parse g (Tokenizer.tokenize "a b"))
        Assert.True(Cyk.parse g (Tokenizer.tokenize "a b a b"))
        Assert.True(Cyk.parse g (Tokenizer.tokenize "a a b b"))
        Assert.False(Cyk.parse g (Tokenizer.tokenize "a a"))
        Assert.False(Cyk.parse g (Tokenizer.tokenize "a"))

    [<Fact>]
    let ``Round-trip: EBNF -> RSM -> BNF matches original BNF for grammar1`` () =
        let bnf = grammar1
        let rsm = RsmBuilder.buildRSMFromText "S -> a S b S | eps"
        let roundtrip = RsmToGrammar.convert rsm

        for s in grammar1Accept do
            Assert.Equal(Cyk.parse bnf (Tokenizer.tokenize s), Cyk.parse roundtrip (Tokenizer.tokenize s))

        for s in grammar1Reject do
            Assert.Equal(Cyk.parse bnf (Tokenizer.tokenize s), Cyk.parse roundtrip (Tokenizer.tokenize s))

    [<Fact>]
    let ``Round-trip: EBNF Dyck matches BNF Dyck`` () =
        let bnf = grammar1
        let rsm = RsmBuilder.buildRSMFromText "S -> ( a S b )*"
        let roundtrip = RsmToGrammar.convert rsm

        for s in grammar1Accept do
            Assert.Equal(Cyk.parse bnf (Tokenizer.tokenize s), Cyk.parse roundtrip (Tokenizer.tokenize s))

    [<Fact>]
    let ``Round-trip for expression grammar`` () =
        let bnf =
            Grammar.parseGrammar
                "
E -> T op_plus E
E -> T
T -> F op_mul T
T -> F
F -> x
"

        let rsm =
            RsmBuilder.buildRSMFromText
                "
E -> T op_plus E | T
T -> F op_mul T | F
F -> x
"

        let roundtrip = RsmToGrammar.convert rsm

        let testStrings = [ "x"; "x op_plus x"; "x op_mul x"; "x op_plus x op_mul x" ]

        for s in testStrings do
            let tokens = Tokenizer.tokenize s
            Assert.Equal(Cyk.parse bnf tokens, Cyk.parse roundtrip tokens)

    [<Fact>]
    let ``RSM to grammar produces non-empty grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a+"
        let g = RsmToGrammar.convert rsm
        Assert.NotEmpty(g.rules)
        Assert.True(Cyk.parse g (Tokenizer.tokenize "a"))
        Assert.True(Cyk.parse g (Tokenizer.tokenize "a a a"))
        Assert.False(Cyk.parse g (Tokenizer.tokenize ""))


module PropertyTests =

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module RoundTripPropertyTests =

        [<Property>]
        let ``EBNF -> RSM -> BNF matches original BNF grammar1`` (s: string) =
            let bnf = grammar1
            let rsm = RsmBuilder.buildRSMFromText "S -> a S b S | eps"
            let roundtrip = RsmToGrammar.convert rsm

            Cyk.parse bnf (Tokenizer.tokenize s) = Cyk.parse roundtrip (Tokenizer.tokenize s)

        [<Property>]
        let ``EBNF Dyck -> RSM -> BNF matches BNF grammar1`` (s: string) =
            let bnf = grammar1
            let rsm = RsmBuilder.buildRSMFromText "S -> ( a S b )*"
            let roundtrip = RsmToGrammar.convert rsm

            Cyk.parse bnf (Tokenizer.tokenize s) = Cyk.parse roundtrip (Tokenizer.tokenize s)

    [<Properties(Arbitrary = [| typeof<ExprStringGenerators> |])>]
    module ExprPropertyTests =

        [<Property>]
        let ``EBNF expr -> RSM -> BNF matches equivalent BNF grammar`` (s: string) =
            if s.Contains("(") || s.Contains(")") then
                true
            else
                let bnf =
                    Grammar.parseGrammar
                        "
E -> T op_plus E
E -> T
T -> F op_mul T
T -> F
F -> x
"

                let rsm =
                    RsmBuilder.buildRSMFromText
                        "
E -> T op_plus E | T
T -> F op_mul T | F
F -> x
"

                let roundtrip = RsmToGrammar.convert rsm

                let sWithOps = s.Replace("+", "op_plus").Replace("*", "op_mul")

                Cyk.parse bnf (Tokenizer.tokenize sWithOps) = Cyk.parse roundtrip (Tokenizer.tokenize sWithOps)
