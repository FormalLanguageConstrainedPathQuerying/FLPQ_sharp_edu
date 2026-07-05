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
