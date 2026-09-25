namespace FLPQ.Printers

open FLPQ.Languages

/// TeX rendering of regular expressions in math mode (RPQ query display).
module RegexpTeX =

    /// A single-character terminal renders as-is; longer names render via \text{...}
    /// with LaTeX escaping. The check applies to the printed name.
    let private termToTeX (name: string) : string =
        if String.length name = 1 then
            name
        else
            @"\text{" + AutomatonTikz.escapeLatex name + "}"

    /// Binary-operator children need parentheses; other constructors are atomic or
    /// already self-parenthesized (RStar renders as (l)^*).
    let rec private paren (terminal: 't -> string) (nonterm: 'nt -> string) (r: Regexp<'t, 'nt>) : string =
        match r with
        | RSeq _
        | RAlt _ -> "(" + toTeX terminal nonterm r + ")"
        | _ -> toTeX terminal nonterm r

    /// Render a regexp as a math-mode formula: REps → \varepsilon, REmpty → \varnothing,
    /// RTerm → name, RNonterm → \text{X}, RSeq → l / r, RAlt → l \mid r, RStar → (l)^*.
    and toTeX (terminal: 't -> string) (nonterm: 'nt -> string) (r: Regexp<'t, 'nt>) : string =
        match r with
        | REps -> @"\varepsilon"
        | REmpty -> @"\varnothing"
        | RTerm(Terminal t) -> termToTeX (terminal t)
        | RNonterm(Nonterminal nt) -> @"\text{" + AutomatonTikz.escapeLatex (nonterm nt) + "}"
        | RSeq(l, rr) -> paren terminal nonterm l + " / " + paren terminal nonterm rr
        | RAlt(l, rr) -> paren terminal nonterm l + " \mid " + paren terminal nonterm rr
        | RStar rp -> "(" + toTeX terminal nonterm rp + ")^*"
