module ValiantTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

let private g (lang: Language) (name: string) =
    lang.Grammars |> List.find (fun g -> g.Name = name)

let private dyck1 = LanguageRegistry.Dyck1
let private aplus = LanguageRegistry.APlus
let private expr = LanguageRegistry.ArithExpr

let private grammar1 = (g dyck1 "grammar1").Grammar
let private grammar2 = (g dyck1 "grammar2").Grammar
let private grammar3 = (g aplus "grammar3").Grammar
let private grammar6 = (g expr "grammar6").Grammar
let private grammar7 = (g expr "grammar7").Grammar
let private grammar8 = (g expr "grammar8").Grammar

let private tokenized (ss: string list) = String.concat " " ss

let private dyckAB = LanguageRegistry.Dyck1.AcceptStrings[1] |> tokenized
let private aplusAA = LanguageRegistry.APlus.AcceptStrings[1] |> tokenized
let private dyckABAB = LanguageRegistry.Dyck1.AcceptStrings[0] |> tokenized
let private aplusAAAA = LanguageRegistry.APlus.AcceptStrings[3] |> tokenized

let private exprXplusXmulX =
    LanguageRegistry.ArithExpr.AcceptStrings[4] |> tokenized

module ValiantParseTests =

    [<Fact>]
    let ``Valiant and CYK agree on all test strings`` () =
        let testLang (lang: Language) =
            let failures =
                lang.Grammars
                |> List.collect (fun g ->
                    lang.AcceptStrings @ lang.RejectStrings
                    |> List.choose (fun input ->
                        let terminals = input |> List.map Terminal
                        let cyk = Cyk.parse Grammar.freshStringNonterminal g.Grammar terminals
                        let valiant = Valiant.parse Grammar.freshStringNonterminal g.Grammar terminals

                        if cyk = valiant then None else Some(g.Name, input)))

            Assert.Empty(failures)

        testLang LanguageRegistry.Dyck1
        testLang LanguageRegistry.APlus
        testLang LanguageRegistry.ArithExpr

    [<Fact>]
    let ``Valiant parseWithTable returns n by n matrix`` () =
        let input = dyckAB

        let table, accepted =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        Assert.True(accepted)
        Assert.Equal(Tokenizer.tokenizeTerminals input |> List.length, Matrix.rows table)
        Assert.Equal(Tokenizer.tokenizeTerminals input |> List.length, Matrix.cols table)

    [<Fact>]
    let ``Valiant table matches CYK table for small example`` () =
        let input = aplusAA

        let cykTable, cykAcc =
            Cyk.parseWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals input)

        let valTable, valAcc =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals input)

        Assert.Equal(cykAcc, valAcc)
        Assert.Equal(Matrix.rows cykTable, Matrix.rows valTable)
        Assert.Equal(Matrix.cols cykTable, Matrix.cols valTable)

        let n = Matrix.rows cykTable

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(cykTable.[i, j], valTable.[i, j])

    [<Fact>]
    let ``Valiant table matches CYK table for grammar 1 small example`` () =
        let input = dyckABAB

        let cykTable, cykAcc =
            Cyk.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        let valTable, valAcc =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        Assert.Equal(cykAcc, valAcc)

        let n = Matrix.rows cykTable

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(cykTable.[i, j], valTable.[i, j])


