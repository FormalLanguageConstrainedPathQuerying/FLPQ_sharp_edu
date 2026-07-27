module StressTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

open TestGrammars

let private balancedGrammar = Grammar.parseGrammar "S -> a S b S\nS -> eps"

let private makeAnBn (n: int) : string =
    let aPart = System.String.Concat(Array.replicate n "a ")
    let bPart = System.String.Concat(Array.replicate n "b ")
    (aPart + bPart).Trim()

[<Fact>]
[<Trait("Category", "Stress")>]
let ``CYK accepts a^30 b^30 (60 tokens)`` () =
    let input = makeAnBn 30
    let tokens = Tokenizer.tokenizeTerminals input
    Assert.Equal(60, tokens.Length)
    Assert.True(Cyk.parse Grammar.freshStringNonterminal balancedGrammar tokens)

[<Fact>]
[<Trait("Category", "Stress")>]
let ``Valiant accepts a^30 b^30 (60 tokens)`` () =
    let input = makeAnBn 30
    let tokens = Tokenizer.tokenizeTerminals input
    Assert.Equal(60, tokens.Length)
    Assert.True(Valiant.parse Grammar.freshStringNonterminal balancedGrammar tokens)

[<Fact>]
[<Trait("Category", "Stress")>]
let ``Modified Valiant accepts a^25 b^25 (50 tokens)`` () =
    let input = makeAnBn 25
    let tokens = Tokenizer.tokenizeTerminals input

    let _, accepted =
        Valiant.parseModifiedWithTable Grammar.freshStringNonterminal balancedGrammar tokens

    Assert.True(accepted)

[<Fact>]
[<Trait("Category", "Stress")>]
let ``CYK and Valiant agree on a^n b^n for n=1..20`` () =
    for n in 1..20 do
        let input = makeAnBn n
        let tokens = Tokenizer.tokenizeTerminals input

        let cykResult = Cyk.parse Grammar.freshStringNonterminal balancedGrammar tokens

        let valiantResult =
            Valiant.parse Grammar.freshStringNonterminal balancedGrammar tokens

        Assert.Equal(cykResult, valiantResult)

[<Properties(Arbitrary = [| typeof<StressStringGenerators> |], MaxTest = 5)>]
module StressProperties =

    [<Property>]
    [<Trait("Category", "Stress")>]
    let ``CYK and Valiant agree on balanced grammar for long strings`` (s: string) =
        let tokens = Tokenizer.tokenizeTerminals s

        let cykResult = Cyk.parse Grammar.freshStringNonterminal balancedGrammar tokens

        let valiantResult =
            Valiant.parse Grammar.freshStringNonterminal balancedGrammar tokens

        cykResult = valiantResult

module NfaToDfaStress =

    [<Fact>]
    [<Trait("Category", "Stress")>]
    let ``NFA to DFA with 50-state chain succeeds`` () =
        let states = [ 0..49 ]
        let transitions = [ for i in 0..48 -> (i, "a", i + 1) ]

        let nfa =
            Nfa.fromTransitions states transitions Set.empty (Set.singleton 0) (Set.singleton 49)

        let dfa = Automaton.toDfa nfa
        Assert.True(Dfa.stateCount dfa > 0)

    [<Fact>]
    [<Trait("Category", "Stress")>]
    let ``NFA to DFA with 30-state diamond succeeds`` () =
        let n = 30
        let states = [ 0 .. n - 1 ]
        let transitions = [ for i in 0 .. n - 2 -> (i, "a", i + 1) ]
        let epsTransitions = Set.singleton (n - 2, 0)

        let nfa =
            Nfa.fromTransitions states transitions epsTransitions (Set.singleton 0) (Set.singleton (n - 1))

        let dfa = Automaton.toDfa nfa
        Assert.True(Dfa.stateCount dfa > 0)

    [<Properties(Arbitrary = [| typeof<StressNfaGenerators> |], MaxTest = 5)>]
    module StressNfaProperties =

        [<Property>]
        [<Trait("Category", "Stress")>]
        let ``toDfa terminates for large random NFAs`` (nfa: NFA<string, int>) =
            let dfa = Automaton.toDfa nfa
            Dfa.stateCount dfa > 0

module LRStress =

    let private multiLevelGrammar levelCount =
        let nonterms = [ for i in 1..levelCount -> sprintf "E%d" i ]
        let start = "E1"

        let productions =
            [ for i in 1 .. levelCount - 1 do
                  yield sprintf "%s -> %s + %s" (nonterms.[i - 1]) (nonterms.[i - 1]) (nonterms.[i])
                  yield sprintf "%s -> %s" (nonterms.[i - 1]) (nonterms.[i]) ]
            @ [ sprintf "%s -> ( E1 )" (List.last nonterms)
                sprintf "%s -> x" (List.last nonterms) ]

        sprintf "%s\n" (System.String.Join("\n", productions)) |> Grammar.parseGrammar

    [<Fact>]
    [<Trait("Category", "Stress")>]
    let ``LR0 automaton for 35-level expression grammar has 100+ states`` () =
        let grammar = multiLevelGrammar 35
        let freshStart = Nonterminal(grammar.Start |> fun (Nonterminal n) -> n + "'")
        let aug = LRAutomaton.augmentGrammar freshStart grammar
        let dfa = LRAutomaton.buildLR0 aug
        let stateCount = Dfa.stateCount dfa
        Assert.True(stateCount >= 100, sprintf "Expected >= 100 states, got %d" stateCount)

    [<Fact>]
    [<Trait("Category", "Stress")>]
    let ``LR table for 35-level expression grammar builds successfully`` () =
        let grammar = multiLevelGrammar 35
        let freshStart = Nonterminal(grammar.Start |> fun (Nonterminal n) -> n + "'")
        let aug = LRAutomaton.augmentGrammar freshStart grammar
        let table = LRParser.buildLR0Table aug Grammar.eoiSymbol
        Assert.True(Map.count table.Action > 0)
        Assert.True(Map.count table.GoTo > 0)
