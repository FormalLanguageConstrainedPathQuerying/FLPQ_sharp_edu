module EbnfParserTests

open Xunit
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open TestGrammars
open FLPQ.TestUtilities

module EbnfParseTests =

    [<Fact>]
    let ``Parse simple ident regexp`` () =
        let rules = EbnfParser.parseEbnf "S -> x"
        Assert.Equal(1, List.length rules)

    [<Fact>]
    let ``Parse epsilon grammar`` () =
        let rules = EbnfParser.parseEbnf "S -> eps"
        Assert.Equal(1, List.length rules)
        let nt, regexp = rules.Head
        Assert.Equal(Nonterminal "S", nt)
        Assert.Equal(REps, regexp)

    [<Fact>]
    let ``Parse grammar with star`` () =
        let rules = EbnfParser.parseEbnf "S -> y*"
        Assert.Equal(1, List.length rules)

    [<Fact>]
    let ``Parse grammar with alternative`` () =
        let rules = EbnfParser.parseEbnf "S -> a | b"
        Assert.Equal(1, List.length rules)

    [<Fact>]
    let ``Parse grammar with parens`` () =
        let rules = EbnfParser.parseEbnf "S -> ( a )"
        Assert.Equal(1, List.length rules)

    [<Fact>]
    let ``Parse grammar with plus and optional`` () =
        let rules = EbnfParser.parseEbnf "S -> a+ b?"
        Assert.Equal(1, List.length rules)

    [<Fact>]
    let ``Group rules with same LHS`` () =
        let rules = EbnfParser.parseEbnf "S -> a\nS -> b"
        let grouped = EbnfParser.groupRules rules
        Assert.Equal(1, Map.count grouped)

    [<Fact>]
    let ``Parse multiple rules`` () =
        let rules =
            EbnfParser.parseEbnf
                "
E -> T op_plus E | T
T -> F op_mul T | F
F -> x
"

        Assert.Equal(3, List.length rules)

        let grouped = EbnfParser.groupRules rules
        Assert.Equal(3, Map.count grouped)


module EbnfParserWhitespaceTests =

    [<Fact>]
    let ``Whitespace variants of a (a | b) produce identical blocks`` () =
        let rsm1 = RsmBuilder.buildRSMFromText "S -> a (a | b)"
        let rsm2 = RsmBuilder.buildRSMFromText "S -> a (a|b)"
        let rsm3 = RsmBuilder.buildRSMFromText "S -> a(a |b)"

        let blocksEqual (b1: RsmBlock<string, string>) (b2: RsmBlock<string, string>) =
            b1.Nonterminal = b2.Nonterminal
            && Dfa.stateCount b1.Dfa = Dfa.stateCount b2.Dfa
            && b1.Dfa.StartState = b2.Dfa.StartState
            && b1.Dfa.FinalStates = b2.Dfa.FinalStates
            && Dfa.alphabet b1.Dfa = Dfa.alphabet b2.Dfa

        let b1_2 =
            RSM.blocks rsm1
            |> Seq.zip (RSM.blocks rsm2)
            |> Seq.forall (fun (a, b) -> blocksEqual a b)

        let b1_3 =
            RSM.blocks rsm1
            |> Seq.zip (RSM.blocks rsm3)
            |> Seq.forall (fun (a, b) -> blocksEqual a b)

        Assert.True b1_2
        Assert.True b1_3

    [<Fact>]
    let ``Whitespace variants of a S | (eps) produce identical blocks`` () =
        let rsm1 = RsmBuilder.buildRSMFromText "S -> a S | (eps)"
        let rsm2 = RsmBuilder.buildRSMFromText "S -> a S | ((eps))"
        let rsm3 = RsmBuilder.buildRSMFromText "S -> a S |(eps)"
        let rsm4 = RsmBuilder.buildRSMFromText "S -> a S |eps"

        let blocksEqual (b1: RsmBlock<string, string>) (b2: RsmBlock<string, string>) =
            b1.Nonterminal = b2.Nonterminal
            && Dfa.stateCount b1.Dfa = Dfa.stateCount b2.Dfa
            && b1.Dfa.StartState = b2.Dfa.StartState
            && b1.Dfa.FinalStates = b2.Dfa.FinalStates
            && Dfa.alphabet b1.Dfa = Dfa.alphabet b2.Dfa

        let blocks1 = RSM.blocks rsm1
        let blocks2 = RSM.blocks rsm2
        let blocks3 = RSM.blocks rsm3
        let blocks4 = RSM.blocks rsm4

        Assert.True(blocks1 |> Seq.zip blocks2 |> Seq.forall (fun (a, b) -> blocksEqual a b))
        Assert.True(blocks1 |> Seq.zip blocks3 |> Seq.forall (fun (a, b) -> blocksEqual a b))
        Assert.True(blocks1 |> Seq.zip blocks4 |> Seq.forall (fun (a, b) -> blocksEqual a b))

    [<Fact>]
    let ``Whitespace variants of a (a (a | b)) produce identical blocks`` () =
        let rsm1 = RsmBuilder.buildRSMFromText "S -> a (a ( a | b))"
        let rsm2 = RsmBuilder.buildRSMFromText "S -> a(a (a | b))"
        let rsm3 = RsmBuilder.buildRSMFromText "S -> a (a ( a |     b))"

        let blocksEqual (b1: RsmBlock<string, string>) (b2: RsmBlock<string, string>) =
            b1.Nonterminal = b2.Nonterminal
            && Dfa.stateCount b1.Dfa = Dfa.stateCount b2.Dfa
            && b1.Dfa.StartState = b2.Dfa.StartState
            && b1.Dfa.FinalStates = b2.Dfa.FinalStates
            && Dfa.alphabet b1.Dfa = Dfa.alphabet b2.Dfa

        let blocks1 = RSM.blocks rsm1
        let blocks2 = RSM.blocks rsm2
        let blocks3 = RSM.blocks rsm3

        Assert.True(blocks1 |> Seq.zip blocks2 |> Seq.forall (fun (a, b) -> blocksEqual a b))
        Assert.True(blocks1 |> Seq.zip blocks3 |> Seq.forall (fun (a, b) -> blocksEqual a b))


