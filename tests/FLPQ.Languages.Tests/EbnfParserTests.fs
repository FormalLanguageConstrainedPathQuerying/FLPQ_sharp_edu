module EbnfParserTests

open System
open System.IO
open Xunit
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

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
    let ``Parse explicit slash concatenation`` () =
        let r1 = EbnfParser.parseEbnf "S -> a / b"
        let r2 = EbnfParser.parseEbnf "S -> a b"
        Assert.Equal<(Nonterminal<string> * Regexp<string, string>) list>(r2, r1)

    [<Fact>]
    let ``Parse book RPQ example walk/(O | R)+/walk`` () =
        let rules = EbnfParser.parseEbnf "S -> walk / (O | R)+ / walk"
        Assert.Equal(1, List.length rules)
        let _, r = rules.Head
        let alt = RAlt(RNonterm(Nonterminal "O"), RNonterm(Nonterminal "R"))
        Assert.Equal(RSeq(RTerm(Terminal "walk"), RSeq(RSeq(alt, RStar alt), RTerm(Terminal "walk"))), r)

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


module RegexpDerivativeTests =

    // Shapes whose syntactic derivative closure was unbounded before mkAlt
    // flattened nested alternations: buildDfaFromRegex looped forever on them,
    // and derive eventually stack-overflowed on the ever-growing states.
    let private blowupShapes: (string * Regexp<string, string>) list =
        let a = RTerm(Terminal "a")
        let b = RTerm(Terminal "b")

        [ "(a*)(aa)*", RSeq(RStar a, RStar(RSeq(a, a)))
          "((aa)eps|a)*", RStar(RAlt(RSeq(RSeq(a, a), REps), a))
          "(a*+b*)*", RStar(RAlt(RStar a, RStar b)) ]

    /// Direct (exponential) language-membership interpreter for cross-checking
    /// the derivative-built DFA against the original regexp semantics.
    let rec private matches (r: Regexp<string, string>) (input: string list) : bool =
        let rec star (p: Regexp<string, string>) (rest: string list) : bool =
            if List.isEmpty rest then
                true
            else
                [ for i in 1 .. rest.Length do
                      let piece = List.take i rest

                      if matches p piece && star p (List.skip i rest) then
                          true ]
                |> List.exists id

        match r with
        | REps -> List.isEmpty input
        | REmpty -> false
        | RTerm(Terminal t) -> input = [ t ]
        | RNonterm _ -> false
        | RSeq(l, r2) ->
            [ for i in 0 .. input.Length do
                  if matches l (List.take i input) && matches r2 (List.skip i input) then
                      true ]
            |> List.exists id
        | RAlt(l, r2) -> matches l input || matches r2 input
        | RStar p -> star p input

    let rec private allStrings (len: int) : string list list =
        if len = 0 then
            [ [] ]
        else
            [ for s in allStrings (len - 1) do
                  yield s @ [ "a" ]
                  yield s @ [ "b" ] ]

    let private shortAlphabetStrings () : string list list =
        [ for len in 0..5 do
              yield! allStrings len ]

    [<Fact>]
    let ``buildDfaFromRegex terminates and is correct on previously unbounded shapes`` () =
        for name, r in blowupShapes do
            let deriveFn (r: Regexp<string, string>) (sym: string) =
                Regexp.derive r (RsmSymbol.RTerm(Terminal sym))

            let dfa = Regexp.buildDfaFromRegex [ "a"; "b" ] deriveFn r

            Assert.True(Dfa.isDeterministic dfa, sprintf "%s: DFA is not deterministic" name)
            Assert.True(Dfa.stateCount dfa < 20, sprintf "%s: DFA has %d states" name (Dfa.stateCount dfa))

            for s in shortAlphabetStrings () do
                let expected = matches r s
                let actual = Dfa.accept dfa (List.map Terminal s)
                let ok = expected = actual
                Assert.True(ok, sprintf "%s: input %A expected=%b actual=%b" name s expected actual)


