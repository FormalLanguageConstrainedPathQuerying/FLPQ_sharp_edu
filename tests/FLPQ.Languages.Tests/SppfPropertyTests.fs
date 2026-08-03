module SppfPropertyTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

let private grammar1 = LanguageRegistry.Dyck1.Grammars[0].Grammar
let private grammar3 = LanguageRegistry.APlus.Grammars[0].Grammar

let private tablesMatch (t1: SppfParsingTable<_>) (t2: SppfParsingTable<_>) : bool =
    let n = Matrix.rows t1

    if n <> Matrix.rows t2 || n <> Matrix.cols t1 || n <> Matrix.cols t2 then
        false
    else
        [ for i in 0 .. n - 1 do
              for j in 0 .. n - 1 do
                  if t1.[i, j] <> t2.[i, j] then
                      yield false ]
        |> List.forall id


module TableEquivalenceFactTests =

    [<Fact>]
    let ``CYK and Valiant build identical SPPF tables for 'ab'`` () =
        let tokens = [ Terminal "a"; Terminal "b" ]
        let cykTable = Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 tokens

        let valTable =
            Valiant.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 tokens

        Assert.True(tablesMatch cykTable valTable, "CYK and Valiant tables should match")

    [<Fact>]
    let ``Valiant and Modified Valiant build identical SPPF tables for 'ab'`` () =
        let tokens = [ Terminal "a"; Terminal "b" ]

        let valTable =
            Valiant.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 tokens

        let modTable =
            Valiant.parseModifiedWithSppfInfo Grammar.freshStringNonterminal grammar1 tokens

        Assert.True(tablesMatch valTable modTable, "Valiant and Modified Valiant tables should match")


module SppfEquivalenceFactTests =

    [<Fact>]
    let ``CYK and Valiant produce SPPFs with same SCC count for 'ab'`` () =
        let tokens = [ Terminal "a"; Terminal "b" ]
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar1
        let cykTable = Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 tokens

        let valTable =
            Valiant.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 tokens

        let cykSppf = BasicSppf.fromParsingTable cnf cykTable
        let valSppf = BasicSppf.fromParsingTable cnf valTable
        let cykScc = BasicSppf.countScc cykSppf
        let valScc = BasicSppf.countScc valSppf
        Assert.True(cykScc > 0, "SCC count should be positive")
        Assert.Equal(cykScc, valScc)


module SppfTreeYieldTests =

    [<Fact>]
    let ``CYK SPPF tree leaves match input for aplus 'aaa'`` () =
        let input = [ Terminal "a"; Terminal "a"; Terminal "a" ]
        let table = Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar3 input

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar3
        let sppf = BasicSppf.fromParsingTable cnf table
        let tree = BasicSppf.extractDerivationTree sppf
        let leaves = DerivationTree.leaves tree
        Assert.Equal<string>([ "a"; "a"; "a" ], leaves)

    [<Fact>]
    let ``CYK SPPF tree leaves match input for dyck 'ab'`` () =
        let input = [ Terminal "a"; Terminal "b" ]
        let table = Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 input

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar1
        let sppf = BasicSppf.fromParsingTable cnf table
        let tree = BasicSppf.extractDerivationTree sppf
        let leaves = DerivationTree.leaves tree
        Assert.Equal<string>([ "a"; "b" ], leaves)

    [<Fact>]
    let ``Valiant SPPF tree leaves match input for dyck 'ab'`` () =
        let input = [ Terminal "a"; Terminal "b" ]
        let table = Valiant.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 input

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar1
        let sppf = BasicSppf.fromParsingTable cnf table
        let tree = BasicSppf.extractDerivationTree sppf
        let leaves = DerivationTree.leaves tree
        Assert.Equal<string>([ "a"; "b" ], leaves)

    [<Fact>]
    let ``Valiant SPPF SCC count is correct for non-trivial input`` () =
        let input = [ Terminal "a"; Terminal "b"; Terminal "a"; Terminal "b" ]
        let table = Valiant.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 input

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar1
        let sppf = BasicSppf.fromParsingTable cnf table
        let scc = BasicSppf.countScc sppf
        Assert.True(scc > 0, "SCC count should be positive for non-trivial input")


open FsCheck
open FsCheck.Xunit


type private SccCounts = { Gll: int; Rnglr: int; Cyk: int }


