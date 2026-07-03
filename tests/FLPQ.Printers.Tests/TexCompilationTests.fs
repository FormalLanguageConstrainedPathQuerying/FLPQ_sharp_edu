module TexCompilationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_template.tex")

[<Fact>]
[<Trait("Category", "TeX")>]
let ``CYK table TeX compiles with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S\nS -> a"

    let trace =
        Cyk.parseWithTrace Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a")

    let step = trace.[0]
    let tex = CykTeX.tableToTeX step.table
    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``CYK all steps TeX compile with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S\nS -> a"

    let trace =
        Cyk.parseWithTrace Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a")

    for step in trace do
        let tex = CykTeX.tableToTeX step.table
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LL step input TeX compiles with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S b S\nS -> eps"
    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"

    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps SymbolTeX.toLaTeX steps
    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath step.input)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR step input TeX compiles with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S\nS -> a"
    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug
    let tokens = Tokenizer.tokenizeTerminals "a a"

    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps SymbolTeX.toLaTeX steps

    for step in vizSteps do
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath step.input)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``Valiant trace TeX compiles with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S\nS -> a"

    let trace =
        Valiant.parseWithTrace Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a")

    Assert.NotEmpty(trace)

    for step in trace do
        let tex =
            MatrixTeX.toTeX false false (fun s -> if Set.isEmpty s then @"\cdot" else string s) step.table

        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``Modified Valiant trace TeX compiles with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S\nS -> a"

    let trace =
        Valiant.parseModifiedWithTrace Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a")

    Assert.NotEmpty(trace)

    let cellPrinter (s: Set<Nonterminal<string>>) =
        if Set.isEmpty s then @"\cdot" else string s

    for step in trace do
        let tex = ValiantTeX.modifiedStepToTeX step
        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``Modified Valiant trace TeX with expression grammar compiles`` () =
    let grammar6 =
        Grammar.parseGrammar
            "
S -> x
S -> S + S
S -> S * S
S -> ( S )
"

    let trace =
        Valiant.parseModifiedWithTrace Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals "x + x")

    Assert.NotEmpty(trace)

    let cellPrinter (s: Set<Nonterminal<string>>) =
        if Set.isEmpty s then @"\cdot" else string s

    for step in trace do
        let tex = ValiantTeX.modifiedStepToTeX step
        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

let private tabularTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_tabular_template.tex")

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LL table TeX compiles with pdflatex for grammar1`` () =
    let g = Grammar.parseGrammar "S -> a S b S\nS -> eps"
    let firstMap = FirstFollow.firstK g 1
    let followMap = FirstFollow.followK g 1
    let table = LLParser.buildTable g 1

    let tex = LLTableTeX.tableToTeX SymbolTeX.toLaTeX g 1 firstMap followMap table

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
        Grammar.parseGrammar
            "
E -> a T
T -> b E
T -> c
"

    let firstMap = FirstFollow.firstK g 1
    let followMap = FirstFollow.followK g 1
    let table = LLParser.buildTable g 1

    let tex = LLTableTeX.tableToTeX SymbolTeX.toLaTeX g 1 firstMap followMap table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

    Assert.Contains(@"$E$", tex)
    Assert.Contains(@"$T$", tex)

    let hlineCount =
        tex.Split([| @"\hline" |], System.StringSplitOptions.None).Length - 1

    Assert.Equal(3, hlineCount)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``SLR(1) table TeX compiles for grammar1`` () =
    let g = Grammar.parseGrammar "S -> a S b S\nS -> eps"
    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug

    let tex = LRTableTeX.tableToTeX SymbolTeX.toLaTeX aug table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

    Assert.Contains(@"$s_", tex)
    Assert.Contains(@"$r_", tex)
    Assert.Contains(@"acc", tex)
    Assert.Contains(@"S", tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR(0) table TeX shows shift-reduce conflicts`` () =
    let g = Grammar.parseGrammar "S -> a S b S\nS -> eps"
    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildLR0Table aug

    let tex = LRTableTeX.tableToTeX SymbolTeX.toLaTeX aug table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

    Assert.True(table.conflicts.Length > 0)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``CLR(1) table TeX compiles for grammar1`` () =
    let g = Grammar.parseGrammar "S -> a S b S\nS -> eps"
    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildCLR1Table aug

    let tex = LRTableTeX.tableToTeX SymbolTeX.toLaTeX aug table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``SLR(1) table TeX for grammar7 has goto columns`` () =
    let g =
        Grammar.parseGrammar
            "
E -> E + T
E -> T
T -> T * F
T -> F
F -> ( E )
F -> x
"

    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug

    let tex = LRTableTeX.tableToTeX SymbolTeX.toLaTeX aug table

    Assert.True(ExternalTools.compileTexStringWithTemplate tabularTemplatePath tex)

    Assert.Contains(@"E", tex)
    Assert.Contains(@"T", tex)
    Assert.Contains(@"F", tex)
