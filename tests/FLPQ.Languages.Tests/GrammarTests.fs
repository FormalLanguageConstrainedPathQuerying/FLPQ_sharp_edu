module GrammarTests

open Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

module FactTests =

    [<Fact>]
    let ``parseGrammar parses single rule`` () =
        let text =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_aSbS_no_eps").Text

        let g = Grammar.parseGrammar text

        Assert.Equal(1, List.length g.Rules)
        Assert.Equal(Nonterminal "S", g.Start)
        Assert.Equal(Nonterminal "S", g.Rules.Head.Lhs)
        Assert.Equal(4, Rhs.length g.Rules.Head.Rhs)

    [<Fact>]
    let ``parseGrammar parses multiple rules`` () =
        let text = (LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "grammar2").Text

        let g = Grammar.parseGrammar text

        Assert.Equal(3, List.length g.Rules)
        Assert.Equal(Nonterminal "S", g.Start)

    [<Fact>]
    let ``parseGrammar parses eps as Epsilon right-hand side`` () =
        let text =
            (LanguageRegistry.findGrammar LanguageRegistry.EpsilonOnly "grammarEps").Text

        let g = Grammar.parseGrammar text

        Assert.Equal(1, List.length g.Rules)
        Assert.True(Rhs.isEpsilon g.Rules.Head.Rhs)

    [<Fact>]
    let ``parseGrammar ignores empty lines`` () =
        let text =
            "

        S -> a

        S -> eps

        "

        let g = Grammar.parseGrammar text

        Assert.Equal(2, List.length g.Rules)

    [<Fact>]
    let ``parseGrammar classifies terminals and nonterminals`` () =
        let text =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_aBcD").Text

        let g = Grammar.parseGrammar text

        let rhsList = Rhs.toListWithEpsilon g.Rules.Head.Rhs
        Assert.Equal(4, List.length rhsList)

        match rhsList.[0] with
        | Symbol.T(Terminal "a") -> ()
        | _ -> Assert.Fail("Expected terminal 'a'")

        match rhsList.[1] with
        | Symbol.N(Nonterminal "B") -> ()
        | _ -> Assert.Fail("Expected nonterminal 'B'")

        match rhsList.[2] with
        | Symbol.T(Terminal "c") -> ()
        | _ -> Assert.Fail("Expected terminal 'c'")

        match rhsList.[3] with
        | Symbol.N(Nonterminal "D") -> ()
        | _ -> Assert.Fail("Expected nonterminal 'D'")

    [<Fact>]
    let ``parseGrammar start nonterminal is from first rule`` () =
        let text =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_A_a__S_b").Text

        let g = Grammar.parseGrammar text
        Assert.Equal(Nonterminal "A", g.Start)

    [<Fact>]
    let ``parseGrammar throws on empty input`` () =
        Assert.Throws<System.ArgumentException>(fun () -> Grammar.parseGrammar "" |> ignore)

    [<Fact>]
    let ``parseGrammar parses grammar from task 6 example 1`` () =
        let text = (LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "grammar1").Text

        let g = Grammar.parseGrammar text

        Assert.Equal(2, List.length g.Rules)
        Assert.Equal(Nonterminal "S", g.Start)

        let r1 = g.Rules.[0]
        let r1List = Rhs.toListWithEpsilon r1.Rhs
        Assert.Equal(4, List.length r1List)

        match r1List.[0] with
        | Symbol.T(Terminal "a") -> ()
        | _ -> Assert.Fail("Expected 'a'")

        match r1List.[1] with
        | Symbol.N(Nonterminal "S") -> ()
        | _ -> Assert.Fail("Expected 'S'")

        match r1List.[2] with
        | Symbol.T(Terminal "b") -> ()
        | _ -> Assert.Fail("Expected 'b'")

        match r1List.[3] with
        | Symbol.N(Nonterminal "S") -> ()
        | _ -> Assert.Fail("Expected 'S'")

        let r2 = g.Rules.[1]
        Assert.True(Rhs.isEpsilon r2.Rhs)

    [<Fact>]
    let ``parseGrammar handles rule with single symbol`` () =
        let text = (LanguageRegistry.findGrammar LanguageRegistry.SingleA "grammarS2a").Text
        let g = Grammar.parseGrammar text

        Assert.Equal(1, Rhs.length g.Rules.Head.Rhs)

    [<Fact>]
    let ``parseGrammar handles multiple spaces between symbols`` () =
        let text = "S -> a   S    b"
        let g = Grammar.parseGrammar text

        Assert.Equal(3, Rhs.length g.Rules.Head.Rhs)


