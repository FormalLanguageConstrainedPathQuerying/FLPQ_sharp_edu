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
      IsRsmDerived: bool
      DoesNotCoverFullLanguage: bool }

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
                        | Symbol.T(Terminal t) -> t
                        | Symbol.N(Nonterminal nt') -> nt'
                        | Symbol.Epsilon -> "eps")
                    |> String.concat " "

            $"{nt} -> {rhsStr}")
        |> String.concat "\n"

    let private isRegexPureConcat (r: Regexp<string, string>) : bool =
        let rec check (r: Regexp<string, string>) =
            match r with
            | RTerm _
            | RNonterm _
            | REps
            | REmpty -> true
            | RSeq(l, r) -> check l && check r
            | RAlt _
            | RStar _ -> false

        check r

    let private isEbnfText (text: string) : bool =
        let hasEbnfChars =
            text.Contains('+')
            || text.Contains('*')
            || text.Contains('?')
            || text.Contains("| ")
            || text.Contains(" |")
            || (text.Contains('(') && not (text.Contains("->")))

        if not hasEbnfChars then
            false
        else
            try
                let rules = EbnfParser.parseEbnf text
                rules |> List.exists (fun (_, regex) -> not (isRegexPureConcat regex))
            with _ ->
                true

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
        let ambiguousEps =
            mkEntry
                "ambiguousEps"
                "S -> a S b S\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous Dyck-1; LL(1)-compatible"

        let ambiguousWithConcat =
            mkEntry
                "ambiguousWithConcat"
                "S -> a S b\nS -> eps\nS -> S S"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous Dyck-1; S->SS creates direct left-recursion; not LL(1), not LR(k)"

        let leftRecursiveEps =
            mkEntry
                "leftRecursiveEps"
                "S -> S a S b\nS -> eps"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "left-recursive Dyck-1; S->S a S b has direct left-recursion"

        let ebnfStar =
            mkEntry
                "ebnfStar"
                "S -> (a S b)*"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "EBNF regex (a S b)* generating Dyck-1; for RSM-based tests"

        let singleRuleNoEps =
            mkEntry
                "singleRuleNoEps"
                "S -> a S b S"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = true }
                "single-rule Dyck-like grammar (no epsilon); used in parseGrammar single-rule test"

        { Name = "Dyck1 (balanced a/b)"
          Description =
            "All strings over {a,b} where every prefix has at least as many a's as b's, and total #a = total #b."
          Grammars =
            [ ambiguousEps
              ambiguousWithConcat
              leftRecursiveEps
              ebnfStar
              singleRuleNoEps ]
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
        let rightRecursive =
            mkEntry
                "rightRecursive"
                "S -> a S\nS -> a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "right-recursive a^+; LL(1)-compatible, SLR(1)-compatible"

        let leftRecursive =
            mkEntry
                "leftRecursive"
                "S -> S a\nS -> a"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "left-recursive a^+; S->S a has direct left-recursion"

        let ambiguousBinaryTernary =
            mkEntry
                "ambiguousBinaryTernary"
                "S -> S S\nS -> S S S\nS -> a"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous left-recursive a^+"

        let viaNullableA =
            mkEntry
                "viaNullableA"
                "S -> a a A\nS -> a A\nA -> a A\nA -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous a^+ via nullable A"

        let explicitVariants =
            mkEntry
                "explicitVariants"
                "S -> a\nS -> a a\nS -> a a A\nS -> a a a A\nA -> a A\nA -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous a^+ with explicit short-string productions"

        let ambiguousWithSingleRule =
            mkEntry
                "ambiguousWithSingleRule"
                "S -> a\nS -> S S\nS -> S S S"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous left-recursive a^+"

        let ebnfNaStar =
            mkEntry
                "ebnfNaStar"
                "S -> N a*\nN -> (a a) | a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "EBNF S -> N a*; N -> (a a) | a; generates a^+; for RSM-based tests"

        let ebnfAStarN =
            mkEntry
                "ebnfAStarN"
                "S -> a* N\nN -> a | (a a)"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "EBNF S -> a* N; N -> a | (a a); generates a^+; for RSM-based tests"

        { Name = "a^+ (one or more a's)"
          Description = "L = {a^n | n >= 1}"
          Grammars =
            [ rightRecursive
              leftRecursive
              ambiguousBinaryTernary
              viaNullableA
              explicitVariants
              ambiguousWithSingleRule
              ebnfNaStar
              ebnfAStarN ]
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
        let ambiguous =
            mkEntry
                "ambiguous"
                "S -> eps\nS -> a a S\nS -> a S"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous a*; multiple derivations for a^n"

        let palindromeLike =
            mkEntry
                "palindromeLike"
                "S -> a S a\nS -> a\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "a* via palindrome-like construction; ambiguous"

        let ebnfNStar =
            mkEntry
                "ebnfNStar"
                "S -> N*\nN -> a | (a a)"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "EBNF S -> N*; N -> a | (a a); generates a*; for RSM-based tests"

        let rightRecursiveWithEps =
            mkEntry
                "rightRecursiveWithEps"
                "S -> a S\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "right-recursive grammar with epsilon; used in toCnf tests"

        { Name = "a* (zero or more a's)"
          Description = "L = {a^n | n >= 0}"
          Grammars = [ ambiguous; palindromeLike; ebnfNStar; rightRecursiveWithEps ]
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
        let ambiguous =
            mkEntry
                "ambiguous"
                "S -> x\nS -> S add S\nS -> S mult S\nS -> lbr S rbr"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous expression grammar; no precedence/associativity; not LL, not LR"

        let leftAssoc =
            mkEntry
                "leftAssoc"
                "E -> E add T\nE -> T\nT -> T mult F\nT -> F\nF -> lbr E rbr\nF -> x"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "unambiguous; left-associative add, mult with proper precedence; SLR(1)-compatible"

        let rightAssoc =
            mkEntry
                "rightAssoc"
                "E -> T add E\nE -> T\nT -> F mult T\nT -> F\nF -> lbr E rbr\nF -> x"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "unambiguous; right-associative add, mult with proper precedence"

        let simplified =
            mkEntry
                "simplified"
                "E -> E add T\nE -> T\nT -> x"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = true }
                "simplified expression grammar for FirstFollow property test"

        { Name = "ArithExpr (arithmetic expressions)"
          Description = "Arithmetic expressions over {x, add, mult, lbr, rbr}."
          Grammars = [ ambiguous; leftAssoc; rightAssoc; simplified ]
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
        let variantA =
            mkEntry
                "variantA"
                "S -> S1\nS -> S2\nS1 -> a b S c\nS1 -> eps\nS2 -> a x S y\nS2 -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous (empty via S1 or S2); two independent tracks"

        let variantB =
            mkEntry
                "variantB"
                "S -> S1\nS -> S2\nS1 -> a b S c\nS -> eps\nS2 -> a x S y"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous; same language as variantA, different epsilon handling"

        { Name = "TwoTrackDyck (ab/c, ax/y)"
          Description = "Two independent Dyck-style tracks: one pairs a b with c, the other pairs a x with y."
          Grammars = [ variantA; variantB ]
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
        let rightRecursive =
            mkEntry
                "rightRecursive"
                "S -> a S\nS -> b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "right-recursive a^n b; unambiguous"

        { Name = "a^n b"
          Description = "L = {a^n b | n >= 0}"
          Grammars = [ rightRecursive ]
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
        let classic =
            mkEntry
                "classic"
                "S -> a S b\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "classic a^n b^n; unambiguous; LL(1)-compatible"

        { Name = "a^n b^n"
          Description = "L = {a^n b^n | n >= 0}"
          Grammars = [ classic ]
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
        let rightNullable =
            mkEntry
                "rightNullable"
                "S -> A B\nA -> a A\nA -> eps\nB -> b B\nB -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "a^m b^n; right-nullable A and B; unambiguous"

        { Name = "a^m b^n"
          Description = "L = {a^m b^n | m,n >= 0}"
          Grammars = [ rightNullable ]
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
        let singleRule =
            mkEntry
                "singleRule"
                "S -> a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "trivial single-terminal grammar"

        let shortUnitChain =
            mkEntry
                "shortUnitChain"
                "S -> A\nA -> B\nB -> a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "unit-production chain S -> A -> B -> a; used in toCnf and GrammarTests"

        let longUnitChain =
            mkEntry
                "longUnitChain"
                "S -> A\nA -> B\nB -> C\nC -> D\nD -> a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "long unit-production chain S->A->B->C->D->a; used in toCnf tests"

        let viaIntermediate =
            mkEntry
                "viaIntermediate"
                "S -> N\nN -> A\nA -> a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "unit-production chain S->N->A->a; used in data/example_grammar_chain.bnf"

        { Name = "SingleA ({a})"
          Description = "L = {a}"
          Grammars = [ singleRule; shortUnitChain; longUnitChain; viaIntermediate ]
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
        let singleRule =
            mkEntry
                "singleRule"
                "S -> a b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "trivial two-terminal grammar"

        let twoRule =
            mkEntry
                "twoRule"
                "S -> a B\nB -> b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "two-rule simple grammar S -> a B, B -> b; used in FirstFollow and LL visualization tests"

        { Name = "SingleAB ({ab})"
          Description = "L = {ab}"
          Grammars = [ singleRule; twoRule ]
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
        let singleRule =
            mkEntry
                "singleRule"
                "S -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = true
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "simplest epsilon grammar"

        let viaIntermediate =
            mkEntry
                "viaIntermediate"
                "S -> N\nN -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "epsilon via intermediate nonterminal"

        let viaBinary =
            mkEntry
                "viaBinary"
                "S -> N N\nN -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = true
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "epsilon via nullable binary production; CNF-compatible"

        let viaKleeneStar =
            mkEntry
                "viaKleeneStar"
                "S -> N*\nN -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "epsilon via Kleene star of nullable; uses EBNF"

        let selfRecursive =
            mkEntry
                "selfRecursive"
                "S -> S S\nS -> eps"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = true
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "epsilon via self-recursive binary production; ambiguous; CNF-compatible"

        let viaChain =
            mkEntry
                "viaChain"
                "S -> A B\nA -> C D\nB -> D C\nD -> eps\nC -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "epsilon via chain of nullable nonterminals"

        let viaAmbiguous =
            mkEntry
                "viaAmbiguous"
                "S -> A\nS -> B\nA -> C D\nB -> D C\nD -> eps\nC -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguously epsilon via alternative paths to nullable nonterminals"

        let viaCascade =
            mkEntry
                "viaCascade"
                "S -> A\nA -> B\nB -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "epsilon via cascade of unit productions"

        { Name = "EpsilonOnly ({epsilon})"
          Description = "L = {epsilon}. The empty-string-only language, expressed via various grammar constructions."
          Grammars =
            [ singleRule
              viaIntermediate
              viaBinary
              viaKleeneStar
              selfRecursive
              viaChain
              viaAmbiguous
              viaCascade ]
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
        let k2 =
            mkEntry
                "k2"
                "S -> a b A\nS -> a a B\nA -> c\nB -> d"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "LL(2) grammar: a b A vs a a B requires k=2 lookahead; LL(1) has conflict"

        { Name = "LL2Test ({abc, aad})"
          Description = "L = {abc, aad}. Test grammar for LL(2) lookahead resolution."
          Grammars = [ k2 ]
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
        let k3 =
            mkEntry
                "k3"
                "S -> a b c A\nS -> a b d B\nA -> x\nB -> y"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "LL(3) grammar: a b c A vs a b d B requires k=3 lookahead; LL(2) has conflict"

        { Name = "LL3Test ({abcx, abdy})"
          Description = "L = {abcx, abdy}. Test grammar for LL(3) lookahead resolution."
          Grammars = [ k3 ]
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
        let alternation =
            mkEntry
                "alternation"
                "S -> a\nS -> b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "alternation: S -> a | b"

        { Name = "AltAB ({a, b})"
          Description = "L = {a, b}. Two-alternative terminal language."
          Grammars = [ alternation ]
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
        let ebnfConcat =
            mkEntry
                "ebnfConcat"
                "S -> S1 S2\nS1 -> (a S1 b)*\nS2 -> (c S2 d)*"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = true
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "EBNF S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)*; for multi-block RSM tests"

        { Name = "DualDyck ((a^n b^n)(c^m d^m))"
          Description = "Concatenation of two independent Dyck languages: a-b blocks then c-d blocks."
          Grammars = [ ebnfConcat ]
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
        let rightAssoc =
            mkEntry
                "rightAssoc"
                "E -> T op_plus E\nE -> T\nT -> F op_mul T\nT -> F\nF -> x"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "expression grammar with op_plus/op_mul operators; for RSM round-trip testing"

        { Name = "OpExpr (binary operators x, op_plus, op_mul)"
          Description = "Binary expressions over {x, op_plus, op_mul}."
          Grammars = [ rightAssoc ]
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

    let DoubleA: Language =
        let singleRule =
            mkEntry
                "singleRule"
                "S -> a a"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "trivial single-rule grammar for {aa}"

        { Name = "DoubleA ({aa})"
          Description = "L = {aa}"
          Grammars = [ singleRule ]
          AcceptStrings = [ [ Terminal "a"; Terminal "a" ] ]
          RejectStrings =
            [ []
              [ Terminal "a" ]
              [ Terminal "a"; Terminal "a"; Terminal "a" ]
              [ Terminal "b" ] ]
          GenString = constantGen "a a" }

    let AOrEps: Language =
        let ebnfAlt =
            mkEntry
                "ebnfAlt"
                "S -> a | eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  HasEpsilon = true
                  IsAmbiguous = false
                  IsInCnf = false
                  IsRsmDerived = true
                  DoesNotCoverFullLanguage = false }
                "EBNF grammar S -> a | eps; generates {a, eps}"

        { Name = "AOrEps ({a, eps})"
          Description = "L = {a, epsilon}"
          Grammars = [ ebnfAlt ]
          AcceptStrings = [ []; [ Terminal "a" ] ]
          RejectStrings = [ [ Terminal "a"; Terminal "a" ]; [ Terminal "b" ] ]
          GenString = MyGen.elements [ ""; "a" ] }

    let private abPlusGen: Gen<string> =
        MyGen.choose (1, 12)
        |> MyGen.bind (fun len ->
            MyGen.choose (0, 1)
            |> MyGen.listOfLength len
            |> MyGen.map (fun bits -> bits |> List.map (fun b -> if b = 0 then "a" else "b") |> String.concat " "))

    let ABPlus: Language =
        let ambiguousConcat =
            mkEntry
                "ambiguousConcat"
                "S -> S S\nS -> a\nS -> b"
                { HasLeftRecursion = true
                  HasDirectLeftRecursion = true
                  IsAmbiguous = true
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "ambiguous two-terminal grammar S -> S S, S -> a, S -> b"

        { Name = "ABPlus ({a,b}+)"
          Description = "All non-empty strings over {a, b}."
          Grammars = [ ambiguousConcat ]
          AcceptStrings =
            [ [ Terminal "a" ]
              [ Terminal "b" ]
              [ Terminal "a"; Terminal "a" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "b"; Terminal "a" ]
              [ Terminal "b"; Terminal "b" ] ]
          RejectStrings = [ []; [ Terminal "c" ] ]
          GenString = abPlusGen }

    let FourTerm: Language =
        let singleRule =
            mkEntry
                "singleRule"
                "S -> a b c d"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "single-rule grammar for {abcd}"

        { Name = "FourTerm ({abcd})"
          Description = "L = {abcd}"
          Grammars = [ singleRule ]
          AcceptStrings = [ [ Terminal "a"; Terminal "b"; Terminal "c"; Terminal "d" ] ]
          RejectStrings =
            [ []
              [ Terminal "a" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "c" ]
              [ Terminal "a"; Terminal "b"; Terminal "c"; Terminal "d"; Terminal "e" ] ]
          GenString = constantGen "a b c d" }

    let MixedPairs: Language =
        let mixedRule =
            mkEntry
                "mixedRule"
                "S -> A a B b\nA -> a\nB -> b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "grammar with mixed terminals and nonterminals in RHS"

        { Name = "MixedPairs ({aabb})"
          Description = "L = {aabb}"
          Grammars = [ mixedRule ]
          AcceptStrings = [ [ Terminal "a"; Terminal "a"; Terminal "b"; Terminal "b" ] ]
          RejectStrings =
            [ []
              [ Terminal "a" ]
              [ Terminal "a"; Terminal "b" ]
              [ Terminal "a"; Terminal "b"; Terminal "a" ]
              [ Terminal "b"; Terminal "b" ] ]
          GenString = constantGen "a a b b" }

    let AX: Language =
        let startNotFirst =
            mkEntry
                "startNotFirst"
                "A -> x\nS -> a A"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "grammar where start symbol is not the first rule"

        { Name = "AX ({x})"
          Description = "L = {x}"
          Grammars = [ startNotFirst ]
          AcceptStrings = [ [ Terminal "x" ] ]
          RejectStrings =
            [ []
              [ Terminal "x"; Terminal "a" ]
              [ Terminal "a" ]
              [ Terminal "a"; Terminal "a" ] ]
          GenString = constantGen "x" }

    let SingleB: Language =
        let startFromA =
            mkEntry
                "startFromA"
                "A -> a\nS -> b"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "two-rule grammar where first rule is not the start symbol"

        { Name = "SingleB ({a})"
          Description = "L = {a}"
          Grammars = [ startFromA ]
          AcceptStrings = [ [ Terminal "a" ] ]
          RejectStrings = [ []; [ Terminal "b" ]; [ Terminal "a"; Terminal "a" ] ]
          GenString = constantGen "a" }

    let TestInfraGrammars: Language =
        let cnfAdjacent =
            mkEntry
                "cnfAdjacent"
                "S -> A B\nS -> a\nA -> B C\nB -> b\nC -> c"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "CNF-adjacent grammar; used in toCnf tests"

        let multiNontermWithEps =
            mkEntry
                "multiNontermWithEps"
                "S -> A B C D\nS -> eps\nA -> a\nB -> b\nC -> c\nD -> d\nE -> A B"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "multi-nonterminal grammar with epsilon; used in toCnf CNF-rules-only test"

        let mixedTerminalsNonterminals =
            mkEntry
                "mixedTerminalsNonterminals"
                "S -> a B c D"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "single rule with mixed terminal/nonterminal symbols; used in parseGrammar classification test"

        let twoRuleWithEps =
            mkEntry
                "twoRuleWithEps"
                "S -> a\nS -> eps"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = true
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "two-rule grammar with explicit epsilon; used in parseGrammar blank-lines test (test inserts blank lines around this text)"

        let mutualRecursion =
            mkEntry
                "mutualRecursion"
                "E -> a T\nT -> b E\nT -> c"
                { HasLeftRecursion = false
                  HasDirectLeftRecursion = false
                  IsAmbiguous = false
                  HasEpsilon = false
                  IsInCnf = false
                  IsRsmDerived = false
                  DoesNotCoverFullLanguage = false }
                "multi-nonterminal grammar with mutual recursion; used in LL table visualization tests"

        { Name = "TestInfraGrammars (ad-hoc test grammars)"
          Description = "Miscellaneous grammars used in parser and transformation tests."
          Grammars =
            [ cnfAdjacent
              multiNontermWithEps
              mixedTerminalsNonterminals
              twoRuleWithEps
              mutualRecursion ]
          AcceptStrings = []
          RejectStrings = []
          GenString = MyGen.constant "a" }

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
          OpExpr
          DoubleA
          AOrEps
          ABPlus
          FourTerm
          MixedPairs
          AX
          SingleB
          TestInfraGrammars ]

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
