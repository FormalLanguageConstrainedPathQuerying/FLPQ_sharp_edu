namespace FLPQ.Printers

open FLPQ.Languages

/// TeX rendering of regular expressions in math mode (RPQ query display).
module RegexpTeX =

    /// A single-character terminal renders as-is; longer names render via \text{...}
    /// with LaTeX escaping.
    let private termToTeX (t: string) : string =
        if String.length t = 1 then
            t
        else
            @"\text{" + AutomatonTikz.escapeLatex t + "}"

    /// Binary-operator children need parentheses; other constructors are atomic or
    /// already self-parenthesized (RStar renders as (l)^*).
    let rec private paren (r: Regexp<string, string>) : string =
        match r with
        | RSeq _
        | RAlt _ -> "(" + toTeX r + ")"
        | _ -> toTeX r

    /// Render a regexp as a math-mode formula: REps → \varepsilon, REmpty → \varnothing,
    /// RTerm → name, RNonterm → \text{X}, RSeq → l / r, RAlt → l \mid r, RStar → (l)^*.
    and toTeX (r: Regexp<string, string>) : string =
        match r with
        | REps -> @"\varepsilon"
        | REmpty -> @"\varnothing"
        | RTerm(Terminal t) -> termToTeX t
        | RNonterm(Nonterminal nt) -> @"\text{" + AutomatonTikz.escapeLatex nt + "}"
        | RSeq(l, rr) -> paren l + " / " + paren rr
        | RAlt(l, rr) -> paren l + " \mid " + paren rr
        | RStar rp -> "(" + toTeX rp + ")^*"