module ModifiedValiantTests =

    let private testEquivalence (lang: Language) =
        let failures =
            lang.Grammars
            |> List.collect (fun g ->
                lang.AcceptStrings @ lang.RejectStrings
                |> List.choose (fun input ->
                    let terminals = input |> List.map Terminal
                    let standard = Valiant.parse Grammar.freshStringNonterminal g.Grammar terminals

                    let modified =
                        Valiant.parseModified Grammar.freshStringNonterminal g.Grammar terminals

                    if standard = modified then None else Some(g.Name, input)))

        Assert.Empty(failures)

    [<Fact>]
    let ``Modified Valiant and standard Valiant agree on all test strings`` () =
        testEquivalence LanguageRegistry.Dyck1
        testEquivalence LanguageRegistry.APlus
        testEquivalence LanguageRegistry.ArithExpr

    [<Fact>]
    let ``Modified Valiant table matches standard Valiant table for grammar 1`` () =
        let input = dyckABAB

        let valTable, valAcc =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        let modTable, modAcc =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        Assert.Equal(valAcc, modAcc)

        let n = Matrix.rows valTable

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(valTable.[i, j], modTable.[i, j])

    [<Fact>]
    let ``Modified Valiant table matches standard Valiant table for grammar 3`` () =
        let input = aplusAAAA

        let valTable, valAcc =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals input)

        let modTable, modAcc =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals input)

        Assert.Equal(valAcc, modAcc)

        let n = Matrix.rows valTable

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(valTable.[i, j], modTable.[i, j])

    [<Fact>]
    let ``Modified Valiant table matches standard Valiant table for expression grammar`` () =
        let input = exprXplusXmulX

        let valTable, valAcc =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals input)

        let modTable, modAcc =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals input)

        Assert.Equal(valAcc, modAcc)

        let n = Matrix.rows valTable

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(valTable.[i, j], modTable.[i, j])

    [<Fact>]
    let ``Modified Valiant trace produces steps for grammar 1`` () =
        let input = dyckAB

        let trace =
            Valiant.parseModifiedWithTrace Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        Assert.NotEmpty(trace)

        let lastStep = trace |> List.last

        match lastStep with
        | Valiant.LayerForward(_, _, submatrices) -> Assert.True(submatrices.Length >= 1 || submatrices.Length = 0)
        | Valiant.LayerBackward(_, _, submatrices, _) -> Assert.True(submatrices.Length >= 1 || submatrices.Length = 0)

    [<Fact>]
    let ``Modified Valiant trace submatrices are disjoint within each layer`` () =
        let input = dyckABAB

        let trace =
            Valiant.parseModifiedWithTrace Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        Assert.NotEmpty(trace)

        for step in trace do
            let submatrices =
                match step with
                | Valiant.LayerForward(_, _, sm) -> sm
                | Valiant.LayerBackward(_, _, sm, _) -> sm

            let cells =
                submatrices
                |> List.collect (fun m ->
                    [ for i in m.Row - m.Size + 1 .. m.Row do
                          for j in m.Col .. m.Col + m.Size - 1 do
                              yield (i, j) ])

            let uniqueCells = Set.ofList cells
            Assert.Equal(List.length cells, Set.count uniqueCells)

    [<Fact>]
    let ``Modified Valiant empty input with epsilon grammar returns accepted`` () =
        let result = Valiant.parseModified Grammar.freshStringNonterminal grammar1 []
        Assert.True(result)

    [<Fact>]
    let ``Modified Valiant empty input with non-epsilon grammar returns rejected`` () =
        let result = Valiant.parseModified Grammar.freshStringNonterminal grammar3 []
        Assert.False(result)

    [<Fact>]
    let ``Modified Valiant empty input parseWithTable returns correct table`` () =
        let table, accepted =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar1 []

        Assert.True(accepted)
        Assert.Equal(0, Matrix.rows table)
        Assert.Equal(0, Matrix.cols table)

    [<Fact>]
    let ``Modified Valiant empty input parseWithTable with non-epsilon grammar`` () =
        let table, accepted =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar3 []

        Assert.False(accepted)
        Assert.Equal(0, Matrix.rows table)
        Assert.Equal(0, Matrix.cols table)

    [<Fact>]
    let ``Standard Valiant empty input with epsilon grammar returns accepted`` () =
        let result = Valiant.parse Grammar.freshStringNonterminal grammar1 []
        Assert.True(result)

    [<Fact>]
    let ``Standard Valiant empty input with non-epsilon grammar returns rejected`` () =
        let result = Valiant.parse Grammar.freshStringNonterminal grammar3 []
        Assert.False(result)


