module PathIndexTeXTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.Printers
open FLPQ.TestUtilities

open GoldenHelpers

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_template.tex")

module PathIndexGolden =

    [<Fact>]
    let ``GLL path index TeX golden for S->aaA|aA, A->aA|eps, input aa`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a a A
        S -> a A
        A -> a A
        A -> eps
        "

        let rsm = TestHelpers.grammarToRsm g
        let input = [ "a"; "a" ]
        let graph = TestHelpers.terminalsToGraph input
        let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])
        TestHelpers.assertPathIndexInvariant "GLL golden" pathIndex
        let tex = PathIndexTeX.toTeX string string pathIndex
        verifyGolden "path_index_gll_aa.tex" (wrapInTemplate templatePath tex)

    [<Fact>]
    let ``RNGLR path index TeX golden for S->aaA|aA, A->aA|eps, input aa`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a a A
        S -> a A
        A -> a A
        A -> eps
        "

        let rsm = TestHelpers.grammarToRsm g
        let startNt = (RSM.startBlock rsm).Nonterminal
        let freshStart = Nonterminal("S'")
        let input = [ "a"; "a" ]
        let graph = TestHelpers.terminalsToGraph input
        let rsmFixed = { rsm with StartBlock = startNt }
        let pathIndex = Rnglr.buildPathIndex freshStart rsmFixed graph
        TestHelpers.assertPathIndexInvariant "RNGLR golden" pathIndex
        let tex = PathIndexTeX.toTeX string string pathIndex
        verifyGolden "path_index_rnglr_aa.tex" (wrapInTemplate templatePath tex)

module PathIndexCompilation =

    [<Fact>]
    [<Trait("Category", "TeX")>]
    let ``GLL path index TeX compiles with lualatex`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a a A
        S -> a A
        A -> a A
        A -> eps
        "

        let rsm = TestHelpers.grammarToRsm g
        let input = [ "a"; "a" ]
        let graph = TestHelpers.terminalsToGraph input
        let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])
        TestHelpers.assertPathIndexInvariant "GLL compilation" pathIndex
        let tex = PathIndexTeX.toTeX string string pathIndex
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

    [<Fact>]
    [<Trait("Category", "TeX")>]
    let ``RNGLR path index TeX compiles with lualatex`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a a A
        S -> a A
        A -> a A
        A -> eps
        "

        let rsm = TestHelpers.grammarToRsm g
        let startNt = (RSM.startBlock rsm).Nonterminal
        let freshStart = Nonterminal("S'")
        let input = [ "a"; "a" ]
        let graph = TestHelpers.terminalsToGraph input
        let rsmFixed = { rsm with StartBlock = startNt }
        let pathIndex = Rnglr.buildPathIndex freshStart rsmFixed graph
        TestHelpers.assertPathIndexInvariant "RNGLR compilation" pathIndex
        let tex = PathIndexTeX.toTeX string string pathIndex
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)
