module AlgorithmTypesTests

open Xunit
open Argu
open FLPQ.Cli.AlgorithmTypes

[<Fact>]
let ``displayName of CYK`` () = Assert.Equal("CYK", displayName CYK)

[<Fact>]
let ``displayName of Valiant`` () =
    Assert.Equal("Valiant", displayName Valiant)

[<Fact>]
let ``displayName of LL`` () = Assert.Equal("LL", displayName LL)

[<Fact>]
let ``displayName of LR0`` () = Assert.Equal("LR(0)", displayName LR0)

[<Fact>]
let ``displayName of SLR1`` () =
    Assert.Equal("SLR(1)", displayName SLR1)

[<Fact>]
let ``displayName of CLR1`` () =
    Assert.Equal("CLR(1)", displayName CLR1)

[<Fact>]
let ``displayName of ValiantModified`` () =
    Assert.Equal("Valiant (modified)", displayName ValiantModified)

[<Fact>]
let ``displayName of GLL`` () = Assert.Equal("GLL", displayName GLL)

[<Fact>]
let ``displayName of RNGLR`` () =
    Assert.Equal("RNGLR", displayName RNGLR)

[<Fact>]
let ``parse algorithm CYK from CLI args`` () =
    let args = [| "-a"; "CYK" |]
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse(args)
    Assert.Equal(CYK, results.GetResult Algorithm)

[<Fact>]
let ``parse algorithm Valiant from CLI args`` () =
    let args = [| "-a"; "Valiant" |]
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse(args)
    Assert.Equal(Valiant, results.GetResult Algorithm)

[<Fact>]
let ``parse algorithm with long option`` () =
    let args = [| "--algorithm"; "LL" |]
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse(args)
    Assert.Equal(LL, results.GetResult Algorithm)

[<Fact>]
let ``invalid algorithm name throws`` () =
    let args = [| "-a"; "InvalidAlgo" |]
    let parser = ArgumentParser.Create<Arguments>()
    Assert.Throws<ArguParseException>(fun () -> parser.Parse(args) |> ignore)

[<Fact>]
let ``displayName of AzimovCFPQ`` () =
    Assert.Equal("Azimov CFPQ", displayName AzimovCFPQ)

[<Fact>]
let ``parse algorithm AzimovCFPQ from CLI args`` () =
    let args = [| "-a"; "AzimovCFPQ" |]
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse(args)
    Assert.Equal(AzimovCFPQ, results.GetResult Algorithm)

[<Fact>]
let ``parse --semantics simplePath`` () =
    let args = [| "-a"; "BelyaninRPQ"; "--semantics"; "simplePath" |]
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse(args)
    Assert.Equal(PathQuerySemantics.SimplePath, results.GetResult Semantics)

[<Fact>]
let ``parse -semantics reachability (single dash)`` () =
    let args = [| "-a"; "BelyaninRPQ"; "-semantics"; "reachability" |]
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse(args)
    Assert.Equal(PathQuerySemantics.Reachability, results.GetResult Semantics)

[<Fact>]
let ``parse --semantics allPaths`` () =
    let args = [| "-a"; "AzimovCFPQ"; "--semantics"; "allPaths" |]
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse(args)
    Assert.Equal(PathQuerySemantics.AllPaths, results.GetResult Semantics)

[<Fact>]
let ``semantics is absent by default and defaults to reachability`` () =
    let args = [| "-a"; "BelyaninRPQ" |]
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse(args)
    Assert.False(results.Contains Semantics)

    Assert.Equal(
        PathQuerySemantics.Reachability,
        results.GetResult(Semantics, defaultValue = PathQuerySemantics.Reachability)
    )

[<Fact>]
let ``summary flag is parsed`` () =
    let args = [| "-a"; "CYK"; "-s" |]
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse(args)
    Assert.True(results.Contains Summary)

[<Fact>]
let ``use-dot flag is parsed`` () =
    let args = [| "-a"; "LR0"; "--use-dot" |]
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse(args)
    Assert.True(results.Contains UseDot)
