module SummaryTests

open Xunit
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
