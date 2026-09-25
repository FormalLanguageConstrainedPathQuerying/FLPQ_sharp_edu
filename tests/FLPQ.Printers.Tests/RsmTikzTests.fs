module RsmTikzTests

open System
open System.Text.RegularExpressions
open Xunit
open FSharpPlus.Data
open FLPQ.GraphAnalysis
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.TestUtilities

let private declPattern = Regex(@"s(\d+) \[")
let private edgePattern = Regex(@"s\d+ ->")

/// Node declaration with its full option list (edge lines never match: they have no space before `[`).
let private nodeDeclPattern = Regex(@"s(\d+) \[([^\]]*)\];")

/// Number of state declarations carrying the `double` option.
/// The word-boundary match counts `double distance=1.5pt` as part of the same option.
let private doubleCircledCount (tikz: string) : int =
    [ for m in nodeDeclPattern.Matches(tikz) do
          if Regex.IsMatch(m.Groups.[2].Value, @"(^|,\s*)double(\s*,|$)") then
              1
          else
              0 ]
    |> List.sum

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

[<Fact>]
let ``RSM tikz epsilon edge label is math mode and compiles with lualatex`` () =
    // EBNF-derived RSMs encode epsilon as "start state is final" (no AEpsilon edges),
    // so build a minimal RSM with an explicit epsilon transition to cover the
    // AEpsilon rendering arm (task 269).
    let transitions = Matrix.init 2 2 None
    transitions.[0, 1] <- Some(NonEmptySet.singleton AutomatonLabel.AEpsilon)

    let blockStart = System.Collections.Generic.Dictionary<Nonterminal<string>, int>()
    blockStart.[Nonterminal "S"] <- 0

    let rsm =
        { Transitions = transitions
          StateCount = 2
          StateInfo =
            [| { BlockNonterminal = Nonterminal "S"
                 LocalState = 0
                 IsFinal = false }
               { BlockNonterminal = Nonterminal "S"
                 LocalState = 1
                 IsFinal = true } |]
          BlockStart = blockStart
          FinalStates = set [ 1 ]
          StartBlock = Nonterminal "S" }

    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let tikz = RsmTikz.extendedRsmToTikz string string ersm None

    // The epsilon label must be math-mode TeX, not escaped literal source.
    Assert.Contains(@"""$\varepsilon$"", dotted", tikz)
    Assert.DoesNotContain(@"\textbackslash varepsilon", tikz)

    let tikzTemplatePath =
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
let ``RSM tikz state content is the bare global index`` () =
    for entry in
        [ (LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm
          (LanguageRegistry.findGrammar LanguageRegistry.ArithExpr "leftAssoc").Rsm ] do
        let freshStart = Nonterminal "S'"
        let ersm = ExtendedRSM.create freshStart entry
        let rsm = ersm.ExtendedRsm
        let tikz = RsmTikz.extendedRsmToTikz string string ersm None

        let decls =
            [ for m in nodeDeclPattern.Matches(tikz) do
                  Int32.Parse(m.Groups.[1].Value), m.Groups.[2].Value ]

        Assert.Equal(rsm.StateCount, decls.Length)

        for idx, opts in decls do
            let contentMatch = Regex.Match(opts, @"^as=\{(\d+)\}")
            Assert.True(contentMatch.Success, sprintf "state %d: no numeric content in %s" idx opts)
            Assert.Equal(idx, Int32.Parse(contentMatch.Groups.[1].Value))

[<Fact>]
let ``RSM tikz nonterminal edges carry the bare nonterminal name`` () =
    for entry in
        [ (LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm
          (LanguageRegistry.findGrammar LanguageRegistry.ArithExpr "leftAssoc").Rsm
          (LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "ebnfStar").Rsm ] do
        let freshStart = Nonterminal "S'"
        let ersm = ExtendedRSM.create freshStart entry
        let rsm = ersm.ExtendedRsm
        let tikz = RsmTikz.extendedRsmToTikz string string ersm None

        Assert.DoesNotContain("call ", tikz)

        // Every nonterminal that labels a transition appears as a bare edge label.
        for nt in TestHelpers.callLabeledNonterminals rsm do
            let (Nonterminal ntName) = nt
            Assert.Contains(sprintf "->[\"%s\"" ntName, tikz)

[<Fact>]
let ``RSM tikz highlighted final state stays double circled with lightblue fill`` () =
    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let ext = ersm.ExtendedRsm
    // Covers highlighted start+final, plain final, and S' final states.
    for f in Set.toList ext.FinalStates do
        let tikz = RsmTikz.extendedRsmToTikz string string ersm (Some f)

        let declLine =
            tikz.Split('\n') |> Array.find (fun l -> l.Trim().StartsWith(sprintf "s%d [" f))

        Assert.Contains("double", declLine)
        Assert.Contains("fill=lightblue!20", declLine)
        Assert.Equal(Set.count ext.FinalStates, doubleCircledCount tikz)

/// Render every GLL step's RSM TikZ for the given grammar and input.
let private renderGllStepRsmTikz
    (rsm: RSM<string, string>)
    (input: string list)
    : ExtendedRSM<string, string> * string list =
    let freshStart = Nonterminal "S'"
    let graph = GLL.stringToGraph input
    let vertexCount = Graph.vertexCount graph
    let ersm = ExtendedRSM.create freshStart rsm
    let pathIndex, steps = GLL.buildPathIndexWithSteps freshStart ersm graph

    let vizSteps =
        GllStepVisualizer.renderSteps string string ersm steps pathIndex vertexCount graph

    (ersm, vizSteps |> List.map (fun s -> s.RsmTikz))

[<Fact>]
let ``every GLL step RSM tikz has exactly the final states doubly circled`` () =
    for rsm, input in
        [ ((LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm, [ "a"; "a"; "b"; "b" ])
          ((LanguageRegistry.findGrammar LanguageRegistry.DoubleA "singleRule").Rsm, [ "a"; "a" ])
          ((LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "ebnfStar").Rsm, [ "a"; "b"; "a"; "b" ])
          (LanguageRegistry.APlus.Grammars.[0].Rsm, [ "a"; "a" ]) ] do
        let ersm, tikzs = renderGllStepRsmTikz rsm input

        Assert.NotEmpty tikzs

        let expected = Set.count ersm.ExtendedRsm.FinalStates

        for i, tikz in List.indexed tikzs do
            let actual = doubleCircledCount tikz
            // Bind the comparison: `Assert.True(actual = expected, msg)` would parse
            // `actual = expected` as a named argument (FS0691).
            let matches = actual = expected

            Assert.True(
                matches,
                sprintf "step %d: doubly circled count %d must equal the final-state count %d" i actual expected
            )