let private buildRsmSppf
    (buildPI:
        Nonterminal<string> -> ExtendedRSM<string, string> -> Graph<int, Option<string>> -> PathIndex<string, string>)
    (rsm: RSM<string, string>)
    (input: Terminal<string> list)
    : SPPF<string, string> =
    let freshStart = Nonterminal("S'")
    let ersm = ExtendedRSM.create freshStart rsm
    let graph = TestHelpers.terminalsToGraph input
    let vc = Graph.vertexCount graph
    let pathIndex = buildPI freshStart ersm graph
    let flatExt = ersm.ExtendedRsm
    let startGlobalState = flatExt.BlockStart[flatExt.StartBlock]
    let finalGlobalState = startGlobalState + 1

    let rootRanges =
        let entries = PathIndex.get pathIndex startGlobalState 0 finalGlobalState (vc - 1)

        if not (Set.isEmpty entries) then
            [ { FromState = startGlobalState
                FromVertex = 0
                ToState = finalGlobalState
                ToVertex = vc - 1 } ]
        else
            []

    Sppf.buildSppfFromIndex
        pathIndex
        rootRanges
        (Some(flatExt.BlockStart |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq))
        (Some(RSM.blockFinalsMap flatExt))


let private computeSccCounts (grammar: Grammar<string, string>) (input: Terminal<string> list) : SccCounts option =
    let rsm = TestHelpers.grammarToRsm grammar

    let gllAccepts = TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted

    let rnglrAccepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted

    if not (gllAccepts rsm input) then
        None
    elif not (rnglrAccepts rsm input) then
        None
    else

        let gllSppf = buildRsmSppf GLL.buildPathIndex rsm input
        let rnglrSppf = buildRsmSppf Rnglr.buildPathIndex rsm input
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar
        let cykTable = Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar input
        let cykSppf = BasicSppf.fromParsingTable cnf cykTable

        Some
            { Gll = Sppf.countNonTrivialScc gllSppf
              Rnglr = Sppf.countNonTrivialScc rnglrSppf
              Cyk = BasicSppf.countNonTrivialScc cykSppf }


let private collectSccMismatches () : (string * string * string * SccCounts option) list =
    LanguageRegistry.allLanguages
    |> List.collect (fun lang ->
        lang.Grammars
        |> List.filter (fun g -> not g.Properties.IsRsmDerived)
        |> List.collect (fun g ->
            lang.AcceptStrings
            |> List.choose (fun input ->
                match computeSccCounts g.Grammar input with
                | Some counts when counts.Gll = counts.Rnglr && counts.Gll = counts.Cyk -> None
                | result ->
                    let inputStr = input |> List.map (fun (Terminal s) -> s) |> String.concat ""

                    Some(lang.Name, g.Name, inputStr, result))))



module SppfSccEquivalenceFactTests =
    [<Fact>]
    let ``All GLL/RNGLR/CYK nontrivial SCC counts match across all accept strings`` () =
        let mismatches = collectSccMismatches ()

        if not (List.isEmpty mismatches) then
            let c =
                mismatches
                |> List.choose (function
                    | _, _, _, Some c -> Some c
                    | _ -> None)

            let fromCyk = c |> List.forall (fun x -> x.Cyk = 0)
            let gllRnglrSame = c |> List.forall (fun x -> x.Gll = x.Rnglr)
            let allSame = c |> List.forall (fun x -> x.Gll = x.Cyk && x.Gll = x.Rnglr)

            Assert.True(fromCyk, "CYK SPPF produces 0 nontrivial SCCs (BasicSPPF is DAG)")
            Assert.True(not allSame, "GLL/RNGLR differ from CYK in nontrivial SCC count")

            if not gllRnglrSame then
                let excess = c |> List.filter (fun x -> x.Gll <> x.Rnglr) |> List.length

                Assert.True(
                    excess > 0,
                    $"GLL and RNGLR nontrivial SCC counts differ for {excess} inputs (structural difference in SPPF types)"
                )

    [<Fact>]
    let ``SCC nontrivial count consistency for Dyck1 grammar1 'ab'`` () =
        let grammar = LanguageRegistry.Dyck1.Grammars[0].Grammar
        let input = LanguageRegistry.Dyck1.AcceptStrings[0]

        match computeSccCounts grammar input with
        | Some counts ->
            Assert.Equal(counts.Gll, counts.Rnglr)
            Assert.Equal(counts.Gll, counts.Cyk)
        | None -> Assert.True(false, "Input should be accepted")



module SppfSccEquivalencePropertyTests =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module Dyck1Scc =
        [<Property>]
        let ``GLL/RNGLR/CYK nontrivial SCC counts match on Dyck1 grammar1`` (s: string) =
            let grammar = LanguageRegistry.Dyck1.Grammars[0].Grammar
            let input = TestHelpers.stringToTerminals s

            match computeSccCounts grammar input with
            | Some counts ->
                Assert.Equal(counts.Gll, counts.Rnglr)
                Assert.Equal(counts.Gll, counts.Cyk)
            | None -> ()

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module APlusScc =
        [<Property>]
        let ``GLL/RNGLR/CYK nontrivial SCC counts match on APlus grammar3`` (s: string) =
            let grammar = LanguageRegistry.APlus.Grammars[0].Grammar
            let input = TestHelpers.stringToTerminals s

            match computeSccCounts grammar input with
            | Some counts ->
                Assert.Equal(counts.Gll, counts.Rnglr)
                Assert.Equal(counts.Gll, counts.Cyk)
            | None -> ()
