module TeXRendererTests

open FLPQ.Languages
open FLPQ.Printers
open Xunit

[<Fact>]
let ``inputRow with no tokens renders an epsilon row`` () =
    let tex = TeXRenderer.inputRow (SymbolTeX.terminalContent string) [] 0
    Assert.Equal(@"\begin{pNiceMatrix}[margin=2pt] \varepsilon \end{pNiceMatrix}", tex)

[<Fact>]
let ``inputRow underlines the token at the given position`` () =
    let tokens = [ Terminal "a"; Terminal "b" ]
    let tex = TeXRenderer.inputRow (SymbolTeX.terminalContent string) tokens 1
    Assert.Contains(@"\underbar{b}", tex)
    Assert.DoesNotContain(@"\underbar{a}", tex)

[<Fact>]
let ``inputRow escapes TeX special characters in tokens`` () =
    let tokens = [ Terminal "$" ]
    let tex = TeXRenderer.inputRow (SymbolTeX.terminalContent string) tokens 0
    Assert.Contains(@"\underbar{\$}", tex)
