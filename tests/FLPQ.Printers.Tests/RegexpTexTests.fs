module RegexpTexTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

let private mathWrapTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_math_wrap_template.tex")

// --- Per-constructor exact output ---

[<Fact>]
let ``toTeX: epsilon and empty`` () =
    Assert.Equal(@"\varepsilon", RegexpTeX.toTeX id id REps)
    Assert.Equal(@"\varnothing", RegexpTeX.toTeX id id REmpty)

[<Fact>]
let ``toTeX: single-letter terminal as-is, multi-char via \text`` () =
    Assert.Equal("a", RegexpTeX.toTeX id id (RTerm(Terminal "a")))
    Assert.Equal(@"\text{walk}", RegexpTeX.toTeX id id (RTerm(Terminal "walk")))

[<Fact>]
let ``toTeX: nonterminal via \text`` () =
    Assert.Equal(@"\text{S}", RegexpTeX.toTeX id id (RNonterm(Nonterminal "S")))

[<Fact>]
let ``toTeX: sequence with slash`` () =
    let r = RSeq(RTerm(Terminal "a"), RTerm(Terminal "b"))
    Assert.Equal("a / b", RegexpTeX.toTeX id id r)

[<Fact>]
let ``toTeX: alternative with mid`` () =
    let r = RAlt(RTerm(Terminal "a"), RTerm(Terminal "b"))
    Assert.Equal(@"a \mid b", RegexpTeX.toTeX id id r)

[<Fact>]
let ``toTeX: star parenthesizes the operand`` () =
    Assert.Equal(@"(a)^*", RegexpTeX.toTeX id id (RStar(RTerm(Terminal "a"))))

// --- Nesting parentheses ---

[<Fact>]
let ``toTeX: nested sequence gets parentheses`` () =
    let r = RSeq(RSeq(RTerm(Terminal "a"), RTerm(Terminal "b")), RTerm(Terminal "c"))
    Assert.Equal(@"(a / b) / c", RegexpTeX.toTeX id id r)

[<Fact>]
let ``toTeX: sequence inside alternative gets parentheses`` () =
    let r = RAlt(RSeq(RTerm(Terminal "a"), RTerm(Terminal "b")), RTerm(Terminal "c"))
    Assert.Equal(@"(a / b) \mid c", RegexpTeX.toTeX id id r)

[<Fact>]
let ``toTeX: star of sequence keeps its own parentheses`` () =
    let r = RStar(RSeq(RTerm(Terminal "a"), RTerm(Terminal "b")))
    Assert.Equal(@"(a / b)^*", RegexpTeX.toTeX id id r)

[<Fact>]
let ``toTeX: book query walk/(O | R)+/walk`` () =
    let o = RTerm(Terminal "O")
    let rr = RTerm(Terminal "R")
    let r = RSeq(RTerm(Terminal "walk"), RStar(RAlt(o, rr)))
    Assert.Equal(@"\text{walk} / (O \mid R)^*", RegexpTeX.toTeX id id r)

// --- LaTeX escaping of special characters ---

[<Fact>]
let ``toTeX: escapes LaTeX specials in long terminal names`` () =
    let r = RTerm(Terminal "a#b")
    Assert.Equal(@"\text{a\#b}", RegexpTeX.toTeX id id r)

// --- Compilation ---

[<Fact>]
[<Trait("Category", "TeX")>]
let ``toTeX of a nested query compiles with lualatex`` () =
    let r =
        RSeq(RTerm(Terminal "walk"), RStar(RAlt(RTerm(Terminal "O"), RTerm(Terminal "R"))))

    let tex = RegexpTeX.toTeX id id r
    Assert.True(ExternalTools.compileTexStringWithTemplate mathWrapTemplatePath tex)
