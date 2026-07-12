module RSMTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

let private makeSBlock () : RsmBlock<string, string> =
    let nt = Nonterminal "S"
    let aSym = RsmSymbol.RTerm(Terminal "a")
    let transitions = [ (0, aSym, 1) ]
    let dfa = Dfa.fromTransitions [ 0; 1 ] transitions 0 (set [ 1 ])

    { Nonterminal = nt; Dfa = dfa }

let private makeSimpleRSM () : RSM<string, string> =
    { Blocks = [ makeSBlock () ]
      StartBlock = Nonterminal "S" }


module RsmBlockTests =

    [<Fact>]
    let ``Block has correct nonterminal`` () =
        let block = makeSBlock ()
        Assert.Equal(Nonterminal "S", block.Nonterminal)

    [<Fact>]
    let ``Block DFA has correct start and final states`` () =
        let block = makeSBlock ()
        Assert.Equal(0, block.Dfa.StartState)
        Assert.Equal<int>(set [ 1 ], block.Dfa.FinalStates)

    [<Fact>]
    let ``Block DFA has correct transitions`` () =
        let block = makeSBlock ()
        let aSym = RsmSymbol.RTerm(Terminal "a")
        let result = Dfa.move block.Dfa 0 aSym
        Assert.Equal(Some 1, result)

    [<Fact>]
    let ``Block DFA has correct alphabet`` () =
        let block = makeSBlock ()
        let alphabet = Dfa.alphabet block.Dfa
        Assert.Contains(RsmSymbol.RTerm(Terminal "a"), alphabet)
        Assert.Equal(1, Set.count alphabet)


module RSMTests =

    [<Fact>]
    let ``RSM accessor blocks returns all blocks`` () =
        let rsm = makeSimpleRSM ()
        let blks = RSM.blocks rsm
        Assert.Equal(1, List.length blks)

    [<Fact>]
    let ``RSM accessor startBlock returns correct block`` () =
        let rsm = makeSimpleRSM ()
        let sb = RSM.startBlock rsm
        Assert.Equal(Nonterminal "S", sb.Nonterminal)

    [<Fact>]
    let ``RSM accessor blockOf finds existing block`` () =
        let rsm = makeSimpleRSM ()
        let found = RSM.blockOf (Nonterminal "S") rsm
        Assert.True(found.IsSome)
        Assert.Equal(Nonterminal "S", found.Value.Nonterminal)

    [<Fact>]
    let ``RSM accessor blockOf returns None for missing block`` () =
        let rsm = makeSimpleRSM ()
        let found = RSM.blockOf (Nonterminal "A") rsm
        Assert.True(found.IsNone)

    [<Fact>]
    let ``RSM accessor nonterminals returns all nonterminals`` () =
        let rsm = makeSimpleRSM ()
        let nts = RSM.nonterminals rsm
        Assert.Equal<Nonterminal<string>>([ Nonterminal "S" ] :> _ seq, nts)

    [<Fact>]
    let ``RSM accessor terminals returns all terminals`` () =
        let rsm = makeSimpleRSM ()
        let terms = RSM.terminals rsm
        Assert.Equal<Terminal<string>>([ Terminal "a" ] :> _ seq, terms)

    [<Fact>]
    let ``RSM accessor startStates returns start state of each block`` () =
        let rsm = makeSimpleRSM ()
        let starts = RSM.startStates rsm
        Assert.Equal<int>(set [ 0 ], starts)

    [<Fact>]
    let ``RSM accessor stateCount returns total number of states`` () =
        let rsm = makeSimpleRSM ()
        Assert.Equal(2, RSM.stateCount rsm)

    [<Fact>]
    let ``RSM with two blocks`` () =
        let ntA = Nonterminal "A"
        let ntB = Nonterminal "B"
        let xSym = RsmSymbol.RTerm(Terminal "x")

        let blockA =
            { Nonterminal = ntA
              Dfa = Dfa.fromTransitions [ 0; 1 ] [ (0, xSym, 1) ] 0 (set [ 1 ]) }

        let blockB =
            { Nonterminal = ntB
              Dfa = Dfa.fromTransitions [ 0; 1 ] [ (0, xSym, 1) ] 0 (set [ 1 ]) }

        let rsm =
            { Blocks = [ blockA; blockB ]
              StartBlock = ntA }

        Assert.Equal(2, rsm.Blocks.Length)
        Assert.Equal(ntA, rsm.StartBlock)
        Assert.Equal<int>(set [ 0 ], RSM.startStates rsm)


