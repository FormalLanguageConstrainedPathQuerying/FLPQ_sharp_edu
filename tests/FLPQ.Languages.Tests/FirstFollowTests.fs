module FirstFollowTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

module FactTests =

    [<Fact>]
    let ``firstK for grammar3 with k=1`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S
        S -> a
        "

        let first = FirstFollow.firstK g 1

        Assert.Contains(Nonterminal "S", Map.keys first)
        Assert.Equal<Symbol<string, string> list>(set [ [ T(Terminal "a") ] ], Map.find (Nonterminal "S") first)

    [<Fact>]
    let ``firstK for grammar1 with k=1 includes epsilon`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S b S
        S -> eps
        "

        let first = FirstFollow.firstK g 1

        Assert.Equal<Symbol<string, string> list>(
            set [ [ T(Terminal "a") ]; [ Epsilon ] ],
            Map.find (Nonterminal "S") first
        )

    [<Fact>]
    let ``followK for grammar1 with k=1`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S b S
        S -> eps
        "

        let follow = FirstFollow.followK g 1

        let sFollow = Map.find (Nonterminal "S") follow
        Assert.Contains<Symbol<string, string> list>([ Epsilon ], sFollow)
        Assert.Contains<Symbol<string, string> list>([ T(Terminal "b") ], sFollow)

    [<Fact>]
    let ``firstK with k=2 for grammar3`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S
        S -> a
        "

        let first = FirstFollow.firstK g 2

        Assert.Equal<Symbol<string, string> list>(
            set [ [ T(Terminal "a") ]; [ T(Terminal "a"); T(Terminal "a") ] ],
            Map.find (Nonterminal "S") first
        )

    [<Fact>]
    let ``firstK handles expression grammar 7`` () =
        let g =
            Grammar.parseGrammar
                "
        E -> E + T
        E -> T
        T -> T * F
        T -> F
        F -> ( E )
        F -> x
        "

        let first = FirstFollow.firstK g 1
        let eFirst = Map.find (Nonterminal "E") first

        Assert.Contains<Symbol<string, string> list>([ T(Terminal "x") ], eFirst)
        Assert.Contains<Symbol<string, string> list>([ T(Terminal "(") ], eFirst)

    [<Fact>]
    let ``firstKOfString concatenates correctly`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a B
        B -> b
        "

        let first = FirstFollow.firstK g 2

        let firstB = FirstFollow.firstKOfString first 2 [ N(Nonterminal "B") ]
        Assert.Equal<Symbol<string, string> list>(set [ [ T(Terminal "b") ] ], firstB)

    [<Fact>]
    let ``firstK with k=0 returns only epsilon`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a
        "

        let first = FirstFollow.firstK g 0
        Assert.Equal<Symbol<string, string> list>(set [ [ Epsilon ] ], Map.find (Nonterminal "S") first)

    [<Fact>]
    let ``followK for grammar3 with k=1`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S
        S -> a
        "

        let follow = FirstFollow.followK g 1

        Assert.Equal<Symbol<string, string> list>(set [ [ Epsilon ] ], Map.find (Nonterminal "S") follow)

    [<Fact>]
    let ``followK for expression grammar 7 with k=1`` () =
        let g =
            Grammar.parseGrammar
                "
        E -> E + T
        E -> T
        T -> T * F
        T -> F
        F -> ( E )
        F -> x
        "

        let follow = FirstFollow.followK g 1
        let eFollow = Map.find (Nonterminal "E") follow

        Assert.Contains<Symbol<string, string> list>([ T(Terminal "+") ], eFollow)
        Assert.Contains<Symbol<string, string> list>([ T(Terminal ")") ], eFollow)
        Assert.Contains<Symbol<string, string> list>([ Epsilon ], eFollow)
