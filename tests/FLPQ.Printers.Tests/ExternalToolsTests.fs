module ExternalToolsTests

open System.IO
open Xunit
open FLPQ.Printers
open FLPQ.TestUtilities

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_template.tex")

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotStringToInfo parses a simple graph`` () =
    let dot = "digraph G {\n  a -> b;\n  a -> c;\n}\n"

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(3, info.NodeCount)
    Assert.Equal(2, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotStringToNodePositions returns distinct coordinates per node`` () =
    let dot = "digraph G {\n  rankdir=LR;\n  a -> b;\n}\n"

    let positions = ExternalTools.compileDotStringToNodePositions dot
    Assert.Equal(2, Map.count positions)
    Assert.True(Map.containsKey "a" positions)
    Assert.True(Map.containsKey "b" positions)

    // In a left-to-right layout the source (a) is placed left of the target (b).
    let ax = fst (Map.find "a" positions)
    let bx = fst (Map.find "b" positions)
    Assert.True(ax < bx)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotString returns true for valid dot`` () =
    let dot = "digraph G { a -> b }"
    Assert.True(ExternalTools.compileDotString dot)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotString returns false for invalid dot`` () =
    let dot = "this is not dot syntax !!!"
    Assert.False(ExternalTools.compileDotString dot)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotFileToPdf produces non-empty PDF`` () =
    TestHelpers.withTempDir (fun tmpDir ->
        let dotPath = Path.Combine(tmpDir, "g.dot")
        let pdfPath = Path.Combine(tmpDir, "g.pdf")
        File.WriteAllText(dotPath, "digraph G { a -> b }")
        Assert.True(ExternalTools.compileDotFileToPdf dotPath pdfPath)
        Assert.True(File.Exists pdfPath)
        Assert.True(FileInfo(pdfPath).Length > 0L))

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexStringWithTemplate succeeds on minimal TeX`` () =
    let tex = "x^2 + y^2 = z^2"
    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexStringWithTemplate fails on broken TeX`` () =
    let tex = "\\thiscommanddoesnotexist{foo}"
    Assert.False(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexFile produces non-empty PDF in output directory`` () =
    TestHelpers.withTempDir (fun tmpDir ->
        let texPath = Path.Combine(tmpDir, "doc.tex")
        File.WriteAllText(texPath, "\\documentclass{article}\\begin{document}Hello\\end{document}")
        Assert.True(ExternalTools.compileTexFile texPath tmpDir)
        let pdfPath = Path.Combine(tmpDir, "doc.pdf")
        Assert.True(File.Exists pdfPath)
        Assert.True(FileInfo(pdfPath).Length > 0L))

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexFileTwice produces non-empty PDF`` () =
    TestHelpers.withTempDir (fun tmpDir ->
        let texPath = Path.Combine(tmpDir, "doc.tex")
        File.WriteAllText(texPath, "\\documentclass{article}\\begin{document}Hello\\end{document}")
        Assert.True(ExternalTools.compileTexFileTwice texPath tmpDir)
        let pdfPath = Path.Combine(tmpDir, "doc.pdf")
        Assert.True(File.Exists pdfPath)
        Assert.True(FileInfo(pdfPath).Length > 0L))

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotStringToNodePositions throws on invalid dot`` () =
    TestHelpers.assertThrows "node-position extraction failed" (fun () ->
        ExternalTools.compileDotStringToNodePositions "this is not dot syntax !!!"
        |> ignore)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotFileToPdf creates the output directory when missing`` () =
    TestHelpers.withTempDir (fun tmpDir ->
        let dotPath = Path.Combine(tmpDir, "g.dot")
        File.WriteAllText(dotPath, "digraph G { a -> b }")
        let pdfPath = Path.Combine(tmpDir, "nested", "dir", "g.pdf")
        Assert.True(ExternalTools.compileDotFileToPdf dotPath pdfPath)
        Assert.True(File.Exists pdfPath))

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotFileToPdf returns false for invalid dot`` () =
    TestHelpers.withTempDir (fun tmpDir ->
        let dotPath = Path.Combine(tmpDir, "bad.dot")
        File.WriteAllText(dotPath, "this is not dot syntax !!!")
        let pdfPath = Path.Combine(tmpDir, "bad.pdf")
        Assert.False(ExternalTools.compileDotFileToPdf dotPath pdfPath))

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotFileToPdf returns false when the output path is under a regular file`` () =
    TestHelpers.withTempDir (fun tmpDir ->
        File.WriteAllText(Path.Combine(tmpDir, "blocker"), "x")
        let dotPath = Path.Combine(tmpDir, "g.dot")
        File.WriteAllText(dotPath, "digraph G { a -> b }")
        let pdfPath = Path.Combine(tmpDir, "blocker", "sub", "g.pdf")
        Assert.False(ExternalTools.compileDotFileToPdf dotPath pdfPath))

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexStringWithTemplateLog returns success and the lualatex log`` () =
    let ok, log = ExternalTools.compileTexStringWithTemplateLog templatePath "x^2"
    Assert.True(ok)
    Assert.Contains("Output written on", log)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexFile creates the output directory when missing`` () =
    TestHelpers.withTempDir (fun tmpDir ->
        let texPath = Path.Combine(tmpDir, "doc.tex")
        File.WriteAllText(texPath, "\\documentclass{article}\\begin{document}Hello\\end{document}")
        let outDir = Path.Combine(tmpDir, "out", "nested")
        Assert.True(ExternalTools.compileTexFile texPath outDir)
        Assert.True(File.Exists(Path.Combine(outDir, "doc.pdf"))))

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexFile returns false for broken TeX`` () =
    TestHelpers.withTempDir (fun tmpDir ->
        let texPath = Path.Combine(tmpDir, "bad.tex")

        File.WriteAllText(
            texPath,
            "\\documentclass{article}\\begin{document}\\thiscommanddoesnotexist{foo}\\end{document}"
        )

        Assert.False(ExternalTools.compileTexFile texPath tmpDir))

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexFile returns false when the output directory cannot be created`` () =
    TestHelpers.withTempDir (fun tmpDir ->
        File.WriteAllText(Path.Combine(tmpDir, "blocker"), "x")
        let texPath = Path.Combine(tmpDir, "doc.tex")
        File.WriteAllText(texPath, "\\documentclass{article}\\begin{document}Hello\\end{document}")
        let outDir = Path.Combine(tmpDir, "blocker", "out")
        Assert.False(ExternalTools.compileTexFile texPath outDir))

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexFileTwice returns false when the first pass fails`` () =
    TestHelpers.withTempDir (fun tmpDir ->
        let texPath = Path.Combine(tmpDir, "bad.tex")

        File.WriteAllText(
            texPath,
            "\\documentclass{article}\\begin{document}\\thiscommanddoesnotexist{foo}\\end{document}"
        )

        Assert.False(ExternalTools.compileTexFileTwice texPath tmpDir))
