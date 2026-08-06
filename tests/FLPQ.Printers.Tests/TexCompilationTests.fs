module TexCompilationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.TestUtilities

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_template.tex")

[<Fact>]
[<Trait("Category", "TeX")>]
let ``CYK table TeX compiles with lualatex`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.APlus "grammar3").Grammar

    let trace =
        Cyk.parseWithTrace Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a")

    let step = trace.[0]
    let tex = CykTeX.tableToTeX string step.Table
    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``CYK all steps TeX compile with lualatex`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.APlus "grammar3").Grammar

    let trace =
        Cyk.parseWithTrace Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a")

    for step in trace do
        let tex = CykTeX.tableToTeX string step.Table
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LL step input TeX compiles with lualatex`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "grammar1").Grammar
    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"

    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps (SymbolTeX.toLaTeX string string) steps
    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath step.Input)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR step input TeX compiles with lualatex`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.APlus "grammar3").Grammar
    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol
    let tokens = Tokenizer.tokenizeTerminals "a a"

    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps (SymbolTeX.toLaTeX string string) steps

    for step in vizSteps do
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath step.Input)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``Valiant trace TeX compiles with lualatex`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.APlus "grammar3").Grammar

    let trace =
        Valiant.parseWithTrace Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a a a")

    Assert.NotEmpty(trace)

    for step in trace do
        let tex =
            MatrixTeX.toTeX false false (fun s -> if Set.isEmpty s then @"\cdot" else string s) step.Table

        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``Modified Valiant trace TeX compiles with lualatex`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.APlus "grammar3").Grammar

    let trace =
        Valiant.parseModifiedWithTrace Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a")

    Assert.NotEmpty(trace)

    let cellPrinter (s: Set<Nonterminal<string>>) =
        if Set.isEmpty s then @"\cdot" else string s

    for step in trace do
        let tex = ValiantTeX.modifiedStepToTeX string step
        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``Modified Valiant trace TeX with expression grammar compiles`` () =
    let grammar6 = LanguageRegistry.ArithExpr.Grammars.[0].Grammar

    let trace =
        Valiant.parseModifiedWithTrace Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals "x add x")

    Assert.NotEmpty(trace)

    let cellPrinter (s: Set<Nonterminal<string>>) =
        if Set.isEmpty s then @"\cdot" else string s

    for step in trace do
        let tex = ValiantTeX.modifiedStepToTeX string step
        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

