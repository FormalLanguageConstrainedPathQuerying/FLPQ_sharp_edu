module FirstFollowTests

open Xunit
open FsCheck
open FsCheck.Xunit
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
        Assert.Equal<Symbol<string, string> list>(set [ [ Symbol.T(Terminal "a") ] ], Map.find (Nonterminal "S") first)

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
            set [ [ Symbol.T(Terminal "a") ]; [ Symbol.Epsilon ] ],
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
        Assert.Contains<Symbol<string, string> list>([ Symbol.Epsilon ], sFollow)
        Assert.Contains<Symbol<string, string> list>([ Symbol.T(Terminal "b") ], sFollow)

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
            set
                [ [ Symbol.T(Terminal "a") ]
                  [ Symbol.T(Terminal "a"); Symbol.T(Terminal "a") ] ],
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

        Assert.Contains<Symbol<string, string> list>([ Symbol.T(Terminal "x") ], eFirst)
        Assert.Contains<Symbol<string, string> list>([ Symbol.T(Terminal "(") ], eFirst)

    [<Fact>]
    let ``firstKOfString concatenates correctly`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a B
        B -> b
        "

        let first = FirstFollow.firstK g 2

        let firstB = FirstFollow.firstKOfString first 2 [ Symbol.N(Nonterminal "B") ]
        Assert.Equal<Symbol<string, string> list>(set [ [ Symbol.T(Terminal "b") ] ], firstB)

    [<Fact>]
    let ``firstK with k=0 returns only epsilon`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a
        "

        let first = FirstFollow.firstK g 0
        Assert.Equal<Symbol<string, string> list>(set [ [ Symbol.Epsilon ] ], Map.find (Nonterminal "S") first)

    [<Fact>]
    let ``followK for grammar3 with k=1`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S
        S -> a
        "

        let follow = FirstFollow.followK g 1

        Assert.Equal<Symbol<string, string> list>(set [ [ Symbol.Epsilon ] ], Map.find (Nonterminal "S") follow)

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

        Assert.Contains<Symbol<string, string> list>([ Symbol.T(Terminal "+") ], eFollow)
        Assert.Contains<Symbol<string, string> list>([ Symbol.T(Terminal ")") ], eFollow)
        Assert.Contains<Symbol<string, string> list>([ Symbol.Epsilon ], eFollow)

module PropertyTests =

    let private enumerateDerivations (g: Grammar<string, string>) (maxDepth: int) : Set<Symbol<string, string> list> =
        let rec derive (current: Symbol<string, string> list) (depth: int) : Set<Symbol<string, string> list> =
            if depth <= 0 then
                Set.singleton current
            else
                let expansionResults =
                    g.Rules
                    |> List.collect (fun r ->
                        match current with
                        | Symbol.N nt :: rest when nt = r.Lhs ->
                            let rhsSyms = Rhs.toNonEpsilonList r.Rhs
                            derive (rhsSyms @ rest) (depth - 1) |> Set.toList
                        | _ -> [])
                    |> Set.ofList

                if Set.isEmpty expansionResults then
                    Set.singleton current
                else
                    expansionResults

        derive [ Symbol.N g.Start ] maxDepth

    let private prefixes (k: int) (sentences: Set<Symbol<string, string> list>) : Set<Symbol<string, string> list> =
        sentences
        |> Set.map (fun syms ->
            let taken = List.truncate k syms
            if List.isEmpty taken then [ Symbol.Epsilon ] else taken)

    [<Property(MaxTest = 200)>]
    let ``firstK matches brute-force derivation`` () =
        let grammars =
            [ Grammar.parseGrammar "S -> a S b S\nS -> eps"
              Grammar.parseGrammar "S -> a S\nS -> a"
              Grammar.parseGrammar "S -> a B\nB -> b"
              Grammar.parseGrammar "S -> S S\nS -> a\nS -> b"
              Grammar.parseGrammar "E -> E + T\nE -> T\nT -> x" ]

        grammars
        |> List.forall (fun g ->
            let k = 1

            let computed = FirstFollow.firstK g k

            let allDerivable = enumerateDerivations g 5

            let nonterminals = Grammar.nonterminalsOf g

            nonterminals
            |> Set.forall (fun nt ->
                let computedFirst = Map.tryFind nt computed |> Option.defaultValue Set.empty

                let derived =
                    allDerivable
                    |> Set.filter (fun syms ->
                        match syms with
                        | Symbol.N nt' :: _ when nt' = nt -> true
                        | _ -> false)
                    |> prefixes k

                not (Set.isEmpty computedFirst)
                || Set.isEmpty derived
                   && (Set.isEmpty derived || Set.isSubset derived computedFirst)))
