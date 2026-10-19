module FirstFollowTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

module FactTests =

    [<Fact>]
    let ``firstK for grammar3 with k=1`` () =
        let g = LanguageRegistry.APlus.Grammars.[0].Grammar

        let first = FirstFollow.firstK g 1

        Assert.Contains(Nonterminal "S", Map.keys first)
        Assert.Equal<Symbol<string, string> list>(set [ [ Symbol.T(Terminal "a") ] ], Map.find (Nonterminal "S") first)

    [<Fact>]
    let ``firstK for grammar1 with k=1 includes epsilon`` () =
        let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

        let first = FirstFollow.firstK g 1

        Assert.Equal<Symbol<string, string> list>(
            set [ [ Symbol.T(Terminal "a") ]; [ Symbol.Epsilon ] ],
            Map.find (Nonterminal "S") first
        )

    [<Fact>]
    let ``followK for grammar1 with k=1`` () =
        let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

        let follow = FirstFollow.followK g 1

        let sFollow = Map.find (Nonterminal "S") follow
        Assert.Contains<Symbol<string, string> list>([ Symbol.Epsilon ], sFollow)
        Assert.Contains<Symbol<string, string> list>([ Symbol.T(Terminal "b") ], sFollow)

    [<Fact>]
    let ``firstK with k=2 for grammar3`` () =
        let g = LanguageRegistry.APlus.Grammars.[0].Grammar

        let first = FirstFollow.firstK g 2

        Assert.Equal<Symbol<string, string> list>(
            set
                [ [ Symbol.T(Terminal "a") ]
                  [ Symbol.T(Terminal "a"); Symbol.T(Terminal "a") ] ],
            Map.find (Nonterminal "S") first
        )

    [<Fact>]
    let ``firstK handles expression grammar 7`` () =
        let g = LanguageRegistry.ArithExpr.Grammars.[1].Grammar

        let first = FirstFollow.firstK g 1
        let eFirst = Map.find (Nonterminal "E") first

        Assert.Contains<Symbol<string, string> list>([ Symbol.T(Terminal "x") ], eFirst)
        Assert.Contains<Symbol<string, string> list>([ Symbol.T(Terminal "lbr") ], eFirst)

    [<Fact>]
    let ``firstKOfString concatenates correctly`` () =
        let g = (LanguageRegistry.findGrammar LanguageRegistry.SingleAB "twoRule").Grammar

        let first = FirstFollow.firstK g 2

        let firstB = FirstFollow.firstKOfString first 2 [ Symbol.N(Nonterminal "B") ]
        Assert.Equal<Symbol<string, string> list>(set [ [ Symbol.T(Terminal "b") ] ], firstB)

    [<Fact>]
    let ``firstK with k=0 returns only epsilon`` () =
        let g = LanguageRegistry.SingleA.Grammars.[0].Grammar

        let first = FirstFollow.firstK g 0
        Assert.Equal<Symbol<string, string> list>(set [ [ Symbol.Epsilon ] ], Map.find (Nonterminal "S") first)

    [<Fact>]
    let ``followK for grammar3 with k=1`` () =
        let g = LanguageRegistry.APlus.Grammars.[0].Grammar

        let follow = FirstFollow.followK g 1

        Assert.Equal<Symbol<string, string> list>(set [ [ Symbol.Epsilon ] ], Map.find (Nonterminal "S") follow)

    [<Fact>]
    let ``followK for expression grammar 7 with k=1`` () =
        let g = LanguageRegistry.ArithExpr.Grammars.[1].Grammar

        let follow = FirstFollow.followK g 1
        let eFollow = Map.find (Nonterminal "E") follow

        Assert.Contains<Symbol<string, string> list>([ Symbol.T(Terminal "add") ], eFollow)
        Assert.Contains<Symbol<string, string> list>([ Symbol.T(Terminal "rbr") ], eFollow)
        Assert.Contains<Symbol<string, string> list>([ Symbol.Epsilon ], eFollow)

module PropertyTests =

    let private enumerateFrom
        (g: Grammar<string, string>)
        (start: Symbol<string, string> list)
        (maxDepth: int)
        : Set<Symbol<string, string> list> =
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

        derive start maxDepth

    [<Fact>]
    let ``firstK matches brute-force derivation`` () =
        let grammars =
            [ LanguageRegistry.Dyck1.Grammars.[0].Grammar
              LanguageRegistry.APlus.Grammars.[0].Grammar
              (LanguageRegistry.findGrammar LanguageRegistry.SingleAB "twoRule").Grammar
              (LanguageRegistry.findGrammar LanguageRegistry.ABPlus "ambiguousConcat").Grammar
              (LanguageRegistry.findGrammar LanguageRegistry.ArithExpr "simplified").Grammar ]

        grammars
        |> List.forall (fun g ->
            let k = 1

            let computed = FirstFollow.firstK g k

            let nonterminals = Grammar.nonterminalsOf g

            nonterminals
            |> Set.forall (fun nt ->
                let computedFirst = Map.tryFind nt computed |> Option.defaultValue Set.empty

                // Brute-force FIRST: first symbol of every fully terminal string
                // derivable from nt ([Epsilon] when nt derives the empty string).
                let derived =
                    enumerateFrom g [ Symbol.N nt ] 5
                    |> Set.filter (fun syms ->
                        List.forall
                            (function
                            | Symbol.T _ -> true
                            | _ -> false)
                            syms)
                    |> Set.map (function
                        | [] -> [ Symbol.Epsilon ]
                        | s :: _ -> [ s ])

                Set.isSubset derived computedFirst))


module FirstFollowEdgeTests =

    [<Fact>]
    let ``firstK handles epsilon symbol and undefined nonterminal in right-hand side`` () =
        // Manually constructed grammar: S -> a eps (Symbol.Epsilon inside Symbols)
        // and T -> b U where U is undefined (missing from firstMap). Exercises the
        // Symbol.Epsilon loop arm and the missing-nonterminal arm of firstOfSymbols.
        let g: Grammar<string, string> =
            { Rules =
                [ { Lhs = Nonterminal "S"
                    Rhs = Symbols(NonEmptyList.ofList [ Symbol.T(Terminal "a"); Symbol.Epsilon ]) }
                  { Lhs = Nonterminal "T"
                    Rhs = Symbols(NonEmptyList.ofList [ Symbol.T(Terminal "b"); Symbol.N(Nonterminal "U") ]) } ]
              Start = Nonterminal "S" }

        let firstMap = FirstFollow.firstK g 2
        Assert.True(Set.contains [ Symbol.T(Terminal "a") ] (Map.find (Nonterminal "S") firstMap))
        Assert.True(Set.contains [ Symbol.T(Terminal "b") ] (Map.find (Nonterminal "T") firstMap))
