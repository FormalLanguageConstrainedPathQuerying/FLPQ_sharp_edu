module DerivationTreeVisualizationTests

open System
open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

let private checkDotCompiles (dot: string) : bool =
    let tempFile = Path.GetTempFileName()
    File.WriteAllText(tempFile, dot)

    try
        let processInfo = new Diagnostics.Process()
        processInfo.StartInfo.FileName <- "dot"
        processInfo.StartInfo.Arguments <- "-Tplain " + tempFile
        processInfo.StartInfo.RedirectStandardOutput <- true
        processInfo.StartInfo.RedirectStandardError <- true
        processInfo.StartInfo.UseShellExecute <- false
        processInfo.Start() |> ignore
        processInfo.WaitForExit(5000) |> ignore
        processInfo.ExitCode = 0
    finally
        File.Delete(tempFile)

[<Fact>]
let ``leaf tree dot compiles`` () =
    let tree = Leaf(T(Terminal "x"))
    let dot = DerivationTreeVisualizer.toDot string tree
    Assert.Contains("digraph DerivationTree", dot)
    Assert.Contains("shape=box", dot)
    Assert.True(checkDotCompiles dot)

[<Fact>]
let ``node with children dot compiles`` () =
    let tree =
        Node(Nonterminal "S", [ Leaf(T(Terminal "a")); Node(Nonterminal "B", [ Leaf(T(Terminal "b")) ]) ])

    let dot = DerivationTreeVisualizer.toDot string tree
    Assert.Contains("digraph DerivationTree", dot)
    Assert.True(checkDotCompiles dot)

[<Fact>]
let ``epsilon leaf dot compiles`` () =
    let tree = Node(Nonterminal "S", [ Leaf(Epsilon) ])
    let dot = DerivationTreeVisualizer.toDot string tree
    Assert.True(checkDotCompiles dot)

[<Fact>]
let ``LR parser tree dot compiles`` () =
    let grammar = Grammar.parseGrammar "S -> a S\nS -> a"
    let freshStart = Nonterminal(grammar.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart grammar
    let table = LRParser.buildSLR1Table aug

    match LRParser.parse aug table (Tokenizer.tokenize "a a") with
    | Some tree ->
        let dot = DerivationTreeVisualizer.toDot string tree
        Assert.True(checkDotCompiles dot)
    | None -> Assert.Fail("Failed to parse")
