module SummaryTexSectionTests

open System.IO
open Xunit
open FLPQ.Printers
open FLPQ.TestUtilities

let private emptyTemplates: SummaryTeX.StepTemplates =
    { Gll = ""
      GllTikz = ""
      Rnglr = ""
      RnglrTikz = ""
      Arroyuelo = ""
      ArroyueloTikz = "" }

[<Fact>]
let ``SummaryKind ToString renders each kind`` () =
    Assert.Equal("table", SummaryTeX.SummaryKind.TablePerStep.ToString())
    Assert.Equal("ll", SummaryTeX.SummaryKind.LL.ToString())
    Assert.Equal("lr", SummaryTeX.SummaryKind.LR.ToString())
    Assert.Equal("gll", SummaryTeX.SummaryKind.GLL.ToString())
    Assert.Equal("rnglr", SummaryTeX.SummaryKind.RNGLR.ToString())
    Assert.Equal("arroyuelorpq", SummaryTeX.SummaryKind.ArroyueloRPQ.ToString())

[<Fact>]
let ``collectSteps on a non-existent directory returns an empty array`` () =
    let missing =
        Path.Combine(Path.GetTempPath(), "no_such_viz_dir_" + Path.GetRandomFileName())

    Assert.Empty(SummaryTeX.collectSteps missing)

[<Fact>]
let ``LL header section includes the LL table when present`` () =
    TestHelpers.withTempDir (fun dir ->
        File.WriteAllText(Path.Combine(dir, "ll_table.tex"), "LLTABLE")

        let lines =
            SummaryTeX.headerSection dir SummaryTeX.SummaryKind.LL None None [] false

        let text = String.concat "\n" lines

        Assert.Contains("LL Parsing Table", text)
        Assert.Contains("LLTABLE", text))

[<Fact>]
let ``LR header section includes table and PDF automaton, skips absent tikz`` () =
    TestHelpers.withTempDir (fun dir ->
        File.WriteAllText(Path.Combine(dir, "lr_table.tex"), "LRTABLE")

        let lines =
            SummaryTeX.headerSection dir SummaryTeX.SummaryKind.LR (Some "dot_pdfs/lr_automaton.pdf") None [] false

        let text = String.concat "\n" lines

        Assert.Contains("LR Parsing Table", text)
        Assert.Contains("LRTABLE", text)
        Assert.Contains(@"\includegraphics[width=0.9\textwidth,keepaspectratio]{{dot_pdfs/lr_automaton.pdf}}", text)
        // Only the PDF automaton section is present (the tikz one is absent).
        Assert.Equal(1, text.Split([| "LR Automaton" |], System.StringSplitOptions.None).Length - 1))

[<Fact>]
let ``LR header section includes tikz automaton, skips absent pdf`` () =
    TestHelpers.withTempDir (fun dir ->
        File.WriteAllText(Path.Combine(dir, "lr_table.tex"), "LRTABLE")

        let lines =
            SummaryTeX.headerSection dir SummaryTeX.SummaryKind.LR None (Some "TIKZBODY") [] false

        let text = String.concat "\n" lines

        Assert.Contains("TIKZBODY", text)
        Assert.Contains(@"\resizebox{0.98\textwidth}{!}{%%", text)
        Assert.DoesNotContain("\includegraphics", text))

[<Fact>]
let ``GLL header in tikz mode omits the input section when input.tikz.tex is absent`` () =
    TestHelpers.withTempDir (fun dir ->
        let lines =
            SummaryTeX.headerSection dir SummaryTeX.SummaryKind.GLL None None [] true

        let text = String.concat "\n" lines

        Assert.Contains("Color Legend", text)
        Assert.DoesNotContain("Input String", text))

[<Fact>]
let ``RNGLR header in tikz mode orders extended RSM before LR automaton before table`` () =
    TestHelpers.withTempDir (fun dir ->
        File.WriteAllText(Path.Combine(dir, "ext_rsm.tikz.tex"), "EXTRSMTIKZ")
        File.WriteAllText(Path.Combine(dir, "rnglr_table.tex"), "RNGLRTABLE")

        let lines =
            SummaryTeX.headerSection dir SummaryTeX.SummaryKind.RNGLR None (Some "AUTOTIKZ") [] true

        let text = String.concat "\n" lines

        let rsmIdx = text.IndexOf("Extended RSM")
        let autoIdx = text.IndexOf("LR Automaton")
        let tableIdx = text.IndexOf("RNGLR Parsing Table")
        Assert.True(rsmIdx >= 0 && autoIdx > rsmIdx && tableIdx > autoIdx)
        Assert.Contains(@"max totalheight=\textheight", text))

