namespace FLPQ.RPQ

open System.IO
open FLPQ.Languages

/// Parsing of RPQ query regular expressions from EBNF files.
/// An RPQ query is a regular expression over graph edge labels: the first rule's
/// right-hand side of the EBNF file is taken as the query, and every identifier in it
/// is an edge label (nonterminal references are mapped to terminals).
module RpqInput =

    /// Map nonterminal references to terminals: in an RPQ query every symbol is an edge label.
    let rec mapNontermsToTerms (r: Regexp<string, string>) : Regexp<string, string> =
        match r with
        | REps -> REps
        | REmpty -> REmpty
        | RTerm t -> RTerm t
        | RNonterm(Nonterminal s) -> RTerm(Terminal s)
        | RSeq(l, r2) -> RSeq(mapNontermsToTerms l, mapNontermsToTerms r2)
        | RAlt(l, r2) -> RAlt(mapNontermsToTerms l, mapNontermsToTerms r2)
        | RStar rp -> RStar(mapNontermsToTerms rp)

    /// Parse EBNF text into an RPQ query regexp: the first rule's right-hand side,
    /// with nonterminal references mapped to terminals.
    let parseRegexpText (text: string) : Regexp<string, string> =
        match EbnfParser.parseEbnf text with
        | (_, rhs) :: _ -> mapNontermsToTerms rhs
        | [] -> failwith "RPQ regexp file must contain at least one rule"

    /// Parse an RPQ query regexp from a file.
    let parseRegexpFile (path: string) : Regexp<string, string> =
        File.ReadAllText path |> parseRegexpText
