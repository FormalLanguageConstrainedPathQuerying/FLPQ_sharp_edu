module SppfTikzTests

open System.Text.RegularExpressions
open Xunit
open FLPQ.GraphAnalysis
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

/// Standalone range node label: math-mode range with subscript indices (task 273).
let private rangeLabelPattern =
    Regex(@"as=\{\$\[s_\{\d+\},v_\{\d+\}\]\\to\[s_\{\d+\},v_\{\d+\}\]\$\}")

/// Intermediate node label: I_{m,p} prefix plus the math-mode range (task 273).
let private intermediateLabelPattern =
    Regex(@"as=\{\$I_\{\d+,\d+\}\$ @ \$\[s_\{\d+\},v_\{\d+\}\]\\to\[s_\{\d+\},v_\{\d+\}\]\$\}")

/// Pre-task-273 formats: brace-less range and I(m,p) prefix.
let private oldRangePattern = Regex(@"\[s\d+,v\d+\]")
let private oldIntermediatePattern = Regex(@"I\(\d+,\d+\)")

/// The 4-token ANBN accept string ("a a b b") from the registry.
let private anbnInput: string list =
    LanguageRegistry.ANBN.AcceptStrings
    |> List.find (fun tokens -> List.length tokens = 4)
    |> List.map (fun (Terminal t) -> t)

/// SPPF of ANBN classic (S -> a S b | eps) over the 4-token accept string,
/// rendered to TikZ. Contains range nodes (root + packed alternatives) and
/// intermediate nodes from the two nested `a S b` derivations.
let private classicSppfTikz () : (SPPF<string, string> * string) =
    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm
    let input = anbnInput
    let graph = GLL.stringToGraph input
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let vertexCount = Graph.vertexCount graph
    let pathIndex, _ = GLL.buildPathIndexWithSteps freshStart ersm graph
    let sppf = Sppf.buildSppfFromExtendedRsm pathIndex ersm.ExtendedRsm vertexCount
    sppf, SppfTikz.toTikz string string sppf

let private nodeCount (sppf: SPPF<string, string>) (pred: SppfNodeInfo<string, string> -> bool) : int =
    let count = Graph.vertexCount sppf.Graph

    [ for i in 0 .. count - 1 do
          if pred (Graph.getVertex i sppf.Graph) then 1 else 0 ]
    |> List.sum

[<Fact>]
let ``range nodes render as math-mode ranges with subscript indices`` () =
    let sppf, tikz = classicSppfTikz ()

    let expected =
        nodeCount sppf (function
            | SppfNodeInfo.SppfRange _ -> true
            | _ -> false)

    Assert.True(expected > 0, "test SPPF must contain range nodes")
    Assert.Equal(expected, rangeLabelPattern.Matches(tikz).Count)

[<Fact>]
let ``intermediate nodes render as I_{m,p} with math-mode subscript indices`` () =
    let sppf, tikz = classicSppfTikz ()

    let expected =
        nodeCount sppf (function
            | SppfNodeInfo.SppfIntermediate _ -> true
            | _ -> false)

    Assert.True(expected > 0, "test SPPF must contain intermediate nodes")
    Assert.Equal(expected, intermediateLabelPattern.Matches(tikz).Count)

[<Fact>]
let ``pre-task-273 label formats are absent`` () =
    let _, tikz = classicSppfTikz ()
    Assert.False(oldRangePattern.IsMatch(tikz), $"old range format found:\n{tikz}")
    Assert.False(oldIntermediatePattern.IsMatch(tikz), $"old intermediate format found:\n{tikz}")
