namespace FLPQ.TestUtilities

open FsCheck
open FLPQ.Languages
open FLPQ.LinearAlgebra

module MyGen = FsCheck.FSharp.Gen

/// Manually-verified grammar properties.
/// These are assertions about the grammar, not automatically detected.
[<Struct>]
type GrammarProperties =
    { HasLeftRecursion: bool
      HasDirectLeftRecursion: bool
      IsAmbiguous: bool
      HasEpsilon: bool
      IsInCnf: bool
      IsEbnf: bool }

/// A grammar annotated with its known properties.
type AnnotatedGrammar =
    { Name: string
      Grammar: Grammar<string, string>
      Augmented: Grammar<string, string>
      Properties: GrammarProperties
      EbnfText: string
      Notes: string }

/// A formal language with its grammars, known accept/reject strings, and a generator.
type Language =
    { Name: string
      Description: string
      Grammars: AnnotatedGrammar list
      AcceptStrings: (string list) list
      RejectStrings: (string list) list
      GenString: Gen<string> }

module LanguageRegistry =

    let private parseG s = Grammar.parseGrammar s

    let private augmentStringGrammar (g: Grammar<string, string>) =
        let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
        LRAutomaton.augmentGrammar freshStart g

    let private annotate (name: string) (ebnf: string) (props: GrammarProperties) (notes: string) : AnnotatedGrammar =
        let g = parseG ebnf
        let aug = augmentStringGrammar g
        let props = { props with IsEbnf = false }

        { Name = name
          Grammar = g
          Augmented = aug
          Properties = props
          EbnfText = ebnf
          Notes = notes }

    let private annotateEbnf
        (name: string)
        (ebnf: string)
        (props: GrammarProperties)
        (notes: string)
        : AnnotatedGrammar =
        let g = parseG "S -> a"
        let aug = augmentStringGrammar g
        let props = { props with IsEbnf = true }

        { Name = name
          Grammar = g
          Augmented = aug
          Properties = props
          EbnfText = ebnf
          Notes = notes }

    let private abStringGen: Gen<string> =
        MyGen.choose (0, 12)
        |> MyGen.bind (fun len ->
            MyGen.choose (0, 1)
            |> MyGen.listOfLength len
            |> MyGen.map (fun bits -> bits |> List.map (fun b -> if b = 0 then "a" else "b") |> String.concat " "))

    let private aStringGen: Gen<string> =
        MyGen.choose (0, 15)
        |> MyGen.map (fun len ->
            if len = 0 then
                ""
            else
                System.String.Concat(Array.replicate len "a": string array).Trim())

    let private exprStringGen: Gen<string> =
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

        MyGen.choose (0, 4) |> MyGen.bind genExpr

    let private abcdxyStringGen: Gen<string> =
        let chars = [ "a"; "b"; "c"; "d"; "x"; "y" ]

        MyGen.choose (0, 8)
        |> MyGen.bind (fun len -> MyGen.listOfLength len (MyGen.elements chars) |> MyGen.map (String.concat " "))

    let private constantGen (s: string) : Gen<string> = MyGen.constant s

    // ============================================================
    // Dyck1: balanced a/b with interleaving
    // ============================================================

    let Dyck1: Language =
        let grammar1 =
            annotate
                "grammar1"
                "S -> a S b S\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguous Dyck-1; LL(1)-compatible"

        let grammar2 =
            annotate
                "grammar2"
                "S -> a S b\nS -> eps\nS -> S S"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguous Dyck-1; S->SS creates direct left-recursion; not LL(1), not LR(k)"

        let grammarSaSbEps =
            annotate
                "grammarSaSb_eps"
                "S -> S a S b\nS -> eps"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "left-recursive Dyck-1; S->S a S b has direct left-recursion"

        let grammarDyckEbnf =
            annotateEbnf
                "grammar_dyck_ebnf"
                "(a S b)*"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "EBNF regex (a S b)* generating Dyck-1; for RSM-based tests"

        { Name = "Dyck1 (balanced a/b)"
          Description =
            "All strings over {a,b} where every prefix has at least as many a's as b's, and total #a = total #b."
          Grammars = [ grammar1; grammar2; grammarSaSbEps; grammarDyckEbnf ]
          AcceptStrings =
            [ [ "a"; "b"; "a"; "b" ]
              [ "a"; "b" ]
              []
              [ "a"; "a"; "b"; "b" ]
              [ "a"; "a"; "b"; "a"; "b"; "b" ]
              [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]
              [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ] ]
          RejectStrings =
            [ [ "a"; "a" ]
              [ "b"; "b" ]
              [ "a"; "b"; "b" ]
              [ "a"; "b"; "b"; "a" ]
              [ "b" ]
              [ "a" ]
              [ "a"; "b"; "a"; "b"; "a" ] ]
          GenString = abStringGen }

    // ============================================================
    // APlus: a^+ (one or more a's)
    // ============================================================

    let APlus: Language =
        let grammar3 =
            annotate
                "grammar3"
                "S -> a S\nS -> a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "right-recursive a^+; LL(1)-compatible, SLR(1)-compatible"

        let grammar4 =
            annotate
                "grammar4"
                "S -> S a\nS -> a"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "left-recursive a^+; S->S a has direct left-recursion"

        let grammar5 =
            annotate
                "grammar5"
                "S -> S S\nS -> S S S\nS -> a"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguous left-recursive a^+"

        let grammar11 =
            annotate
                "grammar11"
                "S -> a a A\nS -> a A\nA -> a A\nA -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguous a^+ via nullable A"

        let grammar12 =
            annotate
                "grammar12"
                "S -> a\nS -> a a\nS -> a a A\nS -> a a a A\nA -> a A\nA -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguous a^+ with explicit short-string productions"

        let grammar14 =
            annotate
                "grammar14"
                "S -> a\nS -> S S\nS -> S S S"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguous left-recursive a^+"

        let grammarAplusEbnf1 =
            annotateEbnf
                "grammar_aplus_ebnf1"
                "S -> N a*\nN -> (a a) | a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "EBNF S -> N a*; N -> (a a) | a; generates a^+; for RSM-based tests"

        let grammarAplusEbnf2 =
            annotateEbnf
                "grammar_aplus_ebnf2"
                "S -> a* N\nN -> a | (a a)"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "EBNF S -> a* N; N -> a | (a a); generates a^+; for RSM-based tests"

        { Name = "a^+ (one or more a's)"
          Description = "L = {a^n | n >= 1}"
          Grammars =
            [ grammar3
              grammar4
              grammar5
              grammar11
              grammar12
              grammar14
              grammarAplusEbnf1
              grammarAplusEbnf2 ]
          AcceptStrings =
            [ [ "a" ]
              [ "a"; "a" ]
              [ "a"; "a"; "a" ]
              [ "a"; "a"; "a"; "a" ]
              [ "a"; "a"; "a"; "a"; "a" ] ]
          RejectStrings =
            [ []
              [ "b" ]
              [ "a"; "b" ]
              [ "a"; "a"; "b" ]
              [ "a"; "a"; "a"; "b" ]
              [ "a"; "b"; "a"; "a" ] ]
          GenString = aStringGen }

    // ============================================================
    // AStar: a* (zero or more a's)
    // ============================================================

    let AStar: Language =
        let grammar13 =
            annotate
                "grammar13"
                "S -> eps\nS -> a a S\nS -> a S"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguous a*; multiple derivations for a^n"

        let grammarASaA_eps =
            annotate
                "grammar_aSa_eps"
                "S -> a S a\nS -> a\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "a* via palindrome-like construction; ambiguous"

        let grammarAstarEbnf =
            annotateEbnf
                "grammar_astar_ebnf"
                "S -> N*\nN -> a | (a a)"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "EBNF S -> N*; N -> a | (a a); generates a*; for RSM-based tests"

        { Name = "a* (zero or more a's)"
          Description = "L = {a^n | n >= 0}"
          Grammars = [ grammar13; grammarASaA_eps; grammarAstarEbnf ]
          AcceptStrings =
            [ []
              [ "a" ]
              [ "a"; "a" ]
              [ "a"; "a"; "a" ]
              [ "a"; "a"; "a"; "a" ]
              [ "a"; "a"; "a"; "a"; "a" ] ]
          RejectStrings =
            [ [ "b" ]
              [ "a"; "b" ]
              [ "a"; "a"; "b" ]
              [ "a"; "a"; "a"; "b" ]
              [ "a"; "b"; "a"; "a" ] ]
          GenString = aStringGen }

    // ============================================================
    // ArithExpr: arithmetic expressions with x, +, *, ()
    // ============================================================

    let ArithExpr: Language =
        let grammar6 =
            annotate
                "grammar6"
                "S -> x\nS -> S + S\nS -> S * S\nS -> ( S )"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguous expression grammar; no precedence/associativity; not LL, not LR"

        let grammar7 =
            annotate
                "grammar7"
                "E -> E + T\nE -> T\nT -> T * F\nT -> F\nF -> ( E )\nF -> x"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "unambiguous; left-associative +, * with proper precedence; SLR(1)-compatible"

        let grammar8 =
            annotate
                "grammar8"
                "E -> T + E\nE -> T\nT -> F * T\nT -> F\nF -> ( E )\nF -> x"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "unambiguous; right-associative +, * with proper precedence"

        { Name = "ArithExpr (arithmetic expressions)"
          Description = "Arithmetic expressions over {x, +, *, (, )}."
          Grammars = [ grammar6; grammar7; grammar8 ]
          AcceptStrings =
            [ [ "x" ]
              [ "("; "x"; ")" ]
              [ "("; "x"; ")"; "*"; "x" ]
              [ "x"; "+"; "x" ]
              [ "x"; "+"; "x"; "*"; "x" ]
              [ "x"; "*"; "("; "x"; "+"; "x"; ")" ]
              [ "("; "x"; "*"; "("; "x"; "+"; "x"; ")"; ")" ] ]
          RejectStrings = [ []; [ "("; ")" ]; [ "+"; "x" ]; [ "x"; "+" ]; [ "x"; "+"; "("; ")" ] ]
          GenString = exprStringGen }

    // ============================================================
    // TwoTrackDyck: ab--c and ax--y nesting
    // ============================================================

    let TwoTrackDyck: Language =
        let grammar9 =
            annotate
                "grammar9"
                "S -> S1\nS -> S2\nS1 -> a b S c\nS1 -> eps\nS2 -> a x S y\nS2 -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguous (empty via S1 or S2); two independent tracks"

        let grammar10 =
            annotate
                "grammar10"
                "S -> S1\nS -> S2\nS1 -> a b S c\nS -> eps\nS2 -> a x S y"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguous; same language as grammar9, different epsilon handling"

        { Name = "TwoTrackDyck (ab/c, ax/y)"
          Description = "Two independent Dyck-style tracks: one pairs a b with c, the other pairs a x with y."
          Grammars = [ grammar9; grammar10 ]
          AcceptStrings =
            [ []
              [ "a"; "b"; "c" ]
              [ "a"; "x"; "y" ]
              [ "a"; "b"; "a"; "b"; "c"; "c" ]
              [ "a"; "x"; "a"; "x"; "y"; "y" ]
              [ "a"; "x"; "a"; "b"; "c"; "y" ]
              [ "a"; "b"; "a"; "x"; "y"; "c" ] ]
          RejectStrings =
            [ [ "a" ]
              [ "x" ]
              [ "y" ]
              [ "c" ]
              [ "a"; "x"; "c" ]
              [ "a"; "b"; "y" ]
              [ "a"; "x"; "a"; "b" ]
              [ "a"; "b"; "a"; "x"; "y" ]
              [ "a"; "x"; "a"; "b"; "c" ]
              [ "a"; "x"; "a"; "b"; "y" ] ]
          GenString = abcdxyStringGen }

    // ============================================================
    // ANB: a^n b (n >= 0)
    // ============================================================

    let ANB: Language =
        let grammarASB =
            annotate
                "grammar_aS_b"
                "S -> a S\nS -> b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "right-recursive a^n b; unambiguous"

        { Name = "a^n b"
          Description = "L = {a^n b | n >= 0}"
          Grammars = [ grammarASB ]
          AcceptStrings = [ [ "b" ]; [ "a"; "b" ]; [ "a"; "a"; "b" ]; [ "a"; "a"; "a"; "b" ] ]
          RejectStrings = [ []; [ "a" ]; [ "a"; "a" ]; [ "a"; "a"; "a" ]; [ "b"; "a" ] ]
          GenString = abStringGen }

    // ============================================================
    // ANBN: a^n b^n (n >= 0)
    // ============================================================

    let ANBN: Language =
        let grammarASbEps =
            annotate
                "grammar_aSb_eps"
                "S -> a S b\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "classic a^n b^n; unambiguous; LL(1)-compatible"

        { Name = "a^n b^n"
          Description = "L = {a^n b^n | n >= 0}"
          Grammars = [ grammarASbEps ]
          AcceptStrings = [ []; [ "a"; "b" ]; [ "a"; "a"; "b"; "b" ]; [ "a"; "a"; "a"; "b"; "b"; "b" ] ]
          RejectStrings =
            [ [ "a" ]
              [ "b" ]
              [ "a"; "a"; "b" ]
              [ "a"; "b"; "b" ]
              [ "a"; "b"; "a"; "b" ] ]
          GenString = abStringGen }

    // ============================================================
    // AStarBStar: a^m b^n (m,n >= 0)
    // ============================================================

    let AStarBStar: Language =
        let grammarRightNullable =
            annotate
                "grammarRightNullable"
                "S -> A B\nA -> a A\nA -> eps\nB -> b B\nB -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "a^m b^n; right-nullable A and B; unambiguous"

        { Name = "a^m b^n"
          Description = "L = {a^m b^n | m,n >= 0}"
          Grammars = [ grammarRightNullable ]
          AcceptStrings = [ []; [ "a" ]; [ "b" ]; [ "a"; "b" ]; [ "a"; "a"; "b" ]; [ "a"; "b"; "b" ] ]
          RejectStrings = [ [ "b"; "a" ]; [ "a"; "b"; "a" ] ]
          GenString = abStringGen }

    // ============================================================
    // SingleA: {a}
    // ============================================================

    let SingleA: Language =
        let grammarS2a =
            annotate
                "grammarS2a"
                "S -> a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "trivial single-terminal grammar"

        { Name = "SingleA ({a})"
          Description = "L = {a}"
          Grammars = [ grammarS2a ]
          AcceptStrings = [ [ "a" ] ]
          RejectStrings = [ []; [ "b" ]; [ "a"; "a" ]; [ "a"; "b" ] ]
          GenString = constantGen "a" }

    // ============================================================
    // SingleAB: {ab}
    // ============================================================

    let SingleAB: Language =
        let grammarAB =
            annotate
                "grammarAB"
                "S -> a b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "trivial two-terminal grammar"

        { Name = "SingleAB ({ab})"
          Description = "L = {ab}"
          Grammars = [ grammarAB ]
          AcceptStrings = [ [ "a"; "b" ] ]
          RejectStrings = [ []; [ "a" ]; [ "b" ]; [ "b"; "a" ]; [ "a"; "a"; "b" ] ]
          GenString = constantGen "a b" }

    // ============================================================
    // EpsilonOnly: {epsilon}
    // ============================================================

    let EpsilonOnly: Language =
        let grammarEps =
            annotate
                "grammarEps"
                "S -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = true
                  IsEbnf = false }
                "simplest epsilon grammar"

        let grammarNtoEps =
            annotate
                "grammarNtoEps"
                "S -> N\nN -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "epsilon via intermediate nonterminal"

        let grammarNNtoEps =
            annotate
                "grammarNNtoEps"
                "S -> N N\nN -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = true
                  IsEbnf = false }
                "epsilon via nullable binary production; CNF-compatible"

        let grammarNStarEps =
            annotate
                "grammarNStarEps"
                "S -> N*\nN -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "epsilon via Kleene star of nullable; uses EBNF"

        let grammarSSeps =
            annotate
                "grammarSSeps"
                "S -> S S\nS -> eps"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = true
                  IsEbnf = false }
                "epsilon via self-recursive binary production; ambiguous; CNF-compatible"

        let grammarChainEps =
            annotate
                "grammarChainEps"
                "S -> A B\nA -> C D\nB -> D C\nD -> eps\nC -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "epsilon via chain of nullable nonterminals"

        let grammarAltEps =
            annotate
                "grammarAltEps"
                "S -> A\nS -> B\nA -> C D\nB -> D C\nD -> eps\nC -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "ambiguously epsilon via alternative paths to nullable nonterminals"

        let grammarCascade =
            annotate
                "grammarCascade"
                "S -> A\nA -> B\nB -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "epsilon via cascade of unit productions"

        { Name = "EpsilonOnly ({epsilon})"
          Description = "L = {epsilon}. The empty-string-only language, expressed via various grammar constructions."
          Grammars =
            [ grammarEps
              grammarNtoEps
              grammarNNtoEps
              grammarNStarEps
              grammarSSeps
              grammarChainEps
              grammarAltEps
              grammarCascade ]
          AcceptStrings = [ [] ]
          RejectStrings = [ [ "a" ]; [ "b" ]; [ "a"; "b" ]; [ "a"; "a" ]; [ "b"; "b" ] ]
          GenString = constantGen "" }

    // ============================================================
    // LL2Test: {abc, aad} — tests LL(k) lookahead resolution
    // ============================================================

    let LL2Test: Language =
        let ll2Grammar =
            annotate
                "ll2Grammar"
                "S -> a b A\nS -> a a B\nA -> c\nB -> d"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "LL(2) grammar: a b A vs a a B requires k=2 lookahead; LL(1) has conflict"

        { Name = "LL2Test ({abc, aad})"
          Description = "L = {abc, aad}. Test grammar for LL(2) lookahead resolution."
          Grammars = [ ll2Grammar ]
          AcceptStrings = [ [ "a"; "b"; "c" ]; [ "a"; "a"; "d" ] ]
          RejectStrings =
            [ []
              [ "a" ]
              [ "a"; "b" ]
              [ "a"; "a" ]
              [ "a"; "b"; "d" ]
              [ "a"; "a"; "c" ] ]
          GenString = MyGen.elements [ "a b c"; "a a d" ] }

    // ============================================================
    // LL3Test: {abcx, abdy} — tests LL(k) lookahead resolution
    // ============================================================

    let LL3Test: Language =
        let ll3Grammar =
            annotate
                "ll3Grammar"
                "S -> a b c A\nS -> a b d B\nA -> x\nB -> y"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "LL(3) grammar: a b c A vs a b d B requires k=3 lookahead; LL(2) has conflict"

        { Name = "LL3Test ({abcx, abdy})"
          Description = "L = {abcx, abdy}. Test grammar for LL(3) lookahead resolution."
          Grammars = [ ll3Grammar ]
          AcceptStrings = [ [ "a"; "b"; "c"; "x" ]; [ "a"; "b"; "d"; "y" ] ]
          RejectStrings =
            [ []
              [ "a" ]
              [ "a"; "b" ]
              [ "a"; "b"; "c" ]
              [ "a"; "b"; "d" ]
              [ "a"; "b"; "c"; "y" ]
              [ "a"; "b"; "d"; "x" ] ]
          GenString = MyGen.elements [ "a b c x"; "a b d y" ] }

    // ============================================================
    // AltAB: {a, b}
    // ============================================================

    let AltAB: Language =
        let grammarAltAB =
            annotate
                "grammar_alt_ab"
                "S -> a\nS -> b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsEbnf = false }
                "alternation: S -> a | b"

        { Name = "AltAB ({a, b})"
          Description = "L = {a, b}. Two-alternative terminal language."
          Grammars = [ grammarAltAB ]
          AcceptStrings = [ [ "a" ]; [ "b" ] ]
          RejectStrings = [ []; [ "a"; "a" ]; [ "a"; "b" ]; [ "b"; "a" ] ]
          GenString = MyGen.elements [ "a"; "b" ] }

    // ============================================================
    // DualDyck: (a^n b^n)(c^m d^m)
    // ============================================================

    let DualDyck: Language =
        let dualDyckEbnf =
            annotateEbnf
                "grammar_dual_dyck"
                "S -> S1 S2\nS1 -> (a S1 b)*\nS2 -> (c S2 d)*"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsEbnf = false }
                "EBNF S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)*; for multi-block RSM tests"

        { Name = "DualDyck ((a^n b^n)(c^m d^m))"
          Description = "Concatenation of two independent Dyck languages: a-b blocks then c-d blocks."
          Grammars = [ dualDyckEbnf ]
          AcceptStrings =
            [ [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]
              [ "a"
                "a"
                "a"
                "b"
                "a"
                "b"
                "b"
                "a"
                "b"
                "b"
                "c"
                "c"
                "d"
                "c"
                "d"
                "d" ]
              [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b"; "c"; "d" ] ]
          RejectStrings = []
          GenString = MyGen.constant "" }

    /// All languages in the registry.
    let allLanguages: Language list =
        [ Dyck1
          APlus
          AStar
          ArithExpr
          TwoTrackDyck
          ANB
          ANBN
          AStarBStar
          AltAB
          SingleA
          SingleAB
          EpsilonOnly
          LL2Test
          LL3Test
          DualDyck ]
