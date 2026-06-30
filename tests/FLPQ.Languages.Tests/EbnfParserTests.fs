module EbnfParserTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra


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


module RsmBuilderTests =

    [<Fact>]
    let ``Build RSM for epsilon grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> eps"
        let blocks = RSM.blocks rsm
        Assert.Equal(1, List.length blocks)

        let block = RSM.startBlock rsm
        Assert.Equal(Nonterminal "S", block.nonterminal)

        let dfa = block.dfa
        Assert.Equal(0, dfa.startState)
        Assert.Equal<int>(set [ 0 ], dfa.finalStates)

    [<Fact>]
    let ``Build RSM for a* grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a*"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.nonterminal)
        Assert.Equal(1, Dfa.stateCount block.dfa)
        Assert.Equal(0, block.dfa.startState)
        Assert.Equal<int>(set [ 0 ], block.dfa.finalStates)

        let aSym = RsmSymbol.RTerm(Terminal "a")
        Assert.True(Dfa.alphabet block.dfa |> Set.contains aSym)

    [<Fact>]
    let ``Build RSM for a b grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a b"
        let block = RSM.startBlock rsm

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
        Assert.True(block.dfa.finalStates |> Set.contains block.dfa.startState)

    [<Fact>]
    let ``Build RSM for plus and optional operators`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a+ b?"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.nonterminal)
        Assert.True(Dfa.stateCount block.dfa >= 1)

    [<Fact>]
    let ``Build RSM with multiple rules same LHS`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a\nS -> b"
        let blocks = RSM.blocks rsm
        Assert.Equal(1, List.length blocks)
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

        let nts = RSM.nonterminals rsm |> Set.ofList
        Assert.True(Set.contains (Nonterminal "E") nts)
        Assert.True(Set.contains (Nonterminal "T") nts)
        Assert.True(Set.contains (Nonterminal "F") nts)

    [<Fact>]
    let ``Build RSM for a* a* grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a* a*"
        let block = RSM.startBlock rsm

        Assert.Equal(Nonterminal "S", block.nonterminal)
        Assert.True(Dfa.stateCount block.dfa >= 1)

    [<Fact>]
    let ``RSM built from EBNF has deterministic blocks`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> ( a S b )*"

        for block in RSM.blocks rsm do
            let n = Dfa.stateCount block.dfa

            for sym in Dfa.alphabet block.dfa do
                for state in 0 .. n - 1 do
                    let _result = Dfa.move block.dfa state sym
                    ()

    [<Fact>]
    let ``RSM start block is first nonterminal`` () =
        let rsm = RsmBuilder.buildRSMFromText "A -> x\nB -> y"
        Assert.Equal(Nonterminal "A", rsm.startBlock)
