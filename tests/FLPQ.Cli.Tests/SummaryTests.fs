module SummaryTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Summary
open FLPQ.Cli.AlgorithmTypes
open FLPQ.Printers

[<Fact>]
let ``algorithmToKind for CYK is TablePerStep`` () =
    Assert.Equal(SummaryTeX.SummaryKind.TablePerStep, algorithmToKind CYK)

[<Fact>]
let ``algorithmToKind for Valiant is TablePerStep`` () =
    Assert.Equal(SummaryTeX.SummaryKind.TablePerStep, algorithmToKind Valiant)

[<Fact>]
let ``algorithmToKind for LL is LL`` () =
    Assert.Equal(SummaryTeX.SummaryKind.LL, algorithmToKind LL)

[<Fact>]
let ``algorithmToKind for LR0 is LR`` () =
    Assert.Equal(SummaryTeX.SummaryKind.LR, algorithmToKind LR0)

[<Fact>]
let ``algorithmToKind for SLR1 is LR`` () =
    Assert.Equal(SummaryTeX.SummaryKind.LR, algorithmToKind SLR1)

[<Fact>]
let ``algorithmToKind for CLR1 is LR`` () =
    Assert.Equal(SummaryTeX.SummaryKind.LR, algorithmToKind CLR1)

[<Fact>]
let ``algorithmLower for CYK`` () = Assert.Equal("cyk", algorithmLower CYK)

[<Fact>]
let ``algorithmLower for CLR1`` () =
    Assert.Equal("clr1", algorithmLower CLR1)

[<Fact>]
let ``algorithmToKind for ValiantModified is TablePerStep`` () =
    Assert.Equal(SummaryTeX.SummaryKind.TablePerStep, algorithmToKind ValiantModified)

[<Fact>]
let ``algorithmToKind for GLL is GLL`` () =
    Assert.Equal(SummaryTeX.SummaryKind.GLL, algorithmToKind GLL)

[<Fact>]
let ``algorithmToKind for RNGLR is RNGLR`` () =
    Assert.Equal(SummaryTeX.SummaryKind.RNGLR, algorithmToKind RNGLR)

[<Fact>]
let ``algorithmLower for Valiant`` () =
    Assert.Equal("valiant", algorithmLower Valiant)

[<Fact>]
let ``algorithmLower for ValiantModified`` () =
    Assert.Equal("valiantmodified", algorithmLower ValiantModified)

[<Fact>]
let ``algorithmLower for LL`` () = Assert.Equal("ll", algorithmLower LL)

[<Fact>]
let ``algorithmLower for LR0`` () = Assert.Equal("lr0", algorithmLower LR0)

[<Fact>]
let ``algorithmLower for SLR1`` () =
    Assert.Equal("slr1", algorithmLower SLR1)

[<Fact>]
let ``algorithmLower for GLL`` () = Assert.Equal("gll", algorithmLower GLL)

[<Fact>]
let ``algorithmLower for RNGLR`` () =
    Assert.Equal("rnglr", algorithmLower RNGLR)

/// Runs `f` inside two fresh temp directories (vizDir, resultDir), removed afterwards.
let private withTempDirs (f: string * string -> unit) : unit =
    let vizDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let resultDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory vizDir |> ignore

    try
        f (vizDir, resultDir)
    finally
        for d in [ vizDir; resultDir ] do
            try
                Directory.Delete(d, true)
            with _ ->
                ()

[<Fact>]
let ``buildSummary returns false when a top-level dot file is malformed`` () =
    withTempDirs (fun (vizDir, resultDir) ->
        File.WriteAllText(Path.Combine(vizDir, "bad.dot"), "this is not dot syntax !!!")

        let ok =
            Summary.buildSummary (Helpers.findSummaryTemplate ()) SLR1 vizDir resultDir false

        Assert.False(ok))

[<Fact>]
let ``buildSummary returns false when a step dot file is malformed`` () =
    withTempDirs (fun (vizDir, resultDir) ->
        let stepDir = Path.Combine(vizDir, "step_0")
        Directory.CreateDirectory stepDir |> ignore
        File.WriteAllText(Path.Combine(stepDir, "bad.dot"), "this is not dot syntax !!!")

        let ok =
            Summary.buildSummary (Helpers.findSummaryTemplate ()) SLR1 vizDir resultDir false

        Assert.False(ok))

[<Fact>]
let ``buildSummary for SLR1 includes the LR automaton PDF when lr_automaton.dot exists`` () =
    withTempDirs (fun (vizDir, resultDir) ->
        File.WriteAllText(Path.Combine(vizDir, "lr_automaton.dot"), "digraph G { a -> b }")

        let ok =
            Summary.buildSummary (Helpers.findSummaryTemplate ()) SLR1 vizDir resultDir false

        Assert.True(ok)

        let merged = Path.Combine(resultDir, "slr1", "slr1_merged.tex")
        let text = File.ReadAllText merged
        Assert.Contains("lr_automaton.pdf", text))

[<Fact>]
let ``buildSummary for SLR1 without automaton files omits the automaton section`` () =
    withTempDirs (fun (vizDir, resultDir) ->
        let ok =
            Summary.buildSummary (Helpers.findSummaryTemplate ()) SLR1 vizDir resultDir false

        Assert.True(ok)

        let merged = Path.Combine(resultDir, "slr1", "slr1_merged.tex")
        let text = File.ReadAllText merged
        Assert.DoesNotContain("LR Automaton", text))

[<Fact>]
let ``buildSummary for GLL includes the extended RSM PDF when ext_rsm.dot exists`` () =
    withTempDirs (fun (vizDir, resultDir) ->
        File.WriteAllText(Path.Combine(vizDir, "ext_rsm.dot"), "digraph G { a -> b }")

        let ok =
            Summary.buildSummary (Helpers.findSummaryTemplate ()) GLL vizDir resultDir false

        Assert.True(ok)

        let merged = Path.Combine(resultDir, "gll", "gll_merged.tex")
        let text = File.ReadAllText merged
        Assert.Contains("Extended RSM", text))