module RsmBuilderPropertyTests =

    open FsCheck
    open FsCheck.Xunit

    [<Fact>]
    let ``buildRSMFromText produces deterministic blocks`` () =
        let ebnfTexts =
            [ "S -> a S b\nS -> eps"
              "S -> a b\nS -> a S b"
              "S -> ( a | b ) S\nS -> eps"
              "S -> a\nA -> b" ]

        ebnfTexts
        |> List.forall (fun text ->
            let rsm = RsmBuilder.buildRSMFromText text

            rsm.Blocks |> List.forall (fun block -> Dfa.isDeterministic block.Dfa))

    [<Fact>]
    let ``buildRSMFromText blocks match nonterminal count`` () =
        let ebnfTexts =
            [ "S -> a S b\nS -> eps"
              "S -> a b\nS -> a S b"
              "S -> ( a | b ) S\nS -> eps"
              "S -> a\nA -> b" ]

        ebnfTexts
        |> List.forall (fun text ->
            let rsm = RsmBuilder.buildRSMFromText text
            let rules = EbnfParser.parseEbnf text
            let grouped = EbnfParser.groupRules rules
            rsm.Blocks.Length = Map.count grouped)

    [<Fact>]
    let ``buildRSMFromText handles single terminal rule`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a"
        Assert.Equal(1, rsm.Blocks.Length)
        Assert.True(Dfa.isDeterministic rsm.Blocks.Head.Dfa)


module ExtendedRSMTests =

    [<Fact>]
    let ``create produces ExtendedRSM with correct original RSM`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a"
        let freshStart = Nonterminal "S'"
        let extRsm = ExtendedRSM.create freshStart rsm

        Assert.Equal(rsm, ExtendedRSM.originalRsm extRsm)

    [<Fact>]
    let ``create produces ExtendedRSM with correct fresh start`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a"
        let freshStart = Nonterminal "S'"
        let extRsm = ExtendedRSM.create freshStart rsm

        Assert.Equal(freshStart, ExtendedRSM.freshStart extRsm)

    [<Fact>]
    let ``extRsm has fresh start as start block`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a"
        let freshStart = Nonterminal "S'"
        let extRsm = ExtendedRSM.create freshStart rsm

        Assert.Equal(freshStart, ExtendedRSM.extRsm extRsm |> RSM.startBlock |> (fun b -> b.Nonterminal))

    [<Fact>]
    let ``extRsm has one more block than original`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a"
        let freshStart = Nonterminal "S'"
        let extRsm = ExtendedRSM.create freshStart rsm

        Assert.Equal(rsm.Blocks.Length + 1, (ExtendedRSM.extRsm extRsm).Blocks.Length)

    [<Fact>]
    let ``flattenExtRsm successfully flattens extended RSM`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a b"
        let freshStart = Nonterminal "S'"
        let extRsm = ExtendedRSM.create freshStart rsm
        let flat = ExtendedRSM.flattenExtRsm extRsm

        Assert.True(flat.StateInfo.Length > 0)
        Assert.True(flat.BlockStart.Count > 0)

    [<Fact>]
    let ``extBlocks returns blocks including fresh start block`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a"
        let freshStart = Nonterminal "S'"
        let extRsm = ExtendedRSM.create freshStart rsm
        let blocks = ExtendedRSM.extBlocks extRsm

        Assert.Equal(rsm.Blocks.Length + 1, blocks.Length)
        Assert.Equal(freshStart, blocks.Head.Nonterminal)

    [<Fact>]
    let ``stateCount of extended RSM is greater than original`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a b | c"
        let freshStart = Nonterminal "S'"
        let extRsm = ExtendedRSM.create freshStart rsm

        Assert.True(ExtendedRSM.stateCount extRsm > RSM.stateCount rsm)

    [<Fact>]
    let ``originalStartBlock returns correct block from original RSM`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a"
        let freshStart = Nonterminal "S'"
        let extRsm = ExtendedRSM.create freshStart rsm

        let origBlock = ExtendedRSM.originalStartBlock extRsm
        Assert.Equal(Nonterminal "S", origBlock.Nonterminal)
        Assert.Equal(RSM.startBlock rsm, origBlock)

    [<Fact>]
    let ``originalStartNonterminal returns S from simple grammar`` () =
        let rsm = RsmBuilder.buildRSMFromText "A -> b\nS -> a A"
        let freshStart = Nonterminal "S'"
        let extRsm = ExtendedRSM.create freshStart rsm

        Assert.Equal(Nonterminal "A", ExtendedRSM.originalStartNonterminal extRsm)

    [<Fact>]
    let ``create is idempotent for fresh start`` () =
        let rsm = RsmBuilder.buildRSMFromText "S -> a"
        let freshStart = Nonterminal "S'"
        let ext1 = ExtendedRSM.create freshStart rsm
        let ext2 = ExtendedRSM.create freshStart rsm

        Assert.Equal(ExtendedRSM.extRsm ext1, ExtendedRSM.extRsm ext2)