[<Fact>]
let ``RNGLR header in tikz mode omits DOT RSM figures even when rsmPdfs is non-empty`` () =
    TestHelpers.withTempDir (fun dir ->
        let lines =
            SummaryTeX.headerSection
                dir
                SummaryTeX.SummaryKind.RNGLR
                None
                None
                [ ("RSM", "dot_pdfs/rsm_blocks.pdf") ]
                true

        let text = String.concat "\n" lines
        Assert.DoesNotContain("\includegraphics", text))

[<Fact>]
let ``RNGLR header in dot mode includes the extended RSM and LR automaton PDFs`` () =
    TestHelpers.withTempDir (fun dir ->
        let lines =
            SummaryTeX.headerSection
                dir
                SummaryTeX.SummaryKind.RNGLR
                (Some "dot_pdfs/lr_automaton.pdf")
                None
                [ ("Extended RSM", "dot_pdfs/ext_rsm.pdf") ]
                false

        let text = String.concat "\n" lines

        Assert.Contains(@"\includegraphics[width=0.9\textwidth,keepaspectratio]{{dot_pdfs/ext_rsm.pdf}}", text)
        Assert.Contains(@"\includegraphics[width=0.9\textwidth,keepaspectratio]{{dot_pdfs/lr_automaton.pdf}}", text)
        Assert.DoesNotContain("adjustbox", text))

[<Fact>]
let ``tableStepSection without table.tex renders only the step header`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_1")
        Directory.CreateDirectory stepDir |> ignore

        let lines = SummaryTeX.tableStepSection stepDir 1 SummaryTeX.wrapCenter
        Assert.Equal<string list>([ SummaryTeX.section "Step 1" ], lines))

[<Fact>]
let ``stackStepSection in tikz mode includes tree and input when present`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_2")
        Directory.CreateDirectory stepDir |> ignore
        File.WriteAllText(Path.Combine(stepDir, "tree_and_stack.tikz.tex"), "TIKZTREE")
        File.WriteAllText(Path.Combine(stepDir, "input.tex"), "INPUTTEX")

        let lines = SummaryTeX.stackStepSection stepDir 2 "step_2" true
        let text = String.concat "\n" lines

        Assert.Contains("Step 2", text)
        Assert.Contains("TIKZTREE", text)
        Assert.Contains("INPUTTEX", text))

[<Fact>]
let ``stackStepSection in tikz mode without files renders only the step header`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_3")
        Directory.CreateDirectory stepDir |> ignore

        let lines = SummaryTeX.stackStepSection stepDir 3 "step_3" true
        Assert.Equal<string list>([ SummaryTeX.section "Step 3" ], lines))

[<Fact>]
let ``stackStepSection in dot mode includes the tree PDF and input`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_4")
        Directory.CreateDirectory stepDir |> ignore
        File.WriteAllText(Path.Combine(stepDir, "input.tex"), "INPUTTEX")

        let lines = SummaryTeX.stackStepSection stepDir 4 "step_4" false
        let text = String.concat "\n" lines

        Assert.Contains(@"dot_pdfs/step_4_tree_and_stack.pdf", text)
        Assert.Contains("INPUTTEX", text))

[<Fact>]
let ``stackStepSection in dot mode without input renders header and picture only`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_5")
        Directory.CreateDirectory stepDir |> ignore

        let lines = SummaryTeX.stackStepSection stepDir 5 "step_5" false
        let text = String.concat "\n" lines

        Assert.Contains(@"dot_pdfs/step_5_tree_and_stack.pdf", text)
        Assert.DoesNotContain("INPUTTEX", text))

[<Fact>]
let ``gllStepSection in dot mode fills empty placeholders for missing files`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_0")
        Directory.CreateDirectory stepDir |> ignore

        let template =
            "D=__DESCRIPTORS_TABLE__|N=__NEW_DESCRIPTORS__|P=__PATH_INDEX__|G=__STEP_GSS_PDF__|R=__STEP_RSM_PDF__|I=__STEP_INPUT_PDF__"

        let lines = SummaryTeX.gllStepSection stepDir 0 template "" false

        Assert.Equal(3, List.length lines)
        Assert.Equal(SummaryTeX.section "Initialization", lines.[0])

        Assert.Equal(
            "D=|N=|P=|G=dot_pdfs/step_0_gss.pdf|R=dot_pdfs/step_0_rsm.pdf|I=dot_pdfs/step_0_input.pdf",
            lines.[1]
        ))

