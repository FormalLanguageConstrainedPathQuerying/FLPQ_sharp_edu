module GrammarTests

open Xunit
open FLPQ.Core

module FactTests =

    [<Fact>]
    let ``parseGrammar parses single rule`` () =
        let text = "S -> a S b S"
        let g = Grammar.parseGrammar text

        Assert.Equal(1, List.length g.rules)
        Assert.Equal(Nonterminal "S", g.start)
        Assert.Equal(Nonterminal "S", g.rules.Head.lhs)
        Assert.Equal(4, List.length g.rules.Head.rhs)

    [<Fact>]
    let ``parseGrammar parses multiple rules`` () =
        let text =
            "
        S -> a S b
        S -> eps
        S -> S S
        "

        let g = Grammar.parseGrammar text

        Assert.Equal(3, List.length g.rules)
        Assert.Equal(Nonterminal "S", g.start)

    [<Fact>]
    let ``parseGrammar parses eps as empty right-hand side`` () =
        let text = "S -> eps"
        let g = Grammar.parseGrammar text

        Assert.Equal(1, List.length g.rules)
        Assert.Empty(g.rules.Head.rhs)

    [<Fact>]
    let ``parseGrammar ignores empty lines`` () =
        let text =
            "

        S -> a

        S -> eps

        "

        let g = Grammar.parseGrammar text

        Assert.Equal(2, List.length g.rules)

    [<Fact>]
    let ``parseGrammar classifies terminals and nonterminals`` () =
        let text = "S -> a B c D"
        let g = Grammar.parseGrammar text

        let rhs = g.rules.Head.rhs
        Assert.Equal(4, List.length rhs)

        match rhs.[0] with
        | T(Terminal "a") -> ()
        | _ -> Assert.Fail("Expected terminal 'a'")

        match rhs.[1] with
        | N(Nonterminal "B") -> ()
        | _ -> Assert.Fail("Expected nonterminal 'B'")

        match rhs.[2] with
        | T(Terminal "c") -> ()
        | _ -> Assert.Fail("Expected terminal 'c'")

        match rhs.[3] with
        | N(Nonterminal "D") -> ()
        | _ -> Assert.Fail("Expected nonterminal 'D'")

    [<Fact>]
    let ``parseGrammar start nonterminal is from first rule`` () =
        let text =
            "
        A -> a
        S -> b
        "

        let g = Grammar.parseGrammar text
        Assert.Equal(Nonterminal "A", g.start)

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

        Assert.Equal(2, List.length g.rules)
        Assert.Equal(Nonterminal "S", g.start)

        let r1 = g.rules.[0]
        Assert.Equal(4, List.length r1.rhs)

        match r1.rhs.[0] with
        | T(Terminal "a") -> ()
        | _ -> Assert.Fail("Expected 'a'")

        match r1.rhs.[1] with
        | N(Nonterminal "S") -> ()
        | _ -> Assert.Fail("Expected 'S'")

        match r1.rhs.[2] with
        | T(Terminal "b") -> ()
        | _ -> Assert.Fail("Expected 'b'")

        match r1.rhs.[3] with
        | N(Nonterminal "S") -> ()
        | _ -> Assert.Fail("Expected 'S'")

        let r2 = g.rules.[1]
        Assert.Empty(r2.rhs)

    [<Fact>]
    let ``parseGrammar handles rule with single symbol`` () =
        let text = "S -> a"
        let g = Grammar.parseGrammar text

        Assert.Equal(1, List.length g.rules.Head.rhs)

    [<Fact>]
    let ``parseGrammar handles multiple spaces between symbols`` () =
        let text = "S -> a   S    b"
        let g = Grammar.parseGrammar text

        Assert.Equal(3, List.length g.rules.Head.rhs)


module CnfTests =

    let private isCnf (g: Grammar<string, string>) : bool =
        g.rules
        |> List.forall (fun r ->
            match r.rhs with
            | [] -> r.lhs = g.start
            | [ T _ ] -> true
            | [ N _; N _ ] -> true
            | _ -> false)

    let private nonterminalsOfCnf (g: Grammar<string, string>) : Set<Nonterminal<string>> =
        g.rules |> List.map (fun r -> r.lhs) |> Set.ofList

    let private allRhsSymbolsAreNonterminals (g: Grammar<string, string>) : bool =
        g.rules
        |> List.forall (fun r ->
            match r.rhs with
            | [] -> true
            | [ T _ ] -> true
            | [ N _; N _ ] -> true
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
        let cnf = Grammar.toCnf g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles epsilon production`` () =
        let text =
            "
        S -> a S
        S -> eps
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf g

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
        let cnf = Grammar.toCnf g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf handles long right-hand sides`` () =
        let text = "S -> a b c d"
        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf g

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
        let cnf = Grammar.toCnf g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 1 grammar to CNF`` () =
        let text =
            "
        S -> a S b S
        S -> eps
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf g

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
        let cnf = Grammar.toCnf g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 3 grammar to CNF`` () =
        let text =
            "
        S -> a S
        S -> a
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf g

        Assert.True(isCnf cnf)

    [<Fact>]
    let ``toCnf converts task 6 example 4 grammar to CNF`` () =
        let text =
            "
        S -> S a
        S -> a
        "

        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf g

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
        let cnf = Grammar.toCnf g

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
        let cnf = Grammar.toCnf g

        Assert.True(isCnf cnf)
        Assert.True(allRhsSymbolsAreNonterminals cnf)

    [<Fact>]
    let ``toCnf handles grammar with only epsilon`` () =
        let text = "S -> eps"
        let g = Grammar.parseGrammar text
        let cnf = Grammar.toCnf g

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
        let cnf = Grammar.toCnf g

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
        let cnf = Grammar.toCnf g

        let hasUnit =
            cnf.rules
            |> List.exists (fun r ->
                match r.rhs with
                | [ N _ ] -> true
                | _ -> false)

        Assert.False(hasUnit)
