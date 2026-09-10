module RsmTikzTests

open System
open System.Text.RegularExpressions
open Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

let private declPattern = Regex(@"s(\d+) \[")
let private edgePattern = Regex(@"s\d+ ->")

/// Global state indices of the block for the given nonterminal.
let private blockStates (nt: Nonterminal<string>) (rsm: RSM<string, string>) : int list =
    [ for i in 0 .. rsm.StateCount - 1 do
          if rsm.StateInfo.[i].BlockNonterminal = nt then
              i ]

/// Declaration positions (character offsets) of the given states in the TikZ output.
let private declPositions (tikz: string) (states: int list) : int list =
    let all =
        [ for m in declPattern.Matches(tikz) do
              Int32.Parse(m.Groups.[1].Value), m.Index ]

    all |> List.filter (fun (i, _) -> List.contains i states) |> List.map snd

let private transitionSymbolCount (rsm: RSM<string, string>) : int =
    [ for i in 0 .. rsm.StateCount - 1 do
          for j in 0 .. rsm.StateCount - 1 do
              match rsm.Transitions.[i, j] with
              | Some labels -> NonEmptySet.toSeq labels |> Seq.length
              | None -> 0 ]
    |> List.sum

[<Fact>]
let ``RSM tikz uses top-to-bottom component packing in a single graph`` () =
    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let tikz = RsmTikz.extendedRsmToTikz string string ersm None

    Assert.Contains("components go down left aligned", tikz)
    Assert.Equal(1, Regex.Matches(tikz, @"\\graph").Count)

[<Fact>]
let ``RSM tikz declares every state exactly once`` () =
    for entry in
        [ (LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm
          (LanguageRegistry.findGrammar LanguageRegistry.ArithExpr "leftAssoc").Rsm ] do
        let freshStart = Nonterminal "S'"
        let ersm = ExtendedRSM.create freshStart entry
        let rsm = ersm.ExtendedRsm
        let tikz = RsmTikz.extendedRsmToTikz string string ersm None

        let declared =
            [ for m in declPattern.Matches(tikz) do
                  Int32.Parse(m.Groups.[1].Value) ]

        Assert.Equal(rsm.StateCount, declared.Length)
        Assert.Equal<int list>([ 0 .. rsm.StateCount - 1 ], List.sort declared)

[<Fact>]
let ``RSM tikz declares the S' block before all other blocks`` () =
    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.ArithExpr "leftAssoc").Rsm
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let ext = ersm.ExtendedRsm
    let tikz = RsmTikz.extendedRsmToTikz string string ersm None

    let sPrimeStates = blockStates freshStart ext

    let otherStates =
        [ for i in 0 .. ext.StateCount - 1 do
              if not (List.contains i sPrimeStates) then
                  i ]

    Assert.NotEmpty(sPrimeStates)
    Assert.NotEmpty(otherStates)

    let sPrimePositions = declPositions tikz sPrimeStates
    let otherPositions = declPositions tikz otherStates

    Assert.True(List.max sPrimePositions < List.min otherPositions, "S' block must be declared first")

[<Fact>]
let ``RSM tikz labels every block start state with the nonterminal name`` () =
    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.ArithExpr "leftAssoc").Rsm
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let ext = ersm.ExtendedRsm
    let tikz = RsmTikz.extendedRsmToTikz string string ersm None

    for nt in RSM.nonterminals ext do
        let startGlobal = ext.BlockStart.[nt]
        let (Nonterminal ntName) = nt

        let declLine =
            tikz.Split('\n')
            |> Array.tryFind (fun l -> l.Trim().StartsWith(sprintf "s%d [" startGlobal))

        Assert.True(declLine.IsSome, sprintf "declaration for state %d not found" startGlobal)
        Assert.Contains(sprintf "label=left:%s" ntName, declLine.Value)

[<Fact>]
let ``RSM tikz emits one edge line per transition symbol`` () =
    for entry in
        [ (LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm
          (LanguageRegistry.findGrammar LanguageRegistry.ArithExpr "leftAssoc").Rsm
          (LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "ebnfStar").Rsm ] do
        let freshStart = Nonterminal "S'"
        let ersm = ExtendedRSM.create freshStart entry
        let rsm = ersm.ExtendedRsm
        let tikz = RsmTikz.extendedRsmToTikz string string ersm None

        Assert.Equal(transitionSymbolCount rsm, edgePattern.Matches(tikz).Count)
