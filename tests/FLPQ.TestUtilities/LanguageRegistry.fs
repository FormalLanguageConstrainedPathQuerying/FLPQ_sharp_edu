namespace FLPQ.TestUtilities

open FsCheck
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb

/// Manually-verified grammar properties.
/// These are assertions about the grammar, not automatically detected.
[<Struct>]
type GrammarProperties =
    { HasLeftRecursion: bool
      HasDirectLeftRecursion: bool
      IsAmbiguous: bool
      HasEpsilon: bool
      IsInCnf: bool
      IsRsmDerived: bool }

/// A grammar annotated with its known properties.
/// Text is the canonical source; Grammar, AugmentedGrammar, and Rsm are derived from it.
type AnnotatedGrammar =
    { Name: string
      Text: string
      Grammar: Grammar<string, string>
      AugmentedGrammar: Grammar<string, string>
      Rsm: RSM<string, string>
      Properties: GrammarProperties
      Notes: string }

/// A formal language with its grammars, known accept/reject strings, and a generator.
type Language =
    { Name: string
      Description: string
      Grammars: AnnotatedGrammar list
      AcceptStrings: (Terminal<string> list) list
      RejectStrings: (Terminal<string> list) list
      GenString: Gen<string> }

module LanguageRegistry =

    let private augmentStringGrammar (g: Grammar<string, string>) =
        let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
        LRAutomaton.augmentGrammar freshStart g

    let private ebnfOperators = set [ "+"; "*"; "("; ")"; "|"; "?"; "eps" ]

    let private grammarToEbnfText (g: Grammar<string, string>) : string =
        g.Rules
        |> List.map (fun r ->
            let (Nonterminal nt) = r.Lhs

            let rhsStr =
                match r.Rhs with
                | Rhs.EpsilonRhs -> "eps"
                | Rhs.Symbols symbols ->
                    NonEmptyList.toList symbols
                    |> List.map (fun s ->
                        match s with
                        | Symbol.T(Terminal t) when Set.contains t ebnfOperators -> $"( {t} )"
                        | Symbol.T(Terminal t) -> t
                        | Symbol.N(Nonterminal nt') -> nt'
                        | Symbol.Epsilon -> "eps")
                    |> String.concat " "

            $"{nt} -> {rhsStr}")
        |> String.concat "\n"

    let private isEbnfText (text: string) =
        text.Contains('*')
        || text.Contains('+')
        || text.Contains('?')
        || text.Contains("| ")
        || text.Contains(" |")
        || (text.Contains('(') && not (text.Contains(" -> ")))

    let private parseGrammarSafe (text: string) =
        if isEbnfText text then
            None
        else
            try
                let g = Grammar.parseGrammar text

                let rsm =
                    RsmBuilder.buildRSMFromText (grammarToEbnfText g)
                    |> fun r -> { r with StartBlock = g.Start }

                Some(g, rsm)
            with _ ->
                None

    let private mkEntry (name: string) (text: string) (props: GrammarProperties) (notes: string) : AnnotatedGrammar =
        let grammar, rsm, isRsmDerived =
            match parseGrammarSafe text with
            | Some(g, r) -> (g, r, false)
            | None ->
                let rsm = RsmBuilder.buildRSMFromText text
                let grammar = RsmToGrammar.convert rsm
                (grammar, rsm, true)

        let augmented = augmentStringGrammar grammar

        let props =
            { props with
                IsRsmDerived = isRsmDerived }

        { Name = name
          Text = text
          Grammar = grammar
          AugmentedGrammar = augmented
          Rsm = rsm
          Properties = props
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
        let operators = [| "add"; "mult" |]

        let rec genExpr depth =
            if depth <= 0 then
                MyGen.elements terminals
            else
                MyGen.choose (0, 2)
                |> MyGen.bind (fun choice ->
                    match choice with
                    | 0 -> MyGen.elements terminals
                    | 1 -> genExpr (depth - 1) |> MyGen.map (fun inner -> "lbr " + inner + " rbr")
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
            mkEntry
                "grammar1"
                "S -> a S b S\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguous Dyck-1; LL(1)-compatible"

        let grammar2 =
            mkEntry
                "grammar2"
                "S -> a S b\nS -> eps\nS -> S S"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguous Dyck-1; S->SS creates direct left-recursion; not LL(1), not LR(k)"

        let grammarSaSbEps =
            mkEntry
                "grammarSaSb_eps"
                "S -> S a S b\nS -> eps"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "left-recursive Dyck-1; S->S a S b has direct left-recursion"

        let grammarDyckEbnf =
            mkEntry
                "grammar_dyck_ebnf"
                "S -> (a S b)*"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "EBNF regex (a S b)* generating Dyck-1; for RSM-based tests"

        { Name = "Dyck1 (balanced a/b)"
          Description =
            "All strings over {a,b} where every prefix has at least as many a's as b's, and total #a = total #b."
          Grammars = [ grammar1; grammar2; grammarSaSbEps; grammarDyckEbnf ]
          AcceptStrings =
            [ [ Terminal "a"; Terminal "b"; Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "b" ]
              []
              [ Terminal "a"; Terminal "a"; Terminal "b"; Terminal "b" ]
              [ Terminal "a"
                Terminal "a"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "b" ]
              [ Terminal "a"
                Terminal "a"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "b"
                Terminal "a"
                Terminal "b" ]
              [ Terminal "a"
                Terminal "a"
                Terminal "a"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "b" ] ]
          RejectStrings =
            [ [ Terminal "a"; Terminal "a" ]
              [ Terminal "b"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "b"; Terminal "a" ]
              [ Terminal "b" ]
              [ Terminal "a" ]
              [ Terminal "a"; Terminal "b"; Terminal "a"; Terminal "b"; Terminal "a" ] ]
          GenString = abStringGen }

    // ============================================================
    // APlus: a^+ (one or more a's)
    // ============================================================

    let APlus: Language =
        let grammar3 =
            mkEntry
                "grammar3"
                "S -> a S\nS -> a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "right-recursive a^+; LL(1)-compatible, SLR(1)-compatible"

        let grammar4 =
            mkEntry
                "grammar4"
                "S -> S a\nS -> a"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "left-recursive a^+; S->S a has direct left-recursion"

        let grammar5 =
            mkEntry
                "grammar5"
                "S -> S S\nS -> S S S\nS -> a"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguous left-recursive a^+"

        let grammar11 =
            mkEntry
                "grammar11"
                "S -> a a A\nS -> a A\nA -> a A\nA -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguous a^+ via nullable A"

        let grammar12 =
            mkEntry
                "grammar12"
                "S -> a\nS -> a a\nS -> a a A\nS -> a a a A\nA -> a A\nA -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguous a^+ with explicit short-string productions"

        let grammar14 =
            mkEntry
                "grammar14"
                "S -> a\nS -> S S\nS -> S S S"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguous left-recursive a^+"

        let grammarAplusEbnf1 =
            mkEntry
                "grammar_aplus_ebnf1"
                "S -> N a*\nN -> (a a) | a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "EBNF S -> N a*; N -> (a a) | a; generates a^+; for RSM-based tests"

        let grammarAplusEbnf2 =
            mkEntry
                "grammar_aplus_ebnf2"
                "S -> a* N\nN -> a | (a a)"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
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
            [ [ Terminal "a" ]
              [ Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "a"; Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "a"; Terminal "a"; Terminal "a"; Terminal "a" ] ]
          RejectStrings =
            [ []
              [ Terminal "b" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "a"; Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "a"; Terminal "a" ] ]
          GenString = aStringGen }

    // ============================================================
    // AStar: a* (zero or more a's)
    // ============================================================

    let AStar: Language =
        let grammar13 =
            mkEntry
                "grammar13"
                "S -> eps\nS -> a a S\nS -> a S"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguous a*; multiple derivations for a^n"

        let grammarASaA_eps =
            mkEntry
                "grammar_aSa_eps"
                "S -> a S a\nS -> a\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "a* via palindrome-like construction; ambiguous"

        let grammarAstarEbnf =
            mkEntry
                "grammar_astar_ebnf"
                "S -> N*\nN -> a | (a a)"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "EBNF S -> N*; N -> a | (a a); generates a*; for RSM-based tests"

        { Name = "a* (zero or more a's)"
          Description = "L = {a^n | n >= 0}"
          Grammars = [ grammar13; grammarASaA_eps; grammarAstarEbnf ]
          AcceptStrings =
            [ []
              [ Terminal "a" ]
              [ Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "a"; Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "a"; Terminal "a"; Terminal "a"; Terminal "a" ] ]
          RejectStrings =
            [ [ Terminal "b" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "a"; Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "a"; Terminal "a" ] ]
          GenString = aStringGen }

    // ============================================================
    // ArithExpr: arithmetic expressions with x, +, *, ()
    // ============================================================

    let ArithExpr: Language =
        let grammar6 =
            mkEntry
                "grammar6"
                "S -> x\nS -> S add S\nS -> S mult S\nS -> lbr S rbr"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguous expression grammar; no precedence/associativity; not LL, not LR"

        let grammar7 =
            mkEntry
                "grammar7"
                "E -> E add T\nE -> T\nT -> T mult F\nT -> F\nF -> lbr E rbr\nF -> x"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "unambiguous; left-associative add, mult with proper precedence; SLR(1)-compatible"

        let grammar8 =
            mkEntry
                "grammar8"
                "E -> T add E\nE -> T\nT -> F mult T\nT -> F\nF -> lbr E rbr\nF -> x"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "unambiguous; right-associative add, mult with proper precedence"

        { Name = "ArithExpr (arithmetic expressions)"
          Description = "Arithmetic expressions over {x, add, mult, lbr, rbr}."
          Grammars = [ grammar6; grammar7; grammar8 ]
          AcceptStrings =
            [ [ Terminal "x" ]
              [ Terminal "lbr"; Terminal "x"; Terminal "rbr" ]
              [ Terminal "lbr"; Terminal "x"; Terminal "rbr"; Terminal "mult"; Terminal "x" ]
              [ Terminal "x"; Terminal "add"; Terminal "x" ]
              [ Terminal "x"; Terminal "add"; Terminal "x"; Terminal "mult"; Terminal "x" ]
              [ Terminal "x"
                Terminal "mult"
                Terminal "lbr"
                Terminal "x"
                Terminal "add"
                Terminal "x"
                Terminal "rbr" ]
              [ Terminal "lbr"
                Terminal "x"
                Terminal "mult"
                Terminal "lbr"
                Terminal "x"
                Terminal "add"
                Terminal "x"
                Terminal "rbr"
                Terminal "rbr" ] ]
          RejectStrings =
            [ []
              [ Terminal "lbr"; Terminal "rbr" ]
              [ Terminal "add"; Terminal "x" ]
              [ Terminal "x"; Terminal "add" ]
              [ Terminal "x"; Terminal "add"; Terminal "lbr"; Terminal "rbr" ] ]
          GenString = exprStringGen }

    // ============================================================
    // TwoTrackDyck: ab--c and ax--y nesting
    // ============================================================

    let TwoTrackDyck: Language =
        let grammar9 =
            mkEntry
                "grammar9"
                "S -> S1\nS -> S2\nS1 -> a b S c\nS1 -> eps\nS2 -> a x S y\nS2 -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguous (empty via S1 or S2); two independent tracks"

        let grammar10 =
            mkEntry
                "grammar10"
                "S -> S1\nS -> S2\nS1 -> a b S c\nS -> eps\nS2 -> a x S y"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguous; same language as grammar9, different epsilon handling"

        { Name = "TwoTrackDyck (ab/c, ax/y)"
          Description = "Two independent Dyck-style tracks: one pairs a b with c, the other pairs a x with y."
          Grammars = [ grammar9; grammar10 ]
          AcceptStrings =
            [ []
              [ Terminal "a"; Terminal "b"; Terminal "c" ]
              [ Terminal "a"; Terminal "x"; Terminal "y" ]
              [ Terminal "a"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "c"
                Terminal "c" ]
              [ Terminal "a"
                Terminal "x"
                Terminal "a"
                Terminal "x"
                Terminal "y"
                Terminal "y" ]
              [ Terminal "a"
                Terminal "x"
                Terminal "a"
                Terminal "b"
                Terminal "c"
                Terminal "y" ]
              [ Terminal "a"
                Terminal "b"
                Terminal "a"
                Terminal "x"
                Terminal "y"
                Terminal "c" ] ]
          RejectStrings =
            [ [ Terminal "a" ]
              [ Terminal "x" ]
              [ Terminal "y" ]
              [ Terminal "c" ]
              [ Terminal "a"; Terminal "x"; Terminal "c" ]
              [ Terminal "a"; Terminal "b"; Terminal "y" ]
              [ Terminal "a"; Terminal "x"; Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "a"; Terminal "x"; Terminal "y" ]
              [ Terminal "a"; Terminal "x"; Terminal "a"; Terminal "b"; Terminal "c" ]
              [ Terminal "a"; Terminal "x"; Terminal "a"; Terminal "b"; Terminal "y" ] ]
          GenString = abcdxyStringGen }

    // ============================================================
    // ANB: a^n b (n >= 0)
    // ============================================================

    let ANB: Language =
        let grammarASB =
            mkEntry
                "grammar_aS_b"
                "S -> a S\nS -> b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "right-recursive a^n b; unambiguous"

        { Name = "a^n b"
          Description = "L = {a^n b | n >= 0}"
          Grammars = [ grammarASB ]
          AcceptStrings =
            [ [ Terminal "b" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "a"; Terminal "a"; Terminal "b" ] ]
          RejectStrings =
            [ []
              [ Terminal "a" ]
              [ Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "a"; Terminal "a" ]
              [ Terminal "b"; Terminal "a" ] ]
          GenString = abStringGen }

    // ============================================================
    // ANBN: a^n b^n (n >= 0)
    // ============================================================

    let ANBN: Language =
        let grammarASbEps =
            mkEntry
                "grammar_aSb_eps"
                "S -> a S b\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "classic a^n b^n; unambiguous; LL(1)-compatible"

        { Name = "a^n b^n"
          Description = "L = {a^n b^n | n >= 0}"
          Grammars = [ grammarASbEps ]
          AcceptStrings =
            [ []
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "a"; Terminal "b"; Terminal "b" ]
              [ Terminal "a"
                Terminal "a"
                Terminal "a"
                Terminal "b"
                Terminal "b"
                Terminal "b" ] ]
          RejectStrings =
            [ [ Terminal "a" ]
              [ Terminal "b" ]
              [ Terminal "a"; Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "a"; Terminal "b" ] ]
          GenString = abStringGen }

    // ============================================================
    // AStarBStar: a^m b^n (m,n >= 0)
    // ============================================================

    let AStarBStar: Language =
        let grammarRightNullable =
            mkEntry
                "grammarRightNullable"
                "S -> A B\nA -> a A\nA -> eps\nB -> b B\nB -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "a^m b^n; right-nullable A and B; unambiguous"

        { Name = "a^m b^n"
          Description = "L = {a^m b^n | m,n >= 0}"
          Grammars = [ grammarRightNullable ]
          AcceptStrings =
            [ []
              [ Terminal "a" ]
              [ Terminal "b" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "b" ] ]
          RejectStrings = [ [ Terminal "b"; Terminal "a" ]; [ Terminal "a"; Terminal "b"; Terminal "a" ] ]
          GenString = abStringGen }

    // ============================================================
    // SingleA: {a}
    // ============================================================

    let SingleA: Language =
        let grammarS2a =
            mkEntry
                "grammarS2a"
                "S -> a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "trivial single-terminal grammar"

        { Name = "SingleA ({a})"
          Description = "L = {a}"
          Grammars = [ grammarS2a ]
          AcceptStrings = [ [ Terminal "a" ] ]
          RejectStrings =
            [ []
              [ Terminal "b" ]
              [ Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "b" ] ]
          GenString = constantGen "a" }

    // ============================================================
    // SingleAB: {ab}
    // ============================================================

    let SingleAB: Language =
        let grammarAB =
            mkEntry
                "grammarAB"
                "S -> a b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "trivial two-terminal grammar"

        { Name = "SingleAB ({ab})"
          Description = "L = {ab}"
          Grammars = [ grammarAB ]
          AcceptStrings = [ [ Terminal "a"; Terminal "b" ] ]
          RejectStrings =
            [ []
              [ Terminal "a" ]
              [ Terminal "b" ]
              [ Terminal "b"; Terminal "a" ]
              [ Terminal "a"; Terminal "a"; Terminal "b" ] ]
          GenString = constantGen "a b" }

    // ============================================================
    // EpsilonOnly: {epsilon}
    // ============================================================

    let EpsilonOnly: Language =
        let grammarEps =
            mkEntry
                "grammarEps"
                "S -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = true
                  IsRsmDerived = false }
                "simplest epsilon grammar"

        let grammarNtoEps =
            mkEntry
                "grammarNtoEps"
                "S -> N\nN -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "epsilon via intermediate nonterminal"

        let grammarNNtoEps =
            mkEntry
                "grammarNNtoEps"
                "S -> N N\nN -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = true
                  IsRsmDerived = false }
                "epsilon via nullable binary production; CNF-compatible"

        let grammarNStarEps =
            mkEntry
                "grammarNStarEps"
                "S -> N*\nN -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "epsilon via Kleene star of nullable; uses EBNF"

        let grammarSSeps =
            mkEntry
                "grammarSSeps"
                "S -> S S\nS -> eps"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = true
                  IsRsmDerived = false }
                "epsilon via self-recursive binary production; ambiguous; CNF-compatible"

        let grammarChainEps =
            mkEntry
                "grammarChainEps"
                "S -> A B\nA -> C D\nB -> D C\nD -> eps\nC -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "epsilon via chain of nullable nonterminals"

        let grammarAltEps =
            mkEntry
                "grammarAltEps"
                "S -> A\nS -> B\nA -> C D\nB -> D C\nD -> eps\nC -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "ambiguously epsilon via alternative paths to nullable nonterminals"

        let grammarCascade =
            mkEntry
                "grammarCascade"
                "S -> A\nA -> B\nB -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
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
          RejectStrings =
            [ [ Terminal "a" ]
              [ Terminal "b" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "a" ]
              [ Terminal "b"; Terminal "b" ] ]
          GenString = constantGen "" }

    // ============================================================
    // LL2Test: {abc, aad} — tests LL(k) lookahead resolution
    // ============================================================

    let LL2Test: Language =
        let ll2Grammar =
            mkEntry
                "ll2Grammar"
                "S -> a b A\nS -> a a B\nA -> c\nB -> d"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "LL(2) grammar: a b A vs a a B requires k=2 lookahead; LL(1) has conflict"

        { Name = "LL2Test ({abc, aad})"
          Description = "L = {abc, aad}. Test grammar for LL(2) lookahead resolution."
          Grammars = [ ll2Grammar ]
          AcceptStrings =
            [ [ Terminal "a"; Terminal "b"; Terminal "c" ]
              [ Terminal "a"; Terminal "a"; Terminal "d" ] ]
          RejectStrings =
            [ []
              [ Terminal "a" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "b"; Terminal "d" ]
              [ Terminal "a"; Terminal "a"; Terminal "c" ] ]
          GenString = MyGen.elements [ "a b c"; "a a d" ] }

    // ============================================================
    // LL3Test: {abcx, abdy} — tests LL(k) lookahead resolution
    // ============================================================

    let LL3Test: Language =
        let ll3Grammar =
            mkEntry
                "ll3Grammar"
                "S -> a b c A\nS -> a b d B\nA -> x\nB -> y"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "LL(3) grammar: a b c A vs a b d B requires k=3 lookahead; LL(2) has conflict"

        { Name = "LL3Test ({abcx, abdy})"
          Description = "L = {abcx, abdy}. Test grammar for LL(3) lookahead resolution."
          Grammars = [ ll3Grammar ]
          AcceptStrings =
            [ [ Terminal "a"; Terminal "b"; Terminal "c"; Terminal "x" ]
              [ Terminal "a"; Terminal "b"; Terminal "d"; Terminal "y" ] ]
          RejectStrings =
            [ []
              [ Terminal "a" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "c" ]
              [ Terminal "a"; Terminal "b"; Terminal "d" ]
              [ Terminal "a"; Terminal "b"; Terminal "c"; Terminal "y" ]
              [ Terminal "a"; Terminal "b"; Terminal "d"; Terminal "x" ] ]
          GenString = MyGen.elements [ "a b c x"; "a b d y" ] }

    // ============================================================
    // AltAB: {a, b}
    // ============================================================

    let AltAB: Language =
        let grammarAltAB =
            mkEntry
                "grammar_alt_ab"
                "S -> a\nS -> b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "alternation: S -> a | b"

        { Name = "AltAB ({a, b})"
          Description = "L = {a, b}. Two-alternative terminal language."
          Grammars = [ grammarAltAB ]
          AcceptStrings = [ [ Terminal "a" ]; [ Terminal "b" ] ]
          RejectStrings =
            [ []
              [ Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "b"; Terminal "a" ] ]
          GenString = MyGen.elements [ "a"; "b" ] }

    // ============================================================
    // DualDyck: (a^n b^n)(c^m d^m)
    // ============================================================

    let DualDyck: Language =
        let dualDyckEbnf =
            mkEntry
                "grammar_dual_dyck"
                "S -> S1 S2\nS1 -> (a S1 b)*\nS2 -> (c S2 d)*"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false }
                "EBNF S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)*; for multi-block RSM tests"

        { Name = "DualDyck ((a^n b^n)(c^m d^m))"
          Description = "Concatenation of two independent Dyck languages: a-b blocks then c-d blocks."
          Grammars = [ dualDyckEbnf ]
          AcceptStrings =
            [ [ Terminal "a"
                Terminal "a"
                Terminal "a"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "b" ]
              [ Terminal "a"
                Terminal "a"
                Terminal "a"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "b"
                Terminal "c"
                Terminal "c"
                Terminal "d"
                Terminal "c"
                Terminal "d"
                Terminal "d" ]
              [ Terminal "a"
                Terminal "a"
                Terminal "a"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "b"
                Terminal "a"
                Terminal "b"
                Terminal "b"
                Terminal "c"
                Terminal "d" ] ]
          RejectStrings = []
          GenString = MyGen.constant "" }

    let private opExprStringGen: Gen<string> =
        let terminals = [ "x"; "x op_plus x"; "x op_mul x"; "x op_plus x op_mul x" ]
        MyGen.elements terminals

    let OpExpr: Language =
        let grammarOpExpr =
            mkEntry
                "grammarOpExpr"
                "E -> T op_plus E\nE -> T\nT -> F op_mul T\nT -> F\nF -> x"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false }
                "expression grammar with op_plus/op_mul operators; for RSM round-trip testing"

        { Name = "OpExpr (binary operators x, op_plus, op_mul)"
          Description = "Binary expressions over {x, op_plus, op_mul}."
          Grammars = [ grammarOpExpr ]
          AcceptStrings =
            [ [ Terminal "x" ]
              [ Terminal "x"; Terminal "op_plus"; Terminal "x" ]
              [ Terminal "x"; Terminal "op_mul"; Terminal "x" ]
              [ Terminal "x"
                Terminal "op_plus"
                Terminal "x"
                Terminal "op_mul"
                Terminal "x" ]
              [ Terminal "x"
                Terminal "op_mul"
                Terminal "x"
                Terminal "op_plus"
                Terminal "x" ]
              [ Terminal "x"
                Terminal "op_plus"
                Terminal "x"
                Terminal "op_plus"
                Terminal "x"
                Terminal "op_mul"
                Terminal "x" ] ]
          RejectStrings =
            [ []
              [ Terminal "op_plus" ]
              [ Terminal "op_mul" ]
              [ Terminal "op_plus"; Terminal "x" ]
              [ Terminal "x"; Terminal "op_plus" ]
              [ Terminal "x"; Terminal "x" ]
              [ Terminal "x"; Terminal "op_plus"; Terminal "op_mul"; Terminal "x" ] ]
          GenString = opExprStringGen }

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
          DualDyck
          OpExpr ]

    /// Look up a grammar by name within a language.
    let findGrammar (lang: Language) (name: string) : AnnotatedGrammar =
        lang.Grammars |> List.find (fun g -> g.Name = name)