module RsmBuilderTests =

    [<Fact>]
    let ``Build RSM for epsilon grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> eps"
        let blocks = RSM.blocks rsm
        Assert.Equal(1, List.length blocks)

        let block = RSM.startBlock rsm
        Assert.Equal(Nonterminal "S", block.Nonterminal)
        Assert.True(Dfa.isDeterministic block.Dfa)

        let dfa = block.Dfa
        Assert.Equal(0, dfa.StartState)
        Assert.Equal<int>(set [ 0 ], dfa.FinalStates)

    [<Fact>]
    let ``Build RSM for a* grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a*"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.Nonterminal)
        Assert.True(Dfa.isDeterministic block.Dfa)
        Assert.Equal(1, Dfa.stateCount block.Dfa)
        Assert.Equal(0, block.Dfa.StartState)
        Assert.Equal<int>(set [ 0 ], block.Dfa.FinalStates)

        let aSym = RsmSymbol.RTerm(Terminal "a")
        Assert.True(Dfa.alphabet block.Dfa |> Set.contains aSym)

    [<Fact>]
    let ``Build RSM for a b grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a b"
        let block = RSM.startBlock rsm

        Assert.True(Dfa.isDeterministic block.Dfa)
        Assert.Equal(3, Dfa.stateCount block.Dfa)

        let aSym = RsmSymbol.RTerm(Terminal "a")
        let bSym = RsmSymbol.RTerm(Terminal "b")
        let alphabet = Dfa.alphabet block.Dfa
        Assert.True(Set.contains aSym alphabet)
        Assert.True(Set.contains bSym alphabet)

    [<Fact>]
    let ``Build RSM for Dyck language EBNF`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> ( a S b )*"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.Nonterminal)
        Assert.True(Dfa.isDeterministic block.Dfa)
        Assert.True(block.Dfa.FinalStates |> Set.contains block.Dfa.StartState)

    [<Fact>]
    let ``Build RSM for plus and optional operators`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a+ b?"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.Nonterminal)
        Assert.True(Dfa.isDeterministic block.Dfa)
        Assert.True(Dfa.stateCount block.Dfa >= 1)

    [<Fact>]
    let ``Build RSM with multiple rules same LHS`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a\nS -> b"
        let blocks = RSM.blocks rsm
        Assert.Equal(1, List.length blocks)
        Assert.True(Dfa.isDeterministic blocks.Head.Dfa)
        Assert.True(Dfa.stateCount blocks.Head.Dfa >= 1)

    [<Fact>]
    let ``Build RSM for expression grammar`` () =
        let rsm =
            RsmBuilder.buildRSMFromText
                "
E -> T op_plus E | T
T -> F op_mul T | F
F -> x
"

        let blocks = RSM.blocks rsm
        Assert.Equal(3, List.length blocks)

        for block in blocks do
            Assert.True(Dfa.isDeterministic block.Dfa)

        let nts = RSM.nonterminals rsm |> Set.ofList
        Assert.True(Set.contains (Nonterminal "E") nts)
        Assert.True(Set.contains (Nonterminal "T") nts)
        Assert.True(Set.contains (Nonterminal "F") nts)

    [<Fact>]
    let ``Build RSM for a* a* grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a* a*"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.Nonterminal)
        Assert.True(Dfa.isDeterministic block.Dfa)

        Assert.Equal(2, Dfa.stateCount block.Dfa)

        Assert.True(block.Dfa.FinalStates |> Set.contains 0)
        Assert.True(block.Dfa.FinalStates |> Set.contains 1)
        Assert.Equal(0, block.Dfa.StartState)

    [<Fact>]
    let ``RSM built from EBNF has deterministic blocks`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> ( a S b )*"

        for block in RSM.blocks rsm do
            Assert.True(Dfa.isDeterministic block.Dfa)

    [<Fact>]
    let ``RSM start block is first nonterminal`` () =
        let rsm = RsmBuilder.buildRSMFromText "A -> x\nB -> y"
        Assert.Equal(Nonterminal "A", rsm.StartBlock)


module EbnfPropertyTests =

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module ParsingEquivalenceTests =

        [<Property>]
        let ``S -> a S | eps accepts same as S -> (a*) (a*)`` (s: string) =
            let rsm1 = RsmBuilder.buildRSMFromText "S -> a S | eps"
            let g1 = RsmToGrammar.convert rsm1

            let rsm2 = RsmBuilder.buildRSMFromText "S -> a* a*"
            let g2 = RsmToGrammar.convert rsm2

            Cyk.parse Grammar.freshStringNonterminal g1 (Tokenizer.tokenizeTerminals s) = Cyk.parse
                Grammar.freshStringNonterminal
                g2
                (Tokenizer.tokenizeTerminals s)
