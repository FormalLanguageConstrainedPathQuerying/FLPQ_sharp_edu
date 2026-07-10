module TokenizerTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.TestUtilities

module TokenizeStringsTests =

    [<Fact>]
    let ``null returns empty`` () =
        Assert.Empty(Tokenizer.tokenizeStrings null)

    [<Fact>]
    let ``empty string returns empty`` () =
        Assert.Empty(Tokenizer.tokenizeStrings "")

    [<Fact>]
    let ``whitespace only returns empty`` () =
        Assert.Empty(Tokenizer.tokenizeStrings "   ")
        Assert.Empty(Tokenizer.tokenizeStrings "\t")
        Assert.Empty(Tokenizer.tokenizeStrings " \t  ")

    [<Fact>]
    let ``single terminal`` () =
        Assert.Equal<string>([ "a" ], Tokenizer.tokenizeStrings "a")

    [<Fact>]
    let ``multiple terminals`` () =
        Assert.Equal<string>([ "a"; "b"; "c" ], Tokenizer.tokenizeStrings "a b c")

    [<Fact>]
    let ``multi-character terminals`` () =
        Assert.Equal<string>([ "ab"; "cde"; "f" ], Tokenizer.tokenizeStrings "ab cde f")

    [<Fact>]
    let ``leading spaces`` () =
        Assert.Equal<string>([ "a"; "b" ], Tokenizer.tokenizeStrings "  a b")

    [<Fact>]
    let ``trailing spaces`` () =
        Assert.Equal<string>([ "a"; "b" ], Tokenizer.tokenizeStrings "a b  ")

    [<Fact>]
    let ``multiple spaces between terminals`` () =
        Assert.Equal<string>([ "a"; "b" ], Tokenizer.tokenizeStrings "a   b")

    [<Fact>]
    let ``tabs are NOT separators`` () =
        Assert.Equal<string>([ "a\tb" ], Tokenizer.tokenizeStrings "a\tb")

    [<Fact>]
    let ``newlines are NOT separators`` () =
        Assert.Equal<string>([ "a\nb" ], Tokenizer.tokenizeStrings "a\nb")

    [<Fact>]
    let ``mixed whitespace — only spaces split`` () =
        Assert.Equal<string>([ "a"; "b\nc" ], Tokenizer.tokenizeStrings "a  b\nc")

module TokenizeTests =

    [<Fact>]
    let ``empty string returns empty`` () = Assert.Empty(Tokenizer.tokenize "")

    [<Fact>]
    let ``single terminal returns one symbol`` () =
        let result = Tokenizer.tokenize "x"

        Assert.Equal<Terminal<string>>(
            Terminal "x",
            match result with
            | [ Symbol.T t ] -> t
            | _ -> failwith "wrong"
        )

    [<Fact>]
    let ``multiple terminals`` () =
        let result = Tokenizer.tokenize "a b c"
        Assert.Equal(3, List.length result)

        match result with
        | [ Symbol.T(Terminal "a"); Symbol.T(Terminal "b"); Symbol.T(Terminal "c") ] -> ()
        | _ -> failwith "wrong terminals"

    [<Fact>]
    let ``whitespace only`` () =
        Assert.Empty(Tokenizer.tokenize "  \t ")

module TokenizeTerminalsTests =

    [<Fact>]
    let ``empty string returns empty`` () =
        Assert.Empty(Tokenizer.tokenizeTerminals "")

    [<Fact>]
    let ``single terminal`` () =
        Assert.Equal<Terminal<string> list>([ Terminal "x" ], Tokenizer.tokenizeTerminals "x")

    [<Fact>]
    let ``multiple terminals`` () =
        Assert.Equal<Terminal<string> list>([ Terminal "a"; Terminal "b" ], Tokenizer.tokenizeTerminals "a b")

module TerminalsToSymbolsTests =

    [<Fact>]
    let ``empty list returns empty`` () =
        Assert.Empty(Tokenizer.terminalsToSymbols<int, string> [])

    [<Fact>]
    let ``wraps terminals as T symbols`` () =
        let result = Tokenizer.terminalsToSymbols [ Terminal 1; Terminal 2 ]
        Assert.Equal<Symbol<int, string> list>([ Symbol.T(Terminal 1); Symbol.T(Terminal 2) ], result)

module TokenizeGenTests =

    [<Fact>]
    let ``custom classifier`` () =
        let classify (s: string) : Symbol<string, string> =
            if s.Length > 0 && System.Char.IsUpper(s.[0]) then
                Symbol.N(Nonterminal s)
            else
                Symbol.T(Terminal s)

        let result = Tokenizer.tokenizeGen classify "S a b"

        Assert.Equal<Symbol<string, string> list>(
            [ Symbol.N(Nonterminal "S"); Symbol.T(Terminal "a"); Symbol.T(Terminal "b") ],
            result
        )

    [<Fact>]
    let ``identity classifier wraps all as terminals`` () =
        let result = Tokenizer.tokenizeGen (fun s -> Symbol.T(Terminal s)) "a b c"

        Assert.Equal<Symbol<string, string> list>(
            [ Symbol.T(Terminal "a"); Symbol.T(Terminal "b"); Symbol.T(Terminal "c") ],
            result
        )

module PropertyTests =

    type TokenStringGenerators =

        static member TokenString() : Arbitrary<string> =
            FsCheck.FSharp.Gen.choose (0, 5)
            |> FsCheck.FSharp.Gen.bind (fun n ->
                FsCheck.FSharp.Gen.listOfLength n (FsCheck.FSharp.Gen.elements [ "a"; "b"; "c" ]))
            |> FsCheck.FSharp.Gen.map (String.concat " ")
            |> FsCheck.FSharp.Arb.fromGen

    [<Properties(Arbitrary = [| typeof<TokenStringGenerators> |])>]
    module TokenizeStringsProperties =

        [<Property>]
        let ``tokenizeStrings result has no empty tokens`` (s: string) =
            Tokenizer.tokenizeStrings s |> List.forall (fun t -> t.Length > 0)

        [<Property>]
        let ``tokenizeStrings of joined tokens roundtrips`` (s: string) =
            let tokens = Tokenizer.tokenizeStrings s
            let rejoined = String.concat " " tokens

            if s.Trim().Length = 0 then
                List.isEmpty (Tokenizer.tokenizeStrings rejoined)
            else
                Tokenizer.tokenizeStrings rejoined = tokens

        [<Property>]
        let ``tokenizeStrings is idempotent`` (s: string) =
            let tokens = Tokenizer.tokenizeStrings s
            let rejoined = String.concat " " tokens
            Tokenizer.tokenizeStrings rejoined = tokens

    [<Properties(Arbitrary = [| typeof<TokenStringGenerators> |])>]
    module TokenizeProperties =

        [<Property>]
        let ``tokenizeTerminals is consistent with tokenizeStrings`` (s: string) =
            let strings = Tokenizer.tokenizeStrings s
            let terminals = Tokenizer.tokenizeTerminals s
            let expected = strings |> List.map Terminal
            terminals = expected

        [<Property>]
        let ``tokenize count equals tokenizeStrings count`` (s: string) =
            (Tokenizer.tokenize s |> List.length) = (Tokenizer.tokenizeStrings s |> List.length)
