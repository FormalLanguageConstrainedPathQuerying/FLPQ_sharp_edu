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
