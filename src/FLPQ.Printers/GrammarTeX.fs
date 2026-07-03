namespace FLPQ.Printers

open System.Text
open FSharpPlus.Data
open FLPQ.Languages

/// TeX rendering for grammar rules.
module GrammarTeX =

    let private renderGrammar (showNumbers: bool) (g: Grammar<'t, 'nt>) : string =
        let orderedRules =
            let startRules, otherRules = g.rules |> List.partition (fun r -> r.lhs = g.start)
            startRules @ otherRules

        let sb = StringBuilder()
        sb.AppendLine(@"\begin{align*}") |> ignore

        for idx in 0 .. orderedRules.Length - 1 do
            let rule = orderedRules.[idx]
            let lhs = SymbolTeX.nonterminalContent rule.lhs

            let rhs =
                match rule.rhs with
                | EpsilonRhs -> @"\varepsilon"
                | Symbols nel -> NonEmptyList.toList nel |> List.map SymbolTeX.toLaTeX |> String.concat "\\ "

            let prefix = if showNumbers then sprintf "[%d] " idx else ""

            sb.AppendLine(sprintf @"%s%s &\rightarrow %s \\" prefix lhs rhs) |> ignore

        sb.Append(@"\end{align*}") |> ignore
        sb.ToString()

    /// Render a grammar as a TeX align* environment.
    /// Productions are ordered: start nonterminal first, then the rest.
    /// Production numbers are not printed.
    let grammarToTeX (g: Grammar<'t, 'nt>) : string = renderGrammar false g

    /// Render a grammar as a TeX align* environment with production numbers (0-based).
    let grammarToTeXWithNumbers (g: Grammar<'t, 'nt>) : string = renderGrammar true g
