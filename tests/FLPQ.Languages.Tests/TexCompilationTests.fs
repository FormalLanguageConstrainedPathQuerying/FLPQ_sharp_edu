module TexCompilationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_template.tex")

[<Fact>]
[<Trait("Category", "TeX")>]
let ``CYK table TeX compiles with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S\nS -> a"
    let trace = Cyk.parseWithTrace g (Tokenizer.tokenize "a a")

    let step = trace.[0]
    let tex = Cyk.tableToTeX string step.table
    Assert.True(TestUtils.checkTexCompiles templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``CYK all steps TeX compile with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S\nS -> a"
    let trace = Cyk.parseWithTrace g (Tokenizer.tokenize "a a")

    for step in trace do
        let tex = Cyk.tableToTeX string step.table
        Assert.True(TestUtils.checkTexCompiles templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LL step stack and input TeX compile with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S b S\nS -> eps"
    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenize "a b"

    let steps = LLVisualizer.visualizeSteps string g table 1 tokens
    Assert.NotEmpty(steps)

    for step in steps do
        Assert.True(TestUtils.checkTexCompiles templatePath step.stack)
        Assert.True(TestUtils.checkTexCompiles templatePath step.input)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LR step stack and input TeX compile with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S\nS -> a"
    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug
    let tokens = Tokenizer.tokenize "a a"

    let steps = LRVisualizer.visualizeSteps string aug table tokens

    for step in steps do
        Assert.True(TestUtils.checkTexCompiles templatePath step.stack)
        Assert.True(TestUtils.checkTexCompiles templatePath step.input)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``Valiant trace TeX compiles with pdflatex`` () =
    let g = Grammar.parseGrammar "S -> a S\nS -> a"
    let trace = Valiant.parseWithTrace g (Tokenizer.tokenizeStrings "a a")
    Assert.NotEmpty(trace)

    for step in trace do
        let tex =
            Matrix.toTeX false false (fun s -> if Set.isEmpty s then @"\cdot" else string s) step.table

        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.True(TestUtils.checkTexCompiles templatePath tex)

let private tabularTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_tabular_template.tex")

let private symbolToStr (sym: Symbol<string, string>) =
    match sym with
    | T(Terminal t) -> t
    | N(Nonterminal nt) -> nt
    | Epsilon -> "\\varepsilon"

[<Fact>]
[<Trait("Category", "TeX")>]
let ``LL table TeX compiles with pdflatex for grammar1`` () =
    let g = Grammar.parseGrammar "S -> a S b S\nS -> eps"
    let firstMap = FirstFollow.firstK g 1
    let followMap = FirstFollow.followK g 1
    let table = LLParser.buildTable g 1

    let tex = LLParser.tableToTeX symbolToStr g 1 firstMap followMap table

    Assert.True(TestUtils.checkTexCompiles tabularTemplatePath tex)

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

    let tex = LLParser.tableToTeX symbolToStr g 1 firstMap followMap table

    Assert.True(TestUtils.checkTexCompiles tabularTemplatePath tex)

    Assert.Contains(@"$E$", tex)
    Assert.Contains(@"$T$", tex)

    let hlineCount =
        tex.Split([| @"\hline" |], System.StringSplitOptions.None).Length - 1

    Assert.Equal(3, hlineCount)
