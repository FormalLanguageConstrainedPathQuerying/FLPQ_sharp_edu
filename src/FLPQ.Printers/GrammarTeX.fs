namespace FLPQ.Printers

open System.Text
open FSharpPlus.Data
open FLPQ.Languages

/// TeX rendering for grammar rules.
module GrammarTeX =

    let private renderRuleContent
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (rule: Rule<'t, 'nt>)
        : string * string =
        let lhs = SymbolTeX.nonterminalContent nonterminalPrinter rule.Lhs

        let rhs =
            match rule.Rhs with
            | EpsilonRhs -> @"\varepsilon"
            | Symbols nel ->
                NonEmptyList.toList nel
                |> List.map (SymbolTeX.toLaTeX terminalPrinter nonterminalPrinter)
                |> String.concat "\\ "

        lhs, rhs

    let private renderGrammar
        (showNumbers: bool)
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (grammar: Grammar<'t, 'nt>)
        : string =
        let sb = StringBuilder()

        if showNumbers then
            sb.AppendLine(@"\begin{alignat*}{3}") |> ignore

            for number, rule in Grammar.numberedRules grammar do
                let lhs, rhs = renderRuleContent terminalPrinter nonterminalPrinter rule

                sb.AppendLine(sprintf "%d) \\ & %s &&\\rightarrow %s \\\\" number lhs rhs)
                |> ignore

            sb.Append(@"\end{alignat*}") |> ignore
        else
            sb.AppendLine(@"\begin{align*}") |> ignore

            for _, rule in Grammar.numberedRules grammar do
                let lhs, rhs = renderRuleContent terminalPrinter nonterminalPrinter rule
                sb.AppendLine(sprintf @"%s &\rightarrow %s \\" lhs rhs) |> ignore

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

    /// Render a grammar as a TeX alignat* environment with 1-based production numbers.
    let grammarToTeXWithNumbers
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (grammar: Grammar<'t, 'nt>)
        : string =
        renderGrammar true terminalPrinter nonterminalPrinter grammar
