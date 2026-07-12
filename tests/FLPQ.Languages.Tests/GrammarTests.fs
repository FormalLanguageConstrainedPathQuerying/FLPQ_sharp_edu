module GrammarTests

open Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra

module FactTests =

    [<Fact>]
    let ``parseGrammar parses single rule`` () =
        let text = "S -> a S b S"
        let g = Grammar.parseGrammar text

        Assert.Equal(1, List.length g.Rules)
        Assert.Equal(Nonterminal "S", g.Start)
        Assert.Equal(Nonterminal "S", g.Rules.Head.Lhs)
        Assert.Equal(4, Rhs.length g.Rules.Head.Rhs)

    [<Fact>]
    let ``parseGrammar parses multiple rules`` () =
        let text =
            "
        S -> a S b
        S -> eps
        S -> S S
        "

        let g = Grammar.parseGrammar text

        Assert.Equal(3, List.length g.Rules)
        Assert.Equal(Nonterminal "S", g.Start)

    [<Fact>]
    let ``parseGrammar parses eps as Epsilon right-hand side`` () =
        let text = "S -> eps"
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
        let text = "S -> a B c D"
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
            "
        A -> a
        S -> b
        "

        let g = Grammar.parseGrammar text
        Assert.Equal(Nonterminal "A", g.Start)

    [<Fact>]
    let ``parseGrammar throws on empty input`` () =
        Assert.Throws<System.ArgumentException>(fun () -> Grammar.parseGrammar "" |> ignore)

    [<Fact>]
    let ``parseGrammar parses grammar from task 6 example 1`` () =
        let text =
            "
        S -> a S b S
        S -> eps
        "

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
        let text = "S -> a"
        let g = Grammar.parseGrammar text

        Assert.Equal(1, Rhs.length g.Rules.Head.Rhs)

    [<Fact>]
    let ``parseGrammar handles multiple spaces between symbols`` () =
        let text = "S -> a   S    b"
        let g = Grammar.parseGrammar text

        Assert.Equal(3, Rhs.length g.Rules.Head.Rhs)


module CnfTests =

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
        let text =
            "
        S -> A B
        S -> a
        A -> B C
        B -> b
        C -> c
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles epsilon production`` () =
        let text =
            "
        S -> a S
        S -> eps
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles unit productions`` () =
        let text =
            "
        S -> A
        A -> B
        B -> a
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles long right-hand sides`` () =
        let text = "S -> a b c d"
        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles terminals in mixed rules`` () =
        let text =
            "
        S -> A a B b
        A -> a
        B -> b
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 1 grammar to CNF`` () =
        let text =
            "
        S -> a S b S
        S -> eps
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 2 grammar to CNF`` () =
        let text =
            "
        S -> a S b
        S -> eps
        S -> S S
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 3 grammar to CNF`` () =
        let text =
            "
        S -> a S
        S -> a
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 4 grammar to CNF`` () =
        let text =
            "
        S -> S a
        S -> a
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 5 grammar to CNF`` () =
        let text =
            "
        S -> S S
        S -> S S S
        S -> a
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf result contains only CNF rules`` () =
        let text =
            "
        S -> A B C D
        S -> eps
        A -> a
        B -> b
        C -> c
        D -> d
        E -> A B
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)
        Assert.True(allRhsSymbolsAreNonterminals cnf)

    [<Fact>]
    let ``toCnf handles grammar with only epsilon`` () =
        let text = "S -> eps"
        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles grammar with unit chain`` () =
        let text =
            "
        S -> A
        A -> B
        B -> C
        C -> D
        D -> a
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf result has no unit productions`` () =
        let text =
            "
        S -> A
        A -> a
        A -> B
        B -> b
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        let hasUnit =
            cnf.Rules
            |> List.exists (fun r ->
                match r.Rhs with
                | Symbols nel ->
                    NonEmptyList.length nel = 1
                    && (match NonEmptyList.head nel with
                        | Symbol.N _ -> true
                        | _ -> false)
                | _ -> false)

        Assert.False(hasUnit)


module PropertyCnfTests =

    open FsCheck
    open FsCheck.Xunit
    open FLPQ.TestUtilities

    [<Fact>]
    let ``toCnf preserves language acceptance`` () =
        let grammars =
            [ Grammar.parseGrammar "S -> a S b S\nS -> eps"
              Grammar.parseGrammar "S -> a S\nS -> a"
              Grammar.parseGrammar "S -> A\nA -> B\nB -> a"
              Grammar.parseGrammar "S -> a b c d"
              Grammar.parseGrammar "S -> A a B b\nA -> a\nB -> b"
              Grammar.parseGrammar "S -> eps"
              Grammar.parseGrammar "S -> S a\nS -> a" ]

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
        let g = Grammar.parseGrammar "S -> a S b\nS -> eps"
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        Assert.Equal(g, ExtendedGrammar.originalGrammar eg)

    [<Fact>]
    let ``create produces ExtendedGrammar with correct fresh start`` () =
        let g = Grammar.parseGrammar "S -> a S b\nS -> eps"
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        Assert.Equal(freshStart, ExtendedGrammar.freshStart eg)

    [<Fact>]
    let ``extGrammar has fresh start as start nonterminal`` () =
        let g = Grammar.parseGrammar "S -> a"
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        Assert.Equal(freshStart, (ExtendedGrammar.extGrammar eg).Start)

    [<Fact>]
    let ``extGrammar has one more rule than original`` () =
        let g = Grammar.parseGrammar "S -> a S b\nS -> eps"
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        Assert.Equal(g.Rules.Length + 1, (ExtendedGrammar.extGrammar eg).Rules.Length)

    [<Fact>]
    let ``extGrammar's first rule is S' -> S`` () =
        let g = Grammar.parseGrammar "S -> a"
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        let firstRule = (ExtendedGrammar.extGrammar eg).Rules.Head
        Assert.Equal(freshStart, firstRule.Lhs)
        Assert.Equal(1, Rhs.length firstRule.Rhs)

    [<Fact>]
    let ``originalStart returns S from simple grammar`` () =
        let g = Grammar.parseGrammar "A -> x\nS -> a A"
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g

        Assert.Equal(Nonterminal "A", ExtendedGrammar.originalStart eg)

    [<Fact>]
    let ``create is idempotent for same inputs`` () =
        let g = Grammar.parseGrammar "S -> a"
        let freshStart = Nonterminal "S'"
        let eg1 = ExtendedGrammar.create freshStart g
        let eg2 = ExtendedGrammar.create freshStart g

        Assert.Equal(ExtendedGrammar.extGrammar eg1, ExtendedGrammar.extGrammar eg2)

    [<Fact>]
    let ``extGrammar preserves all original rules`` () =
        let g = Grammar.parseGrammar "S -> a S b\nS -> eps"
        let freshStart = Nonterminal "S'"
        let eg = ExtendedGrammar.create freshStart g
        let ext = ExtendedGrammar.extGrammar eg

        let originalRules = ext.Rules |> List.skip 1
        Assert.Equal(g.Rules.Length, originalRules.Length)
        Assert.StrictEqual(g.Rules, originalRules)