module EbnfPropertyTests =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
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


module RegexpToStringTests =

    let private tp (Terminal t) = t
    let private np (Nonterminal n) = n

    [<Fact>]
    let ``Regexp.toString renders all seven variants with nested composition`` () =
        // Covers REps, REmpty, RTerm, RNonterm, RSeq, RAlt, RStar in one expression.
        let r: Regexp<string, string> =
            RAlt(RSeq(RTerm(Terminal "a"), RStar(RNonterm(Nonterminal "B"))), RAlt(REps, REmpty))

        Assert.Equal("(a (B)*) | eps | ∅", Regexp.toString tp np r)

    [<Fact>]
    let ``Regexp.derive of REmpty is REmpty`` () =
        Assert.Equal<Regexp<string, string>>(REmpty, Regexp.derive REmpty (RsmSymbol.RTerm(Terminal "a")))


module EbnfErrorPathTests =

    [<Fact>]
    let ``parseEbnf rejects unexpected character`` () =
        TestHelpers.assertThrows "Unexpected character '@'" (fun () -> EbnfParser.parseEbnf "S -> a @")

    [<Fact>]
    let ``parseEbnf rejects unbalanced parenthesis`` () =
        TestHelpers.assertThrows "Expected ')'" (fun () -> EbnfParser.parseEbnf "S -> (a")

    [<Fact>]
    let ``parseEbnf rejects star at atom position`` () =
        TestHelpers.assertThrows "Unexpected token" (fun () -> EbnfParser.parseEbnf "S -> * a")

    [<Fact>]
    let ``parseEbnf rejects extra token after rule`` () =
        TestHelpers.assertThrows "after rule" (fun () -> EbnfParser.parseEbnf "S -> a )")

    [<Fact>]
    let ``parseEbnf rejects lowercase left-hand side`` () =
        TestHelpers.assertThrows "Invalid rule format" (fun () -> EbnfParser.parseEbnf "s -> a")


module RsmBuilderFileTests =

    [<Fact>]
    let ``parseEbnfFile reads rules from file`` () =
        TestHelpers.withTempDir (fun dir ->
            let path = Path.Combine(dir, "grammar.ebnf")
            File.WriteAllText(path, "S -> a b")

            let rules = EbnfParser.parseEbnfFile path
            Assert.Equal<(Nonterminal<string> * Regexp<string, string>) list>(EbnfParser.parseEbnf "S -> a b", rules))

    [<Fact>]
    let ``buildRSMFromFile builds RSM from file`` () =
        TestHelpers.withTempDir (fun dir ->
            let path = Path.Combine(dir, "grammar.ebnf")
            File.WriteAllText(path, "S -> a b")

            let rsm = RsmBuilder.buildRSMFromFile path
            Assert.Equal(Nonterminal "S", rsm.StartBlock)
            Assert.Equal(3, rsm.StateCount)
            Assert.Equal<int>(set [ 2 ], rsm.FinalStates))

    [<Fact>]
    let ``buildRSM builds RSM from grouped rules`` () =
        let rules = EbnfParser.parseEbnf "S -> a b"
        let rsm = RsmBuilder.buildRSM (EbnfParser.groupRules rules)
        Assert.Equal(Nonterminal "S", rsm.StartBlock)
        Assert.Equal(3, rsm.StateCount)
        Assert.Equal<int>(set [ 2 ], rsm.FinalStates)

    [<Fact>]
    let ``buildRSMWithStart rejects empty grammar`` () =
        TestHelpers.assertThrows "at least one rule" (fun () ->
            RsmBuilder.buildRSMWithStart Map.empty (Nonterminal "S"))

    [<Fact>]
    let ``buildRSMFromText rejects empty text`` () =
        TestHelpers.assertThrows "at least one rule" (fun () -> RsmBuilder.buildRSMFromText "")
