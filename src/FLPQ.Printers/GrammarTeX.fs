namespace FLPQ.Printers

open System.Text
open FSharpPlus.Data
open FLPQ.Languages

/// TeX rendering for grammar rules.
module GrammarTeX =

    let private renderGrammar
        (showNumbers: bool)
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (grammar: Grammar<'t, 'nt>)
        : string =
        let orderedRules =
            let startRules, otherRules =
                grammar.rules |> List.partition (fun r -> r.lhs = grammar.start)

            startRules @ otherRules

        let sb = StringBuilder()
        sb.AppendLine(@"\begin{align*}") |> ignore

        for idx in 0 .. orderedRules.Length - 1 do
            let rule = orderedRules.[idx]
            let lhs = SymbolTeX.nonterminalContent nonterminalPrinter rule.lhs

            let rhs =
                match rule.rhs with
                | EpsilonRhs -> @"\varepsilon"
                | Symbols nel ->
                    NonEmptyList.toList nel
                    |> List.map (SymbolTeX.toLaTeX terminalPrinter nonterminalPrinter)
                    |> String.concat "\\ "

            let prefix = if showNumbers then sprintf "[%d] " idx else ""

            sb.AppendLine(sprintf @"%s%s &\rightarrow %s \\" prefix lhs rhs) |> ignore

        sb.Append(@"\end{align*}") |> ignore
        sb.ToString()

    /// Render a grammar as a TeX align* environment.
    /// Productions are ordered: start nonterminal first, then the rest.
    /// Production numbers are not printed.
    let grammarToTeX
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (grammar: Grammar<'t, 'nt>)
        : string =
        renderGrammar false terminalPrinter nonterminalPrinter grammar

    /// Render a grammar as a TeX align* environment with production numbers (0-based).
    let grammarToTeXWithNumbers
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (grammar: Grammar<'t, 'nt>)
        : string =
        renderGrammar true terminalPrinter nonterminalPrinter grammar
