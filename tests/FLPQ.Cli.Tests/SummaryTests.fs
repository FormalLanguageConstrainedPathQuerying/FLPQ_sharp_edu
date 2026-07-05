module SummaryTests

open Xunit
open FLPQ.Cli.Summary
open FLPQ.Cli.AlgorithmTypes

[<Fact>]
let ``algorithmKind for CYK is TablePerStep`` () =
    Assert.Equal(TablePerStep, algorithmKind CYK)

[<Fact>]
let ``algorithmKind for Valiant is TablePerStep`` () =
    Assert.Equal(TablePerStep, algorithmKind Valiant)

[<Fact>]
let ``algorithmKind for LL is StackPerStep`` () =
    Assert.Equal(StackPerStep, algorithmKind LL)

[<Fact>]
let ``algorithmKind for LR0 is StackPerStep`` () =
    Assert.Equal(StackPerStep, algorithmKind LR0)

[<Fact>]
let ``algorithmKind for SLR1 is StackPerStep`` () =
    Assert.Equal(StackPerStep, algorithmKind SLR1)

[<Fact>]
let ``algorithmKind for CLR1 is StackPerStep`` () =
    Assert.Equal(StackPerStep, algorithmKind CLR1)

[<Fact>]
let ``algorithmLower for CYK`` () = Assert.Equal("cyk", algorithmLower CYK)

[<Fact>]
let ``algorithmLower for CLR1`` () =
    Assert.Equal("clr1", algorithmLower CLR1)
