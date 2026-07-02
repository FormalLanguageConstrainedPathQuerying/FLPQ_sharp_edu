namespace FLPQ.Printers

open System.Text
open System.Text.RegularExpressions
open FLPQ.Languages

/// TeX rendering for grammar rules.
module GrammarTeX =

    let private shortNtName (Nonterminal n) =
        Regex.Replace(string n, @"N_CNF_(\d+)", @"N_{$1}")

    let private symToTeX (sym: Symbol<'t, 'nt>) : string =
        match sym with
        | T(Terminal t) -> string t
        | N nt -> shortNtName nt
        | Epsilon -> @"\varepsilon"

    /// Render a grammar as a TeX align* environment.
    let grammarToTeX (g: Grammar<'t, 'nt>) : string =
        let sb = StringBuilder()
        sb.Append(@"\begin{align*}") |> ignore

        for rule in g.rules do
            let lhs = shortNtName rule.lhs

            let rhs =
                match Rhs.toSymbols rule.rhs with
                | [] -> @"\varepsilon"
                | syms -> syms |> List.map symToTeX |> String.concat "\\ "

            sb.AppendLine(sprintf "%s &\rightarrow %s \\\\" lhs rhs) |> ignore

        sb.Append(@"\end{align*}") |> ignore
        sb.ToString()
