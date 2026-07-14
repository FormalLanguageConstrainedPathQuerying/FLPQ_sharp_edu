module TestGrammars

open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

/// S -> a S b S
/// S -> eps
let grammar1 =
    Grammar.parseGrammar
        """
S -> a S b S
S -> eps
"""

/// S -> a S b
/// S -> eps
/// S -> S S
let grammar2 =
    Grammar.parseGrammar
        """
S -> a S b
S -> eps
S -> S S
"""

/// S -> a S
/// S -> a
let grammar3 =
    Grammar.parseGrammar
        """
S -> a S
S -> a
"""

/// S -> S a
/// S -> a
let grammar4 =
    Grammar.parseGrammar
        """
S -> S a
S -> a
"""

/// S -> S S
/// S -> S S S
/// S -> a
let grammar5 =
    Grammar.parseGrammar
        """
S -> S S
S -> S S S
S -> a
"""

let grammar1Accept = [ "a b a b"; "a b"; ""; "a a b b"; "a a b a b b" ]

let grammar1Reject = [ "a a"; "b b"; "a b b"; "a b b a"; "b"; "a"; "a b a b a" ]

let grammar3Accept = [ "a"; "a a"; "a a a a"; "a a a a a" ]

let grammar3Reject = [ ""; "b" ]

/// S -> x
/// S -> S + S
/// S -> S * S
/// S -> ( S )
let grammar6 =
    Grammar.parseGrammar
        """
S -> x
S -> S + S
S -> S * S
S -> ( S )
"""

/// E -> E + T
/// E -> T
/// T -> T * F
/// T -> F
/// F -> ( E )
/// F -> x
let grammar7 =
    Grammar.parseGrammar
        """
E -> E + T
E -> T
T -> T * F
T -> F
F -> ( E )
F -> x
"""

/// E -> T + E
/// E -> T
/// T -> F * T
/// T -> F
/// F -> ( E )
/// F -> x
let grammar8 =
    Grammar.parseGrammar
        """
E -> T + E
E -> T
T -> F * T
T -> F
F -> ( E )
F -> x
"""

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
    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    LRAutomaton.augmentGrammar freshStart g

let augGrammar1 = augmentStringGrammar grammar1

let augGrammar2 = augmentStringGrammar grammar2

let augGrammar3 = augmentStringGrammar grammar3

let augGrammar4 = augmentStringGrammar grammar4

let augGrammar5 = augmentStringGrammar grammar5

let augGrammar6 = augmentStringGrammar grammar6

let augGrammar7 = augmentStringGrammar grammar7

let augGrammar8 = augmentStringGrammar grammar8

/// S -> S1
/// S -> S2
/// S1 -> a b S c
/// S1 -> eps
/// S2 -> a x S y
/// S2 -> eps
let grammar9 =
    Grammar.parseGrammar
        """
    S -> S1
    S -> S2
    S1 -> a b S c
    S1 -> eps
    S2 -> a x S y
    S2 -> eps
    """

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

/// S -> S1
/// S -> S2
/// S1 -> a b S c
/// S -> eps
/// S2 -> a x S y
let grammar10 =
    Grammar.parseGrammar
        """
    S -> S1
    S -> S2
    S1 -> a b S c
    S -> eps
    S2 -> a x S y
    """

let grammar10Accept = grammar9Accept

let grammar10Reject = grammar9Reject

let augGrammar10 = augmentStringGrammar grammar10

/// S -> a a A
/// S -> a A
/// A -> a A
/// A -> eps
let grammar11 =
    Grammar.parseGrammar
        """
S -> a a A
S -> a A
A -> a A
A -> eps
"""

/// S -> a
/// S -> a a
/// S -> a a A
/// S -> a a a A
/// A -> a A
/// A -> eps
let grammar12 =
    Grammar.parseGrammar
        """
S -> a
S -> a a
S -> a a A
S -> a a a A
A -> a A
A -> eps
"""

/// S -> eps
/// S -> a a S
/// S -> a S
let grammar13 =
    Grammar.parseGrammar
        """
S -> eps
S -> a a S
S -> a S
"""

/// S -> a
/// S -> S S
/// S -> S S S
let grammar14 =
    Grammar.parseGrammar
        """
S -> a
S -> S S
S -> S S S
"""

/// S -> a
let grammarS2a =
    Grammar.parseGrammar
        """
S -> a
"""

/// S -> a b
let grammarAB =
    Grammar.parseGrammar
        """
S -> a b
"""

/// S -> a S
/// S -> b
let grammar_aS_b =
    Grammar.parseGrammar
        """
S -> a S
S -> b
"""

/// S -> a S b
/// S -> eps
let grammar_aSb_eps =
    Grammar.parseGrammar
        """
S -> a S b
S -> eps
"""

/// S -> A B
/// A -> a A
/// A -> eps
/// B -> b B
/// B -> eps
let grammarRightNullable =
    Grammar.parseGrammar
        """
S -> A B
A -> a A
A -> eps
B -> b B
B -> eps
"""

/// S -> A
/// A -> B
/// B -> eps
let grammarCascade =
    Grammar.parseGrammar
        """
S -> A
A -> B
B -> eps
"""

/// S -> S a S b
/// S -> eps
let grammarSaSb_eps =
    Grammar.parseGrammar
        """
S -> S a S b
S -> eps
"""

/// S -> eps
let grammarEps =
    Grammar.parseGrammar
        """
S -> eps
"""

/// S -> N
/// N -> eps
let grammarNtoEps =
    Grammar.parseGrammar
        """
S -> N
N -> eps
"""

/// S -> N N
/// N -> eps
let grammarNNtoEps =
    Grammar.parseGrammar
        """
S -> N N
N -> eps
"""

/// S -> N*
/// N -> eps
let grammarNStarEps =
    Grammar.parseGrammar
        """
S -> N*
N -> eps
"""

/// S -> S S
/// S -> eps
let grammarSSeps =
    Grammar.parseGrammar
        """
S -> S S
S -> eps
"""

/// S -> A B
/// A -> C D
/// B -> D C
/// D -> eps
/// C -> eps
let grammarChainEps =
    Grammar.parseGrammar
        """
S -> A B
A -> C D
B -> D C
D -> eps
C -> eps
"""

/// S -> A
/// S -> B
/// A -> C D
/// B -> D C
/// D -> eps
/// C -> eps
let grammarAltEps =
    Grammar.parseGrammar
        """
S -> A
S -> B
A -> C D
B -> D C
D -> eps
C -> eps
"""
