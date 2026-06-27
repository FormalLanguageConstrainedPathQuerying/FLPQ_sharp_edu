module TestGrammars

open FsCheck
open FsCheck.Xunit
open FLPQ.Core

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb

let grammar1 = Grammar.parseGrammar "S -> a S b S\nS -> eps"

let grammar2 = Grammar.parseGrammar "S -> a S b\nS -> eps\nS -> S S"

let grammar3 = Grammar.parseGrammar "S -> a S\nS -> a"

let grammar4 = Grammar.parseGrammar "S -> S a\nS -> a"

let grammar5 = Grammar.parseGrammar "S -> S S\nS -> S S S\nS -> a"

type AbStringGenerators =

    static member AbString() : Arbitrary<string> =
        MyGen.choose (0, 12)
        |> MyGen.bind (fun len ->
            MyGen.choose (0, 1)
            |> MyGen.listOfLength len
            |> MyGen.map (fun bits -> bits |> List.map (fun b -> if b = 0 then 'a' else 'b') |> System.String.Concat))
        |> MyArb.fromGen

let grammar1Accept = [ "abab"; "ab"; ""; "aabb"; "aababb" ]

let grammar1Reject = [ "aa"; "bb"; "abb"; "abba"; "b"; "a"; "ababa" ]

let grammar3Accept = [ "a"; "aa"; "aaaa"; "aaaaa" ]

let grammar3Reject = [ ""; "b" ]

type AStringGenerators =

    static member AString() : Arbitrary<string> =
        MyGen.choose (0, 15)
        |> MyGen.map (fun len -> System.String('a', len))
        |> MyArb.fromGen

let grammar6 =
    Grammar.parseGrammar
        "
S -> x
S -> S + S
S -> S * S
S -> ( S )
"

let grammar7 =
    Grammar.parseGrammar
        "
E -> E + T
E -> T
T -> T * F
T -> F
F -> ( E )
F -> x
"

let grammar8 =
    Grammar.parseGrammar
        "
E -> T + E
E -> T
T -> F * T
T -> F
F -> ( E )
F -> x
"

let exprAccept = [ "x"; "(x)"; "(x)*x"; "x+x"; "x+x*x"; "x*(x+x)"; "(x*(x+x))" ]

let exprReject = [ ""; "()"; "+x"; "x+"; "x+()" ]
