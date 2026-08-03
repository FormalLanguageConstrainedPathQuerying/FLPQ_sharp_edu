module SppfPropertyTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
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
