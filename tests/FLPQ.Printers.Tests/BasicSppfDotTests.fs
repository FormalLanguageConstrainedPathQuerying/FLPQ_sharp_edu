module BasicSppfDotTests

open Xunit
open FLPQ.Languages
open FLPQ.Languages.BasicSppf
open FLPQ.Printers

let private tikzTemplatePath =
    System.IO.Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``simple SPPF dot compiles`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

    let edges = [ (0, 1); (1, 2); (1, 3) ]

    let sppf = BasicSppf.fromEdges vertices edges 0
    let dot = BasicSppfDot.toDot string string sppf

    Assert.Contains("digraph BasicSPPF", dot)
    Assert.Contains("S [0,2]", dot)
    Assert.Contains("a_{0,1}", dot)
    Assert.Contains("b_{1,2}", dot)
    Assert.Contains("0, 0", dot)
    Assert.Contains("shape=rectangle", dot)
    Assert.Contains("shape=circle", dot)
    Assert.Contains("shape=oval", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(4, info.NodeCount)
    Assert.Equal(3, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``SPPF dot with epsilon compiles`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 0)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Epsilon 0 ]

    let edges = [ (0, 1); (1, 2) ]

    let sppf = BasicSppf.fromEdges vertices edges 0
    let dot = BasicSppfDot.toDot string string sppf

    Assert.Contains("varepsilon_{0}", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(3, info.NodeCount)
    Assert.Equal(2, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``SPPF dot root highlighted`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 1)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ]

    let edges = [ (0, 1); (1, 2) ]

    let sppf = BasicSppf.fromEdges vertices edges 0
    let dot = BasicSppfDot.toDot string string sppf

    Assert.Contains("fillcolor=lightgreen", dot)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``SPPF dot production node shows split and rule index`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

    let edges = [ (0, 1); (1, 2); (1, 3) ]

    let sppf = BasicSppf.fromEdges vertices edges 0
    let dot = BasicSppfDot.toDot string string sppf

    Assert.Contains("0, 0", dot)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``simple SPPF tikz compiles`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

    let edges = [ (0, 1); (1, 2); (1, 3) ]

    let sppf = BasicSppf.fromEdges vertices edges 0
    let tikz = BasicSppfTikz.toTikz string string sppf

    Assert.Contains(@"\begin{tikzpicture}", tikz)
    Assert.Contains(@"\graph [layered layout", tikz)
    Assert.Contains("grow'=down", tikz)
    Assert.Contains("S [0,2]", tikz)
    Assert.Contains("0, 0", tikz)
    Assert.Contains("fill=green!30", tikz)
    Assert.Contains("n0", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``SPPF tikz with epsilon compiles`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 0)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Epsilon 0 ]

    let edges = [ (0, 1); (1, 2) ]

    let sppf = BasicSppf.fromEdges vertices edges 0
    let tikz = BasicSppfTikz.toTikz string string sppf

    Assert.Contains("varepsilon", tikz)
    Assert.Contains("fill=green!30", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``SPPF tikz root highlighted`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 1)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ]

    let edges = [ (0, 1); (1, 2) ]

    let sppf = BasicSppf.fromEdges vertices edges 0
    let tikz = BasicSppfTikz.toTikz string string sppf

    Assert.Contains("fill=green!30", tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``SPPF tikz production node shows split and rule index`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

    let edges = [ (0, 1); (1, 2); (1, 3) ]

    let sppf = BasicSppf.fromEdges vertices edges 0
    let tikz = BasicSppfTikz.toTikz string string sppf

    Assert.Contains("0, 0", tikz)
