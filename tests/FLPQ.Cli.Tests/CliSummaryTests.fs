module CliSummaryTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests
open FLPQ.Printers
open FLPQ.TestUtilities

let private baseDir = System.AppContext.BaseDirectory

let private exampleInput = Path.Combine(baseDir, "example_input.txt")
let private exampleRegexp = Path.Combine(baseDir, "example_regexp.txt")
let private exampleGraph = Path.Combine(baseDir, "example_graph.txt")

let private runWithSummary (algorithm: string) (useDot: bool) : string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory outDir |> ignore

    let dotFlag = if useDot then [ "--use-dot" ] else []

    let args =
        Array.append
            [| "-a"
               algorithm
               "-q"
               TestGrammarFiles.exampleGrammar ()
               "-i"
               exampleInput
               "-o"
               outDir
               "-s" |]
            (Array.ofList dotFlag)

    let code = Program.runCli args
    Assert.Equal(0, code)
    outDir

let private mergedTexPath (outDir: string) (algorithm: string) : string =
    let algoLower = algorithm.ToLower()
    Path.Combine(outDir, "results", algoLower, sprintf "%s_merged.tex" algoLower)

let private assertMergedTexExists (outDir: string) (algorithm: string) =
    let texPath = mergedTexPath outDir algorithm
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)
    Assert.True(FileInfo(texPath).Length > 0L, sprintf "Merged TeX is empty: %s" texPath)

let private assertMergedTexCompiles (outDir: string) (algorithm: string) =
    assertMergedTexExists outDir algorithm

    let texPath = mergedTexPath outDir algorithm

    try
        Assert.True(
            ExternalTools.compileTexFile texPath (Path.GetDirectoryName texPath),
            sprintf "Merged TeX failed to compile with lualatex: %s" texPath
        )
    finally
        try
            Directory.Delete(outDir, true)
        with _ ->
            ()

