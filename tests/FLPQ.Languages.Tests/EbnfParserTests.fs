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
            b1.nonterminal = b2.nonterminal
            && Dfa.stateCount b1.dfa = Dfa.stateCount b2.dfa
            && b1.dfa.startState = b2.dfa.startState
            && b1.dfa.finalStates = b2.dfa.finalStates
            && Dfa.alphabet b1.dfa = Dfa.alphabet b2.dfa

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
            b1.nonterminal = b2.nonterminal
            && Dfa.stateCount b1.dfa = Dfa.stateCount b2.dfa
            && b1.dfa.startState = b2.dfa.startState
            && b1.dfa.finalStates = b2.dfa.finalStates
            && Dfa.alphabet b1.dfa = Dfa.alphabet b2.dfa

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
            b1.nonterminal = b2.nonterminal
            && Dfa.stateCount b1.dfa = Dfa.stateCount b2.dfa
            && b1.dfa.startState = b2.dfa.startState
            && b1.dfa.finalStates = b2.dfa.finalStates
            && Dfa.alphabet b1.dfa = Dfa.alphabet b2.dfa

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
        Assert.Equal(Nonterminal "S", block.nonterminal)
        Assert.True(Dfa.isDeterministic block.dfa)

        let dfa = block.dfa
        Assert.Equal(0, dfa.startState)
        Assert.Equal<int>(set [ 0 ], dfa.finalStates)

    [<Fact>]
    let ``Build RSM for a* grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a*"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.nonterminal)
        Assert.True(Dfa.isDeterministic block.dfa)
        Assert.Equal(1, Dfa.stateCount block.dfa)
        Assert.Equal(0, block.dfa.startState)
        Assert.Equal<int>(set [ 0 ], block.dfa.finalStates)

        let aSym = RsmSymbol.RTerm(Terminal "a")
        Assert.True(Dfa.alphabet block.dfa |> Set.contains aSym)

    [<Fact>]
    let ``Build RSM for a b grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a b"
        let block = RSM.startBlock rsm

        Assert.True(Dfa.isDeterministic block.dfa)
        Assert.Equal(3, Dfa.stateCount block.dfa)

        let aSym = RsmSymbol.RTerm(Terminal "a")
        let bSym = RsmSymbol.RTerm(Terminal "b")
        let alphabet = Dfa.alphabet block.dfa
        Assert.True(Set.contains aSym alphabet)
        Assert.True(Set.contains bSym alphabet)

    [<Fact>]
    let ``Build RSM for Dyck language EBNF`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> ( a S b )*"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.nonterminal)
        Assert.True(Dfa.isDeterministic block.dfa)
        Assert.True(block.dfa.finalStates |> Set.contains block.dfa.startState)

    [<Fact>]
    let ``Build RSM for plus and optional operators`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a+ b?"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.nonterminal)
        Assert.True(Dfa.isDeterministic block.dfa)
        Assert.True(Dfa.stateCount block.dfa >= 1)

    [<Fact>]
    let ``Build RSM with multiple rules same LHS`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a\nS -> b"
        let blocks = RSM.blocks rsm
        Assert.Equal(1, List.length blocks)
        Assert.True(Dfa.isDeterministic rsm.blocks.Head.dfa)
        Assert.True(Dfa.stateCount rsm.blocks.Head.dfa >= 1)

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
            Assert.True(Dfa.isDeterministic block.dfa)

        let nts = RSM.nonterminals rsm |> Set.ofList
        Assert.True(Set.contains (Nonterminal "E") nts)
        Assert.True(Set.contains (Nonterminal "T") nts)
        Assert.True(Set.contains (Nonterminal "F") nts)

    [<Fact>]
    let ``Build RSM for a* a* grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a* a*"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.nonterminal)
        Assert.True(Dfa.isDeterministic block.dfa)

        Assert.Equal(2, Dfa.stateCount block.dfa)

        Assert.True(block.dfa.finalStates |> Set.contains 0)
        Assert.True(block.dfa.finalStates |> Set.contains 1)
        Assert.Equal(0, block.dfa.startState)

    [<Fact>]
    let ``RSM built from EBNF has deterministic blocks`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> ( a S b )*"

        for block in RSM.blocks rsm do
            Assert.True(Dfa.isDeterministic block.dfa)

    [<Fact>]
    let ``RSM start block is first nonterminal`` () =
        let rsm = RsmBuilder.buildRSMFromText "A -> x\nB -> y"
        Assert.Equal(Nonterminal "A", rsm.startBlock)


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
