module RegexpToDfaTests

open Xunit
open FLPQ.Languages

/// Local regexp-language matcher used to verify the language of generated DFAs.
let rec private matches (r: Regexp<string, string>) (input: string list) : bool =
    match r with
    | REps -> List.isEmpty input
    | REmpty -> false
    | RTerm(Terminal t) -> input = [ t ]
    | RNonterm _ -> false
    | RSeq(l, rr) ->
        [ for i in 0 .. List.length input do
              if matches l (List.take i input) && matches rr (List.skip i input) then
                  true ]
        |> List.exists id
    | RAlt(l, rr) -> matches l input || matches rr input
    | RStar rp ->
        if List.isEmpty input then
            true
        else
            [ for i in 1 .. List.length input do
                  if matches rp (List.take i input) && matches (RStar rp) (List.skip i input) then
                      true ]
            |> List.exists id

let private allStringsUpTo (alphabet: string list) (maxLen: int) : string list list =
    let rec stringsOfLen (len: int) : string list list =
        if len = 0 then
            [ [] ]
        else
            alphabet
            |> List.collect (fun s -> stringsOfLen (len - 1) |> List.map (fun xs -> s :: xs))

    [ for len in 0..maxLen do
          yield! stringsOfLen len ]

let private terminalsOf (r: Regexp<string, string>) : string list =
    Regexp.symbols r
    |> List.choose (function
        | RsmSymbol.RTerm(Terminal t) -> Some t
        | _ -> None)
    |> List.distinct

let private verifyDfaLanguage (regexp: Regexp<string, string>) () =
    let dfa = Regexp.toDfa regexp
    Assert.True(Dfa.isDeterministic dfa)
    let alphabet = terminalsOf regexp
    Assert.Equal<Set<string>>(Set.ofList alphabet, Dfa.alphabet dfa)

    let mismatches =
        allStringsUpTo alphabet 4
        |> List.filter (fun s -> Dfa.accept dfa (List.map Terminal s) <> matches regexp s)

    Assert.Empty mismatches

[<Fact>]
let ``toDfa of a accepts exactly L(a) up to length 4`` () =
    verifyDfaLanguage (RTerm(Terminal "a")) ()

[<Fact>]
let ``toDfa of a* accepts exactly L(a*) up to length 4`` () =
    verifyDfaLanguage (RStar(RTerm(Terminal "a"))) ()

[<Fact>]
let ``toDfa of a|b accepts exactly L(a|b) up to length 4`` () =
    verifyDfaLanguage (RAlt(RTerm(Terminal "a"), RTerm(Terminal "b"))) ()

[<Fact>]
let ``toDfa of (ab)* accepts exactly L((ab)*) up to length 4`` () =
    verifyDfaLanguage (RStar(RSeq(RTerm(Terminal "a"), RTerm(Terminal "b")))) ()

[<Fact>]
let ``toDfa of eps accepts exactly L(eps)`` () = verifyDfaLanguage REps ()

[<Fact>]
let ``toDfa of a(b|c)*d accepts exactly L(a(b|c)*d) up to length 4`` () =
    let regexp =
        RSeq(RTerm(Terminal "a"), RSeq(RStar(RAlt(RTerm(Terminal "b"), RTerm(Terminal "c"))), RTerm(Terminal "d")))

    verifyDfaLanguage regexp ()