[<Fact>]
let ``gllStepSection in tikz mode fills empty placeholders for missing files`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_1")
        Directory.CreateDirectory stepDir |> ignore

        let template = "G=__STEP_GSS_TIKZ__|R=__STEP_RSM_TIKZ__|I=__STEP_INPUT_TIKZ__"

        let lines = SummaryTeX.gllStepSection stepDir 1 template template true

        Assert.Equal(SummaryTeX.section "Step 1", lines.[0])
        // Missing tikz files are replaced by empty strings; the adjustbox wrap is still applied.
        Assert.Contains("G=\\begin{center}", lines.[1])
        Assert.Contains("|R=\\begin{center}", lines.[1])
        Assert.EndsWith("|I=", lines.[1]))

[<Fact>]
let ``rnglrStepSection in dot mode fills empty placeholders for missing files`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_2")
        Directory.CreateDirectory stepDir |> ignore

        let template =
            "P=__PATH_INDEX__|L=__LR_TABLE__|G=__STEP_GSS_PDF__|I=__STEP_INPUT_PDF__"

        let lines = SummaryTeX.rnglrStepSection stepDir 2 template "" false

        Assert.Equal(SummaryTeX.section "Step 2", lines.[0])
        Assert.Equal("P=|L=|G=dot_pdfs/step_2_gss.pdf|I=dot_pdfs/step_2_input.pdf", lines.[1]))

