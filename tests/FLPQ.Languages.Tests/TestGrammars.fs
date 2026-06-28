module TestGrammars

open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

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
            |> MyGen.map (fun bits -> bits |> List.map (fun b -> if b = 0 then "a" else "b") |> String.concat " "))
        |> MyArb.fromGen

let grammar1Accept = [ "a b a b"; "a b"; ""; "a a b b"; "a a b a b b" ]

let grammar1Reject = [ "a a"; "b b"; "a b b"; "a b b a"; "b"; "a"; "a b a b a" ]

let grammar3Accept = [ "a"; "a a"; "a a a a"; "a a a a a" ]

let grammar3Reject = [ ""; "b" ]

type AStringGenerators =

    static member AString() : Arbitrary<string> =
        MyGen.choose (0, 15)
        |> MyGen.map (fun len ->
            if len = 0 then
                ""
            else
                System.String.Concat(Array.replicate len "a ").Trim())
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

let exprAccept =
    [ "x"
      "( x )"
      "( x ) * x"
      "x + x"
      "x + x * x"
      "x * ( x + x )"
      "( x * ( x + x ) )" ]

let exprReject = [ ""; "( )"; "+ x"; "x +"; "x + ( )" ]

type ExprStringGenerators =

    static member ExprString() : Arbitrary<string> =
        let terminals = [| "x" |]
        let operators = [| "+"; "*" |]

        let rec genExpr depth =
            if depth <= 0 then
                MyGen.elements terminals
            else
                MyGen.choose (0, 2)
                |> MyGen.bind (fun choice ->
                    match choice with
                    | 0 -> MyGen.elements terminals
                    | 1 -> genExpr (depth - 1) |> MyGen.map (fun inner -> "( " + inner + " )")
                    | _ ->
                        genExpr (depth - 1)
                        |> MyGen.bind (fun left ->
                            genExpr (depth - 1)
                            |> MyGen.bind (fun right ->
                                MyGen.elements operators |> MyGen.map (fun op -> left + " " + op + " " + right))))

        MyGen.choose (0, 4) |> MyGen.bind (fun d -> genExpr d) |> MyArb.fromGen

let private augmentStringGrammar (g: Grammar<string, string>) =
    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    LRAutomaton.augmentGrammar freshStart g

let augGrammar1 = augmentStringGrammar grammar1

let augGrammar2 = augmentStringGrammar grammar2

let augGrammar3 = augmentStringGrammar grammar3

let augGrammar4 = augmentStringGrammar grammar4

let augGrammar5 = augmentStringGrammar grammar5

let augGrammar6 = augmentStringGrammar grammar6

let augGrammar7 = augmentStringGrammar grammar7

let augGrammar8 = augmentStringGrammar grammar8