/// Bridges from LanguageRegistry Gen<string> to FsCheck Arbitrary<string>.
module GenToArbitrary =

    type AbString() =
        static member AbString() : Arbitrary<string> =
            LanguageRegistry.Dyck1.GenString |> MyArb.fromGen

    type AString() =
        static member AString() : Arbitrary<string> =
            LanguageRegistry.APlus.GenString |> MyArb.fromGen

    type ExprString() =
        static member ExprString() : Arbitrary<string> =
            LanguageRegistry.ArithExpr.GenString |> MyArb.fromGen

    type AbcdxyString() =
        static member AbcdxyString() : Arbitrary<string> =
            LanguageRegistry.TwoTrackDyck.GenString |> MyArb.fromGen

    type AbcxdString() =
        static member AbcxdString() : Arbitrary<string> =
            let chars = [ "a"; "b"; "c"; "x"; "d" ]

            MyGen.choose (0, 8)
            |> MyGen.bind (fun len -> MyGen.listOfLength len (MyGen.elements chars) |> MyGen.map (String.concat " "))
            |> MyArb.fromGen

    type OpExprString() =
        static member OpExprString() : Arbitrary<string> =
            LanguageRegistry.OpExpr.GenString |> MyArb.fromGen

    type PolyAlphabetString() =
        static member PolyAlphabetString() : Arbitrary<string> =
            let chars = [ "a"; "b"; "c"; "d"; "x"; "y" ]

            MyGen.choose (0, 8)
            |> MyGen.bind (fun len -> MyGen.listOfLength len (MyGen.elements chars) |> MyGen.map (String.concat " "))
            |> MyArb.fromGen
