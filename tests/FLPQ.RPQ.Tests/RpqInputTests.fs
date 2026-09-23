module RpqInputTests

open Xunit
open FLPQ.Languages
open FLPQ.RPQ

[<Fact>]
let ``uppercase labels become terminals`` () =
    let r = RpqInput.parseRegexpText "S -> A / B"
    Assert.Equal(RSeq(RTerm(Terminal "A"), RTerm(Terminal "B")), r)

[<Fact>]
let ``lowercase labels stay terminals`` () =
    let r = RpqInput.parseRegexpText "S -> a / b*"
    Assert.Equal(RSeq(RTerm(Terminal "a"), RStar(RTerm(Terminal "b"))), r)

[<Fact>]
let ``first rule is used when multiple rules are present`` () =
    let r = RpqInput.parseRegexpText "S -> A\nT -> B"
    Assert.Equal(RTerm(Terminal "A"), r)

[<Fact>]
let ``eps in the query is preserved`` () =
    let r = RpqInput.parseRegexpText "S -> eps / A"
    Assert.Equal(RSeq(REps, RTerm(Terminal "A")), r)

[<Fact>]
let ``empty text throws`` () =
    Assert.Throws<System.Exception>(fun () -> RpqInput.parseRegexpText "" |> ignore)

[<Fact>]
let ``whitespace-only text throws`` () =
    Assert.Throws<System.Exception>(fun () -> RpqInput.parseRegexpText "   \n  " |> ignore)