module CnfTests =

    open FLPQ.TestUtilities

    let private isCnf (g: Grammar<string, string>) : bool =
        g.Rules
        |> List.forall (fun r ->
            match r.Rhs with
            | EpsilonRhs -> r.Lhs = g.Start
            | Symbols nel ->
                let syms = NonEmptyList.toList nel

                match syms with
                | [ Symbol.T _ ] -> true
                | [ Symbol.N _; Symbol.N _ ] -> true
                | _ -> false)

    let private allRhsSymbolsAreNonterminals (g: Grammar<string, string>) : bool =
        g.Rules
        |> List.forall (fun r ->
            match r.Rhs with
            | EpsilonRhs -> true
            | Symbols nel ->
                let syms = NonEmptyList.toList nel

                match syms with
                | [ Symbol.T _ ] -> true
                | [ Symbol.N _; Symbol.N _ ] -> true
                | _ -> false)

    [<Fact>]
    let ``toCnf preserves grammar that is already in CNF`` () =
        let g =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_AB_BC_C").Grammar

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles epsilon production`` () =
        let g =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_aS_eps").Grammar

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles unit productions`` () =
        let g =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_A_B_a").Grammar

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles long right-hand sides`` () =
        let g =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_abc").Grammar

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles terminals in mixed rules`` () =
        let g =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_AaBb").Grammar

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 1 grammar to CNF`` () =
        let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 2 grammar to CNF`` () =
        let g = LanguageRegistry.Dyck1.Grammars.[1].Grammar
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 3 grammar to CNF`` () =
        let g = LanguageRegistry.APlus.Grammars.[0].Grammar
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 4 grammar to CNF`` () =
        let g = LanguageRegistry.APlus.Grammars.[1].Grammar
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 5 grammar to CNF`` () =
        let g = LanguageRegistry.APlus.Grammars.[2].Grammar
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf result contains only CNF rules`` () =
        let g =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_ABCDE").Grammar

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)
        Assert.True(allRhsSymbolsAreNonterminals cnf)

    [<Fact>]
    let ``toCnf handles grammar with only epsilon`` () =
        let g = LanguageRegistry.EpsilonOnly.Grammars.[0].Grammar
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles grammar with unit chain`` () =
        let g =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_long_chain").Grammar

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf result has no unreachable or non-generating nonterminals`` () =
        let g = LanguageRegistry.APlus.Grammars.[5].Grammar
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)
        Assert.True(Grammar.allNonterminalsReachable cnf)
        Assert.True(Grammar.allNonterminalsGenerating cnf)

    [<Fact>]
    let ``toCnf produces no unreachable or non-generating nonterminals for all registry grammars`` () =
        let failures =
            LanguageRegistry.allLanguages
            |> List.collect (fun lang ->
                lang.Grammars
                |> List.choose (fun g ->
                    let cnf = Grammar.toCnf Grammar.freshStringNonterminal g.Grammar

                    let reachableOk = Grammar.allNonterminalsReachable cnf
                    let generatingOk = Grammar.allNonterminalsGenerating cnf

                    if not reachableOk || not generatingOk then
                        Some(sprintf "%s/%s: reachable=%b generating=%b" lang.Name g.Name reachableOk generatingOk)
                    else
                        None))

        if not (List.isEmpty failures) then
            Assert.Fail(sprintf "Failures:\n%s" (String.concat "\n" failures))


