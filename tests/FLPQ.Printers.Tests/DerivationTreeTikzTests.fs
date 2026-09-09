module DerivationTreeTikzTests

open System.IO
open System.Text.RegularExpressions
open Xunit
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

let private symbolPrinter = SymbolTeX.toLaTeX string string

let private tikzTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")

let private llSteps () : LLParsingStep<string, string> list =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar
    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    steps

let private lrSteps () : LRParsingStep<string, string> list =
    let g = LanguageRegistry.APlus.Grammars.[0].Grammar
    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol
    let tokens = Tokenizer.tokenizeTerminals "a a"
    let _, steps = LRParser.parseWithSteps aug table tokens
    steps

let private nodeIds (pattern: string) (content: string) : int list =
    Regex.Matches(content, pattern)
    |> Seq.map (fun m -> int m.Groups.[1].Value)
    |> List.ofSeq

[<Fact>]
let ``LL TikZ step contains tikzpicture, same-layer constraint and dashed chain`` () =
    let steps = llSteps ()
    let step = steps.[3]
    let tikz = DerivationTreeTikz.toTikzWithLLStack symbolPrinter step.Tree step.Stack

    Assert.Contains(@"\begin{tikzpicture}", tikz)
    Assert.Contains(@"\end{tikzpicture}", tikz)
    Assert.Contains("layered layout", tikz)
    Assert.Contains("{ [same layer]", tikz)
    Assert.Contains("->[dashed]", tikz)
    Assert.Contains("shape=ellipse", tikz)

[<Fact>]
let ``LR TikZ step contains gray state boxes and same-layer constraint`` () =
    let steps = lrSteps ()
    let step = steps.[3]
    let tikz = DerivationTreeTikz.toTikzWithLRStack symbolPrinter step.Stack

    Assert.Contains(@"\begin{tikzpicture}", tikz)
    Assert.Contains("layered layout", tikz)
    Assert.Contains("{ [same layer]", tikz)
    Assert.Contains("->[dashed]", tikz)
    Assert.Contains("fill=lightgray", tikz)
    Assert.Contains("as={s0}", tikz)

[<Fact>]
let ``LL TikZ node ids match DOT numbering`` () =
    let steps = llSteps ()
    let step = steps.[3]
    let dot = DerivationTreeDot.toDotWithLLStack symbolPrinter step.Tree step.Stack
    let tikz = DerivationTreeTikz.toTikzWithLLStack symbolPrinter step.Tree step.Stack

    let dotIds = nodeIds @"n(\d+) \[label=" dot
    let tikzIds = nodeIds @"n(\d+) \[as=" tikz

    Assert.NotEmpty(dotIds)
    Assert.Equal<int list>(dotIds, tikzIds)

[<Fact>]
let ``LR TikZ node ids match DOT numbering`` () =
    let steps = lrSteps ()
    let step = steps.[3]
    let dot = DerivationTreeDot.toDotWithLRStack symbolPrinter step.Stack
    let tikz = DerivationTreeTikz.toTikzWithLRStack symbolPrinter step.Stack

    let dotIds = nodeIds @"n(\d+) \[label=" dot
    let tikzIds = nodeIds @"n(\d+) \[as=" tikz

    Assert.NotEmpty(dotIds)
    Assert.Equal<int list>(dotIds, tikzIds)