[<Fact>]
let ``rnglrStepSection in tikz mode fills empty placeholders for missing files`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_3")
        Directory.CreateDirectory stepDir |> ignore

        let template = "G=__STEP_GSS_TIKZ__|I=__STEP_INPUT_TIKZ__"

        let lines = SummaryTeX.rnglrStepSection stepDir 3 template template true

        Assert.Equal(SummaryTeX.section "Step 3", lines.[0])
        Assert.Contains("G=\\begin{center}", lines.[1])
        Assert.EndsWith("|I=", lines.[1]))

[<Fact>]
let ``sppfSection in tikz mode falls back to the dot PDF when tikz is absent`` () =
    TestHelpers.withTempDir (fun dir ->
        File.WriteAllText(Path.Combine(dir, "sppf.dot"), "digraph SPPF { a }")

        let lines = SummaryTeX.sppfSection dir true
        let text = String.concat "\n" lines

        Assert.Contains("SPPF (Shared Packed Parse Forest)", text)
        Assert.Contains(@"dot_pdfs/sppf.pdf", text))

[<Fact>]
let ``sppfSection in tikz mode without any figure is empty`` () =
    TestHelpers.withTempDir (fun dir -> Assert.Empty(SummaryTeX.sppfSection dir true))

[<Fact>]
let ``ArroyueloRPQ header in tikz mode orders legend before regexp before DFA before graph`` () =
    TestHelpers.withTempDir (fun dir ->
        File.WriteAllText(Path.Combine(dir, "regexp.tex"), "REGEXPTX")
        File.WriteAllText(Path.Combine(dir, "dfa.tikz.tex"), "DFATIKZ")
        File.WriteAllText(Path.Combine(dir, "graph.tikz.tex"), "GRAPHTIKZ")

        let lines =
            SummaryTeX.headerSection dir SummaryTeX.SummaryKind.ArroyueloRPQ None None [] true

        let text = String.concat "\n" lines

        let legendIdx = text.IndexOf("Color Legend")
        let regexpIdx = text.IndexOf("Query Regular Expression")
        let dfaIdx = text.IndexOf("Query DFA")
        let graphIdx = text.IndexOf("Input Graph")

        Assert.True(
            legendIdx >= 0
            && regexpIdx > legendIdx
            && dfaIdx > regexpIdx
            && graphIdx > dfaIdx
        )

        Assert.Contains("REGEXPTX", text)
        Assert.Contains("DFATIKZ", text)
        Assert.Contains("GRAPHTIKZ", text)
        // The DFA and graph figures use the width-only adjustbox; the regexp is math mode.
        Assert.Equal(
            2,
            text.Split([| @"\begin{adjustbox}{max width=\textwidth}" |], System.StringSplitOptions.None).Length
            - 1
        )

        Assert.Contains("\[\nREGEXPTX\n\]", text))

[<Fact>]
let ``ArroyueloRPQ header in dot mode includes the DFA and graph PDFs`` () =
    TestHelpers.withTempDir (fun dir ->
        File.WriteAllText(Path.Combine(dir, "dfa.dot"), "digraph DFA { a }")
        File.WriteAllText(Path.Combine(dir, "graph.dot"), "digraph G { a }")

        let lines =
            SummaryTeX.headerSection dir SummaryTeX.SummaryKind.ArroyueloRPQ None None [] false

        let text = String.concat "\n" lines

        Assert.Contains(@"\includegraphics[width=0.9\textwidth,keepaspectratio]{{dot_pdfs/dfa.pdf}}", text)
        Assert.Contains(@"\includegraphics[width=0.9\textwidth,keepaspectratio]{{dot_pdfs/graph.pdf}}", text)
        Assert.DoesNotContain("adjustbox", text))

[<Fact>]
let ``ArroyueloRPQ header omits absent regexp, DFA, and graph sections`` () =
    TestHelpers.withTempDir (fun dir ->
        let lines =
            SummaryTeX.headerSection dir SummaryTeX.SummaryKind.ArroyueloRPQ None None [] true

        let text = String.concat "\n" lines

        Assert.Contains("Color Legend", text)
        Assert.DoesNotContain("Query Regular Expression", text)
        Assert.DoesNotContain("Query DFA", text)
        Assert.DoesNotContain("Input Graph", text))

[<Fact>]
let ``arroyueloStepSection in dot mode fills empty placeholders for missing files`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_0")
        Directory.CreateDirectory stepDir |> ignore

        let template = "M=__MATRICES__|T=__STEP_TREE_PDF__|G=__STEP_GRAPH_PDF__"

        let lines = SummaryTeX.arroyueloStepSection stepDir 0 template "" false

        Assert.Equal(3, List.length lines)
        Assert.Equal(SummaryTeX.section "Step 0", lines.[0])
        Assert.Equal("M=|T=dot_pdfs/step_0_tree.pdf|G=dot_pdfs/step_0_graph.pdf", lines.[1]))

[<Fact>]
let ``arroyueloStepSection in tikz mode fills empty placeholders for missing files`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_1")
        Directory.CreateDirectory stepDir |> ignore

        let template = "T=__STEP_TREE_TIKZ__|G=__STEP_GRAPH_TIKZ__"

        let lines = SummaryTeX.arroyueloStepSection stepDir 1 template template true

        Assert.Equal(SummaryTeX.section "Step 1", lines.[0])
        // Missing tikz files are replaced by empty strings; the adjustbox wrap is still applied.
        Assert.Contains("T=\\begin{center}", lines.[1])
        Assert.Contains("|G=\\begin{center}", lines.[1]))

[<Fact>]
let ``buildContent for the LL kind renders stack step sections`` () =
    TestHelpers.withTempDir (fun dir ->
        let stepDir = Path.Combine(dir, "step_0")
        Directory.CreateDirectory stepDir |> ignore
        File.WriteAllText(Path.Combine(stepDir, "tree_and_stack.tikz.tex"), "TIKZTREE")
        File.WriteAllText(Path.Combine(stepDir, "input.tex"), "INPUTTEX")

        let lines =
            SummaryTeX.buildContent "LL" SummaryTeX.SummaryKind.LL dir 1 None None [] emptyTemplates true

        let text = String.concat "\n" lines

        Assert.Contains("Algorithm: LL", text)
        Assert.Contains("Step 0", text)
        Assert.Contains("TIKZTREE", text)
        Assert.Contains("INPUTTEX", text))