let private tabularTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_tabular_template.tex")

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LL table TeX compiles with lualatex for grammar1`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "grammar1").Grammar
    let firstMap = FirstFollow.firstK g 1
    let followMap = FirstFollow.followK g 1
    let table = LLParser.buildTable g 1

    let tex =
        LLTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) g 1 firstMap followMap table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

    Assert.Contains(@"r || c | c || c | c | c", tex)
    Assert.Contains(@"\operatorname{First}", tex)
    Assert.Contains(@"\operatorname{Follow}", tex)
    Assert.Contains(@"a & b & $\$ $", tex)
    Assert.Contains(@"$S$", tex)
    Assert.Contains(@"$S \rightarrow a S b S$", tex)
    Assert.Contains(@"$S \rightarrow \varepsilon$", tex)

    let hlineCount =
        tex.Split([| @"\hline" |], System.StringSplitOptions.None).Length - 1

    Assert.True(hlineCount > 0)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LL table TeX for multi-nonterminal grammar has correct rows`` () =
    let g =
        (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_aT_bE_c").Grammar

    let firstMap = FirstFollow.firstK g 1
    let followMap = FirstFollow.followK g 1
    let table = LLParser.buildTable g 1

    let tex =
        LLTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) g 1 firstMap followMap table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

    Assert.Contains(@"$E$", tex)
    Assert.Contains(@"$T$", tex)

    let hlineCount =
        tex.Split([| @"\hline" |], System.StringSplitOptions.None).Length - 1

    Assert.Equal(3, hlineCount)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``SLR(1) table TeX compiles for grammar1`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "grammar1").Grammar
    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol

    let tex = LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) aug table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

    Assert.Contains(@"$s_", tex)
    Assert.Contains(@"$r_", tex)
    Assert.Contains(@"acc", tex)
    Assert.Contains(@"S", tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR(0) table TeX shows shift-reduce conflicts`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "grammar1").Grammar
    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildLR0Table aug Grammar.eoiSymbol

    let tex = LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) aug table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

    Assert.True(table.Conflicts.Length > 0)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``CLR(1) table TeX compiles for grammar1`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "grammar1").Grammar
    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildCLR1Table aug

    let tex = LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) aug table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``SLR(1) table TeX for grammar7 has goto columns`` () =
    let g = LanguageRegistry.ArithExpr.Grammars.[1].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol

    let tex = LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) aug table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

    Assert.Contains(@"E", tex)
    Assert.Contains(@"T", tex)
    Assert.Contains(@"F", tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LL table TeX compiles with lualatex`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.Dyck1 "grammar1").Grammar
    let first = FirstFollow.firstK g 1
    let follow = FirstFollow.followK g 1
    let table = LLParser.buildTable g 1

    let tex =
        LLTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) g 1 first follow table

    let tabularTemplatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_tabular_template.tex")

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``Matrix TeX compiles with lualatex`` () =
    let m = Matrix.create 3 3 (fun i j -> i * 3 + j + 1)
    let tex = MatrixTeX.toTeX false false string m
    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GLL descriptor with empty range TeX compiles`` () =
    let desc: Descriptor =
        { RsmState = 1
          Vertex = 2
          GssIdx = 0
          MatchedRange = RangeDescriptor.EmptyRange }

    let tex = GllStepVisualizer.descriptorToTeX desc
    Assert.Contains("(1, 2, 0,", tex)
    Assert.Contains(@"\emptyset", tex)
    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GLL descriptor with non-empty range TeX compiles`` () =
    let desc: Descriptor =
        { RsmState = 3
          Vertex = 1
          GssIdx = 5
          MatchedRange =
            RangeDescriptor.NonEmptyRange
                { FromState = 0
                  FromVertex = 0
                  ToState = 2
                  ToVertex = 3 } }

    let tex = GllStepVisualizer.descriptorToTeX desc
    Assert.Contains("(3, 1, 5, R^{0,0}_{2,3})", tex)
    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GLL descriptor queue TeX compiles`` () =
    let desc1: Descriptor =
        { RsmState = 0
          Vertex = 0
          GssIdx = 0
          MatchedRange = RangeDescriptor.EmptyRange }

    let desc2: Descriptor =
        { RsmState = 1
          Vertex = 0
          GssIdx = 1
          MatchedRange =
            RangeDescriptor.NonEmptyRange
                { FromState = 0
                  FromVertex = 0
                  ToState = 1
                  ToVertex = 1 } }

    let queueTex = GllStepVisualizer.queueToTeX [ desc1; desc2 ]
    Assert.Contains(@"(0, 0, 0, \emptyset)", queueTex)
    Assert.Contains("(1, 0, 1, R^{0,0}_{1,1})", queueTex)
    Assert.Contains(@"\begin{gathered}", queueTex)
    Assert.Contains(@"\end{gathered}", queueTex)

    let mathWrapTemplatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_math_wrap_template.tex")

    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath queueTex)
    Assert.True(ExternalTools.compileTexStringWithTemplate mathWrapTemplatePath queueTex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GLL empty descriptor queue TeX compiles`` () =
    let queueTex = GllStepVisualizer.queueToTeX []
    Assert.Equal(@"\emptyset", queueTex)
    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath queueTex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GLL descriptors table TeX compiles`` () =
    let colorTemplatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_color_template.tex")

    let desc1: Descriptor =
        { RsmState = 0
          Vertex = 0
          GssIdx = 0
          MatchedRange = RangeDescriptor.EmptyRange }

    let desc2: Descriptor =
        { RsmState = 1
          Vertex = 0
          GssIdx = 1
          MatchedRange =
            RangeDescriptor.NonEmptyRange
                { FromState = 0
                  FromVertex = 0
                  ToState = 1
                  ToVertex = 1 } }

    let tex =
        GllStepVisualizer.descriptorsTableToTeX (Some desc1) [ desc1; desc2 ] (Set.ofList [ desc2 ])

    Assert.Contains(@"q & i & g & \mathcal{MR}", tex)
    Assert.Contains(@"\rowcolor{yellow!20}", tex)
    Assert.True(ExternalTools.compileTexStringWithTemplate colorTemplatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GLL descriptors table with empty blocks TeX compiles`` () =
    let colorTemplatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_color_template.tex")

    let tex = GllStepVisualizer.descriptorsTableToTeX None [] Set.empty

    Assert.Contains(@"\emptyset", tex)
    Assert.True(ExternalTools.compileTexStringWithTemplate colorTemplatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GLL new descriptors TeX compiles`` () =
    let colorTemplatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_color_template.tex")

    let desc1: Descriptor =
        { RsmState = 0
          Vertex = 0
          GssIdx = 0
          MatchedRange = RangeDescriptor.EmptyRange }

    let desc2: Descriptor =
        { RsmState = 1
          Vertex = 0
          GssIdx = 1
          MatchedRange =
            RangeDescriptor.NonEmptyRange
                { FromState = 0
                  FromVertex = 0
                  ToState = 1
                  ToVertex = 1 } }

    let newSet = Set.ofList [ desc1 ]
    let attemptedSet = Set.ofList [ desc1; desc2 ]
    let tex = GllStepVisualizer.newDescriptorsToTeX newSet attemptedSet

    Assert.Contains(@"\colorbox{green!20}", tex)
    Assert.Contains(@"\colorbox{red!20}", tex)
    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GLL new descriptors empty set TeX compiles`` () =
    let colorTemplatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_color_template.tex")

    let tex = GllStepVisualizer.newDescriptorsToTeX Set.empty Set.empty

    Assert.Contains(@"\emptyset", tex)
    Assert.True(ExternalTools.compileTexStringWithTemplate colorTemplatePath tex)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``Derivation tree dot compiles with graphviz`` () =
    let tree =
        Node(
            Nonterminal "S",
            [ Leaf(Symbol.T(Terminal "a"))
              Node(Nonterminal "B", [ Leaf(Symbol.T(Terminal "b")) ]) ]
        )

    let dot = DerivationTreeDot.toDot string tree
    let info = ExternalTools.compileDotStringToInfo dot
    Assert.True(info.NodeCount > 0)
    Assert.True(info.EdgeCount > 0)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GLL merged summary TeX compiles with lualatex`` () =
    let rsm =
        (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_ebnf_a_eps").Rsm

    let freshStart = Nonterminal "S'"
    let input = [ "a" ]
    let graph = GLL.stringToGraph input
    let vertexCount = Graph.vertexCount graph
    let ersm = ExtendedRSM.create freshStart rsm
    let pathIndex, steps = GLL.buildPathIndexWithSteps freshStart ersm graph

    let vizSteps =
        GllStepVisualizer.renderSteps
            (SymbolTeX.toLaTeX string string)
            string
            string
            ersm
            steps
            pathIndex
            vertexCount
            graph

    let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let dotPdfDir = Path.Combine(tempDir, "dot_pdfs")

    if Directory.Exists tempDir then
        Directory.Delete(tempDir, true)

    Directory.CreateDirectory(tempDir) |> ignore
    Directory.CreateDirectory(dotPdfDir) |> ignore

    let stubPdf = Path.Combine(dotPdfDir, "_stub.pdf")
    File.WriteAllText(Path.Combine(tempDir, "_stub.dot"), "digraph G { a }")

    ExternalTools.compileDotFileToPdf (Path.Combine(tempDir, "_stub.dot")) stubPdf
    |> ignore

    File.Delete(Path.Combine(tempDir, "_stub.dot"))

    for idx in 0 .. vizSteps.Length - 1 do
        let stepDir = Path.Combine(tempDir, sprintf "step_%d" idx)
        Directory.CreateDirectory(stepDir) |> ignore
        File.WriteAllText(Path.Combine(stepDir, "queue.tex"), vizSteps.[idx].Queue)
        File.WriteAllText(Path.Combine(stepDir, "descriptors_table.tex"), vizSteps.[idx].DescriptorsTable)
        File.WriteAllText(Path.Combine(stepDir, "new_descriptors.tex"), vizSteps.[idx].NewDescriptors)
        File.WriteAllText(Path.Combine(stepDir, "gss.dot"), vizSteps.[idx].GssDot)
        File.WriteAllText(Path.Combine(stepDir, "path_index.tex"), vizSteps.[idx].PathIndex)
        File.WriteAllText(Path.Combine(stepDir, "input.dot"), vizSteps.[idx].Input)
        File.WriteAllText(Path.Combine(stepDir, "rsm.dot"), vizSteps.[idx].RsmDot)
        File.Copy(stubPdf, Path.Combine(dotPdfDir, sprintf "step_%d_gss.pdf" idx), true)
        File.Copy(stubPdf, Path.Combine(dotPdfDir, sprintf "step_%d_rsm.pdf" idx), true)
        File.Copy(stubPdf, Path.Combine(dotPdfDir, sprintf "step_%d_input.pdf" idx), true)

    File.WriteAllText(Path.Combine(tempDir, "input.dot"), InputGraphDot.toDot string graph None)
    File.Copy(stubPdf, Path.Combine(dotPdfDir, "input.pdf"), true)

    File.WriteAllText(Path.Combine(tempDir, "path_index.tex"), PathIndexTeX.toTeX string string pathIndex)
    File.Copy(stubPdf, Path.Combine(dotPdfDir, "ext_rsm.pdf"), true)
    File.Copy(stubPdf, Path.Combine(dotPdfDir, "sppf.pdf"), true)

    let rsmSppfPdfs =
        [ ("Extended RSM", "dot_pdfs/ext_rsm.pdf"); ("SPPF", "dot_pdfs/sppf.pdf") ]

    let gllStepTemplatePath =
        [ Path.Combine("data", "GLL_step_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "GLL_step_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "GLL_step_template.tex") ]
        |> List.tryFind File.Exists
        |> Option.defaultWith (fun () ->
            failwithf
                "Could not locate GLL_step_template.tex. Tried: %A"
                [ Path.Combine("data", "GLL_step_template.tex")
                  Path.Combine(System.AppContext.BaseDirectory, "GLL_step_template.tex") ])

    let gllStepTemplate = File.ReadAllText gllStepTemplatePath

    let content =
        SummaryTeX.buildContent
            "GLL"
            SummaryTeX.SummaryKind.GLL
            tempDir
            vizSteps.Length
            None
            None
            rsmSppfPdfs
            gllStepTemplate
            ""
            ""
            ""
            false
        |> String.concat "\n"

    let summaryTemplatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex")

    let template = File.ReadAllText summaryTemplatePath

    let fullTex =
        template.Replace("__ALGORITHM__", "GLL").Replace("__CONTENT__", content)

    let mergedTexPath = Path.Combine(tempDir, "merged.tex")
    File.WriteAllText(mergedTexPath, fullTex)

    try
        Assert.True(ExternalTools.compileTexFile mergedTexPath tempDir)
    finally
        try
            Directory.Delete(tempDir, true)
        with _ ->
            ()

[<Fact>]
[<Trait("Category", "TeX")>]
let ``RNGLR merged summary TeX compiles with lualatex`` () =
    let rsm =
        (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_ebnf_aa").Rsm

    let freshStart = Nonterminal "S'"
    let input = [ "a"; "a" ]
    let graph = GLL.stringToGraph input
    let vertexCount = Graph.vertexCount graph
    let ersm = ExtendedRSM.create freshStart rsm
    let lrTable = RnglrLR.buildLR0Table (ExtendedRSM.extRsm ersm)
    let lrStateCount = Dfa.stateCount lrTable.Automaton

    let rnglrResult = Rnglr.buildPathIndexWithSteps freshStart ersm graph

    let pathIndex = rnglrResult.PathIndex
    let steps = rnglrResult.Steps
    let vertexInfoArr = rnglrResult.VertexInfo

    let vertexInfo (idx: int) = vertexInfoArr.[idx]

    let vizSteps =
        RnglrStepVisualizer.renderSteps string string lrTable lrStateCount vertexInfo steps pathIndex vertexCount graph

    let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let dotPdfDir = Path.Combine(tempDir, "dot_pdfs")

    if Directory.Exists tempDir then
        Directory.Delete(tempDir, true)

    Directory.CreateDirectory(tempDir) |> ignore
    Directory.CreateDirectory(dotPdfDir) |> ignore

    let stubPdf = Path.Combine(dotPdfDir, "_stub.pdf")
    File.WriteAllText(Path.Combine(tempDir, "_stub.dot"), "digraph G { a }")

    ExternalTools.compileDotFileToPdf (Path.Combine(tempDir, "_stub.dot")) stubPdf
    |> ignore

    File.Delete(Path.Combine(tempDir, "_stub.dot"))

    for idx in 0 .. vizSteps.Length - 1 do
        let stepDir = Path.Combine(tempDir, sprintf "step_%d" idx)
        Directory.CreateDirectory(stepDir) |> ignore
        File.WriteAllText(Path.Combine(stepDir, "descriptors_table.tex"), vizSteps.[idx].DescriptorsTable)
        File.WriteAllText(Path.Combine(stepDir, "new_descriptors.tex"), vizSteps.[idx].NewDescriptors)
        File.WriteAllText(Path.Combine(stepDir, "gss.dot"), vizSteps.[idx].GssDot)
        File.WriteAllText(Path.Combine(stepDir, "path_index.tex"), vizSteps.[idx].PathIndex)
        File.WriteAllText(Path.Combine(stepDir, "input.dot"), vizSteps.[idx].Input)
        File.WriteAllText(Path.Combine(stepDir, "lr_table.tex"), vizSteps.[idx].LrTable)
        File.Copy(stubPdf, Path.Combine(dotPdfDir, sprintf "step_%d_gss.pdf" idx), true)
        File.Copy(stubPdf, Path.Combine(dotPdfDir, sprintf "step_%d_input.pdf" idx), true)

    File.WriteAllText(
        Path.Combine(tempDir, "rnglr_table.tex"),
        RnglrTableTeX.tableToTeXTabularOnly string string lrTable
    )

    File.WriteAllText(Path.Combine(tempDir, "path_index.tex"), PathIndexTeX.toTeX string string pathIndex)
    File.Copy(stubPdf, Path.Combine(dotPdfDir, "rsm_blocks.pdf"), true)
    File.Copy(stubPdf, Path.Combine(dotPdfDir, "sppf.pdf"), true)

    let rsmSppfPdfs =
        [ ("RSM", "dot_pdfs/rsm_blocks.pdf"); ("SPPF", "dot_pdfs/sppf.pdf") ]

    let rnglrStepTemplatePath =
        [ Path.Combine("data", "RNGLR_step_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "RNGLR_step_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "RNGLR_step_template.tex") ]
        |> List.tryFind File.Exists
        |> Option.defaultWith (fun () ->
            failwithf
                "Could not locate RNGLR_step_template.tex. Tried: %A"
                [ Path.Combine("data", "RNGLR_step_template.tex")
                  Path.Combine(System.AppContext.BaseDirectory, "RNGLR_step_template.tex") ])

    let rnglrStepTemplate = File.ReadAllText rnglrStepTemplatePath

    let content =
        SummaryTeX.buildContent
            "RNGLR"
            SummaryTeX.SummaryKind.RNGLR
            tempDir
            vizSteps.Length
            None
            None
            rsmSppfPdfs
            ""
            rnglrStepTemplate
            ""
            ""
            false
        |> String.concat "\n"

    let summaryTemplatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex")

    let template = File.ReadAllText summaryTemplatePath

    let fullTex =
        template.Replace("__ALGORITHM__", "RNGLR").Replace("__CONTENT__", content)

    let mergedTexPath = Path.Combine(tempDir, "merged.tex")
    File.WriteAllText(mergedTexPath, fullTex)

    try
        Assert.True(ExternalTools.compileTexFile mergedTexPath tempDir)
    finally
        try
            Directory.Delete(tempDir, true)
        with _ ->
            ()

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GSS tikz compiles with lualatex`` () =
    let tikzTemplatePath =
        [ Path.Combine("data", "tex_tikz_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "tex_tikz_template.tex") ]
        |> List.tryFind File.Exists
        |> Option.defaultWith (fun () -> failwith "Could not locate tex_tikz_template.tex (GSS tikz)")

    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.ANBN "grammar_aSb_eps").Rsm
    let input = [ "a"; "a"; "b"; "b" ]
    let graph = GLL.stringToGraph input
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let pathIndex, steps = GLL.buildPathIndexWithSteps freshStart ersm graph
    let vertexCount = FLPQ.GraphAnalysis.Graph.vertexCount graph

    let vizSteps =
        GllStepVisualizer.renderSteps
            (SymbolTeX.toLaTeX string string)
            string
            string
            ersm
            steps
            pathIndex
            vertexCount
            graph

    Assert.NotEmpty(vizSteps)
    let step0 = vizSteps.[0]
    Assert.True(step0.GssTikz.Length > 0)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath step0.GssTikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``Input graph tikz compiles with lualatex`` () =
    let tikzTemplatePath =
        [ Path.Combine("data", "tex_tikz_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "tex_tikz_template.tex") ]
        |> List.tryFind File.Exists
        |> Option.defaultWith (fun () -> failwith "Could not locate tex_tikz_template.tex (Input graph tikz)")

    let input = [ "a"; "a"; "b"; "b" ]
    let graph = GLL.stringToGraph input
    let tikz = InputGraphTikz.toTikz string graph None
    Assert.True(tikz.Length > 0)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``RSM tikz compiles with lualatex`` () =
    let tikzTemplatePath =
        [ Path.Combine("data", "tex_tikz_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "tex_tikz_template.tex") ]
        |> List.tryFind File.Exists
        |> Option.defaultWith (fun () -> failwith "Could not locate tex_tikz_template.tex (RSM tikz)")

    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.ANBN "grammar_aSb_eps").Rsm
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let tikz = RsmTikz.extendedRsmToTikz string string ersm None
    Assert.True(tikz.Length > 0)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``SPPF tikz compiles with lualatex`` () =
    let tikzTemplatePath =
        [ Path.Combine("data", "tex_tikz_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "tex_tikz_template.tex") ]
        |> List.tryFind File.Exists
        |> Option.defaultWith (fun () -> failwith "Could not locate tex_tikz_template.tex (SPPF tikz)")

    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.ANBN "grammar_aSb_eps").Rsm
    let input = [ "a"; "a"; "b"; "b" ]
    let graph = GLL.stringToGraph input
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let vertexCount = FLPQ.GraphAnalysis.Graph.vertexCount graph
    let pathIndex, steps = GLL.buildPathIndexWithSteps freshStart ersm graph
    let sppf = Sppf.buildSppfFromExtendedRsm pathIndex ersm.ExtendedRsm vertexCount
    let tikz = SppfTikz.toTikz string string sppf
    Assert.True(tikz.Length > 0)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``GLL merged summary TeX with tikz compiles with lualatex`` () =
    let rsm =
        (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_ebnf_aa").Rsm

    let freshStart = Nonterminal "S'"
    let input = [ "a"; "a" ]
    let graph = GLL.stringToGraph input
    let vertexCount = FLPQ.GraphAnalysis.Graph.vertexCount graph
    let ersm = ExtendedRSM.create freshStart rsm
    let pathIndex, steps = GLL.buildPathIndexWithSteps freshStart ersm graph

    let vizSteps =
        GllStepVisualizer.renderSteps
            (SymbolTeX.toLaTeX string string)
            string
            string
            ersm
            steps
            pathIndex
            vertexCount
            graph

    let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    if Directory.Exists tempDir then
        Directory.Delete(tempDir, true)

    Directory.CreateDirectory(tempDir) |> ignore

    let dotPdfDir = Path.Combine(tempDir, "dot_pdfs")
    Directory.CreateDirectory(dotPdfDir) |> ignore

    let stubPdf = Path.Combine(dotPdfDir, "_stub.pdf")
    File.WriteAllText(Path.Combine(tempDir, "_stub.dot"), "digraph G { a }")

    ExternalTools.compileDotFileToPdf (Path.Combine(tempDir, "_stub.dot")) stubPdf
    |> ignore

    File.Delete(Path.Combine(tempDir, "_stub.dot"))

    File.Copy(stubPdf, Path.Combine(dotPdfDir, "ext_rsm.pdf"), true)
    File.Copy(stubPdf, Path.Combine(dotPdfDir, "sppf.pdf"), true)

    for idx in 0 .. vizSteps.Length - 1 do
        let stepDir = Path.Combine(tempDir, sprintf "step_%d" idx)
        Directory.CreateDirectory(stepDir) |> ignore
        File.WriteAllText(Path.Combine(stepDir, "descriptors_table.tex"), vizSteps.[idx].DescriptorsTable)
        File.WriteAllText(Path.Combine(stepDir, "new_descriptors.tex"), vizSteps.[idx].NewDescriptors)
        File.WriteAllText(Path.Combine(stepDir, "path_index.tex"), vizSteps.[idx].PathIndex)
        File.WriteAllText(Path.Combine(stepDir, "gss.tikz.tex"), vizSteps.[idx].GssTikz)
        File.WriteAllText(Path.Combine(stepDir, "input.tikz.tex"), vizSteps.[idx].InputTikz)
        File.WriteAllText(Path.Combine(stepDir, "rsm.tikz.tex"), vizSteps.[idx].RsmTikz)

    File.WriteAllText(Path.Combine(tempDir, "path_index.tex"), PathIndexTeX.toTeX string string pathIndex)
    File.WriteAllText(Path.Combine(tempDir, "input.tikz.tex"), InputGraphTikz.toTikz string graph None)

    let gllStepTikzTemplatePath =
        [ Path.Combine("data", "GLL_step_tikz_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "GLL_step_tikz_template.tex")
          Path.Combine(
              System.AppContext.BaseDirectory,
              "..",
              "..",
              "..",
              "..",
              "..",
              "data",
              "GLL_step_tikz_template.tex"
          ) ]
        |> List.tryFind File.Exists
        |> Option.defaultWith (fun () -> failwith "Could not locate GLL_step_tikz_template.tex")

    let gllStepTikzTemplate = File.ReadAllText gllStepTikzTemplatePath

    let rsmSppfPdfs =
        [ ("Extended RSM", "dot_pdfs/ext_rsm.pdf"); ("SPPF", "dot_pdfs/sppf.pdf") ]

    let content =
        SummaryTeX.buildContent
            "GLL"
            SummaryTeX.SummaryKind.GLL
            tempDir
            vizSteps.Length
            None
            None
            rsmSppfPdfs
            ""
            ""
            gllStepTikzTemplate
            ""
            true
        |> String.concat "\n"

    let template =
        File.ReadAllText(Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex"))

    let fullTex =
        template.Replace("__ALGORITHM__", "GLL").Replace("__CONTENT__", content)

    let mergedTexPath = Path.Combine(tempDir, "merged.tex")
    File.WriteAllText(mergedTexPath, fullTex)

    try
        Assert.True(ExternalTools.compileTexFileTwice mergedTexPath tempDir)
    finally
        try
            Directory.Delete(tempDir, true)
        with _ ->
            ()

[<Fact>]
[<Trait("Category", "TeX")>]
let ``RNGLR merged summary TeX with tikz compiles with lualatex`` () =
    let rsm =
        (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_ebnf_aa").Rsm

    let freshStart = Nonterminal "S'"
    let input = [ "a"; "a" ]
    let graph = GLL.stringToGraph input
    let vertexCount = FLPQ.GraphAnalysis.Graph.vertexCount graph
    let ersm = ExtendedRSM.create freshStart rsm
    let lrTable = RnglrLR.buildLR0Table (ExtendedRSM.extRsm ersm)
    let lrStateCount = Dfa.stateCount lrTable.Automaton

    let rnglrResult2 = Rnglr.buildPathIndexWithSteps freshStart ersm graph

    let pathIndex = rnglrResult2.PathIndex
    let steps = rnglrResult2.Steps
    let vertexInfoArr = rnglrResult2.VertexInfo

    let vertexInfo (idx: int) = vertexInfoArr.[idx]

    let vizSteps =
        RnglrStepVisualizer.renderSteps string string lrTable lrStateCount vertexInfo steps pathIndex vertexCount graph

    let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    if Directory.Exists tempDir then
        Directory.Delete(tempDir, true)

    Directory.CreateDirectory(tempDir) |> ignore

    let dotPdfDir = Path.Combine(tempDir, "dot_pdfs")
    Directory.CreateDirectory(dotPdfDir) |> ignore

    let stubPdf = Path.Combine(dotPdfDir, "_stub.pdf")
    File.WriteAllText(Path.Combine(tempDir, "_stub.dot"), "digraph G { a }")

    ExternalTools.compileDotFileToPdf (Path.Combine(tempDir, "_stub.dot")) stubPdf
    |> ignore

    File.Delete(Path.Combine(tempDir, "_stub.dot"))

    File.Copy(stubPdf, Path.Combine(dotPdfDir, "rsm_blocks.pdf"), true)
    File.Copy(stubPdf, Path.Combine(dotPdfDir, "sppf.pdf"), true)

    for idx in 0 .. vizSteps.Length - 1 do
        let stepDir = Path.Combine(tempDir, sprintf "step_%d" idx)
        Directory.CreateDirectory(stepDir) |> ignore
        File.WriteAllText(Path.Combine(stepDir, "descriptors_table.tex"), vizSteps.[idx].DescriptorsTable)
        File.WriteAllText(Path.Combine(stepDir, "new_descriptors.tex"), vizSteps.[idx].NewDescriptors)
        File.WriteAllText(Path.Combine(stepDir, "path_index.tex"), vizSteps.[idx].PathIndex)
        File.WriteAllText(Path.Combine(stepDir, "lr_table.tex"), vizSteps.[idx].LrTable)
        File.WriteAllText(Path.Combine(stepDir, "gss.tikz.tex"), vizSteps.[idx].GssTikz)
        File.WriteAllText(Path.Combine(stepDir, "input.tikz.tex"), vizSteps.[idx].InputTikz)

    File.WriteAllText(Path.Combine(tempDir, "path_index.tex"), PathIndexTeX.toTeX string string pathIndex)

    File.WriteAllText(
        Path.Combine(tempDir, "rnglr_table.tex"),
        RnglrTableTeX.tableToTeXTabularOnly string string lrTable
    )

    let rnglrStepTikzTemplatePath =
        [ Path.Combine("data", "RNGLR_step_tikz_template.tex")
          Path.Combine(System.AppContext.BaseDirectory, "RNGLR_step_tikz_template.tex")
          Path.Combine(
              System.AppContext.BaseDirectory,
              "..",
              "..",
              "..",
              "..",
              "..",
              "data",
              "RNGLR_step_tikz_template.tex"
          ) ]
        |> List.tryFind File.Exists
        |> Option.defaultWith (fun () -> failwith "Could not locate RNGLR_step_tikz_template.tex")

    let rnglrStepTikzTemplate = File.ReadAllText rnglrStepTikzTemplatePath

    let rsmSppfPdfs =
        [ ("RSM", "dot_pdfs/rsm_blocks.pdf"); ("SPPF", "dot_pdfs/sppf.pdf") ]

    let content =
        SummaryTeX.buildContent
            "RNGLR"
            SummaryTeX.SummaryKind.RNGLR
            tempDir
            vizSteps.Length
            None
            None
            rsmSppfPdfs
            ""
            ""
            ""
            rnglrStepTikzTemplate
            true
        |> String.concat "\n"

    let template =
        File.ReadAllText(Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex"))

    let fullTex =
        template.Replace("__ALGORITHM__", "RNGLR").Replace("__CONTENT__", content)

    let mergedTexPath = Path.Combine(tempDir, "merged.tex")
    File.WriteAllText(mergedTexPath, fullTex)

    try
        Assert.True(ExternalTools.compileTexFileTwice mergedTexPath tempDir)
    finally
        try
            Directory.Delete(tempDir, true)
        with _ ->
            ()