[<Fact>]
let ``LL TikZ with empty stack renders plain tree without same-layer constraint`` () =
    let tree =
        DerivationTree.Node(Nonterminal "S", [ DerivationTree.Leaf(Symbol.T(Terminal "a")) ])

    let tikz = DerivationTreeTikz.toTikzWithLLStack symbolPrinter tree []

    Assert.Contains(@"\begin{tikzpicture}", tikz)
    Assert.DoesNotContain("{ [same layer]", tikz)
    Assert.DoesNotContain("dashed", tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LL TikZ step with mixed-depth frontier compiles with lualatex`` () =
    let steps = llSteps ()
    let step = steps.[3]
    let tikz = DerivationTreeTikz.toTikzWithLLStack symbolPrinter step.Tree step.Stack

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR TikZ step with subtree frame compiles with lualatex`` () =
    let steps = lrSteps ()
    let step = steps.[3]
    let tikz = DerivationTreeTikz.toTikzWithLRStack symbolPrinter step.Stack

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR TikZ single-frame step compiles with lualatex`` () =
    let steps = lrSteps ()
    let step = steps.[0]
    let tikz = DerivationTreeTikz.toTikzWithLRStack symbolPrinter step.Stack

    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

// --- Same-layer coordinate tests -------------------------------------------
// Extract node coordinates from a compiled TikZ picture via \typeout lines in
// the lualatex log, then assert the same-level stack frontier layout property.

let private sameLayerNames (tikz: string) : string list =
    let m = Regex.Match(tikz, @"\{ \[same layer\] (.+) \};")
    Assert.True(m.Success, "same-layer statement not found in generated TikZ")
    m.Groups.[1].Value.Split(',') |> Array.map (fun s -> s.Trim()) |> List.ofArray

let private dumpSameLayerCoords (tikz: string) : Map<string, float * float> =
    let dumps =
        sameLayerNames tikz
        |> List.map (fun n ->
            sprintf
                @"  \makeatletter\pgfpointanchor{%s}{center}\typeout{COORD %s x=\the\pgf@x y=\the\pgf@y}\makeatother"
                n
                n)
        |> String.concat "\n"

    let content =
        tikz.Replace(@"\end{tikzpicture}", dumps + "\n" + @"\end{tikzpicture}")

    let ok, log = ExternalTools.compileTexStringWithTemplateLog tikzTemplatePath content
    Assert.True(ok, sprintf "lualatex failed:\n%s" log)

    log.Split('\n')
    |> Array.filter (fun l -> l.StartsWith("COORD "))
    |> Array.map (fun l ->
        let m = Regex.Match(l, @"^COORD (\S+) x=([-\d.]+)pt\s*y=([-\d.]+)pt")
        Assert.True(m.Success, sprintf "unparseable coordinate line: %s" l)
        (m.Groups.[1].Value, (float m.Groups.[2].Value, float m.Groups.[3].Value)))
    |> Map.ofArray

let private assertSameLevel (coords: Map<string, float * float>) : unit =
    let ys = coords |> Map.values |> Seq.map snd |> Seq.distinct |> List.ofSeq

    if List.length ys <> 1 then
        Assert.Fail(sprintf "same-layer nodes are not on one level: %A" (Map.toList coords))

/// Frontier node IDs in pre-order tree order (lexicographic path order).
let private llFrontierTreeOrder
    (tree: DerivationTree<string, string>)
    (stack: LLStackLeaf<string, string> list)
    : string list =
    let pathToId = System.Collections.Generic.Dictionary<int list, int>()
    let mutable nodeId = 0

    let rec walk (node: DerivationTree<string, string>) (path: int list) : unit =
        nodeId <- nodeId + 1
        pathToId.[path] <- nodeId

        match node with
        | DerivationTree.Leaf _ -> ()
        | DerivationTree.Node(_, children) ->
            for i, child in List.indexed children do
                walk child (path @ [ i ])

    walk tree []

    stack
    |> List.map (fun leaf -> (leaf.Path, pathToId.[leaf.Path]))
    |> List.sortBy fst
    |> List.map (fun (_, id) -> sprintf "n%d" id)

/// Frame root node IDs in stack order (top frame first). Mirrors the renderer's
/// numbering: an LRSymbol frame is its subtree root (one ID), an LRState frame
/// consumes one ID, and subtree children consume the remaining IDs.
let private lrFrameRootIds (stack: LRStackFrame<string, string> list) : string list =
    let mutable nodeId = 0

    let rec walkTree (node: DerivationTree<string, string>) : unit =
        nodeId <- nodeId + 1

        match node with
        | DerivationTree.Leaf _ -> ()
        | DerivationTree.Node(_, children) -> List.iter walkTree children

    let roots = ResizeArray<int>()

    for frame in stack do
        match frame with
        | LRSymbol st ->
            nodeId <- nodeId + 1
            roots.Add(nodeId)

            match st with
            | DerivationTree.Node(_, children) -> List.iter walkTree children
            | DerivationTree.Leaf _ -> ()

        | LRState _ ->
            nodeId <- nodeId + 1
            roots.Add(nodeId)

    List.ofSeq roots |> List.map (sprintf "n%d")

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LL TikZ frontier renders on one level in tree order`` () =
    let steps = llSteps ()
    let step = steps.[3]
    let tikz = DerivationTreeTikz.toTikzWithLLStack symbolPrinter step.Tree step.Stack

    let coords = dumpSameLayerCoords tikz
    assertSameLevel coords

    let expectedOrder = llFrontierTreeOrder step.Tree step.Stack

    let actualOrder =
        coords |> Map.toList |> List.sortBy (fun (_, (x, _)) -> x) |> List.map fst

    Assert.Equal<string list>(expectedOrder, actualOrder)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR TikZ frames render on one level in stack order`` () =
    let steps = lrSteps ()
    let step = steps.[3]
    let tikz = DerivationTreeTikz.toTikzWithLRStack symbolPrinter step.Stack

    let coords = dumpSameLayerCoords tikz
    assertSameLevel coords

    let expectedOrder = lrFrameRootIds step.Stack

    let actualOrder =
        coords |> Map.toList |> List.sortBy (fun (_, (x, _)) -> x) |> List.map fst

    Assert.Equal<string list>(expectedOrder, actualOrder)