module PropertyCnfTests =

    open FsCheck
    open FsCheck.Xunit
    open FLPQ.TestUtilities

    [<Fact>]
    let ``toCnf preserves language acceptance`` () =
        let grammars =
            [ LanguageRegistry.Dyck1.Grammars.[0].Grammar
              LanguageRegistry.APlus.Grammars.[0].Grammar
              (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_A_B_a").Grammar
              (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_abc").Grammar
              (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_AaBb").Grammar
              LanguageRegistry.EpsilonOnly.Grammars.[0].Grammar
              LanguageRegistry.APlus.Grammars.[1].Grammar ]

        let testStrings =
            [ ""
              "a"
              "b"
              "aa"
              "ab"
              "ba"
              "bb"
              "aaa"
              "aab"
              "aba"
              "abb"
              "abab"
              "aabb" ]

        grammars
        |> List.forall (fun g ->
            let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

            testStrings
            |> List.forall (fun s ->
                let tokens =
                    if s = "" then
                        []
                    else
                        s.ToCharArray() |> Array.toList |> List.map (fun c -> Terminal(string c))

                let origResult = Cyk.parse (fun i -> $"_N{i}") g tokens

                let cnfResult = Cyk.parse (fun i -> $"_CNF_N{i}") cnf tokens

                origResult = cnfResult))


module ExtendedGrammarTests =

    [<Fact>]
    let ``create produces ExtendedGrammar with correct original grammar`` () =
        let g = LanguageRegistry.ANBN.Grammars.[0].Grammar
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        Assert.Equal(g, ExtendedGrammar.originalGrammar eg)

    [<Fact>]
    let ``create produces ExtendedGrammar with correct fresh start`` () =
        let g = LanguageRegistry.ANBN.Grammars.[0].Grammar
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        Assert.Equal(freshStart, ExtendedGrammar.freshStart eg)

    [<Fact>]
    let ``extGrammar has fresh start as start nonterminal`` () =
        let g = LanguageRegistry.SingleA.Grammars.[0].Grammar
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        Assert.Equal(freshStart, (ExtendedGrammar.extGrammar eg).Start)

    [<Fact>]
    let ``extGrammar has one more rule than original`` () =
        let g = LanguageRegistry.ANBN.Grammars.[0].Grammar
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        Assert.Equal(g.Rules.Length + 1, (ExtendedGrammar.extGrammar eg).Rules.Length)

    [<Fact>]
    let ``extGrammar's first rule is S' -> S`` () =
        let g = LanguageRegistry.SingleA.Grammars.[0].Grammar
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        let firstRule = (ExtendedGrammar.extGrammar eg).Rules.Head
        Assert.Equal(freshStart, firstRule.Lhs)
        Assert.Equal(1, Rhs.length firstRule.Rhs)

    [<Fact>]
    let ``originalStart returns S from simple grammar`` () =
        let g =
            (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_x_aA").Grammar

        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        Assert.Equal(Nonterminal "A", ExtendedGrammar.originalStart eg)

    [<Fact>]
    let ``create is idempotent for same inputs`` () =
        let g = LanguageRegistry.SingleA.Grammars.[0].Grammar
        let freshStart = Nonterminal "S'"
        let eg1 = ExtendedGrammar.create freshStart g
        let eg2 = ExtendedGrammar.create freshStart g

        Assert.Equal(ExtendedGrammar.extGrammar eg1, ExtendedGrammar.extGrammar eg2)

    [<Fact>]
    let ``extGrammar preserves all original rules`` () =
        let g = LanguageRegistry.ANBN.Grammars.[0].Grammar
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g
        let ext = ExtendedGrammar.extGrammar eg

        let originalRules = ext.Rules |> List.skip 1
        Assert.Equal(g.Rules.Length, originalRules.Length)
        Assert.StrictEqual(g.Rules, originalRules)