[<Fact>]
[<Trait("Category", "Summary")>]
let ``CYK summary produces merged TeX`` () =
    let outDir = runWithSummary "CYK" false
    assertMergedTexExists outDir "CYK"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``Valiant summary produces merged TeX`` () =
    let outDir = runWithSummary "Valiant" false
    assertMergedTexExists outDir "Valiant"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LL summary produces merged TeX`` () =
    let outDir = runWithSummary "LL" false
    assertMergedTexExists outDir "LL"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LL summary default mode embeds step stack-trees as inline TikZ`` () =
    let outDir = runWithSummary "LL" false
    let texPath = Path.Combine(outDir, "results", "ll", "ll_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    Assert.Contains(@"\graph [layered layout", content)
    Assert.Contains("{ [same layer]", content)
    Assert.DoesNotContain("dot_pdfs/step_", content)
    // Step figures are scaled with adjustbox (shrink-only), not resizebox.
    // The LL summary has no other TikZ figures, so resizebox must be absent entirely.
    Assert.Contains(@"\begin{adjustbox}{max width=\textwidth}", content)
    Assert.DoesNotContain(@"\resizebox{0.98\textwidth}", content)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``SLR(1) summary step stack-trees use adjustbox scaling`` () =
    let outDir = runWithSummary "SLR1" false
    let texPath = Path.Combine(outDir, "results", "slr1", "slr1_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    // Step figures use adjustbox. Only presence is asserted: the LR automaton
    // section legitimately keeps \resizebox{0.98\textwidth}.
    Assert.Contains(@"\begin{adjustbox}{max width=\textwidth}", content)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LL summary useDot mode includes step stack-tree PDFs`` () =
    let outDir = runWithSummary "LL" true
    let texPath = Path.Combine(outDir, "results", "ll", "ll_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    Assert.Contains("dot_pdfs/step_0_tree_and_stack.pdf", content)
    Assert.DoesNotContain(@"\graph [layered layout", content)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``SLR(1) summary produces merged TeX`` () =
    let outDir = runWithSummary "SLR1" false
    assertMergedTexExists outDir "SLR1"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LR(0) summary produces merged TeX`` () =
    let outDir = runWithSummary "LR0" false
    assertMergedTexExists outDir "LR0"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``CLR(1) summary produces merged TeX`` () =
    let outDir = runWithSummary "CLR1" false
    assertMergedTexExists outDir "CLR1"

// End-to-end regression tests: the full pipeline (runner + summary) must produce a
// merged TeX that compiles with lualatex. The LL/LR step input rows contain the $
// end-of-input marker, which broke math-mode compilation until TeXRenderer.inputRow
// escaped TeX special characters (task 263).

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LL summary merged TeX compiles with lualatex`` () =
    let outDir = runWithSummary "LL" false
    assertMergedTexCompiles outDir "LL"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``SLR(1) summary merged TeX compiles with lualatex`` () =
    let outDir = runWithSummary "SLR1" false
    assertMergedTexCompiles outDir "SLR1"

let private runWithSummaryEBNFUseDot
    (algorithm: string)
    (grammarText: string)
    (inputText: string)
    (useDot: bool)
    : string =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, grammarText)
    File.WriteAllText(inputFile, inputText)

    let dotFlag = if useDot then [ "--use-dot" ] else []

    let args =
        Array.append
            [| "-a"; algorithm; "-q"; grammarFile; "-i"; inputFile; "-o"; outDir; "-s" |]
            (Array.ofList dotFlag)

    let code = Program.runCli args
    Assert.Equal(0, code)
    outDir

let private runWithSummaryEBNF (algorithm: string) (grammarText: string) (inputText: string) : string =
    runWithSummaryEBNFUseDot algorithm grammarText inputText false

[<Fact>]
[<Trait("Category", "Summary")>]
let ``GLL summary produces merged TeX`` () =
    let outDir =
        runWithSummaryEBNF "GLL" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    assertMergedTexExists outDir "GLL"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``GLL summary color legend includes stored-pops orange row`` () =
    let outDir =
        runWithSummaryEBNF "GLL" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let texPath = Path.Combine(outDir, "results", "gll", "gll_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    Assert.Contains("Stored pops handling triggered at GSS vertex", content)
    Assert.Contains(@"\colorbox{orange!30}", content)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary produces merged TeX`` () =
    let outDir =
        runWithSummaryEBNF "RNGLR" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    assertMergedTexExists outDir "RNGLR"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary color legend includes passing-reductions orange row`` () =
    let outDir =
        runWithSummaryEBNF "RNGLR" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let texPath = Path.Combine(outDir, "results", "rnglr", "rnglr_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    Assert.Contains("Passing reductions handling triggered at GSS vertex", content)
    Assert.Contains(@"\colorbox{orange!30}", content)

// Per-step GSS/RSM figures in TikZ mode are wrapped with the existing wrapTikzAdjustbox
// helper (shrink-only, at most \textwidth). DOT mode includes dot-compiled PDFs instead.

let private stepSections (content: string) : string list =
    // Note: the leading backslash must be doubled — in .NET regex a single \s is the whitespace class.
    System.Text.RegularExpressions.Regex.Matches(
        content,
        @"\\subsection\*\{(Initialization|Step \d+)\}(.*?)(?=\\subsection\*|\Z)",
        System.Text.RegularExpressions.RegexOptions.Singleline
    )
    |> Seq.map (fun m -> m.Groups.[2].Value)
    |> Seq.toList

[<Fact>]
[<Trait("Category", "Summary")>]
let ``GLL summary wraps each step GSS and RSM figure in adjustbox`` () =
    let outDir =
        runWithSummaryEBNF "GLL" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let texPath = Path.Combine(outDir, "results", "gll", "gll_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    let adjustboxBegin = @"\begin{adjustbox}{max width=\textwidth}"

    let sections = stepSections content
    Assert.NotEmpty(sections)

    for sec in sections do
        // Each GLL step carries exactly two wrapped figures: GSS and RSM.
        Assert.Equal(2, TestHelpers.countOccurrences sec adjustboxBegin)

    // The head Extended RSM figure contributes exactly one more adjustbox.
    Assert.Equal(1 + 2 * sections.Length, TestHelpers.countOccurrences content adjustboxBegin)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary wraps each step GSS figure in adjustbox`` () =
    let outDir =
        runWithSummaryEBNF "RNGLR" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let texPath = Path.Combine(outDir, "results", "rnglr", "rnglr_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    let adjustboxBegin = @"\begin{adjustbox}{max width=\textwidth}"

    let sections = stepSections content
    Assert.NotEmpty(sections)

    for sec in sections do
        // Each RNGLR step carries exactly one wrapped figure: GSS (no per-step RSM).
        Assert.Equal(1, TestHelpers.countOccurrences sec adjustboxBegin)

    // The head Extended RSM figure contributes exactly one more width-only adjustbox.
    // The LR automaton head uses the max-totalheight variant (asserted in
    // assertMaxTotalHeightAdjustboxCount), which this needle does not match.
    Assert.Equal(1 + sections.Length, TestHelpers.countOccurrences content adjustboxBegin)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``ValiantModified summary produces merged TeX`` () =
    let outDir = runWithSummary "ValiantModified" false
    assertMergedTexExists outDir "ValiantModified"

// RPQ summaries (Arroyuelo and Belyanin): the header carries the color legend, the query
// regexp (math mode), the query DFA, and the input graph; each step is a two-column section
// (figure + matrices | highlighted graph). There is no SPPF for RPQ — the trailing
// sppfSection is skipped when the sppf files are absent.
let private runRpqWithSummary (algorithm: string) (useDot: bool) : string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory outDir |> ignore

    let dotFlag = if useDot then [ "--use-dot" ] else []

    let args =
        Array.append
            [| "-a"
               algorithm
               "-q"
               exampleRegexp
               "-i"
               exampleGraph
               "-o"
               outDir
               "-s" |]
            (Array.ofList dotFlag)

    let code = Program.runCli args
    Assert.Equal(0, code)
    outDir

[<Fact>]
[<Trait("Category", "Summary")>]
let ``ArroyueloRPQ summary produces merged TeX`` () =
    let outDir = runRpqWithSummary "ArroyueloRPQ" false
    assertMergedTexExists outDir "ArroyueloRPQ"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``ArroyueloRPQ summary header orders legend before regexp before DFA before graph`` () =
    let outDir = runRpqWithSummary "ArroyueloRPQ" false

    let texPath =
        Path.Combine(outDir, "results", "arroyuelorpq", "arroyuelorpq_merged.tex")

    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath

    let legendIdx = content.IndexOf("Color Legend")
    let regexpIdx = content.IndexOf("Query Regular Expression")
    let dfaIdx = content.IndexOf("Query DFA")
    let graphIdx = content.IndexOf("Input Graph")

    Assert.True(
        legendIdx >= 0
        && regexpIdx > legendIdx
        && dfaIdx > regexpIdx
        && graphIdx > dfaIdx
    )

[<Fact>]
[<Trait("Category", "Summary")>]
let ``ArroyueloRPQ summary merged TeX compiles with lualatex`` () =
    let outDir = runRpqWithSummary "ArroyueloRPQ" false
    assertMergedTexCompiles outDir "ArroyueloRPQ"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``ArroyueloRPQ summary dot mode merged TeX compiles with lualatex`` () =
    let outDir = runRpqWithSummary "ArroyueloRPQ" true
    assertMergedTexCompiles outDir "ArroyueloRPQ"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``BelyaninRPQ summary produces merged TeX`` () =
    let outDir = runRpqWithSummary "BelyaninRPQ" false
    assertMergedTexExists outDir "BelyaninRPQ"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``BelyaninRPQ summary header orders legend before regexp before DFA before graph`` () =
    let outDir = runRpqWithSummary "BelyaninRPQ" false

    let texPath =
        Path.Combine(outDir, "results", "belyaninrpq", "belyaninrpq_merged.tex")

    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath

    let legendIdx = content.IndexOf("Color Legend")
    let regexpIdx = content.IndexOf("Query Regular Expression")
    let dfaIdx = content.IndexOf("Query DFA")
    let graphIdx = content.IndexOf("Input Graph")

    Assert.True(
        legendIdx >= 0
        && regexpIdx > legendIdx
        && dfaIdx > regexpIdx
        && graphIdx > dfaIdx
    )

[<Fact>]
[<Trait("Category", "Summary")>]
let ``BelyaninRPQ summary merged TeX compiles with lualatex`` () =
    let outDir = runRpqWithSummary "BelyaninRPQ" false
    assertMergedTexCompiles outDir "BelyaninRPQ"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``BelyaninRPQ summary dot mode merged TeX compiles with lualatex`` () =
    let outDir = runRpqWithSummary "BelyaninRPQ" true
    assertMergedTexCompiles outDir "BelyaninRPQ"

/// Compiles the merged RPQ summary and asserts that the lualatex log left in the output
/// directory by compileTexFile contains no Overfull boxes: the column-width figure wraps
/// and the whole-step adjustbox must keep every step on one page.
let private assertRpqMergedTexCompilesWithoutOverfull (outDir: string) (algorithm: string) =
    let texPath = mergedTexPath outDir algorithm
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    try
        Assert.True(
            ExternalTools.compileTexFile texPath (Path.GetDirectoryName texPath),
            sprintf "Merged TeX failed to compile with lualatex: %s" texPath
        )

        let logPath =
            Path.Combine(Path.GetDirectoryName texPath, sprintf "%s.log" (Path.GetFileNameWithoutExtension texPath))

        Assert.True(File.Exists logPath, sprintf "lualatex log not found: %s" logPath)

        let log = File.ReadAllText logPath
        Assert.DoesNotContain("Overfull", log)
    finally
        try
            Directory.Delete(outDir, true)
        with _ ->
            ()

[<Fact>]
[<Trait("Category", "Summary")>]
let ``ArroyueloRPQ summary merged TeX compiles without Overfull boxes`` () =
    let outDir = runRpqWithSummary "ArroyueloRPQ" false
    assertRpqMergedTexCompilesWithoutOverfull outDir "ArroyueloRPQ"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``ArroyueloRPQ summary dot mode merged TeX compiles without Overfull boxes`` () =
    let outDir = runRpqWithSummary "ArroyueloRPQ" true
    assertRpqMergedTexCompilesWithoutOverfull outDir "ArroyueloRPQ"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``BelyaninRPQ summary merged TeX compiles without Overfull boxes`` () =
    let outDir = runRpqWithSummary "BelyaninRPQ" false
    assertRpqMergedTexCompilesWithoutOverfull outDir "BelyaninRPQ"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``BelyaninRPQ summary dot mode merged TeX compiles without Overfull boxes`` () =
    let outDir = runRpqWithSummary "BelyaninRPQ" true
    assertRpqMergedTexCompilesWithoutOverfull outDir "BelyaninRPQ"

// SPPF must appear exactly once in the GLL/RNGLR merged summary — as the trailing
// "SPPF (Shared Packed Parse Forest)" section. The header no longer carries a
// separate "\subsection*{SPPF}" with the DOT-compiled PDF (task 269).
let private assertSingleTrailingSppfSection (outDir: string) (algorithm: string) =
    let texPath = mergedTexPath outDir algorithm
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath

    // Note: the leading backslash must be doubled — in .NET regex a single \s is the whitespace class.
    let headerMatches =
        System.Text.RegularExpressions.Regex.Matches(content, @"\\subsection\*{SPPF\}")

    let trailingMatches =
        System.Text.RegularExpressions.Regex.Matches(content, @"\\subsection\*{SPPF \(Shared Packed Parse Forest\)}")

    Assert.Equal(0, headerMatches.Count)
    Assert.Equal(1, trailingMatches.Count)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``GLL summary contains exactly one SPPF section (trailing)`` () =
    let outDir =
        runWithSummaryEBNF "GLL" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    assertSingleTrailingSppfSection outDir "GLL"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary contains exactly one SPPF section (trailing)`` () =
    let outDir =
        runWithSummaryEBNF "RNGLR" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    assertSingleTrailingSppfSection outDir "RNGLR"

// Figures wrapped with the max-totalheight adjustbox variant: the trailing SPPF
// section in every summary, plus the RNGLR LR automaton head. Every other adjustbox
// (step GSS/RSM, ext-RSM head) uses the width-only variant.
let private assertMaxTotalHeightAdjustboxCount (outDir: string) (algorithm: string) (expectedCount: int) =
    let texPath = mergedTexPath outDir algorithm
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath

    let variant =
        @"\begin{adjustbox}{max width=\textwidth, max totalheight=\textheight}"

    Assert.Equal(expectedCount, TestHelpers.countOccurrences content variant)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``CYK summary SPPF uses adjustbox with max totalheight`` () =
    let outDir = runWithSummary "CYK" false
    assertMaxTotalHeightAdjustboxCount outDir "CYK" 1

[<Fact>]
[<Trait("Category", "Summary")>]
let ``Valiant summary SPPF uses adjustbox with max totalheight`` () =
    let outDir = runWithSummary "Valiant" false
    assertMaxTotalHeightAdjustboxCount outDir "Valiant" 1

[<Fact>]
[<Trait("Category", "Summary")>]
let ``GLL summary SPPF uses adjustbox with max totalheight`` () =
    let outDir =
        runWithSummaryEBNF "GLL" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    assertMaxTotalHeightAdjustboxCount outDir "GLL" 1

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary SPPF and LR automaton use adjustbox with max totalheight`` () =
    let outDir =
        runWithSummaryEBNF "RNGLR" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    assertMaxTotalHeightAdjustboxCount outDir "RNGLR" 2

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary orders extended RSM before LR automaton before parsing table`` () =
    let outDir =
        runWithSummaryEBNF "RNGLR" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let texPath = Path.Combine(outDir, "results", "rnglr", "rnglr_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath

    let rsmIdx = content.IndexOf("Extended RSM")
    let autoIdx = content.IndexOf("LR Automaton")
    let tableIdx = content.IndexOf("RNGLR Parsing Table")
    Assert.True(rsmIdx >= 0 && autoIdx > rsmIdx && tableIdx > autoIdx)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary tikz mode contains no DOT RSM figure`` () =
    let outDir =
        runWithSummaryEBNF "RNGLR" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let texPath = Path.Combine(outDir, "results", "rnglr", "rnglr_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    Assert.DoesNotContain("dot_pdfs/rsm_blocks.pdf", content)
    Assert.DoesNotContain("dot_pdfs/ext_rsm.pdf", content)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary dot mode includes extended RSM and LR automaton PDFs`` () =
    let outDir =
        runWithSummaryEBNFUseDot "RNGLR" TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput true

    let texPath = Path.Combine(outDir, "results", "rnglr", "rnglr_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    Assert.Contains("dot_pdfs/ext_rsm.pdf", content)
    Assert.Contains("dot_pdfs/lr_automaton.pdf", content)
