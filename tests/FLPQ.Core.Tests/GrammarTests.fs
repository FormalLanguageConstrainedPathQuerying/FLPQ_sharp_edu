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
