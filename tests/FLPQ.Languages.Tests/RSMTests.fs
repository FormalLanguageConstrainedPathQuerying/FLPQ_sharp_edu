module RSMTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

let private makeSBlock () : RsmBlock<string, string> =
    let nt = Nonterminal "S"
    let aSym = RsmSymbol.RTerm(Terminal "a")
    let transitions = [ (0, aSym, 1) ]
    let dfa = Dfa.fromTransitions [ 0; 1 ] transitions 0 (set [ 1 ])

    { RsmBlock.nonterminal = nt; dfa = dfa }

let private makeSimpleRSM () : RSM<string, string> =
    { blocks = [ makeSBlock () ]
      startBlock = Nonterminal "S" }


module RsmBlockTests =

    [<Fact>]
    let ``Block has correct nonterminal`` () =
        let block = makeSBlock ()
        Assert.Equal(Nonterminal "S", block.nonterminal)

    [<Fact>]
    let ``Block DFA has correct start and final states`` () =
        let block = makeSBlock ()
        Assert.Equal(0, block.dfa.startState)
        Assert.Equal<int>(set [ 1 ], block.dfa.finalStates)

    [<Fact>]
    let ``Block DFA has correct transitions`` () =
        let block = makeSBlock ()
        let aSym = RsmSymbol.RTerm(Terminal "a")
        let result = Dfa.move block.dfa 0 aSym
        Assert.Equal(Some 1, result)

    [<Fact>]
    let ``Block DFA has correct alphabet`` () =
        let block = makeSBlock ()
        let alphabet = Dfa.alphabet block.dfa
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
        Assert.Equal(Nonterminal "S", sb.nonterminal)

    [<Fact>]
    let ``RSM accessor blockOf finds existing block`` () =
        let rsm = makeSimpleRSM ()
        let found = RSM.blockOf (Nonterminal "S") rsm
        Assert.True(found.IsSome)
        Assert.Equal(Nonterminal "S", found.Value.nonterminal)

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
            { RsmBlock.nonterminal = ntA
              dfa = Dfa.fromTransitions [ 0; 1 ] [ (0, xSym, 1) ] 0 (set [ 1 ]) }

        let blockB =
            { RsmBlock.nonterminal = ntB
              dfa = Dfa.fromTransitions [ 0; 1 ] [ (0, xSym, 1) ] 0 (set [ 1 ]) }

        let rsm =
            { RSM.blocks = [ blockA; blockB ]
              startBlock = ntA }

        Assert.Equal(2, rsm.blocks.Length)
        Assert.Equal(ntA, rsm.startBlock)
        Assert.Equal<int>(set [ 0 ], RSM.startStates rsm)