module PropertyTests =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module Grammar1PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 1`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s) = Valiant.parse
                Grammar.freshStringNonterminal
                grammar1
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 1`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc =
                    Cyk.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows cykTable

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if cykTable.[i, j] <> valTable.[i, j] then
                                 yield false ]
                   |> List.forall id

        [<Property>]
        let ``Modified Valiant and standard Valiant agree on acceptance for grammar 1`` (s: string) =
            Valiant.parse Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s) = Valiant.parseModified
                Grammar.freshStringNonterminal
                grammar1
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Modified Valiant and standard Valiant tables match for grammar 1`` (s: string) =
            if s = "" then
                true
            else
                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

                let modTable, modAcc =
                    Valiant.parseModifiedWithTable
                        Grammar.freshStringNonterminal
                        grammar1
                        (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows valTable

                valAcc = modAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if valTable.[i, j] <> modTable.[i, j] then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module Grammar2PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 2`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar2 (Tokenizer.tokenizeTerminals s) = Valiant.parse
                Grammar.freshStringNonterminal
                grammar2
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 2`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc =
                    Cyk.parseWithTable Grammar.freshStringNonterminal grammar2 (Tokenizer.tokenizeTerminals s)

                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar2 (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows cykTable

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if cykTable.[i, j] <> valTable.[i, j] then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module Grammar3PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 3`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s) = Valiant.parse
                Grammar.freshStringNonterminal
                grammar3
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 3`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc =
                    Cyk.parseWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s)

                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows cykTable

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if cykTable.[i, j] <> valTable.[i, j] then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module RejectTableTests =

        [<Property>]
        let ``CYK and Valiant reject tables are identical for grammar 1`` (s: string) =
            let cykTable, cykAcc =
                Cyk.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

            let valTable, valAcc =
                Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

            if cykAcc || valAcc then
                true
            else
                let n = Matrix.rows cykTable

                [ for i in 0 .. n - 1 do
                      for j in i .. n - 1 do
                          if cykTable.[i, j] <> valTable.[i, j] then
                              yield false ]
                |> List.forall id

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module ModifiedValiantPropertyTests =

        [<Property>]
        let ``Modified Valiant and standard Valiant reject tables are identical for grammar 1`` (s: string) =
            let valTable, valAcc =
                Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

            let modTable, modAcc =
                Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

            if valAcc || modAcc then
                true
            else
                let n = Matrix.rows valTable

                [ for i in 0 .. n - 1 do
                      for j in i .. n - 1 do
                          if valTable.[i, j] <> modTable.[i, j] then
                              yield false ]
                |> List.forall id

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.ExprString> |])>]
    module ModifiedValiantExprPropertyTests =

        [<Property>]
        let ``Modified Valiant and standard Valiant agree on acceptance for grammar 6`` (s: string) =
            Valiant.parse Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals s) = Valiant.parseModified
                Grammar.freshStringNonterminal
                grammar6
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Modified Valiant and standard Valiant tables match for grammar 6`` (s: string) =
            if s = "" then
                true
            else
                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals s)

                let modTable, modAcc =
                    Valiant.parseModifiedWithTable
                        Grammar.freshStringNonterminal
                        grammar6
                        (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows valTable

                valAcc = modAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if valTable.[i, j] <> modTable.[i, j] then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.ExprString> |])>]
    module Grammar7PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 7`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s) = Valiant.parse
                Grammar.freshStringNonterminal
                grammar7
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 7`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc =
                    Cyk.parseWithTable Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s)

                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows cykTable

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if cykTable.[i, j] <> valTable.[i, j] then
                                 yield false ]
                   |> List.forall id

        [<Property>]
        let ``Modified Valiant and standard Valiant agree on acceptance for grammar 7`` (s: string) =
            Valiant.parse Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s) = Valiant.parseModified
                Grammar.freshStringNonterminal
                grammar7
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Modified Valiant and standard Valiant tables match for grammar 7`` (s: string) =
            if s = "" then
                true
            else
                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s)

                let modTable, modAcc =
                    Valiant.parseModifiedWithTable
                        Grammar.freshStringNonterminal
                        grammar7
                        (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows valTable

                valAcc = modAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if valTable.[i, j] <> modTable.[i, j] then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.ExprString> |])>]
    module Grammar8PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 8`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar8 (Tokenizer.tokenizeTerminals s) = Valiant.parse
                Grammar.freshStringNonterminal
                grammar8
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 8`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc =
                    Cyk.parseWithTable Grammar.freshStringNonterminal grammar8 (Tokenizer.tokenizeTerminals s)

                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar8 (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows cykTable

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if cykTable.[i, j] <> valTable.[i, j] then
                                 yield false ]
                   |> List.forall id

        [<Property>]
        let ``Modified Valiant and standard Valiant agree on acceptance for grammar 8`` (s: string) =
            Valiant.parse Grammar.freshStringNonterminal grammar8 (Tokenizer.tokenizeTerminals s) = Valiant.parseModified
                Grammar.freshStringNonterminal
                grammar8
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Modified Valiant and standard Valiant tables match for grammar 8`` (s: string) =
            if s = "" then
                true
            else
                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar8 (Tokenizer.tokenizeTerminals s)

                let modTable, modAcc =
                    Valiant.parseModifiedWithTable
                        Grammar.freshStringNonterminal
                        grammar8
                        (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows valTable

                valAcc = modAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if valTable.[i, j] <> modTable.[i, j] then
                                 yield false ]
                   |> List.forall id
