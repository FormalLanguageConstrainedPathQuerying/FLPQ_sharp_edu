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
        let g = LanguageRegistry.APlus.Grammars.[3]
        let rsm = g.Rsm
        let freshStart = Nonterminal("S'")
        let ersm = ExtendedRSM.create freshStart rsm
        let input = [ Terminal "a"; Terminal "a" ]
        let graph = TestHelpers.terminalsToGraph input
        let pathIndex = GLL.buildPathIndex freshStart ersm graph
        TestHelpers.assertPathIndexInvariant "GLL golden" pathIndex None None None
        let tex = PathIndexTeX.toTeX string string pathIndex
        verifyGolden "path_index_gll_aa.tex" (wrapInTemplate templatePath tex)

    [<Fact>]
    let ``RNGLR path index TeX golden for S->aaA|aA, A->aA|eps, input aa`` () =
        let g = LanguageRegistry.APlus.Grammars.[3]
        let rsm = g.Rsm
        let startNt = (RSM.startBlock rsm).Nonterminal
        let freshStart = Nonterminal("S'")
        let input = [ Terminal "a"; Terminal "a" ]
        let graph = TestHelpers.terminalsToGraph input
        let rsmFixed = { rsm with StartBlock = startNt }
        let ersm = ExtendedRSM.create freshStart rsmFixed
        let pathIndex = Rnglr.buildPathIndex freshStart ersm graph

        TestHelpers.assertPathIndexInvariant
            "RNGLR golden"
            pathIndex
            (Some(ersm.ExtendedRsm.BlockStart |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq))
            (Some(RSM.blockFinalsMap ersm.ExtendedRsm))
            (Some ersm.ExtendedRsm.FinalStates)

        let tex = PathIndexTeX.toTeX string string pathIndex
        verifyGolden "path_index_rnglr_aa.tex" (wrapInTemplate templatePath tex)

module PathIndexCompilation =

    [<Fact>]
    [<Trait("Category", "TeX")>]
    let ``GLL path index TeX compiles with lualatex`` () =
        let g = LanguageRegistry.APlus.Grammars.[3]
        let rsm = g.Rsm
        let freshStart = Nonterminal("S'")
        let ersm = ExtendedRSM.create freshStart rsm
        let input = [ Terminal "a"; Terminal "a" ]
        let graph = TestHelpers.terminalsToGraph input
        let pathIndex = GLL.buildPathIndex freshStart ersm graph
        TestHelpers.assertPathIndexInvariant "GLL compilation" pathIndex None None None
        let tex = PathIndexTeX.toTeX string string pathIndex
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

    [<Fact>]
    [<Trait("Category", "TeX")>]
    let ``RNGLR path index TeX compiles with lualatex`` () =
        let g = LanguageRegistry.APlus.Grammars.[3]
        let rsm = g.Rsm
        let startNt = (RSM.startBlock rsm).Nonterminal
        let freshStart = Nonterminal("S'")
        let input = [ Terminal "a"; Terminal "a" ]
        let graph = TestHelpers.terminalsToGraph input
        let rsmFixed = { rsm with StartBlock = startNt }
        let ersm = ExtendedRSM.create freshStart rsmFixed
        let pathIndex = Rnglr.buildPathIndex freshStart ersm graph

        TestHelpers.assertPathIndexInvariant
            "RNGLR compilation"
            pathIndex
            (Some(ersm.ExtendedRsm.BlockStart |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq))
            (Some(RSM.blockFinalsMap ersm.ExtendedRsm))
            (Some ersm.ExtendedRsm.FinalStates)

        let tex = PathIndexTeX.toTeX string string pathIndex
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)
