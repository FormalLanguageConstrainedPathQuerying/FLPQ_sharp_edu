module TestGrammars

open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

let grammar1 = Grammar.parseGrammar "S -> a S b S\nS -> eps"

let grammar2 = Grammar.parseGrammar "S -> a S b\nS -> eps\nS -> S S"

let grammar3 = Grammar.parseGrammar "S -> a S\nS -> a"

let grammar4 = Grammar.parseGrammar "S -> S a\nS -> a"

let grammar5 = Grammar.parseGrammar "S -> S S\nS -> S S S\nS -> a"

let grammar1Accept = [ "a b a b"; "a b"; ""; "a a b b"; "a a b a b b" ]

let grammar1Reject = [ "a a"; "b b"; "a b b"; "a b b a"; "b"; "a"; "a b a b a" ]

let grammar3Accept = [ "a"; "a a"; "a a a a"; "a a a a a" ]

let grammar3Reject = [ ""; "b" ]

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

let grammar9 =
    Grammar.parseGrammar
        "
    S -> S1
    S -> S2
    S1 -> a b S c
    S1 -> eps
    S2 -> a x S y
    S2 -> eps
    "

let grammar9Accept =
    [ ""
      "a b c"
      "a x y"
      "a b a b c c"
      "a x a x y y"
      "a x a b c y"
      "a b a x y c" ]

let grammar9Reject =
    [ "a"
      "x"
      "y"
      "c"
      "a x c"
      "a b y"
      "a x a b"
      "a b a x y"
      "a x a b c"
      "a x a b y" ]

let augGrammar9 = augmentStringGrammar grammar9

let grammar10 =
    Grammar.parseGrammar
        "
    S -> S1
    S -> S2
    S1 -> a b S c
    S -> eps
    S2 -> a x S y
    "

let grammar10Accept = grammar9Accept

let grammar10Reject = grammar9Reject

let augGrammar10 = augmentStringGrammar grammar10
